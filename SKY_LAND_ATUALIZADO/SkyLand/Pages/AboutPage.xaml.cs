using System.Windows;
using System.Windows.Controls;

namespace SkyLand.Pages
{
    /// <summary>Página Sobre: identidade, segurança, créditos.</summary>
    public partial class AboutPage : UserControl
    {
        private readonly MainWindow _host;

        public AboutPage(MainWindow host)
        {
            InitializeComponent();
            _host = host ?? throw new ArgumentNullException(nameof(host));

            BuildInfo.Text = "Build compilada em " + DateTime.Today.ToString("dd/MM/yyyy") +
                " • .NET Desktop Runtime 8 embutido (self-contained) • recomenda-se WebView2 atualizado para alguns instaladores.";
        }

        private async void OnCheckClick(object sender, RoutedEventArgs e)
        {
            var info = await UpdateService.CheckAsync(CancellationToken.None);
            if (!info.CheckedOk)
            {
                _host.Toast("Falha na verificação", info.Error ?? "Erro desconhecido.", ToastKind.Error);
                return;
            }
            if (info.HasUpdate)
                _host.Toast("Atualização disponível", "v" + info.LatestVersion + " publicada — veja em Configurações.", ToastKind.Success);
            else if (info.Asset == null)
                _host.Toast("Tudo certo", "Nenhuma release do aplicativo publicada no repositório — build em dia.", ToastKind.Info);
            else
                _host.Toast("Tudo certo", "Você já está na versão mais recente.", ToastKind.Info);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            ProcessRunner.OpenUrl("https://github.com/Joaovictor18383/Sky-land-");
        }
    }
}
