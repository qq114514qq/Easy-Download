using System.Windows;
using EasyDownload.Services;
using EasyDownload.Views;

namespace EasyDownload;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var s = SettingsService.Load();
        ThemeService.Apply(s.Theme);

        var win = new MainWindow(s);
        win.Show();

        // 第一次用弹引导，之后不再弹
        if (!s.FirstRunDone)
        {
            var first = new FirstRunWindow(s) { Owner = win };
            first.ShowDialog();
            s.FirstRunDone = true;
            SettingsService.Save(s);
            win.ApplySettings();
        }
    }
}
