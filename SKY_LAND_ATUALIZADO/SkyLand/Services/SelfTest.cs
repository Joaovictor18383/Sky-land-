using System.IO;
using System.Text;
using System.Windows;

namespace SkyLand
{
    /// <summary>
    /// Teste automático silencioso (executado com --selftest): valida WEAO, GitHub,
    /// sistema de atualização e o pipeline de download sem abrir a janela.
    /// </summary>
    internal static class SelfTest
    {
        public static async Task RunAsync()
        {
            var sb = new StringBuilder();
            try
            {
                sb.AppendLine("=== SKY LAND SELFTEST ===");
                sb.AppendLine("Início: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var weao = await WeaoService.GetExploitsAsync();
                sb.AppendLine("WEAO: " + weao.Count + " itens recebidos.");
                foreach (var n in new[] { "Potassium", "Madium", "Real" })
                {
                    var it = WeaoService.Find(weao, n);
                    sb.AppendLine(n + " => " + (it != null
                        ? (it.UpdateStatus ? "ONLINE" : "DESATUALIZADO") + " " + it.Version
                        : "NÃO ENCONTRADO"));
                }

                var releases = await GitHubService.ListReleasesAsync("Joaovictor18383/Sky-land-");
                sb.AppendLine("GitHub releases: " + releases.Count);
                foreach (var r in releases)
                    sb.AppendLine(" - " + r.Tag + " assets=" + r.Assets.Count);

                var upd = await UpdateService.CheckAsync(CancellationToken.None);
                sb.AppendLine("Update: checkedOk=" + upd.CheckedOk + " hasUpdate=" + upd.HasUpdate +
                    " latest=" + (upd.LatestVersion ?? "(sem release do app publicada)") +
                    " historical=" + upd.AllReleases.Count);

                var dest = Path.Combine(AppPaths.Temp, "selftest_GITHUB_SCRIPT.txt");
                long finalLen = 0;
                await DownloadService.DownloadAsync(
                    "https://raw.githubusercontent.com/Joaovictor18383/Sky-land-/main/GITHUB_SCRIPT.txt",
                    dest,
                    new Progress<DownloadProgress>(p => finalLen = p.Received),
                    CancellationToken.None);
                sb.AppendLine("Download de validação: " + finalLen + " bytes.");

                sb.AppendLine("SELFTEST OK");
            }
            catch (Exception ex)
            {
                sb.AppendLine("SELFTEST FALHOU: " + ex.GetType().Name + ": " + ex.Message);
                if (ex.InnerException != null) sb.AppendLine("  Inner: " + ex.InnerException.Message);
            }

            try
            {
                AppPaths.EnsureFolders();
                File.WriteAllText(Path.Combine(AppPaths.Base, "selftest.txt"), sb.ToString());
            }
            catch { }

            try { Application.Current?.Shutdown(0); } catch { }
        }
    }
}
