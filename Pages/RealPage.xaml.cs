using System.Windows;
using System.Windows.Controls;

namespace SkyLand.Pages
{
    /// <summary>Página Real: download direto da release do repositório.</summary>
    public partial class RealPage : UserControl
    {
        private readonly MainWindow _host;

        public RealPage(MainWindow host)
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
                "Baixar Real",
                "O SKY LAND vai:\n\n" +
                "✓ Verificar e instalar .NET 8.0 Desktop Runtime se necessário\n" +
                "✓ Verificar e instalar Microsoft Edge WebView2 se necessário\n" +
                "✓ Verificar e instalar Visual C++ x64 se necessário\n" +
                "✓ Verificar e instalar Visual C++ x86 se necessário\n" +
                "✓ Baixar RealSetup.exe do GitHub oficial\n" +
                "✓ Salvar na pasta de Downloads configurada\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.Real.RunAsync(
                _host, "Preparando download do Real…",
                ct => ExecutorOps.DownloadRealAsync(OpsStore.Real, _host, ct));
        }

        private void OnDependenciesClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.RealDeps.RunAsync(
                _host, "Verificando dependências…",
                ct => InstallerOps.InstallMadDependenciesAsync(OpsStore.RealDeps, _host, ct));
        }

        private void OnStatusClick(object sender, RoutedEventArgs e) => _host.NavigateTo("other");
    }
}
