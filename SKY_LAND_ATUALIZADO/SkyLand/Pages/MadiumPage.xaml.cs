using System.Windows;
using System.Windows.Controls;

namespace SkyLand.Pages
{
    /// <summary>Página Madium: download/atualização, reparo e dependências.</summary>
    public partial class MadiumPage : UserControl
    {
        private readonly MainWindow _host;

        public MadiumPage(MainWindow host)
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
            var _ = OpsStore.MadiumInstall.RunAsync(
                _host, "Preparando instalador do Madium…",
                ct => ExecutorOps.DownloadMadiumAsync(OpsStore.MadiumInstall, _host, ct));
        }

        private void OnRepairClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.MadiumRepair.RunAsync(
                _host, "Analisando instalação do Madium…",
                ct => ExecutorOps.RepairMadiumAsync(OpsStore.MadiumRepair, _host, ct));
        }

        private void OnDependenciesClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.MadiumDeps.RunAsync(
                _host, "Verificando dependências…",
                ct => InstallerOps.InstallMadDependenciesAsync(OpsStore.MadiumDeps, _host, ct));
        }

        private void OnStatusClick(object sender, RoutedEventArgs e) => _host.NavigateTo("other");
    }
}
