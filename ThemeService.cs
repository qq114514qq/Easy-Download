using System.Windows;

namespace EasyDownload.Services;

public static class ThemeService
{
    /// <summary>切换浅色 / 深色主题</summary>
    public static void Apply(string theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var dicts = app.Resources.MergedDictionaries;

        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? "";
            if (src.Contains("LightTheme") || src.Contains("DarkTheme"))
                dicts.RemoveAt(i);
        }

        var target = theme == "Dark" ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        dicts.Insert(0, new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/{target}", UriKind.Absolute)
        });
    }
}
