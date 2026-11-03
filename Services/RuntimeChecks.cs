using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace SkyLand
{
    /// <summary>Verificação de dependências local: WebView2, .NET Desktop Runtime e Visual C++.</summary>
    internal static class RuntimeChecks
    {
        public static bool WebView2Installed()
        {
            try
            {
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                if (Directory.Exists(Path.Combine(pf86, "Microsoft", "EdgeWebView", "Application")))
                    return true;
                if (Directory.Exists(Path.Combine(pf, "Microsoft", "EdgeWebView", "Application")))
                    return true;

                const string webviewKey =
                    @"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

                foreach (var view in new[] { RegistryView.Default, RegistryView.Registry64 })
                {
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using var key = baseKey.OpenSubKey(webviewKey, false);
                    if (key == null) continue;
                    var pv = key.GetValue("pv") as string;
                    if (!string.IsNullOrWhiteSpace(pv) && pv.Replace("0", "").Trim().Length > 0)
                        return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Verifica se o .NET Desktop Runtime de uma major está instalado (mesma checagem do sistema original).</summary>
        public static bool HasWindowsDesktopRuntime(string major)
        {
            try
            {
                var psi = new ProcessStartInfo("dotnet", "--list-runtimes")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(15000);

                foreach (var line in output.Split('\n'))
                {
                    if (line.IndexOf("Microsoft.WindowsDesktop.App " + major + ".", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>Verificação do Visual C++ Redistributable via chave do registro (igual ao script original).</summary>
        public static bool HasVcRuntime(string architecture)
        {
            try
            {
                string keyPath;
                if (architecture == "x64")
                {
                    keyPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";
                }
                else
                {
                    keyPath = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86";
                }

                using var key = Registry.LocalMachine.OpenSubKey(keyPath, false);
                if (key != null)
                {
                    var installed = key.GetValue("Installed");
                    if (installed != null && Convert.ToInt32(installed) == 1)
                        return true;
                }

                if (architecture != "x64")
                {
                    // Fallback: chave nativa x86
                    using var key2 = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86", false);
                    if (key2 != null)
                    {
                        var installed2 = key2.GetValue("Installed");
                        if (installed2 != null && Convert.ToInt32(installed2) == 1)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>Média/detalhe: índice de núcleos lógicos (uso em mensagens).</summary>
        public static int ProcessorCount => Environment.ProcessorCount;

        public static string QuickSystemInfo()
        {
            var os = Environment.OSVersion;
            string win = os.Version.Major >= 10
                ? (os.Version.Build >= 22000 ? "Windows 11" : "Windows 10")
                : "Windows " + os.Version;
            return win + " x" + (IntPtr.Size == 8 ? "64" : "32") + " — .NET " + Environment.Version;
        }
    }
}
