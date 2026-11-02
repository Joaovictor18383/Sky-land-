using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SkyLand
{
    /// <summary>Visível quando a string é um glifo (ex.: "\uE80F" — código de ícone na faixa Segoe 0xE700+, ou multi-char).</summary>
    public sealed class GlyphVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            bool isGlyph = !string.IsNullOrEmpty(s) &&
                char.GetUnicodeCategory(s[s.Length - 1]) == System.Globalization.UnicodeCategory.PrivateUse;
            return isGlyph ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Visível quando a string é uma letra/monograma curto printable (ex.: "P", "AM").</summary>
    public sealed class LetterVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            bool isLetter = !string.IsNullOrEmpty(s) && s.Length <= 2 &&
                char.GetUnicodeCategory(s[s.Length - 1]) != System.Globalization.UnicodeCategory.PrivateUse;
            return isLetter ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Booleano inverso → Visibility (para alternar painéis).</summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = value is bool v && v;
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Cor do marcador/crachá conforme o tipo de atividade.</summary>
    public sealed class ActivityKindToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string brushKey = value is ActivityKind k
                    ? k switch
                    {
                        ActivityKind.Success => "Brush.Success",
                        ActivityKind.Warn => "Brush.Warning",
                        ActivityKind.Error => "Brush.Danger",
                        _ => "Brush.Info"
                    }
                    : "Brush.Info";

                if (Application.Current.TryFindResource(brushKey) is System.Windows.Media.Brush b)
                    return b;
            }
            catch { }
            return System.Windows.Media.Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Visível quando a string é usada (artes incorporadas); invertível para não esconder fallbacks.</summary>
    public sealed class TextVisibilityConverter : IValueConverter
    {
        /// <summary>Quando verdadeiro, exibe apenas quando o texto é nulo/vazio (fallback do resto da UI).</summary>
        public bool ShowWhenEmpty { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var has = !string.IsNullOrEmpty(value as string);
            if (ShowWhenEmpty) return has ? Visibility.Collapsed : Visibility.Visible;
            return has ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Nome do executor → arte incorporada ao exe (Potassium hexágono, Madium "M", Real em ondas).</summary>
    public sealed class ExecutorNameToArtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = (value as string)?.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            switch (name)
            {
                case "Potassium": return "pack://application:,,,/Assets/potassium.png";
                case "Madium": return "pack://application:,,,/Assets/madium.png";
                case "Real": return "pack://application:,,,/Assets/real.png";
                default: return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
