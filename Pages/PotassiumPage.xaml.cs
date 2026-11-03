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

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar Potassium",
                "O SKY LAND vai:\n\n" +
                "✓ Verificar e instalar .NET 8.0 Desktop Runtime se necessário\n" +
                "✓ Verificar e instalar Microsoft Edge WebView2 se necessário\n" +
                "✓ Verificar e instalar Visual C++ x64 se necessário\n" +
                "✓ Verificar e instalar Visual C++ x86 se necessário\n" +
                "✓ Baixar Potassium.zip do servidor oficial\n" +
                "✓ Salvar na pasta de Downloads configurada\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.Potassium.RunAsync(
                _host, "Preparando download do Potassium…",
                ct => ExecutorOps.DownloadPotassiumAsync(OpsStore.Potassium, _host, ct));
        }

        private void OnDependenciesClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.PotassiumDeps.RunAsync(
                _host, "Verificando dependências…",
                ct => InstallerOps.InstallMadDependenciesAsync(OpsStore.PotassiumDeps, _host, ct));
        }

        private void OnStatusClick(object sender, RoutedEventArgs e) => _host.NavigateTo("other");

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
