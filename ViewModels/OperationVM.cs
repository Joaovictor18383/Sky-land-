using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SkyLand
{
    /// <summary>
    /// Estado de uma operação (download/instalação/verificação) exibida em um DownloadCard:
    /// barra de progresso, porcentagem, velocidade, nome do arquivo, resultado e cancelamento.
    /// </summary>
    public class OpVM : INotifyPropertyChanged
    {
        private readonly string _title;
        private readonly string _description;
        private readonly string _letter;
        private readonly string _glyph;
        private readonly string _artUrl;

        private bool _running;
        private bool _indeterminate = true;
        private bool _cancelVisible;
        private bool _hasResult;
        private bool _isSuccess;
        private bool _isWarning;
        private bool _hasTarget;
        private double _progress;
        private string _percentText = "";
        private string _statusText = "";
        private string _speedText = "—";
        private string _sizeText = "—";
        private string _fileName = "";
        private string _resultText = "";
        private string _targetFolder;
        private string _targetFile;
        private CancellationTokenSource _cts;

        public OpVM(string title, string description, string letter = null, string glyph = null, string art = null)
        {
            _title = title;
            _description = description ?? "";
            _letter = letter;
            _glyph = ResolveGlyph(glyph);
            _artUrl = art;
            CancelCommand = new RelayCommand(_ => Cancel());
            OpenFolderCommand = new RelayCommand(_ => OpenTarget());
        }

        /// <summary>Aceita "\uE713", "E713" ou o próprio caractere.</summary>
        private static string ResolveGlyph(string g)
        {
            if (string.IsNullOrWhiteSpace(g)) return null;
            g = g.Trim();
            if (char.GetUnicodeCategory(g[0]) == System.Globalization.UnicodeCategory.PrivateUse)
                return g;
            if (g.Length == 4 && uint.TryParse(g, System.Globalization.NumberStyles.HexNumber, null, out var code) && code <= 0xFFFF)
                return char.ConvertFromUtf32((int)code);
            return g;
        }

        public string Title => _title;
        public string Description => _description;
        public string Letter => _letter;
        public string Glyph => _glyph;

        /// <summary>Arte incorporada do executor (pack URI); nula quando a operação não tem imagem própria.</summary>
        public string ArtUrl => _artUrl;

        public ICommand CancelCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public bool UseGlyph => !string.IsNullOrEmpty(_glyph);

        public bool IsIdle => !_running && !_hasResult;

        public bool IsRunning
        {
            get => _running;
            private set { if (_running != value) { _running = value; Raise(); Raise(nameof(CanStart)); Raise(nameof(IsIdle)); } }
        }

        public bool CanStart => !IsRunning;

        public bool IsIndeterminate
        {
            get => _indeterminate;
            private set { if (_indeterminate != value) { _indeterminate = value; Raise(); } }
        }

        public bool CancelVisible
        {
            get => _cancelVisible;
            private set { if (_cancelVisible != value) { _cancelVisible = value; Raise(); } }
        }

        public bool HasResult
        {
            get => _hasResult;
            private set { if (_hasResult != value) { _hasResult = value; Raise(); Raise(nameof(IsIdle)); } }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            private set { if (_isSuccess != value) { _isSuccess = value; Raise(); } }
        }

        public bool IsWarning
        {
            get => _isWarning;
        }

        public bool HasTarget
        {
            get => _hasTarget;
            private set { if (_hasTarget != value) { _hasTarget = value; Raise(); } }
        }

        public double Progress
        {
            get => _progress;
            private set { if (Math.Abs(_progress - value) > 0.01) { _progress = value; Raise(); } }
        }

        public string PercentText
        {
            get => _percentText;
            private set { if (_percentText != value) { _percentText = value; Raise(); } }
        }

        public string StatusText
        {
            get => _statusText;
            private set { if (_statusText != value) { _statusText = value; Raise(); } }
        }

        public string SpeedText
        {
            get => _speedText;
            private set { if (_speedText != value) { _speedText = value; Raise(); } }
        }

        public string SizeText
        {
            get => _sizeText;
            private set { if (_sizeText != value) { _sizeText = value; Raise(); } }
        }

        public string FileName
        {
            get => _fileName;
            private set { if (_fileName != value) { _fileName = value; Raise(); } }
        }

        public string ResultText
        {
            get => _resultText;
            private set { if (_resultText != value) { _resultText = value; Raise(); } }
        }

        // ==================== ciclo de vida ====================

        /// <summary>Inicializa a operação; retorna false se já existir outra em andamento.</summary>
        public async Task<bool> RunAsync(
            IShell shell, string busyText, Func<CancellationToken, Task> work, bool cancellable = true)
        {
            if (IsRunning)
            {
                shell?.Toast("Aguarde", "Já existe uma operação em andamento neste item.", ToastKind.Warn);
                return false;
            }

            ResetForRun();
            IsRunning = true;
            CancelVisible = cancellable;
            StatusText = busyText ?? "Iniciando…";
            ActivityService.Add(ActivityKind.Info, "Iniciado: " + _title);

            _cts = new CancellationTokenSource();
            try
            {
                await work(_cts.Token).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is OperationCanceledException &&
                (ex is TaskCanceledException || _cts.IsCancellationRequested))
            {
                SetResult(false, "Operação cancelada.");
                ActivityService.Add(ActivityKind.Warn, "Cancelado: " + _title);
            }
            catch (Win32Exception wex)
            {
                SetResult(false, string.IsNullOrEmpty(wex.Message)
                    ? "O Windows cancelou ou bloqueou a operação."
                    : "O Windows cancelou a operação: " + wex.Message);
                ActivityService.Add(ActivityKind.Error, "Falhou: " + _title + " — " + wex.Message);
            }
            catch (Exception ex)
            {
                var msg = UiHelper.FriendlyError(ex);
                SetResult(false, msg);
                ActivityService.Add(ActivityKind.Error, "Falhou: " + _title + " — " + msg);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                IsRunning = false;
                CancelVisible = false;
                // A retomada de progresso fica preservada para o próximo ciclo.
            }
            return true;
        }

        public void Cancel()
        {
            try { _cts?.Cancel(); } catch { }
        }

        private void ResetForRun()
        {
            HasResult = false;
            IsSuccess = false;
            _targetFolder = null;
            _targetFile = null;
            HasTarget = false;
            Progress = 0;
            PercentText = "";
            SpeedText = "—";
            SizeText = "—";
            FileName = "";
            StatusText = "";
        }

        public void SetStatus(string text)
        {
            StatusText = text;
        }

        public void SetBusy(string text)
        {
            StatusText = text;
            IsIndeterminate = true;
            Progress = 0;
            PercentText = "";
        }

        public void SetDownloadProgress(DownloadProgress p)
        {
            if (p == null) return;
            if (!IsRunning) return;
            FileName = string.IsNullOrEmpty(p.FileName) ? FileName : p.FileName;
            IsIndeterminate = p.Percent < 0;
            if (p.Percent >= 0)
            {
                Progress = p.Percent;
                PercentText = p.Percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
            }
            SizeText = UiHelper.FormatBytes(p.Received) +
                (p.Total > 0 ? " de " + UiHelper.FormatBytes(p.Total) : "");
            SpeedText = p.BytesPerSecond > 0 ? UiHelper.FormatSpeed(p.BytesPerSecond) : SpeedText;
        }

        public void SetResult(bool success, string text, string targetFolder = null, string targetFile = null)
        {
            IsSuccess = success;
            _isWarning = false;
            Raise(nameof(IsWarning));
            ResultText = text ?? "";
            _targetFolder = targetFolder;
            _targetFile = targetFile;
            HasTarget = !string.IsNullOrEmpty(targetFile) || !string.IsNullOrEmpty(targetFolder);
            HasResult = true;
            if (success) { Progress = 100; PercentText = "100%"; IsIndeterminate = false; }

            ActivityService.Add(
                success ? ActivityKind.Success : ActivityKind.Error,
                (success ? "OK: " : "Erro: ") + _title + " — " + ResultText);
        }

        /// <summary>Resultado informativo (nem sucesso, nem falha) — banner neutro.</summary>
        public void SetNeutralResult(string text, string targetFolder = null, string targetFile = null)
        {
            IsSuccess = false;
            _isWarning = true;
            Raise(nameof(IsWarning));
            ResultText = text ?? "";
            _targetFolder = targetFolder;
            _targetFile = targetFile;
            HasTarget = !string.IsNullOrEmpty(targetFile) || !string.IsNullOrEmpty(targetFolder);
            HasResult = true;
        }

        private void OpenTarget()
        {
            try
            {
                if (!string.IsNullOrEmpty(_targetFile))
                {
                    ProcessRunner.OpenFileInFolder(_targetFile);
                    return;
                }
                if (!string.IsNullOrEmpty(_targetFolder))
                {
                    AppPaths.TryCreate(_targetFolder);
                    ProcessRunner.OpenFolder(_targetFolder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível abrir a pasta:\n" + ex.Message, "SKY LAND",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
