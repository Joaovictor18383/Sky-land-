using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SkyLand
{
    /// <summary>Comando simples para uso em código (sem MVVM externo).</summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    /// <summary>Helpers de formação de texto, tempo e mensagens de erro amigáveis.</summary>
    internal static class UiHelper
    {
        public static string FormatBytes(double bytes)
        {
            if (bytes < 0) return "—";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double b = bytes;
            int u = 0;
            while (b >= 1024 && u < units.Length - 1)
            {
                b /= 1024;
                u++;
            }
            string text = b.ToString(u == 0 ? "0" : "0.0", CultureInfo.InvariantCulture);
            return text + " " + units[u];
        }

        public static string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0) return "—";
            return FormatBytes(bytesPerSecond) + "/s";
        }

        public static string FormatClock(DateTime time)
        {
            if (time.Date == DateTime.Today)
                return time.ToString("HH:mm", CultureInfo.CurrentCulture);
            return time.ToString("dd/MM HH:mm", CultureInfo.CurrentCulture);
        }

        public static string ToHex(long value)
        {
            return value.ToString("X", CultureInfo.InvariantCulture);
        }

        /// <summary>Traduz exceções de rede/execução para uma mensagem em português compreensível.</summary>
        public static string FriendlyError(Exception ex)
        {
            if (ex is UserCanceledException) return "Operação cancelada pelo usuário.";

            var msg = ex?.Message ?? "Erro desconhecido.";

            if (ex is HttpRequestException || ex is IOException)
            {
                if (msg.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Arquivo não encontrado no servidor (404). O link pode ter mudado.";
                if (msg.IndexOf("403", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "O servidor recusou o acesso (403). Tente novamente mais tarde.";
                if (msg.IndexOf("No such host", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("getaddrinfo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("nome de host", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Falha de conexão: verifique sua internet.";
                if (msg.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "A conexão expirou. Verifique sua internet e tente novamente.";
                if (msg.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Falha de conexão com o servidor.";
                return "Falha de conexão ou de leitura do arquivo.";
            }

            if (ex is SkyHttpException httpex)
                return "O servidor respondeu com erro HTTP " + httpex.StatusCode + ".";

            if (ex is UnauthorizedAccessException)
                return "Acesso negado pelo Windows ao gravar/ler o arquivo. Feche programas que estejam usando a pasta e tente de novo.";

            if (ex is Win32Exception)
                return "O Windows bloqueou ou cancelou a operação do sistema.";

            if (msg.Length > 220) msg = msg.Substring(0, 220) + "…";
            return msg;
        }

        /// <summary>Encurta textos longos (notas de release, por exemplo).</summary>
        public static string TrimText(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var t = text.Replace("\r", "").Trim();
            if (t.Length <= max) return t;
            return t.Substring(0, max).TrimEnd() + "…";
        }
    }

    /// <summary>Exceção lançada quando o usuário cancela no Windows (UAC etc.).</summary>
    public class UserCanceledException : Exception
    {
        public UserCanceledException() : base("Operação cancelada pelo usuário.") { }
    }
}
