using System.IO;

namespace SkyLand
{
    /// <summary>
    /// Operações dos executores e ferramentas — URLs, destinos e fluxos preservados do
    /// GITHUB_SCRIPT.txt (Potassium, Madium, Real, Fishstrap, Roblox Account Manager,
    /// MultiBloxy, AnyDesk), com interface gráfica e confirmações de segurança.
    /// </summary>
    internal static class ExecutorOps
    {
        public const string PotassiumUrl = "https://files.potassium.pro/OpXOyuApWKTlFzrV.zip";
        public const string MadiumUrl = "https://cdn.getmadium.me/Installer-1.1.0.exe";
        public const string RealUrl = "https://github.com/Joaovictor18383/Executores/releases/download/real-v1/RealSetup.exe";
        public const string FishstrapRepo = "fishstrap/fishstrap";
        public const string RamRepo = "ic3w0lf22/Roblox-Account-Manager";
        public const string MultiBloxyUrl = "https://github.com/Zgoly/MultiBloxy/releases/download/v1.1.0.0/MultiBloxy.exe";
        public const string AnyDeskUrl = "https://download.anydesk.com/AnyDesk.exe";

        private static string Downloads => SettingsStore.GetDownloadsPath();

        /// <summary>Baixa um arquivo direto para a pasta de downloads configurada.</summary>
        private static async Task<string> DownloadToDownloadsAsync(
            OpVM vm, string url, string fileName, CancellationToken ct, string statusText = null)
        {
            vm.SetStatus(statusText ?? "Baixando " + fileName + "…");
            var path = Path.Combine(Downloads, fileName);
            await DownloadService.DownloadAsync(url, path,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);
            return path;
        }

        // ------------------------------------------------------------------ Potassium
        public static async Task DownloadPotassiumAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            // Verificar e instalar dependências (WebView2 para Potassium)
            if (!NetInstaller.IsWebView2Installed())
            {
                var okWebView = await shell.ConfirmAsync(
                    "WebView2 Necessário",
                    "O Potassium requer o Microsoft Edge WebView2, que não está instalado.\nDeseja baixar e instalar agora?",
                    "Instalar", "Mais tarde", false);
                if (okWebView)
                {
                    vm.SetStatus("Instalando WebView2…");
                    await NetInstaller.InstallWebView2Async(msg => vm.SetStatus(msg), ct);
                }
            }

            var path = await DownloadToDownloadsAsync(vm, PotassiumUrl, "Potassium.zip", ct, "Baixando Potassium…");
            vm.SetResult(true, "Potassium.zip baixado com sucesso.", Downloads, path);
        }

        // ------------------------------------------------------------------ Real
        public static async Task DownloadRealAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            // Verificar e instalar dependências (.NET 8, WebView2, VC++ como Madium)
            if (!NetInstaller.IsNet8Installed())
            {
                var okNet8 = await shell.ConfirmAsync(
                    ".NET 8.0 Necessário",
                    "O Real requer o .NET 8.0 Desktop Runtime, que não está instalado.\nDeseja baixar e instalar agora?",
                    "Instalar", "Mais tarde", false);
                if (okNet8)
                {
                    vm.SetStatus("Instalando .NET 8.0…");
                    await NetInstaller.InstallNet8Async(msg => vm.SetStatus(msg), ct);
                }
            }

            if (!NetInstaller.IsWebView2Installed())
            {
                var okWebView = await shell.ConfirmAsync(
                    "WebView2 Necessário",
                    "O Real requer o Microsoft Edge WebView2, que não está instalado.\nDeseja baixar e instalar agora?",
                    "Instalar", "Mais tarde", false);
                if (okWebView)
                {
                    vm.SetStatus("Instalando WebView2…");
                    await NetInstaller.InstallWebView2Async(msg => vm.SetStatus(msg), ct);
                }
            }

            if (!NetInstaller.IsVcInstalled("x64"))
            {
                var okVc64 = await shell.ConfirmAsync(
                    "Visual C++ x64 Necessário",
                    "O Real requer o Visual C++ x64, que não está instalado.\nDeseja baixar e instalar agora?",
                    "Instalar", "Mais tarde", false);
                if (okVc64)
                {
                    vm.SetStatus("Instalando Visual C++ x64…");
                    await NetInstaller.InstallVcAsync("x64", msg => vm.SetStatus(msg), ct);
                }
            }

