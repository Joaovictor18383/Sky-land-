using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SkyLand.Controls
{
    /// <summary>
    /// Card de operação: exibe título/descrição, botões da página, barra de progresso,
    /// porcentagem, velocidade, tamanho e resultado — vinculado a um OpVM via DataContext.
    /// </summary>
    public partial class DownloadCard : UserControl
    {
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
            nameof(Actions), typeof(UIElement), typeof(DownloadCard), new PropertyMetadata(null, OnActionsChanged));

        public DownloadCard()
        {
            InitializeComponent();
        }

        /// <summary>Conteúdo opcional (linha de botões) exibido no corpo do card.</summary>
        public UIElement Actions
        {
            get => (UIElement)GetValue(ActionsProperty);
            set => SetValue(ActionsProperty, value);
        }

        private static void OnActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DownloadCard card) card.ActionsHost.Content = e.NewValue;
        }

        // ==================== efeito suave ao passar o mouse ====================

        private void ArtWrap_MouseEnter(object sender, MouseEventArgs e) => AnimateArt(1.07);

        private void ArtWrap_MouseLeave(object sender, MouseEventArgs e) => AnimateArt(1.0);

        private void AnimateArt(double target)
        {
            if (ArtScale == null) return;
            var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ArtScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            ArtScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }
    }
}
