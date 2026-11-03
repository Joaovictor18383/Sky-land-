using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace SkyLand
{
    /// <summary>Escala de processos: instaladores, encerramento, extração e atalhos do Shell.</summary>
    internal static class ProcessRunner
    {
        /// <summary>Executa um instalador e aguarda a saída (como o original: Start-Process -Wait).</summary>
        public static async Task<int> RunInstallerAsync(string exePath, string args, bool asAdmin, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var psi = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Arguments = args ?? "",
                WorkingDirectory = SafeDir(exePath)
            };
            if (asAdmin) psi.Verb = "runas";

            using var p = Process.Start(psi) ?? throw new IOException("Não foi possível iniciar " + Path.GetFileName(exePath));
            try
            {
                await p.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(true); } catch { }
                throw;
            }
            return p.ExitCode;
        }

        /// <summary>Inicia um executável visível ao usuário (sem aguardar).</summary>
        public static void Start(string path, bool asAdmin = false)
        {
            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = SafeDir(path)
            };
            if (asAdmin) psi.Verb = "runas";
            Process.Start(psi);
        }

        public static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static void OpenFolder(string folder)
        {
            if (Directory.Exists(folder))
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
        }

        /// <summary>Abre a pasta e seleciona o arquivo (mostra claramente onde o download foi salvo).</summary>
        public static void OpenFileInFolder(string file)
        {
            var path = file ?? "";
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
                return;
            }
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                OpenFolder(dir);
        }

        public static void KillProcesses(params string[] names)
        {
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                try
                {
                    foreach (var p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            p.Kill(entireProcessTree: true);
                            p.Dispose();
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        /// <summary>Nomes informados que possuem pelo menos um processo em execução.</summary>
        public static List<string> FindActiveProcesses(IEnumerable<string> names)
        {
            var hits = new List<string>();
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                try
                {
                    var procs = Process.GetProcessesByName(name);
                    if (procs.Length > 0)
                    {
                        hits.Add(name + ".exe (x" + procs.Length + ")");
                        foreach (var p in procs) p.Dispose();
                    }
                }
                catch { }
            }
            return hits;
        }

        public static async Task ExtractZipAsync(string zipPath, string destDir, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(destDir);
                ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);
            }, ct).ConfigureAwait(false);
        }

        private static string SafeDir(string path)
        {
            try { return Path.GetDirectoryName(path); } catch { return null; }
        }
    }
}