            if (!NetInstaller.IsVcInstalled("x86"))
            {
                var okVc86 = await shell.ConfirmAsync(
                    "Visual C++ x86 Necessário",
                    "O Real requer o Visual C++ x86, que não está instalado.\nDeseja baixar e instalar agora?",
                    "Instalar", "Mais tarde", false);
                if (okVc86)
                {
                    vm.SetStatus("Instalando Visual C++ x86…");
                    await NetInstaller.InstallVcAsync("x86", msg => vm.SetStatus(msg), ct);
                }
            }

            vm.SetStatus("Encerrando instalação anterior do Real…");
            ProcessRunner.KillProcesses("RealSetup");
            await Task.Delay(500, ct);

            var path = await DownloadToDownloadsAsync(vm, RealUrl, "RealSetup.exe", ct, "Baixando Real (GitHub)…");
            vm.SetResult(true, "RealSetup.exe baixado com sucesso.", Downloads, path);
        }

        // ------------------------------------------------------------------ MultiBloxy
        public static async Task DownloadMultiBloxyAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            vm.SetStatus("Preparando download…");
            ProcessRunner.KillProcesses("MultiBloxy");

            var path = await DownloadToDownloadsAsync(vm, MultiBloxyUrl, "MultiBloxy.exe", ct);
            vm.SetResult(true, "MultiBloxy.exe baixado com sucesso.", Downloads, path);
        }

        // ------------------------------------------------------------------ AnyDesk
        public static async Task DownloadAnyDeskAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            vm.SetStatus("Preparando download…");
            ProcessRunner.KillProcesses("AnyDesk");

            var path = await DownloadToDownloadsAsync(vm, AnyDeskUrl, "AnyDesk.exe", ct);
            vm.SetResult(true, "AnyDesk.exe baixado com sucesso.", Downloads, path);
        }

        // ------------------------------------------------------------------ Madium
        public static async Task DownloadMadiumAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            var tempFile = Path.Combine(AppPaths.Temp, "Madium_Installer.exe");
            vm.SetStatus("Baixando o instalador do Madium…");
            await DownloadService.DownloadAsync(MadiumUrl, tempFile,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            var ok = await shell.ConfirmAsync(
                "Executar o instalador do Madium",
                "Download concluído com sucesso.\n\n" +
                "Arquivo: " + tempFile + "\n\n" +
                "Executar o instalador agora? Você acompanhará a instalação na janela do Madium.",
                "Executar agora", "Somente o download", false);

            if (ok)
            {
                vm.SetStatus("Executando o instalador do Madium — conclua as etapas na janela dele…");
                try
                {
                    await ProcessRunner.RunInstallerAsync(tempFile, "", false, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    vm.SetResult(false, "Não foi possível executar o Madium: " + UiHelper.FriendlyError(ex));
                    return;
                }

                try { File.Delete(tempFile); } catch { }
                vm.SetResult(true, "Instalador do Madium executado e finalizado.");
            }
            else
            {
                vm.SetNeutralResult(
                    "Download concluído — instalador salvo: Madium_Installer.exe (execute quando quiser).",
                    AppPaths.Temp, tempFile);
            }
        }

        // ------------------------------------------------------------------ Madium — Arrumar problema
        public static async Task RepairMadiumAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            vm.SetBusy("Analisando instalações antigas do Madium…");

            // 1) Coleta do que existe — nada é removido antes da confirmação do usuário.
            var folders = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Madium"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "com.madium.app"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "com.madium.executor")
            };
            var folderHits = folders.Where(f => Directory.Exists(f)).ToList();

            var firstNames = new[] { "Madium", "Madium-launcher", "MadiumLauncher", "MadiumInstaller", "Loader" };
            var processes = ProcessRunner.FindActiveProcesses(firstNames);

            var shortcutRoots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms))
            };
            var shortcuts = new List<string>();
            foreach (var root in shortcutRoots.Distinct())
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                try
                {
                    shortcuts.AddRange(Directory.EnumerateFiles(
                        root, "*Madium*.lnk", SearchOption.AllDirectories));
                }
                catch { }
            }

            var items = new List<string>();
            foreach (var f in folderHits) items.Add("Pasta: " + f);
            foreach (var p in processes) items.Add("Processo: " + p);
            foreach (var s in shortcuts.Take(20)) items.Add("Atalho: " + s);
            if (shortcuts.Count > 20) items.Add("… e mais " + (shortcuts.Count - 20) + " atalho(s)");

            string note;
            if (items.Count == 0)
            {
                note = "Nenhuma instalação antiga do Madium foi encontrada neste computador.\n" +
                       "O processo seguirá direto para o download do instalador novo.";
                var cont = await shell.ConfirmAsync(
                    "Arrumar problemas do Madium", note, "Continuar", "Cancelar", false);
                if (!cont)
                {
                    vm.SetNeutralResult("Operação cancelada — nenhum arquivo foi tocado.");
                    return;
                }
            }
            else
            {
                var listed = items.Take(24).ToList();
                if (items.Count > 24) listed.Add("… e mais " + (items.Count - 24) + " item(ns)");
                var ok = await shell.ConfirmItemsAsync(
                    "Remover instalação antiga do Madium",
                    "Os itens abaixo serão removidos permanentemente para reinstalar limpo o Madium.\n" +
                    "Confira a lista antes de confirmar." + (processes.Count > 0
                        ? "\n\nProcessos em execução serão encerrados automaticamente."
                        : ""),
                    listed,
                    "Remover e reinstalar", "Cancelar", true);

                if (!ok)
                {
                    vm.SetNeutralResult("Cancelado — nenhum arquivo foi removido.");
                    return;
                }

                // 2) Encerramento dos processos
                vm.SetStatus("Encerrando processos do Madium…");
                ProcessRunner.KillProcesses(firstNames);
                await Task.Delay(600, ct);

                // 3) Pastas antigas
                foreach (var folder in folderHits)
                {
                    vm.SetStatus("Removendo: " + folder + "…");
                    try { Directory.Delete(folder, true); }
                    catch (Exception ex)
                    {
                        ActivityService.Add(ActivityKind.Warn,
                            "Pasta protegida (mantida): " + folder + " — " + UiHelper.FriendlyError(ex));
                    }
                }

                // 4) Atalhos
                if (shortcuts.Count > 0)
                {
                    vm.SetStatus("Removendo atalhos antigos…");
                    foreach (var s in shortcuts)
                    {
                        try { File.Delete(s); } catch { }
                    }
                }

                ActivityService.Add(ActivityKind.Info,
                    "Instalação antiga do Madium removida: " + folderHits.Count + " pasta(s), " + shortcuts.Count + " atalho(s).");
            }

            // 5) Baixa do instalador para a Área de Trabalho (igual ao original)
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrWhiteSpace(desktop))
                desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            AppPaths.TryCreate(desktop);

            var installer = Path.Combine(desktop, "Installer-1.1.0.exe");
            vm.SetStatus("Baixando o instalador do Madium (Installer-1.1.0.exe)…");
            await DownloadService.DownloadAsync(MadiumUrl, installer,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            // 6) Dependências VC++ (que o instalador do Madium precisa)
            vm.SetBusy("Verificando dependências (Visual C++)…");
            if (!RuntimeChecks.HasVcRuntime("x64"))
            {
                var okVc64 = await shell.ConfirmAsync(
                    "Necessário: Visual C++ x64",
                    "O Visual C++ x64 está ausente e é requisito do Madium.\nBaixar e instalar agora?", "Instalar", "Mais tarde", false);
                if (okVc64) await InstallerOps.InstallVcAsync(vm, shell, ct, "x64");
            }
            if (!RuntimeChecks.HasVcRuntime("x86"))
            {
                var okVc86 = await shell.ConfirmAsync(
                    "Necessário: Visual C++ x86",
                    "O Visual C++ x86 está ausente e é requisito do Madium.\nBaixar e instalar agora?", "Instalar", "Mais tarde", false);
                if (okVc86) await InstallerOps.InstallVcAsync(vm, shell, ct, "x86");
            }

            // 7) Executa o instalador
            vm.SetBusy("Pronto para reinstalar.");
            var okRun = await shell.ConfirmAsync(
                "Executar instalador do Madium",
                "Instalador salvo na Área de Trabalho:\n" + installer +
                "\n\nExecutar agora e seguir as instruções na tela? (Use o instalador salvo para reinstalar ou atualizar; os avisos anteriores já contêm os detalhes.)",
                "Executar agora", "Mais tarde", false);

            if (okRun)
            {
                vm.SetStatus("Executando Installer-1.1.0.exe — conclua na janela do instalador…");
                try { await ProcessRunner.RunInstallerAsync(installer, "", false, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    vm.SetResult(false, "Não foi possível executar o instalador: " + UiHelper.FriendlyError(ex));
                    return;
                }
                vm.SetResult(true, "Madium reinstalado — instalador executado com sucesso.", desktop, installer);
            }
            else
            {
                vm.SetNeutralResult("Instalador salvo na Área de Trabalho: Installer-1.1.0.exe",
                    desktop, installer);
            }
        }

        // ------------------------------------------------------------------ Fishstrap
        public static async Task DownloadFishstrapAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            if (!RuntimeChecks.HasWindowsDesktopRuntime("6"))
            {
                var okDep = await shell.ConfirmAsync(
                    "Fishstrap precisa do .NET 6",
                    "O Fishstrap requer o .NET Desktop Runtime 6, que não está instalado neste Windows.\n" +
                    "Deseja baixar e instalar o runtime oficial da Microsoft agora?",
                    "Instalar .NET 6", "Mais tarde", false);
                if (okDep)
                {
                    var done = await InstallerOps.InstallDotNetRuntimeAsync(vm, shell, ct, "6");
                    if (!done && !vm.IsSuccess)
                    {
                        vm.SetResult(false, "O Fishstrap precisa do .NET Desktop Runtime 6 para funcionar.");
                        return;
                    }
                }
            }

            vm.SetStatus("Consultando a release mais recente do Fishstrap no GitHub…");
            var found = await GitHubService.FindNamedAssetAsync(FishstrapRepo, "Fishstrap.exe", ct);
            if (found == null)
                throw new IOException("Fishstrap.exe não foi encontrado na release mais recente do GitHub.");

            var (release, asset) = found.Value;
            var dest = Path.Combine(Downloads, "Fishstrap.exe");
            vm.SetStatus("Baixando Fishstrap " + release.Tag + "…");
            await DownloadService.DownloadAsync(asset.DownloadUrl, dest,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            var ok = await shell.ConfirmAsync(
                "Executar o Fishstrap",
                "Download concluído: " + release.Tag + "\n\n" +
                "Arquivo: " + dest + "\n\nExecutar o Fishstrap agora?",
                "Executar agora", "Somente o download", false);

            if (ok)
            {
                vm.SetStatus("Executando Fishstrap…");
                ProcessRunner.Start(dest);
                vm.SetResult(true, "Fishstrap " + release.Tag + " baixado e iniciado.", Downloads, dest);
            }
            else
            {
                vm.SetNeutralResult("Fishstrap " + release.Tag + " baixado.", Downloads, dest);
            }
        }

        // ------------------------------------------------------------------ Roblox Account Manager
        public static async Task DownloadRamAsync(OpVM vm, IShell shell, CancellationToken ct)
        {
            vm.SetStatus("Consultando a release mais recente do Roblox Account Manager…");
            var found = await GitHubService.FindZipAssetAsync(RamRepo, ct);
            if (found == null)
                throw new IOException("Nenhum arquivo ZIP foi encontrado na release do Roblox-Account-Manager.");

            var (release, asset) = found.Value;
            var folder = Path.Combine(Downloads, "Roblox Account Manager");

            if (Directory.Exists(folder))
            {
                var ok = await shell.ConfirmItemsAsync(
                    "Substituir instalação antiga",
                    "A pasta abaixo já existe e será substituída pela versão nova extraída do ZIP.",
                    new List<string> { folder },
                    "Substituir", "Cancelar", true);
                if (!ok)
                {
                    vm.SetNeutralResult("Download cancelado — a pasta existente foi preservada.");
                    return;
                }
            }

            var zip = Path.Combine(AppPaths.Temp, "RobloxAccountManager.zip");
            vm.SetStatus("Baixando Roblox Account Manager " + release.Tag + "…");
            await DownloadService.DownloadAsync(asset.DownloadUrl, zip,
                new Progress<DownloadProgress>(vm.SetDownloadProgress), ct);

            if (Directory.Exists(folder))
            {
                vm.SetStatus("Removendo a pasta antiga…");
                try { Directory.Delete(folder, true); } catch { }
            }

            vm.SetStatus("Extraindo arquivos…");
            await ProcessRunner.ExtractZipAsync(zip, folder, ct);
            try { File.Delete(zip); } catch { }

            vm.SetResult(true, "Roblox Account Manager " + release.Tag + " instalado em Downloads.", folder, null);
        }
    }
}
