using System.Collections.Generic;

namespace SkyLand
{
    public enum ToastKind { Info, Success, Warn, Error }

    /// <summary>Abstração da janela principal para páginas e serviços (diálogos, avisos, toasts).</summary>
    public interface IShell
    {
        Task<bool> ConfirmAsync(string title, string message, string okText, string cancelText, bool danger);
        Task<bool> ConfirmItemsAsync(string title, string message, IReadOnlyList<string> items, string okText, string cancelText, bool danger);
        Task InfoAsync(string title, string message);
        void Toast(string title, string message, ToastKind kind);
        void NavigateTo(string key);
        void RunOnUiThread(Action action);
    }

    /// <summary>Processos que funcionam sem shell (não usados por padrão, atalhos de teste).</summary>
    internal sealed class NullShell : IShell
    {
        public static readonly NullShell Instance = new();
        public Task<bool> ConfirmAsync(string title, string message, string okText, string cancelText, bool danger) => Task.FromResult(false);
        public Task<bool> ConfirmItemsAsync(string title, string message, IReadOnlyList<string> items, string okText, string cancelText, bool danger) => Task.FromResult(false);
        public Task InfoAsync(string title, string message) => Task.CompletedTask;
        public void Toast(string title, string message, ToastKind kind) { }
        public void NavigateTo(string key) { }
        public void RunOnUiThread(Action action) => action();
    }
}
