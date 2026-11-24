using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyLand
{
    /// <summary>Versão do Roblox (Windows) obtida da API pública do RDD/WEAO.</summary>
    public sealed class RobloxVersionInfo
    {
        public string Version { get; set; } = "";
        public string DisplayVersion { get; set; } = "";
        public string DateText { get; set; } = "";
        public string ClientUpload { get; set; } = "";
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Consulta versões do Roblox em https://weao.gg/api/versions/current e /past
    /// (mesma fonte usada por https://rdd.weao.gg/). Cache de 30 minutos.
    /// </summary>
    internal static class RobloxVersionService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(30);

        static RobloxVersionService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("WEAO-3PService");
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public const string SiteUrl = "https://rdd.weao.gg/";
        public const string WeaoUrl = "https://whatexpsare.online/";

        public static List<RobloxVersionInfo> Versions { get; private set; } = new();
        public static DateTime Loaded { get; private set; } = DateTime.MinValue;
        public static bool IsFresh => Versions.Count > 0 && (DateTime.Now - Loaded) < FreshFor;
        public static string LastError { get; private set; }

        public static RobloxVersionInfo Current => Versions.FirstOrDefault(v => v.IsCurrent);

        public static async Task RefreshAsync(CancellationToken ct = default)
        {
            var list = new List<RobloxVersionInfo>();

            var cur = await GetJsonAsync("https://weao.gg/api/versions/current", ct);
            AddPlatform(list, cur, true, "Windows");

            var past = await GetJsonAsync("https://weao.gg/api/versions/past", ct);
            AddPlatform(list, past, false, "Windows");

            if (list.Count == 0)
                throw new SkyHttpException(0, "Nenhuma versão retornada pela API.");

            Versions = list;
            Loaded = DateTime.Now;
            LastError = null;
        }

        private static void AddPlatform(List<RobloxVersionInfo> list, JsonElement root, bool isCurrent, string platform)
        {
            if (root.ValueKind != JsonValueKind.Object) return;
            if (!root.TryGetProperty(platform, out var verEl) || verEl.ValueKind != JsonValueKind.String) return;

            var version = verEl.GetString() ?? "";
            var display = "";
            var dateText = "";

            var dateProp = platform + "Date";
            if (root.TryGetProperty(dateProp, out var dEl) && dEl.ValueKind == JsonValueKind.String)
                dateText = FormatDate(dEl.GetString());

            var respProp = platform + "Response";
            if (root.TryGetProperty(respProp, out var rEl) && rEl.ValueKind == JsonValueKind.Object)
            {
                if (rEl.TryGetProperty("version", out var vEl) && vEl.ValueKind == JsonValueKind.String)
                    display = vEl.GetString() ?? "";
                if (string.IsNullOrEmpty(display))
                    display = version;
            }
            if (string.IsNullOrEmpty(display)) display = version;

            list.Add(new RobloxVersionInfo
            {
                Version = version,
                DisplayVersion = display,
                DateText = dateText,
                ClientUpload = version,
                IsCurrent = isCurrent
            });
        }

        private static string FormatDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            return raw;
        }

        private static async Task<JsonElement> GetJsonAsync(string url, CancellationToken ct)
        {
            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new SkyHttpException((int)resp.StatusCode, "HTTP " + (int)resp.StatusCode);
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }
}
