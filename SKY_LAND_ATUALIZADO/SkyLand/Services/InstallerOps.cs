using System.IO;

namespace SkyLand
{
    /// <summary>
    /// Instalação das dependências oficiais (WebView2, .NET Desktop Runtime, Visual C++),
    /// com confirmação explícita, download com progresso e execução silenciosa — 
    /// mesmo roteiro do GITHUB_SCRIPT.txt, agora com UI profissional.
    /// </summary>
    internal static class InstallerOps
    {
        public const string DotNet6Url =
            "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/6.0.36/windowsdesktop-runtime-6.0.36-win-x64.exe";
        public const string DotNet8Url =
            "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.27/windowsdesktop-runtime-8.0.27-win-x64.exe";
        public const string WebView2Url = "https://go.microsoft.com/fwlink/?linkid=2124701";
        public const string Vc64Url = "https://aka.ms/vc14/vc_redist.x64.exe";
        public const string Vc86Url = "https://aka.ms/vc14/vc_redist.x86.exe";

        // ---------- WebView2 ----------
        public static async Task<bool> InstallWebView2Async(OpVM vm, IShell shell, CancellationToken ct)
        {
            if (RuntimeChecks.WebView2Installed())
            {
                vm.SetResult(true, "WebView2 já está instalado neste Windows.");
                return true;
            }

            var ok = await shell.ConfirmAsync(
                "Instalar Microsoft Edge WebView2",
                "O WebView2 não foi localizado neste computador.\n\n" +
                "Será baixado o instalador oficial da Microsoft e executado em modo " +
                "silencioso (/silent /install). Nenhuma ação administrativa extra é necessária.\n\n" +
                "Deseja continuar?",
                "Instalar", "Mais tarde", false);

            if (!ok)
            {
                vm.SetNeutralResult("Instalação do WebView2 deixada para depois.");
                return false;
            }

            var installer = Path.Combine(AppPaths.Temp, "MicrosoftEdgeWebView2.exe");
            vm.SetStatus("Baixando WebView2…");
            await DownloadService.DownloadAsync(WebView2Url, installer,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            vm.SetStatus("Instalando WebView2 (aguarde, pode demorar alguns segundos)…");
            int code;
            try
            {
                code = await ProcessRunner.RunInstallerAsync(installer, "/silent /install", false, ct);
            }
            catch (Exception ex)
            {
                try { File.Delete(installer); } catch { }
                vm.SetResult(false, "Falha ao instalar o WebView2: " + UiHelper.FriendlyError(ex));
                return false;
            }

            try { File.Delete(installer); } catch { }

            if (RuntimeChecks.WebView2Installed())
            {
                vm.SetResult(true, "WebView2 instalado com sucesso.");
                return true;
            }

            vm.SetResult(code == 0 || code == 3010,
                code == 0 || code == 3010
                    ? "Instalador do WebView2 concluído (código 0)."
                    : "O instalador do WebView2 retornou o código " + code + ".");
            return code == 0 || code == 3010;
        }

        // ---------- .NET Desktop Runtime ----------
        public static async Task<bool> InstallDotNetRuntimeAsync(OpVM vm, IShell shell, CancellationToken ct, string major)
        {
            if (RuntimeChecks.HasWindowsDesktopRuntime(major))
            {
                vm.SetResult(true, ".NET Desktop Runtime " + major + " já está instalado.");
                return true;
            }

            var ok = await shell.ConfirmAsync(
                "Instalar .NET Desktop Runtime " + major,
                "O .NET Desktop Runtime " + major + " não foi localizado.\n\n" +
                "Será baixado o instalador oficial da Microsoft e executado em modo " +
                "silencioso (/install /quiet /norestart). O Windows pode pedir confirmação." +
                "\n\nDeseja continuar?",
                "Instalar", "Mais tarde", false);

            if (!ok)
            {
                vm.SetNeutralResult("Instalação do .NET " + major + " deixada para depois.");
                return false;
            }

            var url = major == "6" ? DotNet6Url : DotNet8Url;
            var installer = Path.Combine(AppPaths.Temp, "dotnet" + major + "_desktop.exe");

            vm.SetStatus("Baixando .NET Desktop Runtime " + major + "…");
            await DownloadService.DownloadAsync(url, installer,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            vm.SetStatus("Instalando .NET Desktop Runtime " + major + "…");
            int code = 1;
            try
            {
                code = await ProcessRunner.RunInstallerAsync(installer, "/install /quiet /norestart", true, ct);
            }
            catch (Exception ex)
            {
                try { File.Delete(installer); } catch { }
                vm.SetResult(false, "Falha ao instalar o .NET " + major + ": " + UiHelper.FriendlyError(ex));
                return false;
            }

            try { File.Delete(installer); } catch { }

            var succeeded = RuntimeChecks.HasWindowsDesktopRuntime(major);
            vm.SetResult(succeeded || code == 0,
                succeeded
                    ? ".NET Desktop Runtime " + major + " instalado."
                    : code == 3010
                        ? "Instalado — é necessário reiniciar o Windows para concluir."
                        : "O instalador do .NET " + major + " retornou o código " + code + ".");
            return vm.IsSuccess;
        }

        // ---------- Visual C++ ----------
        public static async Task<bool> InstallVcAsync(OpVM vm, IShell shell, CancellationToken ct, string arch)
        {
            if (RuntimeChecks.HasVcRuntime(arch))
            {
                vm.SetResult(true, "Visual C++ " + arch + " já está instalado.");
                return true;
            }

            var ok = await shell.ConfirmAsync(
                "Instalar Visual C++ " + arch,
                "O Microsoft Visual C++ Redistributable (" + arch + ") não foi localizado.\n\n" +
                "Será baixado o instalador oficial ('vc_redist." + arch + ".exe') e executado com " +
                "/install /passive /norestart. O Windows vai pedir permissão de administrador.\n\n" +
                "Deseja continuar?",
                "Instalar", "Mais tarde", false);

            if (!ok)
            {
                vm.SetNeutralResult("Instalação do Visual C++ " + arch + " deixada para depois.");
                return false;
            }

            var url = arch == "x64" ? Vc64Url : Vc86Url;
            var installer = Path.Combine(AppPaths.Temp, "vc_redist." + arch + ".exe");

            vm.SetStatus("Baixando Visual C++ " + arch + "…");
            await DownloadService.DownloadAsync(url, installer,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            vm.SetStatus("Instalando Visual C++ " + arch + "…");
            int code = 1;
            try
            {
                code = await ProcessRunner.RunInstallerAsync(installer, "/install /passive /norestart", true, ct);
            }
            catch (Exception ex)
            {
                try { File.Delete(installer); } catch { }
                vm.SetResult(false, "Falha ao instalar o Visual C++ " + arch + ": " + UiHelper.FriendlyError(ex));
                return false;
            }

            try { File.Delete(installer); } catch { }

            // 0 = sucesso, 3010 = precisa reiniciar, 1638 = já instalado (mesma regra do script original)
            var okCode = code == 0 || code == 3010 || code == 1638;
            vm.SetResult(okCode,
                okCode
                    ? (code == 3010
                        ? "Instalado — reinicie o Windows para concluir a instalação."
                        : "Visual C++ " + arch + " instalado/verificado.")
                    : "O Visual C++ " + arch + " retornou o código " + code + ".");
            return okCode;
        }

        // ---------- Pacote completo do Madium ----------
        public static async Task InstallMadDependenciesAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            var ok = await shell.ConfirmAsync(
                "Dependências do Madium",
                "Este processo instala, quando necessário:\n\n" +
                "  1. .NET Desktop Runtime 8  (Microsoft)\n" +
                "  2. Visual C++ x64  (Microsoft)\n" +
                "  3. Visual C++ x86  (Microsoft)\n" +
                "  4. WebView2  (Microsoft)\n\n" +
                "Instaladores oficiais da Microsoft, silenciosos. O Windows pode pedir permissão.\n\n" +
                "Deseja começar?",
                "Iniciar", "Mais tarde", false);

            if (!ok)
            {
                vm.SetNeutralResult("Instalação de dependências cancelada.");
                return;
            }

            var steps = new (string, Func<CancellationToken, Task<bool>>)[]
            {
                (".NET Desktop Runtime 8", t => InstallDotNetRuntimeAsync(vm, shell, t, "8")),
                ("Visual C++ x64", t => InstallVcAsync(vm, shell, t, "x64")),
                ("Visual C++ x86", t => InstallVcAsync(vm, shell, t, "x86")),
                ("WebView2", t => InstallWebView2Async(vm, shell, t)),
            };

            int index = 0;
            var results = new List<string>();
            foreach (var (name, run) in steps)
            {
                index++;
                vm.SetBusy("[" + index + "/4] Verificando e instalando: " + name + "…");
                ActivityService.Add(ActivityKind.Info, "Dependência: verificando " + name);
                var done = await run(ct);
                results.Add((done ? "OK" : "Pendentes") + " — " + name);
            }

            vm.SetResult(results.All(r => r.StartsWith("OK", StringComparison.OrdinalIgnoreCase)),
                "Processo finalizado:\n" + string.Join("\n", results));
        }
    }
}
