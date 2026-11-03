using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkyLand.Pages
{
    /// <summary>Página de configurações: tema, pasta de downloads, atualizações e manutenção.</summary>
    public partial class SettingsPage : UserControl
    {
        private readonly MainWindow _host;
        private AppUpdateInfo _lastCheck;
        private string _downloadedUpdatePath;

        public SettingsPage(MainWindow host)
        {
            InitializeComponent();
            _host = host ?? throw new ArgumentNullException(nameof(host));

            VersionText.Text = "SKY LAND v" + App.VersionText + "  —  Windows x64  —  sem servidor externo: só o GitHub e a WEAO";
            SyncThemeMarks();
            SyncFolderBox();
            SwAutoUpdate.IsChecked = SettingsStore.Current.AutoUpdateCheck;
        }

        // ==================== TEMA ====================

        private void SyncThemeMarks()
        {
            // Carrega o tema salvo
            LoadTheme(SettingsStore.Current.Theme ?? "Black");
        }

        private void LoadTheme(string themeName)
        {
            var app = Application.Current as App;
            if (app == null) return;

            // Remove o tema atual
            var oldTheme = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
            if (oldTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(oldTheme);
            }

            // Carrega o novo tema
            var themeUri = new Uri($"/Themes/Theme.{themeName}.xaml", UriKind.Relative);
            var newTheme = new ResourceDictionary { Source = themeUri };
            app.Resources.MergedDictionaries.Add(newTheme);

            // Salva a preferência
            SettingsStore.Current.Theme = themeName;
            SettingsStore.Save();
        }

        private void OnThemeBlackClick(object sender, RoutedEventArgs e) => LoadTheme("Black");
        private void OnThemeBlueClick(object sender, RoutedEventArgs e) => LoadTheme("Blue");
        private void OnThemeGreenClick(object sender, RoutedEventArgs e) => LoadTheme("Green");
        private void OnThemeRedClick(object sender, RoutedEventArgs e) => LoadTheme("Red");
        private void OnThemePurpleClick(object sender, RoutedEventArgs e) => LoadTheme("Black"); // Purple é o tema Black com accent roxo

        // ==================== PASTA DE DOWNLOADS ====================

        private void SyncFolderBox()
        {
            FolderBox.Text = string.IsNullOrWhiteSpace(SettingsStore.Current.DownloadsDir)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads  (padrão)"
                : SettingsStore.Current.DownloadsDir;
        }

        private void OnFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFolderDialog
                {
                    Title = "Escolher pasta padrão de downloads",
                    InitialDirectory = SettingsStore.GetDownloadsPath()
                };
                if (dlg.ShowDialog() == true)
                {
                    SettingsStore.Current.DownloadsDir = dlg.FolderName;
                    SettingsStore.Save();
                    SyncFolderBox();
                    ActivityService.Add(ActivityKind.Info, "Pasta de downloads alterada: " + dlg.FolderName);
                    _host.Toast("Pasta atualizada", "Os próximos downloads vão para:\n" + dlg.FolderName, ToastKind.Success);
                }
            }
            catch (Exception ex)
            {
                _host.Toast("Aviso", "Não foi possível escolher a pasta: " + UiHelper.FriendlyError(ex), ToastKind.Warn);
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            ProcessRunner.OpenFolder(SettingsStore.GetDownloadsPath());
        }

        private void OnAutoUpdateToggle(object sender, RoutedEventArgs e)
        {
            SettingsStore.Current.AutoUpdateCheck = SwAutoUpdate.IsChecked == true;
            SettingsStore.Save();
        }

        // ==================== ATUALIZAÇÕES ====================

        private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnCheckUpdates.IsEnabled = false;
                _downloadedUpdatePath = null;
                ClearUpdateActions();
                HistoryEmpty.Text = "Consultando o repositório oficial…";
                HistoryEmpty.Visibility = Visibility.Visible;
                HistoryList.ItemsSource = null;

                var info = await UpdateService.CheckAsync(CancellationToken.None);
                _lastCheck = info;

                ActivityService.Add(ActivityKind.Info,
                    info.CheckedOk
                        ? "Verificação de atualização concluída."
                        : "Falha ao verificar atualizações no GitHub.");

                if (!info.CheckedOk)
                {
                    SetBanner(Kind.Danger, "Falha ao consultar o GitHub: " + (info.Error ?? "erro desconhecido."));
                    HistoryEmpty.Text = "Não foi possível carregar o histórico agora.";
                    return;
                }

                // Histórico de releases
                var rows = new ObservableCollection<object>();
                foreach (var r in info.AllReleases)
                {
                    var assets = r.Assets.Count == 1 ? r.Assets[0].Name : (r.Assets.Count + " arquivo(s)");
                    rows.Add(new ReleaseRow(
                        r.Tag + " — " + r.Name + "   •   " + assets,
                        r.PublishedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm") + (r.Prerelease ? "  (preview)" : "")));
                }
                HistoryList.ItemsSource = rows;
                HistoryEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                var hasAppRelease = _lastCheck.Asset != null;
                var isNewer = hasAppRelease &&
                              UpdateService.CompareVersion(_lastCheck.LatestVersion, App.VersionText) > 0;

                if (!hasAppRelease)
                {
                    SetBanner(Kind.Info,
                        "Nenhum setup do SKY LAND foi publicado na página Releases ainda — você está na build mais recente." +
                        (info.AllReleases.Count > 0
                            ? "\nReleases atuais do repositório: " + info.AllReleases.Count + "."
                            : ""));
                }
                else if (isNewer)
                {
                    SetBanner(Kind.Success,
                        "Nova versão disponível: v" + _lastCheck.LatestVersion +
                        " (" + _lastCheck.LatestTag + ")\nArquivo publicado: " + _lastCheck.Asset.Name);
                    BtnDownloadUpdate.Visibility = Visibility.Visible;
                }
                else
                {
                    var note = "Você já está na versão mais recente (v" + App.VersionText + ").";
                    if (hasAppRelease) note += "\nRelease publicada no GitHub: " + _lastCheck.LatestTag + ".";
                    SetBanner(Kind.Info, note);
                }
            }
            catch (Exception ex)
            {
                SetBanner(Kind.Danger, "Falha ao consultar o GitHub: " + UiHelper.FriendlyError(ex));
            }
            finally
            {
                BtnCheckUpdates.IsEnabled = true;
            }
        }

        private async void OnDownloadUpdateClick(object sender, RoutedEventArgs e)
        {
            if (_lastCheck?.Asset == null) return;
            if (OpsStore.UpdateDownload.IsRunning)
            {
                _host.Toast("Aguarde", "O download da atualização já está acontecendo.", ToastKind.Warn);
                return;
            }

            BtnDownloadUpdate.IsEnabled = false;
            try
            {
                var vm = OpsStore.UpdateDownload;
                SetBanner(Kind.Info, "Baixando atualização do SKY LAND…");

                await vm.RunAsync(
                    _host,
                    "Baixando atualização v" + _lastCheck.LatestVersion + "…",
                    async ct =>
                    {
                        try
                        {
                            var path = await UpdateService.DownloadUpdateAsync(
                                _lastCheck,
                                AppPaths.Updates,
                                new Progress<DownloadProgress>(vm.SetDownloadProgress),
                                ct);

                            _downloadedUpdatePath = path;
                            App.MainWindowRef?.Dispatcher.Invoke(() =>
                            {
                                SetBanner(Kind.Success,
                                    "Atualização baixada e verificada:\n" + path +
                                    "\nConfirme a execução quando quiser reinstalar/instalar a nova versão.");
                                BtnRunUpdate.Visibility = Visibility.Visible;
                            });
                            ActivityService.Add(ActivityKind.Success, "Atualização baixada: " + Path.GetFileName(path));
                        }
                        catch (Exception)
                        {
                            App.MainWindowRef?.Dispatcher.Invoke(() =>
                                SetBanner(Kind.Danger,
                                    "Falha ao baixar ou validar a atualização — veja as atividades recentes."));
                            BtnRunUpdate.Visibility = Visibility.Collapsed;
                            throw;
                        }
                    });
            }
            finally
            {
                BtnDownloadUpdate.IsEnabled = true;
            }
        }

        private async void OnRunUpdateClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_downloadedUpdatePath) || !File.Exists(_downloadedUpdatePath))
            {
                _host.Toast("Aviso", "Baixe a atualização antes de executá-la.", ToastKind.Warn);
                return;
            }

            var ok = await _host.ConfirmAsync(
                "Executar atualização do SKY LAND",
                "O arquivo baixado será executado agora:\n" + _downloadedUpdatePath +
                "\n\nDependendo do formato, o próprio instalador encerrará esta versão. Deseja continuar?",
                "Executar agora", "Cancelar", false);

            if (!ok) return;

            try
            {
                string exePath = _downloadedUpdatePath;
                if (exePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extractDir = Path.Combine(AppPaths.Updates, "Extract");
                    exePath = UpdateService.ExtractUpdateZip(_downloadedUpdatePath, extractDir);
                }

                ProcessRunner.Start(exePath, true); // o instalador pode pedir UAC naturalmente

                // Encerra o SKY LAND atual para a versão nova assumir.
                await Task.Delay(1200);

                var _ = Task.Run(() =>
                {
                    Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown(0));
                });
            }
            catch (Exception ex)
            {
                _host.Toast("Falha ao executar", UiHelper.FriendlyError(ex), ToastKind.Error);
            }
        }

        private void ClearUpdateActions()
        {
            BtnDownloadUpdate.Visibility = Visibility.Collapsed;
            BtnRunUpdate.Visibility = Visibility.Collapsed;
        }

        private enum Kind { Info, Success, Danger }

        private void SetBanner(Kind kind, string text)
        {
            UpdateBanner.Background = kind switch
            {
                Kind.Success => TryBrush("Brush.Success.Soft"),
                Kind.Danger => TryBrush("Brush.Danger.Soft") ?? TryBrush("Brush.Danger.Soft"),
                _ => TryBrush("Brush.Info.Soft")
            };
            UpdateBannerText.Foreground = kind switch
            {
                Kind.Success => TryBrush("Brush.Success.Text"),
                Kind.Danger => TryBrush("Brush.Danger.Text"),
                _ => TryBrush("Brush.Info.Text")
            };
            UpdateBannerText.Text = text;
            UpdateBanner.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==================== MANUTENÇÃO ====================

        private async void OnClearTempClick(object sender, RoutedEventArgs e)
        {
            var items = new List<string>();
            try
            {
                if (Directory.Exists(AppPaths.Updates))
                    foreach (var f in Directory.EnumerateFiles(AppPaths.Updates, "*", SearchOption.AllDirectories))
                        items.Add(f);
                if (Directory.Exists(AppPaths.Temp))
                    foreach (var f in Directory.EnumerateFiles(AppPaths.Temp, "*", SearchOption.AllDirectories))
                        items.Add(f);
            }
            catch { }

            if (items.Count == 0)
            {
                _host.Toast("Limpo", "Nenhum arquivo temporário para remover.", ToastKind.Info);
                return;
            }

            var preview = items.Take(10).ToList();
            if (items.Count > 10) preview.Add("… e mais " + (items.Count - 10) + " arquivo(s)");

            var ok = await _host.ConfirmItemsAsync(
                "Limpar arquivos temporários do aplicativo",
                "Estes arquivos serão removidos (atualizações antigas e instaladores temporários):",
                preview, "Limpar agora", "Cancelar", true);

            if (!ok) return;

            int removed = 0, failed = 0;
            try
            {
                if (Directory.Exists(AppPaths.Updates)) Directory.Delete(AppPaths.Updates, true);
                if (Directory.Exists(AppPaths.Temp)) Directory.Delete(AppPaths.Temp, true);
                removed = items.Count;
            }
            catch
            {
                failed = items.Count - removed;
            }

            _downloadedUpdatePath = null;
            ActivityService.Add(ActivityKind.Info,
                "Limpeza de temporários concluída (" + removed + " arquivo(s)" + (failed > 0 ? ", " + failed + " protegidos" : "") + ").");
            _host.Toast("Limpeza concluída",
                removed + " arquivo(s) temporário(s) foram removidos.", ToastKind.Success);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            ProcessRunner.OpenUrl("https://github.com/Joaovictor18383/Sky-land-");
        }

        private void OnClearActivityClick(object sender, RoutedEventArgs e)
        {
            ActivityService.Clear();
            _host.Toast("Registro limpo", "As atividades recentes foram apagadas.", ToastKind.Info);
        }

        // ==================== AUXILIARES ====================

        private Brush TryBrush(string key)
        {
            try { return Application.Current.TryFindResource(key) as Brush; }
            catch { return null; }
        }

        private sealed class ReleaseRow
        {
            public string Text { get; }
            public string DateText { get; }

            public ReleaseRow(string text, string dateText)
            {
                Text = text;
                DateText = dateText;
            }
        }
    }
}
