using System.IO;

namespace SkyLand
{
    /// <summary>Locais de arquivos e pastas usados pelo aplicativo (roaming, temp, updates).</summary>
    internal static class AppPaths
    {
        /// <summary>%APPDATA%\SKY LAND</summary>
        public static string Base { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SKY LAND");

        public static string SettingsFile => Path.Combine(Base, "settings.json");

        public static string ActivityFile => Path.Combine(Base, "activity.json");

        public static string LogFile => Path.Combine(Base, "skyland.log");

        /// <summary>Arquivos temporários usados por instaladores baixados (%TEMP%\SKY LAND).</summary>
        public static string Temp { get; } = Path.Combine(
            (Path.GetTempPath() ?? Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)),
            "SKY LAND");

        /// <summary>Pasta de destino das atualizações do próprio SKY LAND.</summary>
        public static string Updates => Path.Combine(Base, "Updates");

        public static void EnsureFolders()
        {
            TryCreate(Base);
            TryCreate(Temp);
        }

        public static void TryCreate(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch { }
        }
    }
}
