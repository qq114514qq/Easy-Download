using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EasyDownload.Models;
using EasyDownload.Services;

namespace EasyDownload.Views;

public partial class SettingsView : UserControl
{
    private readonly AppSettings _s;
    private readonly System.Action<AppSettings> _onSaved;

    public SettingsView(AppSettings s, System.Action<AppSettings> onSaved)
    {
        _s = s;
        _onSaved = onSaved;
        InitializeComponent();
        Reload();
    }

    public void Reload()
    {
        ThreadSlider.Value = _s.MaxThreads;
        ConcurrentSlider.Value = _s.MaxConcurrent;
        ThrottleSwitch.IsChecked = _s.AutoThrottle;
        ResumeSwitch.IsChecked = _s.ResumeDownload;
        ConfirmSwitch.IsChecked = _s.ConfirmBeforeDownload;
        FolderBox.Text = _s.DownloadFolder;
        ThreadText.Text = _s.MaxThreads.ToString();
        ConcurrentText.Text = _s.MaxConcurrent.ToString();
        UpdateThemeButtons();
    }

    private void UpdateThemeButtons()
    {
        var accent = TryFindResource("AccentButton") as Style;
        var normal = TryFindResource(typeof(Button)) as Style;
        if (accent == null) return;
        LightBtn.Style = _s.Theme == "Light" ? accent : normal;
        DarkBtn.Style = _s.Theme == "Dark" ? accent : normal;
    }

    private void Light_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = "Light";
        ThemeService.Apply("Light");
        UpdateThemeButtons();
    }

    private void Dark_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = "Dark";
        ThemeService.Apply("Dark");
        UpdateThemeButtons();
    }

    private void Thread_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThreadText != null) ThreadText.Text = ((int)ThreadSlider.Value).ToString();
    }

    private void Concurrent_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ConcurrentText != null) ConcurrentText.Text = ((int)ConcurrentSlider.Value).ToString();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(_s.DownloadFolder))
                Process.Start("explorer.exe", _s.DownloadFolder);
        }
        catch { }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _s.MaxThreads = (int)ThreadSlider.Value;
        _s.MaxConcurrent = (int)ConcurrentSlider.Value;
        _s.AutoThrottle = ThrottleSwitch.IsChecked == true;
        _s.ResumeDownload = ResumeSwitch.IsChecked == true;
        _s.ConfirmBeforeDownload = ConfirmSwitch.IsChecked == true;
        _s.DownloadFolder = FolderBox.Text.Trim();

        SettingsService.Save(_s);
        ThemeService.Apply(_s.Theme);
        _onSaved(_s);

        TipText.Text = "已保存";
    }
}
