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

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar Madium",
                "O SKY LAND vai:\n\n" +
                "✓ Baixar o instalador do Madium do servidor oficial\n" +
                "✓ Executar o instalador automaticamente\n" +
                "✓ Você acompanhará a instalação na janela do Madium\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.MadiumInstall.RunAsync(
                _host, "Preparando instalador do Madium…",
                ct => ExecutorOps.DownloadMadiumAsync(OpsStore.MadiumInstall, _host, ct));
        }

        private async void OnRepairClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Reparar Madium",
                "O SKY LAND vai:\n\n" +
                "✓ Analisar instalações antigas do Madium\n" +
                "✓ Encerrar processos do Madium em execução\n" +
                "✓ Remover pastas e atalhos antigos\n" +
                "✓ Baixar o instalador novo para a Área de Trabalho\n" +
                "✓ Verificar e instalar dependências (Visual C++)\n" +
                "✓ Executar o instalador para reinstalar\n\n" +
                "Deseja continuar?",
                "Reparar", "Cancelar", true);

            if (!confirm) return;

            var _ = OpsStore.MadiumRepair.RunAsync(
                _host, "Analisando instalação do Madium…",
                ct => ExecutorOps.RepairMadiumAsync(OpsStore.MadiumRepair, _host, ct));
        }

        private async void OnDependenciesClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Instalar Dependências do Madium",
                "O SKY LAND vai:\n\n" +
                "✓ Verificar e baixar .NET 8.0 Desktop Runtime se necessário\n" +
                "✓ Verificar e baixar Visual C++ x64 se necessário\n" +
                "✓ Verificar e baixar Visual C++ x86 se necessário\n" +
                "✓ Verificar e baixar Microsoft Edge WebView2 se necessário\n" +
                "✓ Instalar automaticamente os componentes faltantes\n\n" +
                "Deseja continuar?",
                "Instalar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.MadiumDeps.RunAsync(
                _host, "Verificando dependências…",
                ct => InstallerOps.InstallMadDependenciesAsync(OpsStore.MadiumDeps, _host, ct));
        }

        private void OnStatusClick(object sender, RoutedEventArgs e) => _host.NavigateTo("other");
    }
}
