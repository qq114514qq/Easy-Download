using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyDownload.Models;

public class DownloadItem : INotifyPropertyChanged
{
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string SavePath { get; set; } = "";

    private long _totalBytes;
    public long TotalBytes
    {
        get => _totalBytes;
        set { _totalBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(SizeText)); }
    }

    private long _receivedBytes;
    public long ReceivedBytes
    {
        get => _receivedBytes;
        set { _receivedBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(SizeText)); }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentText)); }
    }

    /// <summary>服务器不给文件大小时，进度条转圈，别卡在 0% 吓人</summary>
    private bool _isIndeterminate;
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set { _isIndeterminate = value; OnPropertyChanged(); OnPropertyChanged(nameof(PercentText)); }
    }

    private string _status = "等待中";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _speedText = "";
    public string SpeedText
    {
        get => _speedText;
        set { _speedText = value; OnPropertyChanged(); }
    }

    private string _etaText = "";
    /// <summary>剩余时间，如 "3 分 20 秒"</summary>
    public string EtaText
    {
        get => _etaText;
        set { _etaText = value; OnPropertyChanged(); }
    }

    /// <summary>进度百分比文字，如 "47%"</summary>
    public string PercentText
    {
        get
        {
            if (IsIndeterminate) return "";
            return ((int)Math.Min(100, Math.Max(0, Progress))).ToString() + "%";
        }
    }

    public string SizeText => $"{Format(ReceivedBytes)} / {Format(TotalBytes)}";

    /// <summary>后台任务是否真的在跑（不是靠状态文字猜）</summary>
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); }
    }

    public bool IsActive => IsRunning;

    [System.Text.Json.Serialization.JsonIgnore]
    public System.Threading.CancellationTokenSource? Cts { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public System.Diagnostics.Stopwatch? Stopwatch { get; set; }

    // 计数用 Interlocked（多线程累加安全），通知做节流，避免刷爆界面
    private readonly object _notifyLock = new();
    private long _lastNotifiedBytes;
    private readonly System.Diagnostics.Stopwatch _notifySw = System.Diagnostics.Stopwatch.StartNew();

    public void AddReceived(long n)
    {
        System.Threading.Interlocked.Add(ref _receivedBytes, n);

        bool fire;
        lock (_notifyLock)
        {
            var cur = System.Threading.Interlocked.Read(ref _receivedBytes);
            fire = _notifySw.ElapsedMilliseconds >= 200 || (cur - _lastNotifiedBytes) >= 65536;
            if (fire)
            {
                _lastNotifiedBytes = cur;
                _notifySw.Restart();
            }
        }
        if (fire)
        {
            OnPropertyChanged(nameof(ReceivedBytes));
            OnPropertyChanged(nameof(SizeText));
        }
    }

    /// <summary>把秒数格式化成中文可读的剩余时间</summary>
    public static string FormatTime(long seconds)
    {
        if (seconds <= 0) return "0 秒";
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours} 小时 {ts.Minutes} 分";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes} 分 {ts.Seconds} 秒";
        return $"{ts.Seconds} 秒";
    }

    public static string Format(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] unit = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < unit.Length - 1) { v /= 1024; i++; }
        return $"{v:0.##} {unit[i]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        var handler = PropertyChanged;
        if (handler == null) return;

        // 后台线程改的属性，统一丢回界面线程，保证 UI 一定刷新
        try
        {
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d != null && !d.CheckAccess())
            {
                d.BeginInvoke(new System.Action(() =>
                    handler.Invoke(this, new PropertyChangedEventArgs(name))));
                return;
            }
        }
        catch { }

        handler.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
