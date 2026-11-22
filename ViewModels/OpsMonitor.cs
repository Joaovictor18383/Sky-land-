using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SkyLand
{
    /// <summary>
    /// Agregador de operações para o painel inferior da janela principal.
    /// Observa todos os OpVM registrados em OpsStore e expõe a operação
    /// mais relevante (a que está rodando ou o resultado mais recente).
    /// </summary>
    public sealed class OpsMonitor : INotifyPropertyChanged
    {
        private readonly List<OpVM> _ops;
        private readonly HashSet<OpVM> _dismissed = new();
        private readonly Dictionary<OpVM, long> _resultSeq = new();
        private long _seq;
        private OpVM _active;

        public OpsMonitor()
        {
            _ops = new List<OpVM>
            {
                OpsStore.Potassium, OpsStore.PotassiumDeps,
                OpsStore.MadiumInstall, OpsStore.MadiumRepair, OpsStore.MadiumDeps,
                OpsStore.Real, OpsStore.RealDeps,
                OpsStore.WebView2, OpsStore.AnyDesk, OpsStore.Fishstrap,
                OpsStore.Ram, OpsStore.MultiBloxy, OpsStore.UpdateDownload
            };

            foreach (var op in _ops)
                op.PropertyChanged += OnOpChanged;

            CancelCommand = new RelayCommand(_ => _active?.Cancel());
            OpenFolderCommand = new RelayCommand(_ => _active?.OpenFolderCommand?.Execute(null));
            DismissCommand = new RelayCommand(_ => Dismiss());
        }

        public ICommand CancelCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand DismissCommand { get; }

        private void OnOpChanged(object sender, PropertyChangedEventArgs e)
        {
            var op = sender as OpVM;
            if (op == null) return;

            void Refresh()
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                    dispatcher.BeginInvoke(new Action(RefreshCore));
                else
                    RefreshCore();
            }

            void RefreshCore()
            {
                // uma nova execução reabilita o painel mesmo se o resultado anterior foi dispensado
                if (e.PropertyName == nameof(OpVM.IsRunning) && op.IsRunning)
                    _dismissed.Remove(op);

                // registra a ordem real em que cada resultado apareceu
                if (e.PropertyName == nameof(OpVM.HasResult) && op.HasResult)
                    _resultSeq[op] = ++_seq;

                var running = _ops.FirstOrDefault(o => o.IsRunning);
                var result = running == null
                    ? _ops.Where(o => o.HasResult && !_dismissed.Contains(o))
                          .OrderByDescending(o => _resultSeq.TryGetValue(o, out var s) ? s : 0)
                          .FirstOrDefault()
                    : null;
                var next = running ?? result;

                if (ReferenceEquals(_active, next) && next != null)
                {
                    // mesmo alvo: ainda assim propaga atualizações de status/progresso
                    if (ReferenceEquals(op, next)) NotifyAll();
                    return;
                }
                _active = next;
                NotifyAll();
            }

            Refresh();
        }

        private void Dismiss()
        {
            if (_active != null && _active.HasResult)
                _dismissed.Add(_active);
            _active = null;
            NotifyAll();
        }

        // ==================== propriedades expostas ao painel ====================

        private T Get<T>(Func<OpVM, T> f, T fallback = default)
            => _active != null ? f(_active) : fallback;

        public bool IsVisible => _active != null;
        public bool IsRunning => Get(o => o.IsRunning);
        public bool IsIndeterminate => Get(o => o.IsIndeterminate, true);
        public bool HasResult => Get(o => o.HasResult);
        public bool IsSuccess => Get(o => o.IsSuccess);
        public bool IsWarning => Get(o => o.IsWarning);
        public bool IsError => Get(o => o.HasResult && !o.IsSuccess && !o.IsWarning);
        public bool HasTarget => Get(o => o.HasTarget);
        public bool ShowCancel => Get(o => o.CancelVisible);

        /// <summary>Mensagem principal: status durante a execução, resultado ao final.</summary>
        public string HeadlineText
        {
            get
            {
                if (_active == null) return "";
                return _active.HasResult && !_active.IsRunning ? _active.ResultText : _active.StatusText;
            }
        }

        /// <summary>Linha de detalhe: arquivo/tamanho durante download; dica de pasta no resultado.</summary>
        public string DetailText
        {
            get
            {
                if (_active == null) return "";
                if (_active.IsRunning)
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(_active.FileName)) parts.Add(_active.FileName);
                    if (!string.IsNullOrWhiteSpace(_active.SizeText) && _active.SizeText != "—")
                        parts.Add(_active.SizeText);
                    return string.Join("   ·   ", parts);
                }
                if (_active.HasResult && _active.HasTarget)
                    return "Toque em “Abrir pasta” para localizar o arquivo.";
                return "";
            }
        }
        public string TitleText => Get(o => o.Title, "Operação");
        public string StatusText => Get(o => o.StatusText, "");
        public string ResultText => Get(o => o.ResultText, "");
        public string FileName => Get(o => o.FileName, "");
        public string PercentText => Get(o => o.PercentText, "");
        public string SpeedText => Get(o => o.SpeedText, "—");
        public string SizeText => Get(o => o.SizeText, "—");
        public double Progress => Get(o => o.Progress);

        private void NotifyAll()
        {
            foreach (var name in new[]
            {
                nameof(IsVisible), nameof(IsRunning), nameof(IsIndeterminate),
                nameof(HasResult), nameof(IsSuccess), nameof(IsWarning), nameof(IsError),
                nameof(HasTarget), nameof(ShowCancel), nameof(TitleText),
                nameof(StatusText), nameof(ResultText), nameof(FileName),
                nameof(PercentText), nameof(SpeedText), nameof(SizeText),
                nameof(Progress), nameof(HeadlineText), nameof(DetailText)
            })
                Raise(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
