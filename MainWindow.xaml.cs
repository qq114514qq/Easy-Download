using System.Windows;
using System.Windows.Media;
using EasyDownload.Models;
using EasyDownload.Services;
using EasyDownload.Views;

namespace EasyDownload;

public partial class MainWindow : Window
{
    private readonly AppSettings _s;
    private readonly DownloadService _dl;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    private BrowserView? _browser;
    private DownloadsView? _downloads;
    private SettingsView? _settings;

    public MainWindow(AppSettings s)
    {
        _s = s;
        _dl = new DownloadService(s);
        InitializeComponent();
        ThemeService.Apply(s.Theme);
        Navigate(0);

        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromMilliseconds(300)
        };
        _timer.Tick += (_, _) => _dl.Tick();
        _timer.Start();
    }

    /// <summary>引导或设置保存后调用</summary>
    public void ApplySettings()
    {
        _dl.UpdateSettings(_s);
        _settings?.Reload();
        ThemeService.Apply(_s.Theme);
    }

    private void Navigate(int index)
    {
        switch (index)
        {
            case 0:
                _browser ??= new BrowserView(_s, _dl);
                Host.Content = _browser;
                break;
            case 1:
                _downloads ??= new DownloadsView(_s, _dl);
                Host.Content = _downloads;
                break;
            default:
                _settings ??= new SettingsView(_s, _ => ApplySettings());
                Host.Content = _settings;
                break;
        }
        UpdateNav(index);
    }

    private void UpdateNav(int index)
    {
        var hover = (Brush)FindResource("HoverBrush");
        NavHome.Background = index == 0 ? hover : Brushes.Transparent;
        NavDown.Background = index == 1 ? hover : Brushes.Transparent;
        NavSet.Background = index == 2 ? hover : Brushes.Transparent;
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, NavHome)) Navigate(0);
        else if (ReferenceEquals(sender, NavDown)) Navigate(1);
        else Navigate(2);
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = _s.Theme == "Dark" ? "Light" : "Dark";
        ThemeService.Apply(_s.Theme);
        SettingsService.Save(_s);
        _settings?.Reload();
    }

    private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Max_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
