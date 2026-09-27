using System.IO;
using System.Text.Json;
using EasyDownload.Models;

namespace EasyDownload.Services;

public static class SettingsService
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyDownload");

    private static readonly string File = Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(File))
            {
                var json = System.IO.File.ReadAllText(File);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save(AppSettings s)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            System.IO.File.WriteAllText(File,
                JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
