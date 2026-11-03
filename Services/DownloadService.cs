using System.IO;
using System.Net.Http;
using System.Threading;

namespace SkyLand
{
    /// <summary>Erro HTTP com código legível para a interface.</summary>
    public class SkyHttpException : Exception
    {
        public int StatusCode { get; }

        public SkyHttpException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public record DownloadProgress(
        double Percent,          // 0..100 ou -1 = indeterminado
        long Received,
        long Total,
        double BytesPerSecond,
        string FileName);

    /// <summary>
    /// Downloads com HttpClient nativo (sem curl), progresso, velocidade, 3 tentativas
    /// com 2s de intervalo e validação de arquivo não vazio — mesmo comportamento do sistema original.
    /// </summary>
    internal static class DownloadService
    {
        private static readonly HttpClient Http = new HttpClient(
            new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        static DownloadService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("SKY-LAND/1.0");
        }

        public static async Task DownloadAsync(
            string url, string destPath,
            IProgress<DownloadProgress> progress,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Link de download vazio.");
            if (string.IsNullOrWhiteSpace(destPath)) throw new ArgumentException("Destino de download vazio.");

            Exception lastError = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await TryDownloadOnceAsync(url, destPath, progress, ct).ConfigureAwait(false);

                    var fi = new FileInfo(destPath);
                    if (!fi.Exists || fi.Length <= 0)
                        throw new IOException("O arquivo baixado está vazio.");

                    return;
                }
                catch (SkyHttpException)
                {
                    // Erro decisivo do servidor (404/403...): repetir não vai resolver.
                    try { File.Delete(destPath); } catch { }
                    throw;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex;
                    try { File.Delete(destPath); } catch { }
                    if (attempt < 3)
                    {
                        SetSafe(progress, new DownloadProgress(-1, 0, 0, 0, Path.GetFileName(destPath)), downloadFailed: true);
                        await Task.Delay(2000, ct).ConfigureAwait(false);
                    }
                }
            }

            throw lastError ?? new InvalidOperationException("Falha no download.");
        }

        public static async Task<string> GetTextAsync(string url, CancellationToken ct)
        {
            using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new SkyHttpException((int)resp.StatusCode, "HTTP " + (int)resp.StatusCode);
            return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }

        private static void SetSafe(IProgress<DownloadProgress> progress, DownloadProgress p, bool downloadFailed = false)
        {
            try { progress?.Report(p); } catch { }
        }

        private static async Task TryDownloadOnceAsync(
            string url, string destPath,
            IProgress<DownloadProgress> progress,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            AppPaths.TryCreate(Path.GetDirectoryName(destPath));
            try { File.Delete(destPath); } catch { }

            using (var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                if (!resp.IsSuccessStatusCode)
                    throw new SkyHttpException((int)resp.StatusCode, "HTTP " + (int)resp.StatusCode);

                var mediaType = resp.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && mediaType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("O link retornou uma página HTML em vez do arquivo.");

                long total = resp.Content.Headers.ContentLength ?? -1;
                string fileName = Path.GetFileName(destPath);

                SetSafe(progress, new DownloadProgress(total > 0 ? 0 : -1, 0, total, 0, fileName));

                await using (var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
                await using (var dst = new FileStream(
                    destPath, FileMode.Create, FileAccess.Write, FileShare.Read,
                    64 * 1024, useAsync: true))
                {
                    var buffer = new byte[80 * 1024];
                    long received = 0;
                    long windowBytes = 0;
                    var windowStart = DateTime.UtcNow;
                    var lastReport = DateTime.UtcNow;
                    double bps = 0;

                    while (true)
                    {
                        int read = await src.ReadAsync(buffer, ct).ConfigureAwait(false);
                        if (read <= 0) break;

                        await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                        received += read;
                        windowBytes += read;

                        var now = DateTime.UtcNow;
                        if ((now - lastReport).TotalMilliseconds >= 150)
                        {
                            var seconds = (now - windowStart).TotalSeconds;
                            if (seconds > 0.15)
                            {
                                var inst = windowBytes / seconds;
                                bps = bps <= 0 ? inst : bps * 0.5 + inst * 0.5; // média móvel suave
                                windowStart = now;
                                windowBytes = 0;
                            }
                            lastReport = now;

                            var percent = total > 0 ? Math.Round(received * 100.0 / Math.Max(total, 1), 1) : -1;
                            SetSafe(progress, new DownloadProgress(percent, received, total, bps, fileName));
                        }
                    }

                    await dst.FlushAsync(ct).ConfigureAwait(false);
                }
            }

            var fi = new FileInfo(destPath);
            if (!fi.Exists || fi.Length <= 0)
                throw new IOException("O arquivo baixado está vazio.");

            SetSafe(progress, new DownloadProgress(100, fi.Length, fi.Length, 0, Path.GetFileName(destPath)));
        }
    }
}
