using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SkyLand
{
    public partial class App : Application
    {
        internal static string VersionText => "1.0.2";

        internal static string VersionDisplay => "1.0.2";

        /// <summary>Canal de automação de QA (ativo somente com o argumento --uicmd).</summary>
        internal static bool UiCmdMode { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplyTheme(SettingsStore.Current.Theme);
            AppPaths.EnsureFolders();

            if (e.Args != null && e.Args.Contains("--selftest"))
            {
                var ignored = SelfTest.RunAsync();
                return;
            }

            UiCmdMode = e.Args != null &&
                Array.Exists(e.Args, a => a.Equals("--uicmd", StringComparison.OrdinalIgnoreCase));

            // Verificar dependências
            var missingDeps = new List<string>();

            if (!NetInstaller.IsNet8Installed())
                missingDeps.Add(".NET 8.0 Desktop Runtime");

            if (!NetInstaller.IsWebView2Installed())
                missingDeps.Add("Microsoft Edge WebView2");

            if (!NetInstaller.IsVcInstalled("x64"))
                missingDeps.Add("Visual C++ x64");

            if (!NetInstaller.IsVcInstalled("x86"))
                missingDeps.Add("Visual C++ x86");

            if (missingDeps.Count > 0)
            {
                var depsText = string.Join("\n- ", missingDeps);
                var result = MessageBox.Show(
                    "O SKY LAND requer as seguintes dependências:\n\n- " + depsText + "\n\n" +
                    "Deseja baixar e instalar agora?\n\n" +
                    "A instalação será automática.",
                    "SKY LAND - Dependências Necessárias",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    var splash = new Window
                    {
                        Title = "SKY LAND - Instalando Dependências",
                        Width = 500,
                        Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = System.Windows.Media.Brushes.Black
                    };

                    var text = new System.Windows.Controls.TextBlock
                    {
                        Text = "Preparando instalação das dependências...",
                        Foreground = System.Windows.Media.Brushes.White,
                        FontSize = 14,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        TextWrapping = System.Windows.TextWrapping.Wrap
                    };

                    splash.Content = text;
                    splash.Show();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Instalar .NET 8
                            if (!NetInstaller.IsNet8Installed())
                            {
                                await NetInstaller.InstallNet8Async(msg =>
                                {
                                    splash.Dispatcher.Invoke(() => text.Text = msg);
                                }, CancellationToken.None);
                            }

                            // Instalar WebView2
                            if (!NetInstaller.IsWebView2Installed())
                            {
                                await NetInstaller.InstallWebView2Async(msg =>
                                {
                                    splash.Dispatcher.Invoke(() => text.Text = msg);
                                }, CancellationToken.None);
                            }

                            // Instalar VC++ x64
                            if (!NetInstaller.IsVcInstalled("x64"))
                            {
                                await NetInstaller.InstallVcAsync("x64", msg =>
                                {
                                    splash.Dispatcher.Invoke(() => text.Text = msg);
                                }, CancellationToken.None);
                            }

                            // Instalar VC++ x86
                            if (!NetInstaller.IsVcInstalled("x86"))
                            {
                                await NetInstaller.InstallVcAsync("x86", msg =>
                                {
                                    splash.Dispatcher.Invoke(() => text.Text = msg);
                                }, CancellationToken.None);
                            }

                            await splash.Dispatcher.InvokeAsync(() =>
                            {
                                splash.Close();
                                StartMainWindow(e);
                            });
                        }
                        catch (Exception ex)
                        {
                            splash.Dispatcher.Invoke(() =>
                            {
                                splash.Close();
                                MessageBox.Show(
                                    "Falha ao instalar dependências: " + ex.Message + "\n\n" +
                                    "Por favor, instale manualmente:\n" +
                                    "- .NET 8.0: https://dotnet.microsoft.com/download/dotnet/8.0\n" +
                                    "- WebView2: https://go.microsoft.com/fwlink/?linkid=2124701\n" +
                                    "- Visual C++: https://aka.ms/vs/17/release/vc_redist.x64.exe",
                                    "Erro de Instalação",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                Current.Shutdown(1);
                            });
                        }
                    });

                    return;
                }
                else
                {
                    MessageBox.Show(
                        "O SKY LAND não pode funcionar sem as dependências.\n\n" +
                        "Baixe em:\n" +
                        "- .NET 8.0: https://dotnet.microsoft.com/download/dotnet/8.0\n" +
                        "- WebView2: https://go.microsoft.com/fwlink/?linkid=2124701\n" +
                        "- Visual C++: https://aka.ms/vs/17/release/vc_redist.x64.exe",
                        "SKY LAND",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Current.Shutdown(1);
                    return;
                }
            }

            StartMainWindow(e);
        }

        private void StartMainWindow(StartupEventArgs e)
        {
            var win = new MainWindow();
            MainWindowRef = win;
            win.Show();
            win.StartUiHarness();

            // Auto-update automático ao iniciar (se ativado)
            if (SettingsStore.Current.AutoUpdateCheck)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000); // Espera 3 segundos para não travar a inicialização
                    try
                    {
                        var info = await UpdateService.CheckAsync(CancellationToken.None);
                        if (info.CheckedOk && info.HasUpdate)
                        {
                            await win.Dispatcher.InvokeAsync(() =>
                            {
                                win.Toast("Atualização disponível",
                                    "Nova versão v" + info.LatestVersion + " disponível no GitHub.\nVá em Configurações para atualizar.",
                                    ToastKind.Info);
                            });
                        }
                    }
                    catch { }
                });
            }
        }

        internal static MainWindow MainWindowRef { get; set; }

        public static void ApplyTheme(string themeName)
        {
            var file = themeName == "Black" ? "Theme.Black.xaml" : "Theme.DeepBlue.xaml";
            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Themes/" + file, UriKind.RelativeOrAbsolute)
            };
            var app = Current;
            if (app != null && app.Resources.MergedDictionaries.Count > 0)
            {
                app.Resources.MergedDictionaries[0] = dict;
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                File.AppendAllText(AppPaths.LogFile,
                    Environment.NewLine + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + e.Exception);
            }
            catch { }

            try
            {
                MessageBox.Show(
                    "Ocorreu um erro inesperado no SKY LAND.\n\n" + e.Exception.Message,
                    "SKY LAND", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }

            e.Handled = true;
        }
    }
}
