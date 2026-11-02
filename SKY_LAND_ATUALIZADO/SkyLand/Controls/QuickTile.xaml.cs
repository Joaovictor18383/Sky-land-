using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkyLand.Controls
{
    /// <summary>Botão de atalho do painel inicial com visual de card.</summary>
    public partial class QuickTile : UserControl
    {
        public event Action Clicked;

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(QuickTile), new PropertyMetadata(""));

        public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
            nameof(Subtitle), typeof(string), typeof(QuickTile), new PropertyMetadata(""));

        public static readonly DependencyProperty LetterProperty = DependencyProperty.Register(
            nameof(Letter), typeof(string), typeof(QuickTile), new PropertyMetadata(""));

        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(QuickTile), new PropertyMetadata(""));

        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(
            nameof(ImagePath), typeof(string), typeof(QuickTile), new PropertyMetadata(""));

        public QuickTile()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public string Letter
        {
            get => (string)GetValue(LetterProperty);
            set => SetValue(LetterProperty, value);
        }

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>Arte incorporada exibida em destaque (quando definida, substitui letra/glifo).</summary>
        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Clicked?.Invoke();
        }

        // ==================== efeito suave ao passar o mouse ====================

        private void Root_MouseEnter(object sender, MouseEventArgs e) => AnimateArt(1.07);

        private void Root_MouseLeave(object sender, MouseEventArgs e) => AnimateArt(1.0);

        private void AnimateArt(double target)
        {
            if (string.IsNullOrEmpty(ImagePath) || ArtScale == null) return;
            var anim = new System.Windows.Media.Animation.DoubleAnimation(target, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };
            ArtScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            ArtScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }
    }
}
