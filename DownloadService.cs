using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EasyDownload.Models;

namespace EasyDownload.Services;

public class DownloadService
{
    public ObservableCollection<DownloadItem> Items { get; } = new();

    private AppSettings _s;
    private SemaphoreSlim _gate;

    private static readonly HttpClient _http = new()
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    static DownloadService()
    {
        try
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            _http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        }
        catch { }
    }

    public DownloadService(AppSettings s)
    {
        _s = s;
        _gate = new SemaphoreSlim(Math.Max(1, s.MaxConcurrent));
    }

    public void UpdateSettings(AppSettings s)
    {
        _s = s;
    }

    // ---------- 管理 ----------

    public DownloadItem Add(string url, string? suggestedPath = null)
    {
        var name = string.IsNullOrWhiteSpace(suggestedPath) ? NameFromUrl(url) : Path.GetFileName(suggestedPath);
        // 地址里没有文件名时（如 /download?id=123 或带签名的长串），先给个兜底名，
        // 真正的文件名等连上服务器后由 ResolveNameAsync 从 Content-Disposition 纠正。
        if (string.IsNullOrWhiteSpace(name)) name = "下载_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        try { Directory.CreateDirectory(_s.DownloadFolder); } catch { }

        var item = new DownloadItem
        {
            Url = url,
            FileName = name,
            SavePath = Path.Combine(_s.DownloadFolder, name)
        };
        Items.Add(item);
        return item;
    }

    public void Start(DownloadItem item)
    {
        // 关键：以前这里用状态文字"等待中"判断，而新建的任务默认就是"等待中"，
        // 导致 Start 直接 return，永远没真正开始下载 —— 现在改用 IsRunning 判断。
        if (item.IsRunning) return;
        if (item.Status == "已完成") { item.Progress = 100; return; }

        item.IsRunning = true;
        item.Cts = new CancellationTokenSource();
        item.Stopwatch = Stopwatch.StartNew();
        item.Status = "等待中";
        item.IsIndeterminate = false;
        _ = Task.Run(() => RunAsync(item));
    }

    public void Pause(DownloadItem item)
    {
        try { item.Cts?.Cancel(); } catch { }
        item.Status = "已暂停";
        item.SpeedText = "";
    }

    public void Delete(DownloadItem item)
    {
        try { item.Cts?.Cancel(); } catch { }
        try { if (Directory.Exists(item.SavePath + ".parts")) Directory.Delete(item.SavePath + ".parts", true); } catch { }
        Items.Remove(item);
    }

    public void ClearFinished()
    {
        for (int i = Items.Count - 1; i >= 0; i--)
            if (Items[i].Status == "已完成")
                Items.RemoveAt(i);
    }

    /// <summary>由界面定时器调用：刷新进度 / 速度</summary>
    public void Tick()
    {
        foreach (var it in Items)
        {
            if (it.TotalBytes > 0)
                it.Progress = Math.Min(100, it.ReceivedBytes * 100.0 / it.TotalBytes);

            if (it.IsRunning && it.Stopwatch != null)
            {
                var sec = it.Stopwatch.Elapsed.TotalSeconds;
                if (sec > 0.3)
                {
                    var spd = it.ReceivedBytes / sec;
                    it.SpeedText = DownloadItem.Format((long)spd) + "/s";

                    // 剩余时间 = 还剩的字节 / 当前速度
                    if (it.TotalBytes > 0 && spd > 1)
                    {
                        var left = (long)((it.TotalBytes - it.ReceivedBytes) / spd);
                        it.EtaText = left > 0 ? "剩余 " + DownloadItem.FormatTime(left) : "即将完成";
                    }
                }
            }
            else if (!it.IsRunning)
            {
                it.EtaText = "";
            }
            it.OnPropertyChanged(nameof(it.SizeText));
        }
    }

    // ---------- 引擎 ----------

    private async Task RunAsync(DownloadItem item)
    {
        var ct = item.Cts!.Token;
        bool acquired = false;

        try
        {
            await _gate.WaitAsync(ct);
            acquired = true;

            item.Status = "下载中";

            var (total, supportsRange) = await ProbeAsync(item.Url, ct);
            item.TotalBytes = total > 0 ? total : 0;
            item.IsIndeterminate = item.TotalBytes <= 0;

            // 连上服务器后，用真实响应头修正文件名（此时还没建 .parts 目录，改名安全）
            await ResolveNameAsync(item, ct);

            int n = (supportsRange && total > 512 * 1024 && _s.MaxThreads > 1) ? _s.MaxThreads : 1;
            if (total <= 0) n = 1;

            var dir = item.SavePath + ".parts";
            if (_s.ResumeDownload && Directory.Exists(dir))
            {
                long done = 0;
                foreach (var f in Directory.GetFiles(dir))
                {
                    try { done += new FileInfo(f).Length; } catch { }
                }
                item.ReceivedBytes = Math.Min(done, Math.Max(total, done));
            }

            await MultiAsync(item, dir, n, ct);

            item.Progress = 100;
            item.IsIndeterminate = false;
            item.Status = "已完成";
            item.SpeedText = "";
        }
        catch (OperationCanceledException)
        {
            item.Status = "已暂停";
            item.SpeedText = "";
            item.IsIndeterminate = false;
        }
        catch (Exception ex)
        {
            item.Status = "失败：" + ex.Message;
            item.SpeedText = "";
            item.IsIndeterminate = false;
        }
        finally
        {
            item.IsRunning = false;
            item.IsIndeterminate = false;
            if (acquired)
            {
                try { _gate.Release(); } catch { }
            }
        }
    }

    private async Task<(long, bool)> ProbeAsync(string url, CancellationToken ct)
    {
        long total = -1;
        bool range = false;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Range = new RangeHeaderValue(0, 0);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.Content.Headers.ContentRange?.Length != null)
            {
                total = resp.Content.Headers.ContentRange.Length.Value;
                range = true;
            }
            else
            {
                total = resp.Content.Headers.ContentLength ?? -1;
                range = resp.Headers.AcceptRanges.Contains("bytes");
            }
        }
        catch { range = false; }

        if (total <= 0)
        {
            try
            {
                using var head = new HttpRequestMessage(HttpMethod.Head, url);
                using var hr = await _http.SendAsync(head, ct);
                total = hr.Content.Headers.ContentLength ?? -1;
                range = hr.Headers.AcceptRanges.Contains("bytes");
            }
            catch { }
        }
        return (total, range);
    }

    private async Task MultiAsync(DownloadItem item, string dir, int n, CancellationToken ct)
    {
        Directory.CreateDirectory(dir);
        long total = item.TotalBytes;
        long block = n > 1 ? total / n : total;
        long limit = _s.AutoThrottle ? Math.Max(64 * 1024, (1024 * 1024) / Math.Max(1, n)) : 0;

        var tasks = new List<Task>();
        for (int i = 0; i < n; i++)
        {
            long start = i * block;
            long end = (i == n - 1) ? total - 1 : (start + block - 1);
            if (n == 1) end = total - 1;
            tasks.Add(PartAsync(item, dir, i, start, end, limit, ct));
        }

        await Task.WhenAll(tasks);

        item.Status = "合并中";
        Merge(item, dir, n);
    }

    private async Task PartAsync(DownloadItem item, string dir, int index, long start, long end,
                                 long limit, CancellationToken ct)
    {
        var part = Path.Combine(dir, index.ToString());

        // 失败自动重试 3 次：网抖一下不至于整个任务挂掉
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            long done = 0;
            if (!_s.ResumeDownload && File.Exists(part))
            {
                try { File.Delete(part); } catch { }
            }
            if (File.Exists(part)) done = new FileInfo(part).Length;

            long from = start + done;
            if (end > 0 && from > end) return;

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, item.Url);
                if (end > 0) req.Headers.Range = new RangeHeaderValue(from, end);

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();

                Stream src = await resp.Content.ReadAsStreamAsync(ct);
                if (limit > 0) src = new ThrottleStream(src, limit);

                using var fs = new FileStream(part, FileMode.Append, FileAccess.Write, FileShare.None);
                var buf = new byte[81920];
                int read;
                while ((read = await src.ReadAsync(buf, ct)) > 0)
                {
                    await fs.WriteAsync(buf.AsMemory(0, read), ct);
                    item.AddReceived(read);
                }
                await src.DisposeAsync();
                return; // 这块下完了
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                if (attempt == 2) throw;
                try { await Task.Delay(500 * (attempt + 1), ct); } catch { }
            }
        }
    }

    private static void Merge(DownloadItem item, string dir, int n)
    {
        var final = UniquePath(item.SavePath);
        using (var outFs = new FileStream(final, FileMode.Create, FileAccess.Write))
        {
            for (int i = 0; i < n; i++)
            {
                var p = Path.Combine(dir, i.ToString());
                if (!File.Exists(p)) continue;
                using var fs = new FileStream(p, FileMode.Open, FileAccess.Read);
                fs.CopyTo(outFs);
            }
        }
        try { Directory.Delete(dir, true); } catch { }
        item.SavePath = final;
        item.FileName = Path.GetFileName(final);
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; i < 999; i++)
        {
            var p = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(p)) return p;
        }
        return path;
    }

    public static string NameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            // 注意：Uri.LocalPath 已经做过一次解码，这里绝不能再 UnescapeDataString，
            // 否则文件名里原本带 % 的字符会被二次解码 —— 这正是以前名字变乱码的直接原因。
            var name = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(name)) return "";
            if (name.Length > 120) name = name.Substring(0, 120);
            return Sanitize(name);
        }
        catch { return ""; }
    }

    // ================= 文件名纠正 =================

    /// <summary>
    /// 用服务器返回的真实信息修正文件名。
    /// 优先级：Content-Disposition（最权威）＞ 重定向后的最终地址 ＞ 原始地址；
    /// 名字没有扩展名或像乱码时，按 Content-Type 补（如 application/x-msdownload → .exe）。
    /// </summary>
    private async Task ResolveNameAsync(DownloadItem item, CancellationToken ct)
    {
        try
        {
            string? cdName = null, finalUrl = null, mime = null;

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, item.Url);
                req.Headers.Range = new RangeHeaderValue(0, 0);
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (resp.Content.Headers.TryGetValues("Content-Disposition", out var v))
                    cdName = ParseContentDisposition(string.Join(";", v));
                finalUrl = resp.RequestMessage?.RequestUri?.ToString();
                mime = resp.Content.Headers.ContentType?.MediaType;
            }
            catch { }

            if (cdName == null && finalUrl == null)
            {
                try
                {
                    using var head = new HttpRequestMessage(HttpMethod.Head, item.Url);
                    using var hr = await _http.SendAsync(head, ct);
                    if (hr.Content.Headers.TryGetValues("Content-Disposition", out var v2))
                        cdName = ParseContentDisposition(string.Join(";", v2));
                    finalUrl ??= hr.RequestMessage?.RequestUri?.ToString();
                    mime ??= hr.Content.Headers.ContentType?.MediaType;
                }
                catch { }
            }

            // 浏览器或用户已经给了像样的名字（有扩展名、不是乱码），就别去动它
            if (cdName == null && !LooksLikeJunk(item.FileName) && Path.HasExtension(item.FileName))
                return;

            string? best = cdName;
            if (string.IsNullOrWhiteSpace(best) && !string.IsNullOrWhiteSpace(finalUrl))
                best = NameFromUrl(finalUrl!);
            if (string.IsNullOrWhiteSpace(best))
                best = NameFromUrl(item.Url);

            if (string.IsNullOrWhiteSpace(best)) return;
            if (best.Length > 120) best = best.Substring(0, 120);

            // 没扩展名、或整串就是 hash/签名，按 MIME 补一个扩展名
            if (!Path.HasExtension(best) || LooksLikeJunk(best))
            {
                var ext = ExtFromContentType(mime);
                if (ext != null) best += ext;
            }

            best = Sanitize(best);
            if (string.IsNullOrWhiteSpace(best)) return;
            if (best == item.FileName) return;

            item.FileName = best;
            item.SavePath = UniquePath(Path.Combine(_s.DownloadFolder, best));
        }
        catch { }
    }

    /// <summary>解析 Content-Disposition：优先 filename*（RFC 5987，UTF-8），其次 filename（可能是 GBK）</summary>
    private static string? ParseContentDisposition(string header)
    {
        if (string.IsNullOrWhiteSpace(header)) return null;

        // filename*=UTF-8''%E4%B8%AD%E6%96%87.exe
        var m = Regex.Match(header, "filename\\*\\s*=\\s*[^']*'[^']*'([^;]+)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var raw = m.Groups[1].Value.Trim().Trim('"');
            var dec = SafeUnescape(raw);
            if (!string.IsNullOrWhiteSpace(dec)) return dec;
        }

        m = Regex.Match(header, "filename\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        if (!m.Success)
            m = Regex.Match(header, "filename\\s*=\\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;

        var val = m.Groups[1].Value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(val)) return null;

        var utf8 = SafeUnescape(val);
        // 解出一堆替换字符（U+FFFD），说明它其实不是 UTF-8，改按 GB18030 试一次
        if (ContainsPercent(val) && CountReplacement(utf8) > 0)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var gbk = Encoding.GetEncoding("GB18030");
                var decoded = gbk.GetString(PercentToBytes(val));
                if (!string.IsNullOrWhiteSpace(decoded) && CountReplacement(decoded) < CountReplacement(utf8))
                    return decoded;
            }
            catch { }
        }
        return utf8;
    }

    private static string ExtFromContentType(string? mime)
    {
        if (string.IsNullOrWhiteSpace(mime)) return null;
        var t = mime.ToLowerInvariant().Split(';')[0].Trim();
        switch (t)
        {
            case "application/x-msdownload":
            case "application/x-dosexec":
            case "application/exe":
            case "application/x-winexe":
                return ".exe";
            case "application/x-msi":
            case "application/x-windows-installer":
                return ".msi";
            case "application/x-zip-compressed":
            case "application/zip":
                return ".zip";
            case "application/x-rar-compressed":
            case "application/vnd.rar":
                return ".rar";
            case "application/x-7z-compressed":
                return ".7z";
            case "application/x-tar":
                return ".tar";
            case "application/gzip":
            case "application/x-gzip":
                return ".gz";
            case "application/pdf":
                return ".pdf";
            case "application/octet-stream":
                return ".bin";
            case "image/jpeg":
                return ".jpg";
            case "image/png":
                return ".png";
            case "image/gif":
                return ".gif";
            case "image/webp":
                return ".webp";
            case "image/bmp":
                return ".bmp";
            case "video/mp4":
                return ".mp4";
            case "video/x-msvideo":
                return ".avi";
            case "video/x-matroska":
                return ".mkv";
            case "audio/mpeg":
                return ".mp3";
            case "audio/mp4":
                return ".m4a";
            case "text/plain":
                return ".txt";
            case "text/html":
                return ".html";
            case "application/json":
                return ".json";
            case "application/xml":
                return ".xml";
            default:
                return null;
        }
    }

    /// <summary>名字是不是一串看不懂的 hash / 签名 / 兜底名</summary>
    private static bool LooksLikeJunk(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var n = name.Trim();
        if (CountReplacement(n) > 0) return true;          // 含解码失败的乱码字符
        if (n.StartsWith("下载_")) return true;             // 我们自己给的兜底名
        if (!Path.HasExtension(n) && n.Length >= 24) return true;  // 无扩展名的超长串
        return false;
    }

    private static int CountReplacement(string s)
    {
        int n = 0;
        foreach (var c in s) if (c == '\uFFFD') n++;
        return n;
    }

    private static bool ContainsPercent(string s) => s.IndexOf('%') >= 0;

    private static string SafeUnescape(string s)
    {
        if (!ContainsPercent(s)) return s;
        try { return Uri.UnescapeDataString(s); } catch { return s; }
    }

    /// <summary>把 %XX 序列还原成原始字节（保留非 ASCII 字符的 UTF-8 字节），供 GBK 解码用</summary>
    private static byte[] PercentToBytes(string s)
    {
        var list = new List<byte>();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '%' && i + 2 < s.Length && Uri.IsHexDigit(s[i + 1]) && Uri.IsHexDigit(s[i + 2]))
            {
                list.Add((byte)((Uri.FromHex(s[i + 1]) << 4) | Uri.FromHex(s[i + 2])));
                i += 2;
            }
            else
            {
                list.AddRange(Encoding.UTF8.GetBytes(s[i].ToString()));
            }
        }
        return list.ToArray();
    }

    private static string Sanitize(string name)
    {
        name = (name ?? "").Trim().Trim('"');
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Trim();
    }
}

/// <summary>限速流：按每秒字节数限制读取速度</summary>
public class ThrottleStream : Stream
{
    private readonly Stream _inner;
    private readonly long _bytesPerSecond;
    private long _transferred;
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    public ThrottleStream(Stream inner, long bytesPerSecond)
    {
        _inner = inner;
        _bytesPerSecond = Math.Max(1, bytesPerSecond);
    }

    private void Wait(int count)
    {
        var expectedMs = (double)(_transferred + count) / _bytesPerSecond * 1000.0;
        var elapsed = _sw.Elapsed.TotalMilliseconds;
        if (expectedMs > elapsed)
            Thread.Sleep((int)Math.Min(expectedMs - elapsed, 1000));
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Wait(count);
        var read = _inner.Read(buffer, offset, count);
        _transferred += read;
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        Wait(count);
        var read = await _inner.ReadAsync(buffer, offset, count, ct);
        _transferred += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        Wait(buffer.Length);
        var read = await _inner.ReadAsync(buffer, ct);
        _transferred += read;
        return read;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
}
