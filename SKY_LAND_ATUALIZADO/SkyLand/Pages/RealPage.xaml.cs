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

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            var _ = OpsStore.Real.RunAsync(
                _host, "Preparando download do Real…",
                ct => ExecutorOps.DownloadRealAsync(OpsStore.Real, _host, ct));
        }

        private void OnStatusClick(object sender, RoutedEventArgs e) => _host.NavigateTo("other");
    }
}
