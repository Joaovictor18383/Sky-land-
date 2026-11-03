namespace SkyLand
{
    /// <summary>Última consulta à WEAO compartilhada entre páginas (válida por 5 minutos).</summary>
    internal static class StatusCache
    {
        public static List<ExecutorStatus> Last { get; private set; }
        public static DateTime Loaded { get; private set; } = DateTime.MinValue;

        public static void Store(List<ExecutorStatus> list)
        {
            Last = list;
            Loaded = DateTime.Now;
        }

        public static bool IsFresh => Last != null && (DateTime.Now - Loaded).TotalMinutes < 5;

        public static ExecutorStatus Get(string name)
        {
            return Last?.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
