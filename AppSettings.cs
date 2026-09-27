namespace EasyDownload.Models;

public class AppSettings
{
    /// <summary>Light / Dark</summary>
    public string Theme { get; set; } = "Light";

    /// <summary>首次运行引导是否已完成</summary>
    public bool FirstRunDone { get; set; } = false;

    public string HomePage { get; set; } = "https://www.bing.com";

    /// <summary>单文件最大线程数（1 - 32）</summary>
    public int MaxThreads { get; set; } = 32;

    /// <summary>同时下载的文件数</summary>
    public int MaxConcurrent { get; set; } = 4;

    public bool AutoThrottle { get; set; } = false;

    public bool ResumeDownload { get; set; } = true;

    public bool ConfirmBeforeDownload { get; set; } = false;

    public string DownloadFolder { get; set; } =
        System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
}
