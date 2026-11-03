using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkyLand.Pages
{
    /// <summary>Row visual do status de um executor.</summary>
    public class WeaoRow
    {
        public string Name { get; set; }
        public string Detail { get; set; }
        public string State { get; set; }
        public Brush Dot { get; set; }
        public Brush ChipBackground { get; set; }
        public Brush ChipForeground { get; set; }
    }

    /// <summary>Página Outros: status WEAO, AnyDesk e ferramentas.</summary>
    public partial class OtherPage : UserControl
    {
        private readonly MainWindow _host;
        private readonly ObservableCollection<WeaoRow> _rows = new();

        public OtherPage(MainWindow host)
        {
            InitializeComponent();
            _host = host ?? throw new ArgumentNullException(nameof(host));

            StatusList.ItemsSource = _rows;

            // Estado inicial
            if (StatusCache.IsFresh) ApplyRows();
            else EnsurePlaceholder();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!StatusCache.IsFresh)
                await CheckAsync(force: false);
        }

        private async Task CheckAsync(bool force = false)
        {
            try
            {
                if (force || !StatusCache.IsFresh)
                {
                    var items = await WeaoService.GetExploitsAsync();
                    var list = new List<ExecutorStatus> {
                        WeaoService.ToStatus(WeaoService.Find(items, "Potassium"), "Potassium"),
                        WeaoService.ToStatus(WeaoService.Find(items, "Madium"), "Madium"),
                        WeaoService.ToStatus(WeaoService.Find(items, "Real"), "Real"),
                    };
                    StatusCache.Store(list);
                }
                ApplyRows();
            }
            catch (Exception ex)
            {
                var msg = UiHelper.FriendlyError(ex);
                BuildErrorRows(msg);
                ActivityService.Add(ActivityKind.Error, "Status WEAO — " + msg);
            }
        }

        private void EnsurePlaceholder()
        {
            _rows.Clear();
            foreach (var name in new[] { "Potassium", "Madium", "Real" })
            {
                _rows.Add(new WeaoRow
                {
                    Name = name,
                    Detail = "Toque em 'Verificar agora' para consultar.",
                    State = "—",
                    Dot = TryBrush("Brush.Text.Dim") ?? System.Windows.Media.Brushes.Gray,
                    ChipBackground = TryBrush("Brush.Surface") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Text.Secondary") ?? System.Windows.Media.Brushes.Gray
                });
            }
        }

        private void ApplyRows()
        {
            _rows.Clear();
            foreach (var name in new[] { "Potassium", "Madium", "Real" })
            {
                _rows.Add(BuildRow(name));
            }
        }

        private WeaoRow BuildRow(string name)
        {
            var st = StatusCache.Get(name);

            if (st == null)
                return new WeaoRow
                {
                    Name = name,
                    Detail = "Sem dados disponíveis.",
                    State = "—",
                    Dot = TryBrush("Brush.Text.Dim") ?? System.Windows.Media.Brushes.Gray,
                    ChipBackground = TryBrush("Brush.Surface") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Text.Secondary") ?? System.Windows.Media.Brushes.Gray
                };

            if (!string.IsNullOrEmpty(st.Error))
                return new WeaoRow
                {
                    Name = name,
                    Detail = st.Error,
                    State = "Erro de conexão",
                    Dot = TryBrush("Brush.Danger") ?? System.Windows.Media.Brushes.Red,
                    ChipBackground = TryBrush("Brush.Danger.Soft") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Danger.Text") ?? System.Windows.Media.Brushes.Red
                };

            if (!st.Found)
                return new WeaoRow
                {
                    Name = name,
                    Detail = "Não localizado na resposta da API.",
                    State = "Não encontrado",
                    Dot = TryBrush("Brush.Text.Dim") ?? System.Windows.Media.Brushes.Gray,
                    ChipBackground = TryBrush("Brush.Surface") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Text.Secondary") ?? System.Windows.Media.Brushes.Gray
                };

            var date = string.IsNullOrWhiteSpace(st.UpdatedDate) ? "" : "   •   Atualizado em " + st.UpdatedDate;
            if (st.Online)
            {
                return new WeaoRow
                {
                    Name = name,
                    Detail = "Versão encontrada: " + (string.IsNullOrEmpty(st.Version) ? "—" : st.Version) + date,
                    State = "Online",
                    Dot = TryBrush("Brush.Success") ?? System.Windows.Media.Brushes.Green,
                    ChipBackground = TryBrush("Brush.Success.Soft") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Success.Text") ?? System.Windows.Media.Brushes.Green
                };
            }

            return new WeaoRow
            {
                Name = name,
                Detail = "Versão encontrada: " + (string.IsNullOrEmpty(st.Version) ? "—" : st.Version) + date,
                State = "Desatualizado",
                Dot = TryBrush("Brush.Warning") ?? System.Windows.Media.Brushes.Orange,
                ChipBackground = TryBrush("Brush.Warning.Soft") ?? System.Windows.Media.Brushes.Transparent,
                ChipForeground = TryBrush("Brush.Warning.Text") ?? System.Windows.Media.Brushes.Orange
            };
        }

        private void BuildErrorRows(string msg)
        {
            _rows.Clear();
            foreach (var name in new[] { "Potassium", "Madium", "Real" })
            {
                _rows.Add(new WeaoRow
                {
                    Name = name,
                    Detail = "Verifique sua internet e tente novamente — " + msg,
                    State = "Erro de conexão",
                    Dot = TryBrush("Brush.Danger") ?? System.Windows.Media.Brushes.Red,
                    ChipBackground = TryBrush("Brush.Danger.Soft") ?? System.Windows.Media.Brushes.Transparent,
                    ChipForeground = TryBrush("Brush.Danger.Text") ?? System.Windows.Media.Brushes.Red
                });
            }
        }

        private async void OnCheckClick(object sender, RoutedEventArgs e)
        {
            await CheckAsync(force: true);
            ActivityService.Add(ActivityKind.Info, "Status dos executores consultado na WEAO.");
        }

        private void OnOpenWeaoClick(object sender, RoutedEventArgs e)
        {
            ProcessRunner.OpenUrl("https://weao.xyz/status");
        }

        private async void OnAnyDeskClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar AnyDesk",
                "O SKY LAND vai:\n\n" +
                "✓ Baixar AnyDesk.exe do site oficial\n" +
                "✓ Salvar na pasta de Downloads configurada\n" +
                "✓ Encerrar processos AnyDesk em execução antes do download\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.AnyDesk.RunAsync(
                _host, "Preparando download do AnyDesk…",
                ct => ExecutorOps.DownloadAnyDeskAsync(OpsStore.AnyDesk, _host, ct));
        }

        private async void OnFishstrapClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar Fishstrap",
                "O SKY LAND vai:\n\n" +
                "✓ Consultar a release mais recente no GitHub\n" +
                "✓ Baixar Fishstrap.exe (versão mais recente)\n" +
                "✓ Verificar e instalar .NET 6 Desktop Runtime se necessário\n" +
                "✓ Salvar na pasta de Downloads configurada\n" +
                "✓ Executar o Fishstrap automaticamente após o download\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.Fishstrap.RunAsync(
                _host, "Preparando download do Fishstrap…",
                ct => ExecutorOps.DownloadFishstrapAsync(OpsStore.Fishstrap, _host, ct));
        }

        private async void OnRamClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar Roblox Account Manager",
                "O SKY LAND vai:\n\n" +
                "✓ Consultar a release mais recente no GitHub\n" +
                "✓ Baixar o arquivo ZIP mais recente\n" +
                "✓ Extrair para a pasta 'Roblox Account Manager' em Downloads\n" +
                "✓ Substituir instalação antiga se existir\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", true);

            if (!confirm) return;

            var _ = OpsStore.Ram.RunAsync(
                _host, "Preparando download do Roblox Account Manager…",
                ct => ExecutorOps.DownloadRamAsync(OpsStore.Ram, _host, ct));
        }

        private async void OnMultiBloxyClick(object sender, RoutedEventArgs e)
        {
            var confirm = await _host.ConfirmAsync(
                "Baixar MultiBloxy",
                "O SKY LAND vai:\n\n" +
                "✓ Baixar MultiBloxy.exe do GitHub oficial\n" +
                "✓ Salvar na pasta de Downloads configurada\n" +
                "✓ Encerrar processos MultiBloxy em execução antes do download\n\n" +
                "Deseja continuar?",
                "Baixar", "Cancelar", false);

            if (!confirm) return;

            var _ = OpsStore.MultiBloxy.RunAsync(
                _host, "Preparando download do MultiBloxy…",
                ct => ExecutorOps.DownloadMultiBloxyAsync(OpsStore.MultiBloxy, _host, ct));
        }

        private Brush TryBrush(string key)
        {
            try { return Application.Current.TryFindResource(key) as Brush; }
            catch { return null; }
        }
    }
}
