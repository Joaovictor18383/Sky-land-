using System.Windows;
using System.Windows.Controls;

namespace SkyLand.Pages
{
    /// <summary>Página Potassium: download + WebView2 + ferramentas.</summary>
    public partial class PotassiumPage : UserControl
    {
        private readonly MainWindow _host;

        public PotassiumPage(MainWindow host)
        {
            InitializeComponent();
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private void RunTool(OpVM vm, Func<OpVM, IShell, CancellationToken, Task> work)
        {
            var _ = vm.RunAsync(_host, "Iniciando " + vm.Title + "…", ct => work(vm, _host, ct));
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.Potassium.RunAsync(
                _host, "Preparando download do Potassium…",
                ct => ExecutorOps.DownloadPotassiumAsync(OpsStore.Potassium, _host, ct));
        }

        private void OnWebView2Click(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.WebView2.RunAsync(
                _host, "Verificando WebView2…",
                ct => InstallerOps.InstallWebView2Async(OpsStore.WebView2, _host, ct));
        }

        private void OnCopyLinkClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ExecutorOps.PotassiumUrl);
                _host.Toast("Link copiado", ExecutorOps.PotassiumUrl, ToastKind.Info);
            }
            catch
            {
                _host.Toast("Aviso", "Não foi possível copiar o link.", ToastKind.Warn);
            }
        }
    }
}
