using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EasyDownload.Models;
using EasyDownload.Services;

namespace EasyDownload.Views;

public partial class DownloadsView : UserControl
{
    private readonly AppSettings _s;
    private readonly DownloadService _dl;

    public DownloadsView(AppSettings s, DownloadService dl)
    {
        _s = s;
        _dl = dl;
        InitializeComponent();
        List.ItemsSource = _dl.Items;
    }

    public void AddAndStart(string url)
    {
        var item = _dl.Add(url);
        _dl.Start(item);
    }

    private DownloadItem? ItemOf(object sender)
        => ((FrameworkElement)sender).DataContext as DownloadItem;

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        var item = _dl.Add(url);
        if (_s.ConfirmBeforeDownload)
        {
            var r = MessageBox.Show($"是否下载？\n\n{item.FileName}\n{url}",
                "下载确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) { _dl.Delete(item); UrlBox.Clear(); return; }
        }
        _dl.Start(item);
        UrlBox.Clear();
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is DownloadItem it) _dl.Start(it);
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is DownloadItem it) _dl.Pause(it);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is DownloadItem it) _dl.Delete(it);
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => _dl.ClearFinished();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is not DownloadItem it) return;
        try
        {
            if (File.Exists(it.SavePath))
                Process.Start("explorer.exe", $"/select,\"{it.SavePath}\"");
            else if (Directory.Exists(_s.DownloadFolder))
                Process.Start("explorer.exe", _s.DownloadFolder);
        }
        catch { }
    }
}
