using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Win32;

namespace SkyLand
{
    /// <summary>Verifica e instala dependências automaticamente (.NET 6, .NET 8, WebView2, Visual C++).</summary>
    internal static class NetInstaller
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

        // URLs do .bat
        private const string DotNet6Url = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/6.0.36/windowsdesktop-runtime-6.0.36-win-x64.exe";
        private const string DotNet8Url = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.27/windowsdesktop-runtime-8.0.27-win-x64.exe";
        private const string WebView2Url = "https://go.microsoft.com/fwlink/?linkid=2124701";
        private const string Vc64Url = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
        private const string Vc86Url = "https://aka.ms/vs/17/release/vc_redist.x86.exe";

        public static bool IsNet6Installed()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("Microsoft.WindowsDesktop.App") && output.Contains("6.0");
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNet8Installed()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("Microsoft.WindowsDesktop.App") && output.Contains("8.0");
            }
            catch
            {
                return false;
            }
        }

        public static bool IsWebView2Installed()
        {
            try
            {
                var webViewPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft",
                    "EdgeWebView",
                    "Application");

                if (Directory.Exists(webViewPath)) return true;

                webViewPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft",
                    "EdgeWebView",
                    "Application");

                return Directory.Exists(webViewPath);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsVcInstalled(string architecture)
        {
            try
            {
                string[] paths;
                if (architecture == "x64")
                {
                    paths = new[] { @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" };
                }
                else
                {
                    paths = new[]
                    {
                        @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86",
                        @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86"
                    };
                }

                foreach (var path in paths)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    if (key != null)
                    {
                        var installed = key.GetValue("Installed");
                        if (installed != null && installed.Equals(1))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static async Task InstallNet6Async(Action<string> progressCallback, CancellationToken ct)
        {
            await InstallRuntimeAsync(DotNet6Url, "dotnet6_desktop.exe", ".NET 6", progressCallback, ct);
        }

        public static async Task InstallNet8Async(Action<string> progressCallback, CancellationToken ct)
        {
            await InstallRuntimeAsync(DotNet8Url, "dotnet8_desktop.exe", ".NET 8", progressCallback, ct);
        }

        public static async Task InstallWebView2Async(Action<string> progressCallback, CancellationToken ct)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SKY_LAND_WEBVIEW2");
            Directory.CreateDirectory(tempDir);

            var installerPath = Path.Combine(tempDir, "MicrosoftEdgeWebView2.exe");
            progressCallback("Baixando Microsoft Edge WebView2...");

            try
            {
                await DownloadFileAsync(WebView2Url, installerPath, progressCallback, "WebView2", ct);

                progressCallback("Instalando WebView2...");

                var installProcess = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/silent /install",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = Process.Start(installProcess);
                if (process != null)
                {
                    await process.WaitForExitAsync(ct);
                }

                progressCallback("WebView2 instalado com sucesso!");
            }
            finally
            {
                Cleanup(tempDir, installerPath);
            }
        }

        public static async Task InstallVcAsync(string architecture, Action<string> progressCallback, CancellationToken ct)
        {
            var url = architecture == "x64" ? Vc64Url : Vc86Url;
            var tempDir = Path.Combine(Path.GetTempPath(), "SKY_LAND_VC" + architecture);
            Directory.CreateDirectory(tempDir);

            var installerPath = Path.Combine(tempDir, $"vc_redist.{architecture}.exe");
            progressCallback($"Baixando Visual C++ {architecture}...");

            try
            {
                await DownloadFileAsync(url, installerPath, progressCallback, $"Visual C++ {architecture}", ct);

                progressCallback($"Instalando Visual C++ {architecture}...");

                var installProcess = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/install /passive /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = Process.Start(installProcess);
                if (process != null)
                {
                    await process.WaitForExitAsync(ct);
                }

                progressCallback($"Visual C++ {architecture} instalado com sucesso!");
            }
            finally
            {
                Cleanup(tempDir, installerPath);
            }
        }

        private static async Task InstallRuntimeAsync(string url, string fileName, string name, Action<string> progressCallback, CancellationToken ct)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SKY_LAND_" + name.Replace(".", ""));
            Directory.CreateDirectory(tempDir);

            var installerPath = Path.Combine(tempDir, fileName);
            progressCallback($"Baixando {name} Desktop Runtime...");

            try
            {
                await DownloadFileAsync(url, installerPath, progressCallback, name, ct);

                progressCallback($"Instalando {name} Desktop Runtime...");

                var installProcess = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = Process.Start(installProcess);
                if (process != null)
                {
                    await process.WaitForExitAsync(ct);
                }

                progressCallback($"{name} instalado com sucesso!");
            }
            finally
            {
                Cleanup(tempDir, installerPath);
            }
        }

        private static async Task DownloadFileAsync(string url, string destination, Action<string> progressCallback, string name, CancellationToken ct)
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var bytesRead = 0L;

            using var fileStream = File.Create(destination);
            using var stream = await response.Content.ReadAsStreamAsync(ct);

            while (true)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read == 0) break;

                await fileStream.WriteAsync(buffer, 0, read, ct);
                bytesRead += read;

                if (totalBytes > 0)
                {
                    var percent = (int)((bytesRead * 100) / totalBytes);
                    progressCallback($"Baixando {name}... {percent}%");
                }
            }

            fileStream.Close();
        }

        private static void Cleanup(string tempDir, string installerPath)
        {
            try
            {
                if (File.Exists(installerPath)) File.Delete(installerPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }
}
