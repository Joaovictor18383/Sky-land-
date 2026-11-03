using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyLand.Pages
{
    /// <summary>Painel inicial: identidade, status, atalhos e atividades recentes.</summary>
    public partial class DashboardPage : UserControl
    {
        private readonly MainWindow _host;

        public DashboardPage(MainWindow host)
        {
            InitializeComponent();
            _host = host ?? throw new ArgumentNullException(nameof(host));

            TilePotassium.Clicked += () => _host.NavigateTo("potassium");
            TileMadium.Clicked += () => _host.NavigateTo("madium");
            TileReal.Clicked += () => _host.NavigateTo("real");
            TileStatus.Clicked += () => _host.NavigateTo("other");
            TileAnyDesk.Clicked += () => _host.NavigateTo("other");
            TileUpdates.Clicked += () => _host.NavigateTo("settings");

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Carregamento da página - sem status chips
        }
    }
}
