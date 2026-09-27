using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using EasyDownload.Models;
using EasyDownload.Services;

namespace EasyDownload.Views;

public partial class BrowserView : UserControl
{
    private readonly AppSettings _s;
    private readonly DownloadService _dl;

    public BrowserView(AppSettings s, DownloadService dl)
    {
        _s = s;
        _dl = dl;
        InitializeComponent();
        AddTab(s.HomePage);
    }

    private WebView2? CurrentView()
    {
        if (Tabs.SelectedItem is TabItem t && t.Tag is WebView2 wv) return wv;
        return null;
    }

    public void AddTab(string? url = null)
    {
        var wv = new WebView2();

        var title = new TextBlock
        {
            Text = "新标签页",
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 150,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
        };

        var close = new Button
        {
            Content = "&#xE711;",
            FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = 10,
            Padding = new Thickness(4, 2, 4, 2),
            Margin = new Thickness(8, 0, 0, 0),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush")
        };

        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(title);
        header.Children.Add(close);

        var tab = new TabItem { Header = header, Content = wv, Tag = wv };
        close.Click += (_, e) => { e.Handled = true; CloseTab(tab); };

        Tabs.Items.Add(tab);
        Tabs.SelectedItem = tab;

        wv.CoreWebView2InitializationCompleted += (_, e2) =>
        {
            if (!e2.IsSuccess) { title.Text = "加载失败"; return; }
            var core = wv.CoreWebView2;

            // 网页里点下载 → 交给自己的下载器
            core.DownloadStarting += (_, b) =>
            {
                try
                {
                    string url = b.DownloadOperation?.Uri ?? "";
                    string file = b.ResultFilePath ?? "";
                    if (string.IsNullOrWhiteSpace(url)) return;

                    // 先让自家下载器接住，成功后再取消 Edge 的下载；
                    // 万一自己这边挂了，就放开让 Edge 正常下，不至于两头都丢。
                    bool ok = false;
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var item = _dl.Add(url, file);
                            if (_s.ConfirmBeforeDownload)
                            {
                                var r = MessageBox.Show($"是否下载此文件？\n\n{item.FileName}\n{url}",
                                    "下载确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (r != MessageBoxResult.Yes) { _dl.Delete(item); return; }
                            }
                            _dl.Start(item);
                            ok = true;
                        }
                        catch { ok = false; }
                    });

                    b.Handled = ok;
                    if (ok)
                    {
                        try { b.DownloadOperation?.Cancel(); } catch { }
                    }
                }
                catch { }
            };

            core.SourceChanged += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (ReferenceEquals(wv, CurrentView())) AddressBox.Text = core.Source;
                });
            };

            core.DocumentTitleChanged += (_, _) =>
            {
                var t = core.DocumentTitle;
                title.Text = string.IsNullOrWhiteSpace(t) ? "新标签页" : t;
            };

            core.NewWindowRequested += (_, b) => { b.Handled = true; AddTab(b.Uri); };
        };

        if (!string.IsNullOrWhiteSpace(url)) Navigate(url);
    }

    private void CloseTab(TabItem tab)
    {
        if (tab.Tag is WebView2 wv)
        {
            Tabs.Items.Remove(tab);
            try { wv.Dispose(); } catch { }
        }
        if (Tabs.Items.Count == 0) AddTab(_s.HomePage);
    }

    private void Navigate(string url)
    {
        var wv = CurrentView();
        if (wv == null) return;
        var u = url.Trim();
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            u = "https://" + u;
        try { wv.Source = new Uri(u); } catch { }
    }

    private void Go_Click(object sender, RoutedEventArgs e) => Navigate(AddressBox.Text);

    private void AddressBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) Navigate(AddressBox.Text);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        var wv = CurrentView();
        if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoBack) wv.CoreWebView2.GoBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        var wv = CurrentView();
        if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoForward) wv.CoreWebView2.GoForward();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => CurrentView()?.CoreWebView2?.Reload();

    private void NewTab_Click(object sender, RoutedEventArgs e) => AddTab(_s.HomePage);

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var wv = CurrentView();
        if (wv?.CoreWebView2 != null) AddressBox.Text = wv.CoreWebView2.Source;
    }
}
