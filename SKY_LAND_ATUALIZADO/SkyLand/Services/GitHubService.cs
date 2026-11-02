using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SkyLand
{
    public sealed class GhAsset
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Digest { get; set; }
        public string DownloadUrl { get; set; }
    }

    public sealed class GhRelease
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        public bool Prerelease { get; set; }
        public string UrlHtml { get; set; }
        public string Body { get; set; }
        public DateTime PublishedUtc { get; set; }
        public List<GhAsset> Assets { get; set; } = new();
    }

    internal sealed record GhApiAsset(string Name, long Size, string Digest, string BrowserDownloadUrl);
    internal sealed record GhApiRelease(
        string TagName, string Name, bool Prerelease, string HtmlUrl,
        string Body, DateTime? PublishedAt, List<GhApiAsset> Assets);

    /// <summary>Leitura da API do GitHub (releases) com User-Agent próprio — sem dependências externas.</summary>
    internal static class GitHubService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

        static GitHubService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("SKY-LAND");
            Http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public static async Task<List<GhRelease>> ListReleasesAsync(
            string repo, CancellationToken ct = default)
        {
            var url = "https://api.github.com/repos/" + repo + "/releases?per_page=30";
            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new SkyHttpException((int)resp.StatusCode, "HTTP " + (int)resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<List<GhApiRelease>>(json, opts) ?? new List<GhApiRelease>();

            var list = new List<GhRelease>();
            foreach (var r in raw)
            {
                var release = new GhRelease
                {
                    Tag = r.TagName ?? "",
                    Name = r.Name ?? r.TagName ?? "",
                    Prerelease = r.Prerelease,
                    UrlHtml = r.HtmlUrl ?? "",
                    Body = r.Body ?? "",
                    PublishedUtc = r.PublishedAt ?? default
                };
                foreach (var a in r.Assets ?? new List<GhApiAsset>())
                {
                    release.Assets.Add(new GhAsset
                    {
                        Name = a.Name ?? "",
                        Size = a.Size,
                        Digest = a.Digest ?? "",
                        DownloadUrl = a.BrowserDownloadUrl ?? ""
                    });
                }
                list.Add(release);
            }
            return list;
        }

        /// <summary>Busca (na release mais recente) um asset que passe no filtro fornecido.</summary>
        public static async Task<(GhRelease, GhAsset)?> FindAssetAsync(
            string repo, Func<GhAsset, bool> filter, CancellationToken ct)
        {
            var releases = await ListReleasesAsync(repo, ct).ConfigureAwait(false);

            var release = releases.FirstOrDefault(r =>
                !r.Prerelease && r.Assets.Any(filter))
                ?? releases.FirstOrDefault(r => r.Assets.Any(filter));

            if (release == null) return null;
            var asset = release.Assets.First(filter);
            return (release, asset);
        }

        public static Task<(GhRelease, GhAsset)?> FindNamedAssetAsync(string repo, string assetName, CancellationToken ct)
        {
            return FindAssetAsync(repo, a => a.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase), ct);
        }

        public static Task<(GhRelease, GhAsset)?> FindZipAssetAsync(string repo, CancellationToken ct)
        {
            return FindAssetAsync(repo, a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase), ct);
        }

        /// <summary>
        /// Encontra a release mais recente que publica um executável/zip do SKY LAND
        /// (convenção: asset arquivo com "SKY LAND" e extensão .exe ou .zip). Null se não publicado.
        /// </summary>
        public static (GhRelease, GhAsset)? FindAppUpdateAsset(List<GhRelease> releases)
        {
            var rx = new Regex(@"sky[\s._-]*land.*\.(exe|zip)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (var r in releases)
            {
                var asset = r.Assets.FirstOrDefault(a => rx.IsMatch(a.Name ?? ""));
                if (asset != null)
                    return (r, asset);
            }
            return null;
        }
    }

    public sealed class AppUpdateInfo
    {
        public bool CheckedOk { get; set; }
        public string Error { get; set; }
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = App.VersionText;
        public string LatestVersion { get; set; }
        public string LatestTag { get; set; }
        public string Notes { get; set; }
        public string HtmlUrl { get; set; }
        public GhRelease Release { get; set; }
        public GhAsset Asset { get; set; }
        public List<GhRelease> AllReleases { get; set; } = new();
        public bool IsZip => Asset != null && Asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sistema de atualização do próprio SKY LAND (releases oficiais do repositório).</summary>
    internal static class UpdateService
    {
        public static async Task<AppUpdateInfo> CheckAsync(CancellationToken ct)
        {
            var info = new AppUpdateInfo();
            try
            {
                var releases = await GitHubService.ListReleasesAsync("Joaovictor18383/Sky-land-", ct).ConfigureAwait(false);
                info.CheckedOk = true;
                info.HtmlUrl = releases.FirstOrDefault(r =>
                    !string.IsNullOrEmpty(r.UrlHtml))?.UrlHtml ?? "https://github.com/Joaovictor18383/Sky-land-/releases";

                // Histórico é a lista inteira de releases do repositório.
                info.AllReleases = releases;

                var found = GitHubService.FindAppUpdateAsset(releases);
                if (found == null)
                {
                    info.HasUpdate = false;
                    info.LatestVersion = null;
                    info.LatestTag = null;
                    return info;
                }

                var (release, asset) = found.Value;
                info.LatestTag = release.Tag;
                info.LatestVersion = ParseVersionFromText(release.Tag) ?? ParseVersionFromText(release.Name) ?? "1.0";
                info.Release = release;
                info.Asset = asset;
                info.Notes = UiHelper.TrimText(release.Body, 400);
                info.HasUpdate = CompareVersion(info.LatestVersion, info.CurrentVersion) > 0;
            }
            catch (Exception ex)
            {
                info.CheckedOk = false;
                info.Error = UiHelper.FriendlyError(ex);
            }
            return info;
        }

        public static string ParseVersionFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var m = Regex.Match(text, @"(\d+\.\d+(?:\.\d+)?)");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Retorna 1 se a > b, -1 se a < b, 0 se iguais.</summary>
        public static int CompareVersion(string a, string b)
        {
            if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
                return va.CompareTo(vb);

            var cmp = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            return cmp > 0 ? 1 : cmp < 0 ? -1 : 0;
        }

        public static async Task<string> DownloadUpdateAsync(
            AppUpdateInfo info, string destDir,
            IProgress<DownloadProgress> progress, CancellationToken ct)
        {
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, info.Asset.Name);
            await DownloadService.DownloadAsync(info.Asset.DownloadUrl, dest, progress, ct).ConfigureAwait(false);

            // Validação: hash publicado (sha256) quando a release informar um digest.
            if (!string.IsNullOrWhiteSpace(info.Asset.Digest) && info.Asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                var expected = info.Asset.Digest.Substring(7).Trim().ToLowerInvariant();
                var actual = ComputeSha256(dest);
                if (actual != expected)
                {
                    try { File.Delete(dest); } catch { }
                    throw new IOException("O arquivo baixado não corresponde ao hash publicado no GitHub (verificação de segurança).");
                }
            }
            else
            {
                var fi = new FileInfo(dest);
                if (info.Asset.Size > 0 && fi.Length != info.Asset.Size)
                {
                    try { File.Delete(dest); } catch { }
                    throw new IOException("O tamanho do arquivo baixado difere do publicado!");
                }
            }
            return dest;
        }

        public static string ComputeSha256(string path)
        {
            using var hash = SHA256.Create();
            using var stream = File.OpenRead(path);
            var bytes = hash.ComputeHash(stream);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Extrai um ZIP de atualização e localiza o EXE principal.</summary>
        public static string ExtractUpdateZip(string zipPath, string destDir)
        {
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);
            Directory.CreateDirectory(destDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);

            var exe = Directory.EnumerateFiles(destDir, "*.exe", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.IndexOf("sky", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? Directory.EnumerateFiles(destDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (exe == null)
                throw new IOException("Nenhum executável encontrado dentro do pacote de atualização.");
            return exe;
        }
    }
}
