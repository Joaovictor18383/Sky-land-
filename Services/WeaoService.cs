using System.Net.Http;
using System.Text.Json;

namespace SkyLand
{
    public sealed class WeaoItem
    {
        public string Title { get; set; } = "";
        public string Version { get; set; } = "";
        public string UpdatedDate { get; set; } = "";
        public bool UpdateStatus { get; set; }
        public bool Detected { get; set; }
    }

    public sealed record ExecutorStatus(
        string Name,
        bool Found,
        bool Online,          // updateStatus == true
        string Version,
        string UpdatedDate,
        string Error);

    /// <summary>API WEAO (WhatExpsAre.Online) — mesmo endpoint usado pelo sistema original.</summary>
    internal static class WeaoService
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static WeaoService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("WEAO-3PService");
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public const string ApiUrl = "https://weao.xyz/api/status/exploits";

        public static async Task<List<WeaoItem>> GetExploitsAsync(CancellationToken ct = default)
        {
            using var resp = await Http.GetAsync(ApiUrl, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new SkyHttpException((int)resp.StatusCode, "HTTP " + (int)resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = false };
            var list = JsonSerializer.Deserialize<List<WeaoItem>>(json, opts) ?? new List<WeaoItem>();
            return list;
        }

        /// <summary>Localiza um executor por título ( prática do script original: igual exato, depois contém).</summary>
        public static WeaoItem Find(List<WeaoItem> items, string name)
        {
            if (items == null) return null;
            name = name.Trim().ToLowerInvariant();

            var item = items.FirstOrDefault(i =>
                i.Title != null && i.Title.Trim().ToLowerInvariant() == name);
            if (item != null) return item;

            return items.FirstOrDefault(i =>
                i.Title != null && i.Title.ToLowerInvariant().Contains(name));
        }

        public static ExecutorStatus ToStatus(WeaoItem item, string name, string error = null)
        {
            if (error != null)
                return new ExecutorStatus(name, false, false, "", "", error);
            if (item == null)
                return new ExecutorStatus(name, false, false, "", "", null);

            return new ExecutorStatus(
                name,
                true,
                item.UpdateStatus,
                string.IsNullOrWhiteSpace(item.Version) ? "" : "v" + item.Version,
                item.UpdatedDate ?? "",
                null);
        }
    }
}
