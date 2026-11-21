using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SkyLand
{
    public sealed record ChatMessage(string Role, string Content);

    /// <summary>
    /// Cliente da API AgentRouter (https://agentrouter.org) — endpoint em /v1 compatível
    /// com o formato OpenAI (chat completions). A chave vem de %APPDATA%\SKY LAND\settings.json.
    /// </summary>
    internal static class AgentRouterService
    {
        public const string BaseUrl = "https://agentrouter.org/v1";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

        static AgentRouterService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("SKY-LAND");
        }

        public static string ApiKey => SettingsStore.Current.AgentRouterApiKey ?? "";
        public static bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

        /// <summary>Valida a chave consultando GET /v1/models. Retorna a lista de IDs de modelo.</summary>
        public static async Task<List<string>> ListModelsAsync(CancellationToken ct = default)
        {
            var json = await SendAsync(HttpMethod.Get, BaseUrl + "/models", body: null, ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            var ids = new List<string>();
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        ids.Add(id.GetString() ?? "");
                }
            }
            return ids.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        /// <summary>Chat "one-shot": recebe o histórico de mensagens e devolve o texto do Modelo.</summary>
        public static async Task<string> ChatAsync(
            IReadOnlyList<ChatMessage> messages, string model, CancellationToken ct = default)
        {
            if (messages == null || messages.Count == 0)
                throw new ArgumentException("Nenhuma mensagem enviada.", nameof(messages));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Informe o modelo (ex.: claude-sonnet-4-5).", nameof(model));

            var payload = new
            {
                model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            };

            var json = await SendAsync(
                HttpMethod.Post,
                BaseUrl + "/chat/completions",
                JsonSerializer.Serialize(payload),
                ct).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                    return content.GetString() ?? "";
            }

            throw new SkyHttpException(0, "Resposta inesperada da AgentRouter.");
        }

        private static async Task<string> SendAsync(HttpMethod method, string url, string body, CancellationToken ct)
        {
            if (!HasKey)
                throw new InvalidOperationException(
                    "Nenhuma API key do AgentRouter configurada — salve-a em Configurações.");

            using var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
            req.Headers.Accept.ParseAdd("application/json");

            if (!string.IsNullOrEmpty(body))
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                // A mensagem de erro da API costuma vir em JSON { error: { message } }
                string detail = "";
                try
                {
                    using var d = JsonDocument.Parse(text);
                    if (d.RootElement.TryGetProperty("error", out var err))
                        detail = err.TryGetProperty("message", out var m)
                            ? m.GetString() ?? ""
                            : err.ToString() ?? "";
                }
                catch { }

                throw new SkyHttpException(
                    (int)resp.StatusCode,
                    "HTTP " + (int)resp.StatusCode + (string.IsNullOrWhiteSpace(detail) ? "" : " — " + detail));
            }

            return text;
        }
    }
}
