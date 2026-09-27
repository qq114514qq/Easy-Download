using System.Windows;
using System.Windows.Controls;
using EasyDownload.Models;
using EasyDownload.Services;

namespace EasyDownload.Views;

public partial class FirstRunWindow : Window
{
    private readonly AppSettings _s;
    private int _step = 0;

    public FirstRunWindow(AppSettings s)
    {
        _s = s;
        InitializeComponent();
        FThread.Value = s.MaxThreads;
        FConcurrent.Value = s.MaxConcurrent;
        FFolder.Text = s.DownloadFolder;
        FThreadText.Text = s.MaxThreads.ToString();
        FConcurrentText.Text = s.MaxConcurrent.ToString();
        UpdateThemeButtons();
        ShowStep();
    }

    private void TitleBar_Down(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }

    private void UpdateThemeButtons()
    {
        var accent = TryFindResource("AccentButton") as Style;
        var normal = TryFindResource(typeof(Button)) as Style;
        if (accent != null)
        {
            ThemeLight.Style = _s.Theme == "Light" ? accent : normal;
            ThemeDark.Style = _s.Theme == "Dark" ? accent : normal;
        }
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = "Light";
        ThemeService.Apply("Light");
        UpdateThemeButtons();
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _s.Theme = "Dark";
        ThemeService.Apply("Dark");
        UpdateThemeButtons();
    }

    private void FThread_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FThreadText != null) FThreadText.Text = ((int)FThread.Value).ToString();
    }

    private void FConcurrent_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FConcurrentText != null) FConcurrentText.Text = ((int)FConcurrent.Value).ToString();
    }

    private void ShowStep()
    {
        Step0.Visibility = _step == 0 ? Visibility.Visible : Visibility.Collapsed;
        Step1.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;

        StepText.Text = $"第 {_step + 1} / 4 步";
        PrevBtn.IsEnabled = _step > 0;
        NextBtn.Visibility = _step < 3 ? Visibility.Visible : Visibility.Collapsed;
        FinishBtn.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_step > 0) { _step--; ShowStep(); }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_step < 3) { _step++; ShowStep(); }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        _s.MaxThreads = (int)FThread.Value;
        _s.MaxConcurrent = (int)FConcurrent.Value;
        if (!string.IsNullOrWhiteSpace(FFolder.Text)) _s.DownloadFolder = FFolder.Text.Trim();
        _s.FirstRunDone = true;
        SettingsService.Save(_s);
        ThemeService.Apply(_s.Theme);
        DialogResult = true;
        Close();
    }
}
