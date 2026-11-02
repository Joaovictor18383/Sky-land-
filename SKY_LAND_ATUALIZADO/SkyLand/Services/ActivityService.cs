using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SkyLand
{
    public enum ActivityKind { Info, Success, Warn, Error }

    public record ActivityItem(DateTime Time, ActivityKind Kind, string Title)
    {
        public string TimeText =>
            Time.Date == DateTime.Today
                ? Time.ToString("HH:mm", System.Globalization.CultureInfo.CurrentCulture)
                : Time.ToString("dd/MM HH:mm", System.Globalization.CultureInfo.CurrentCulture);
    }

    /// <summary>Registro de atividades recentes exibido no painel inicial e persistido em disco.</summary>
    public static class ActivityService
    {
        public static ObservableCollection<ActivityItem> Items { get; } = new();

        static ActivityService()
        {
            Load();
        }

        public static void Add(ActivityKind kind, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            title = title.Trim();
            if (title.Length > 160) title = title.Substring(0, 160) + "…";

            void UiWork()
            {
                try
                {
                    Items.Insert(0, new ActivityItem(DateTime.Now, kind, title));
                    while (Items.Count > 60) Items.RemoveAt(Items.Count - 1);
                }
                catch { }
            }

            try
            {
                var d = Application.Current?.Dispatcher;
                if (d != null) d.Invoke(UiWork);
                else UiWork();
            }
            catch { UiWork(); }

            Save();
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(AppPaths.ActivityFile)) return;
                var json = File.ReadAllText(AppPaths.ActivityFile);
                var list = JsonSerializer.Deserialize<JsonElement>(json);
                if (list.ValueKind != JsonValueKind.Array) return;

                foreach (var el in list.EnumerateArray())
                {
                    try
                    {
                        var time = el.GetProperty("time").GetDateTime();
                        var kind = (ActivityKind)el.GetProperty("kind").GetInt32();
                        var title = el.GetProperty("title").GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(title)) Items.Add(new ActivityItem(time, kind, title));
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void Save()
        {
            try
            {
                var obj = Items.Take(60).Select(i => new
                {
                    time = i.Time,
                    kind = (int)i.Kind,
                    title = i.Title
                });
                var json = JsonSerializer.Serialize(obj);
                AppPaths.EnsureFolders();
                File.WriteAllText(AppPaths.ActivityFile, json);
            }
            catch { }
        }

        public static void Clear()
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() => Items.Clear());
                if (File.Exists(AppPaths.ActivityFile)) File.Delete(AppPaths.ActivityFile);
            }
            catch { }
        }
    }
}
