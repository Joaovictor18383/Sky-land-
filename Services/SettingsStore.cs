using System.IO;
using System.Text.Json;

namespace SkyLand
{
    public class AppSettings
    {
        public string Theme { get; set; } = "DeepBlue";
        public string DownloadsDir { get; set; } = "";
        public bool AutoUpdateCheck { get; set; } = true;
    }

    /// <summary>Configurações persistidas em %APPDATA%\SKY LAND\settings.json.</summary>
    internal static class SettingsStore
    {
        static SettingsStore()
        {
            Load();
        }

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.SettingsFile));
                    if (s != null) Current = s;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                AppPaths.EnsureFolders();
                File.WriteAllText(AppPaths.SettingsFile,
                    JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        /// <summary>Pasta de downloads escolhida (padrão: Downloads do perfil do usuário).</summary>
        public static string GetDownloadsPath()
        {
            var dir = Current?.DownloadsDir;
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                return dir;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }

        public static void ResetDownloadsPath()
        {
            Current.DownloadsDir = "";
            Save();
        }
    }
}
