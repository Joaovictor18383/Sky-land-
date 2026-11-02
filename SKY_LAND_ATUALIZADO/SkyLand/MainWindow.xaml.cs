using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SkyLand
{
    /// <summary>
    /// Janela principal do SKY LAND: chrome moderno, barra lateral, navegação,
    /// sistema de diálogos (confirmação/lista) e notificações.
    /// </summary>
    public partial class MainWindow : Window, IShell
    {
        private static readonly Dictionary<string, string> NavKeys = new()
        {
            { "NavDash", "dash" },
            { "NavPotassium", "potassium" },
            { "NavMadium", "madium" },
            { "NavReal", "real" },
            { "NavOther", "other" },
            { "NavSettings", "settings" },
            { "NavAbout", "about" }
        };

        private static readonly Dictionary<string, string> Titles = new()
        {
            { "dash", "Início" },
            { "potassium", "Potassium" },
            { "madium", "Madium" },
            { "real", "Real" },
            { "other", "Outros" },
            { "settings", "Configurações" },
            { "about", "Sobre" }
        };

        private readonly Dictionary<string, UserControl> _pages = new();
        private bool _navSuppress;
        private TaskCompletionSource<bool> _dialogTcs;

        public MainWindow()
        {
            // XAML marca o NavDash como checked → evita navegação antes de todos os elementos existirem
            _navSuppress = true;
            InitializeComponent();
            _navSuppress = false;

            ApplyChrome();

            SetWindowIcon();

            StateChanged += (_, _) => OnChromeStateChanged();
            OnChromeStateChanged();

            // navegador p/ página inicial somente após o XAML completo
            _pages.Add("dash", new Pages.DashboardPage(this));
            NavigateTo("dash");

            Loaded += async (_, _) => await CheckUpdatesSilently();
        }

        private void ApplyChrome()
        {
            var chrome = new WindowChrome
            {
                ResizeBorderThickness = new Thickness(7),
                CaptionHeight = 44,
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);
            WindowChrome.SetIsHitTestVisibleInChrome(WindowButtons, true);
            WindowChrome.SetIsHitTestVisibleInChrome(BtnMin, true);
            WindowChrome.SetIsHitTestVisibleInChrome(BtnMax, true);
            WindowChrome.SetIsHitTestVisibleInChrome(BtnClose, true);
        }

        private void SetWindowIcon()
        {
            try
            {
                Icon = BitmapFrame.Create(
                    new Uri("pack://application:,,,/Assets/app.ico", UriKind.RelativeOrAbsolute));
            }
            catch
            {
                try
                {
                    Icon = BitmapFrame.Create(
                        new Uri("pack://application:,,,/Assets/logo.png", UriKind.RelativeOrAbsolute));
                }
                catch { }
            }
        }

        private void OnChromeStateChanged()
        {
            bool maximized = WindowState == WindowState.Maximized;
            RootGrid.Margin = maximized ? new Thickness(0) : new Thickness(14, 16, 14, 22);
            RootBorder.CornerRadius = maximized ? new CornerRadius(0) : new CornerRadius(18);
            RootBorder.Padding = maximized ? new Thickness(2) : new Thickness(0);
        }

        // ==================== NAVEGAÇÃO ====================

        private void OnNavChecked(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            if (btn == null) return;

            if (!NavKeys.TryGetValue(btn.Name, out var key)) return;
            if (_navSuppress) return;

            NavigateTo(key);
        }

        public void NavigateTo(string key)
        {
            if (PageHost == null || CrumbText == null) return; // XAML ainda em carga

            if (!_pages.TryGetValue(key, out var page))
            {
                page = BuildPage(key);
                if (page == null) return;
                _pages[key] = page;
            }

            CrumbText.Text = Titles.ContainsKey(key) ? Titles[key] : "SKY LAND";
            PageHost.Content = page;
            AnimateIn(page);

            // marca o botão correspondente (dispara Checked com guard _navSuppress)
            SetNavChecks(key);
        }

        private void SetNavChecks(string key)
        {
            _navSuppress = true;
            foreach (var pair in NavKeys)
            {
                if (FindName(pair.Key) is RadioButton rb)
                    rb.IsChecked = pair.Value == key;
            }
            _navSuppress = false;
        }

        private UserControl BuildPage(string key)
        {
            try
            {
                switch (key)
                {
                    case "dash": return new Pages.DashboardPage(this);
                    case "potassium": return new Pages.PotassiumPage(this);
                    case "madium": return new Pages.MadiumPage(this);
                    case "real": return new Pages.RealPage(this);
                    case "other": return new Pages.OtherPage(this);
                    case "settings": return new Pages.SettingsPage(this);
                    case "about": return new Pages.AboutPage(this);
                    default: return null;
                }
            }
            catch (Exception ex)
            {
                ActivityService.Add(ActivityKind.Error, "Falha ao abrir a página " + key + ": " + ex.Message);
                return null;
            }
        }

        private static void AnimateIn(UserControl page)
        {
            try
            {
                page.Opacity = 0;
                if (page.RenderTransform is not TranslateTransform)
                {
                    page.RenderTransform = new TranslateTransform(0, 16);
                }
                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var slide = new DoubleAnimation(16, 0, TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                page.BeginAnimation(System.Windows.UIElement.OpacityProperty, fade);
                ((TranslateTransform)page.RenderTransform).BeginAnimation(
                    System.Windows.Media.TranslateTransform.YProperty, slide);
            }
            catch { }
        }

        // ==================== JANELA ====================

        private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ==================== IShell ====================

        public Task<bool> ConfirmAsync(string title, string message, string okText, string cancelText, bool danger)
        {
            return ShowDialogCore(title, message, null, okText == null ? "Confirmar" : okText,
                cancelText, danger, false);
        }

        public Task<bool> ConfirmItemsAsync(string title, string message, IReadOnlyList<string> items,
            string okText, string cancelText, bool danger)
        {
            return ShowDialogCore(title, message, items, okText == null ? "Confirmar" : okText,
                cancelText, danger, false);
        }

        public Task InfoAsync(string title, string message)
        {
            ShowDialogCore(title, message, null, "Fechar", null, false, true);
            return Task.CompletedTask;
        }

        public void Toast(string title, string message, ToastKind kind)
        {
            App.MainWindowRef?.Dispatcher.Invoke(() => ShowToast(title, message, kind));
        }

        public void RunOnUiThread(Action action)
        {
            Dispatcher.Invoke(action);
        }

        // ==================== DIÁLOGOS ====================

        private Task<bool> ShowDialogCore(
            string title, string message, IReadOnlyList<string> items,
            string okText, string cancelText, bool danger, bool info)
        {
            if (_dialogTcs != null) return _dialogTcs.Task;

            var tcs = new TaskCompletionSource<bool>();
            _dialogTcs = tcs;

            var panel = new StackPanel();

            // ícone
            var iconRow = new StackPanel { Orientation = Orientation.Horizontal };
            var kindBrush = GetThemeBrush(danger ? "Brush.Danger" : info ? "Brush.Info" : "Brush.Warning")
                             ?? System.Windows.Media.Brushes.Gray;
            var kindColor = BrushColorOf(kindBrush);
            var glyph = danger ? "\uEA39" : info ? "\uE946" : "\uE7BA";

            var iconBubble = new Border
            {
                Width = 42, Height = 42, CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(0x26, kindColor.R, kindColor.G, kindColor.B)),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBubble.Child = new TextBlock
            {
                Text = glyph,
                FontFamily = (FontFamily)Application.Current.Resources["Font.Icons"],
                FontSize = 18,
                Foreground = kindBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 16, FontWeight = FontWeights.SemiBold,
                Foreground = GetThemeBrush("Brush.Text.Primary") ?? System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };
            iconRow.Children.Add(iconBubble);
            iconRow.Children.Add(titleBlock);
            panel.Children.Add(iconRow);

            // mensagem
            var msg = new TextBlock
            {
                Text = message,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = GetThemeBrush("Brush.Text.Secondary") ?? System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(0, 14, 0, 0)
            };
            panel.Children.Add(msg);

            // lista de itens
            if (items != null && items.Count > 0)
            {
                var listBox = new StackPanel();
                foreach (var it in items)
                {
                    listBox.Children.Add(new TextBlock
                    {
                        Text = "•  " + it,
                        FontSize = 11.5,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = GetThemeBrush("Brush.Text.Primary"),
                        Margin = new Thickness(0, 3, 0, 3)
                    });
                }

                var listScroll = new ScrollViewer
                {
                    MaxHeight = 200,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = listBox
                };
                var listBorder = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = GetThemeBrush("Brush.Surface"),
                    BorderBrush = GetThemeBrush("Brush.Stroke"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 12, 0, 0),
                    Child = listScroll
                };
                panel.Children.Add(listBorder);
            }

            // botões
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            if (!string.IsNullOrEmpty(cancelText))
            {
                var cancel = new Button
                {
                    Content = cancelText,
                    Style = (Style)Application.Current.Resources["BtnGhost"],
                    IsCancel = true,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                cancel.Click += (_, _) => ResolveDialog(false);
                buttons.Children.Add(cancel);
            }

            var ok = new Button
            {
                Content = okText,
                Style = (Style)Application.Current.Resources[danger ? "BtnDanger" : "BtnPrimary"],
                IsDefault = true
            };
            ok.Click += (_, _) => ResolveDialog(true);
            buttons.Children.Add(ok);

            panel.Children.Add(buttons);

            DialogContent.Content = panel;
            DialogOverlay.Opacity = 0;
            DialogOverlay.Visibility = Visibility.Visible;
            DialogOverlay.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));

            return tcs.Task;
        }

        private void ResolveDialog(bool result)
        {
            if (_dialogTcs == null) return;
            var tcs = _dialogTcs;
            _dialogTcs = null;

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (_, _) =>
            {
                DialogOverlay.Visibility = Visibility.Collapsed;
                DialogContent.Content = null;
            };
            DialogOverlay.BeginAnimation(UIElement.OpacityProperty, fade);
            tcs.TrySetResult(result);
        }

        /// <summary>Extrai uma cor sólida de qualquer brush (Solid/Gradient) para halos translúcidos.</summary>
        private static Color BrushColorOf(Brush b)
        {
            if (b is SolidColorBrush scb) return scb.Color;
            if (b is GradientBrush gb && gb.GradientStops.Count > 0)
            {
                var s0 = gb.GradientStops[0].Color;
                var s1 = gb.GradientStops[gb.GradientStops.Count - 1].Color;
                return Color.FromRgb(
                    (byte)((s0.R + s1.R) / 2),
                    (byte)((s0.G + s1.G) / 2),
                    (byte)((s0.B + s1.B) / 2));
            }
            return Colors.Gray;
        }

        private Brush GetThemeBrush(string key)
        {
            try
            {
                if (Application.Current.Resources[key] is Brush b)
                    return b;
            }
            catch { }
            return null;
        }

        // ==================== TOASTS ====================

        private void ShowToast(string title, string message, ToastKind kind)
        {
            try
            {
                while (ToastHost.Children.Count >= 4)
                    ToastHost.Children.RemoveAt(0);

                string glyph; Brush glyphBrush;
                switch (kind)
                {
                    case ToastKind.Success: glyph = "\uE73E"; glyphBrush = GetThemeBrush("Brush.Success"); break;
                    case ToastKind.Warn: glyph = "\uE7BA"; glyphBrush = GetThemeBrush("Brush.Warning"); break;
                    case ToastKind.Error: glyph = "\uEA39"; glyphBrush = GetThemeBrush("Brush.Danger"); break;
                    default: glyph = "\uE946"; glyphBrush = GetThemeBrush("Brush.Info"); break;
                }
                glyphBrush = glyphBrush ?? System.Windows.Media.Brushes.Gray;
                var glyphColor = BrushColorOf(glyphBrush);

                var icon = new Border
                {
                    Width = 30, Height = 30, CornerRadius = new CornerRadius(9),
                    Background = new SolidColorBrush(Color.FromArgb(0x2A, glyphColor.R, glyphColor.G, glyphColor.B)),
                    VerticalAlignment = VerticalAlignment.Top
                };
                icon.Child = new TextBlock
                {
                    Text = glyph,
                    FontFamily = (FontFamily)Application.Current.Resources["Font.Icons"],
                    FontSize = 14,
                    Foreground = glyphBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var textStack = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
                textStack.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetThemeBrush("Brush.Text.Primary")
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = message,
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 0),
                    Foreground = GetThemeBrush("Brush.Text.Secondary")
                });

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(icon, 0);
                Grid.SetColumn(textStack, 1);
                grid.Children.Add(icon);
                grid.Children.Add(textStack);

                var toast = new Border
                {
                    Width = 350,
                    CornerRadius = new CornerRadius(14),
                    Background = GetThemeBrush("Brush.Surface.Solid2"),
                    BorderBrush = GetThemeBrush("Brush.Stroke"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16, 13, 14, 13),
                    Margin = new Thickness(0, 0, 0, 10),
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(0, 0, 0),
                        BlurRadius = 26, ShadowDepth = 6, Direction = -90, Opacity = 0.5
                    },
                    Opacity = 0,
                    RenderTransform = new TranslateTransform(18, 0),
                    Child = grid
                };

                var close = new Button
                {
                    Style = (Style)Application.Current.Resources["WinBtn"],
                    Content = "\uE711",
                    Width = 26, Height = 22,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Opacity = 0.7
                };
                Grid.SetColumn(close, 1);
                grid.Children.Add(close);

                toast.Tag = close;

                ToastHost.Children.Insert(0, toast);

                toast.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
                ((TranslateTransform)toast.RenderTransform).BeginAnimation(
                    System.Windows.Media.TranslateTransform.XProperty,
                    new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });

                close.Click += (_, _) => DismissToast(toast);
                DismissLater(toast);
            }
            catch { }
        }

        private async void DismissLater(Border toast)
        {
            await Task.Delay(4600);
            DismissToast(toast);
        }

        private void DismissToast(Border toast)
        {
            try
            {
                if (toast.Parent == null) return;
                var fade = new DoubleAnimation(toast.Opacity, 0, TimeSpan.FromMilliseconds(180));
                fade.Completed += (_, _) =>
                {
                    ToastHost.Children.Remove(toast);
                };
                toast.BeginAnimation(UIElement.OpacityProperty, fade);
            }
            catch { }
        }

        // ==================== VERIFICAÇÃO AUTOMÁTICA DE ATUALIZAÇÕES ====================

        private async Task CheckUpdatesSilently()
        {
            try
            {
                if (!SettingsStore.Current.AutoUpdateCheck) return;
                var info = await UpdateService.CheckAsync(CancellationToken.None);
                if (!info.CheckedOk) return;

                if (info.HasUpdate)
                {
                    ActivityService.Add(ActivityKind.Info,
                        "Nova versão do SKY LAND disponível: v" + info.LatestVersion);
                    Toast("Atualização disponível",
                        "SKY LAND v" + info.LatestVersion + " está disponível no GitHub. Veja em Configurações.",
                        ToastKind.Info);
                }
            }
            catch { }
        }

        // ==================== CANAL DE AUTOMAÇÃO (QA: executado só com --uicmd) ====================

        private System.Windows.Threading.DispatcherTimer _cmdTimer;

        internal void StartUiHarness()
        {
            if (!App.UiCmdMode) return;

            _cmdTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            _cmdTimer.Tick += (_, _) =>
            {
                try
                {
                    var path = System.IO.Path.Combine(AppPaths.Base, "uicmd.txt");
                    if (!System.IO.File.Exists(path)) return;
                    var line = System.IO.File.ReadAllText(path).Trim();
                    if (line.Length == 0) return;
                    System.IO.File.Delete(path);
                    RunUiCommand(line);
                }
                catch { }
            };
            _cmdTimer.Start();
        }

        private void RunUiCommand(string line)
        {
            try
            {
                var sp = line.Split(new[] { ' ' }, 2);
                var cmd = sp[0].ToLowerInvariant();
                var arg = sp.Length > 1 ? sp[1] : null;

                switch (cmd)
                {
                    case "nav":
                        NavigateTo(arg);
                        break;

                    case "shot":
                        RenderShot(arg);
                        break;

                    case "theme":
                        var t = string.Equals(arg, "Black", StringComparison.OrdinalIgnoreCase) ? "Black" : "DeepBlue";
                        App.ApplyTheme(t);
                        SettingsStore.Current.Theme = t;
                        SettingsStore.Save();
                        ActivityService.Add(ActivityKind.Info, "Tema alterado pelo canal de automação: " + t);
                        break;

                    case "run":
                        RunUiOp(arg);
                        break;

                    case "dialog":
                        // abre o mesmo fluxo do reparo (dialog com lista; sem execução pois auto-cancela)
                        var _ = OpsStore.MadiumRepair.RunAsync(
                            this, "Analisando instalação do Madium…",
                            ct => ExecutorOps.RepairMadiumAsync(OpsStore.MadiumRepair, this, ct));
                        var closer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(5200)
                        };
                        closer.Tick += (_, __) => { ResolveDialog(false); closer.Stop(); };
                        closer.Start();
                        break;

                    case "close":
                        ResolveDialog(false);
                        break;
                }
            }
            catch { }
        }

        private void RunUiOp(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "anydesk":
                    var _ = OpsStore.AnyDesk.RunAsync(
                        this, "Preparando download do AnyDesk…",
                        ct => ExecutorOps.DownloadAnyDeskAsync(OpsStore.AnyDesk, this, ct));
                    break;
                case "webview2":
                    var wv = OpsStore.WebView2.RunAsync(
                        this, "Verificando WebView2…",
                        ct => InstallerOps.InstallWebView2Async(OpsStore.WebView2, this, ct));
                    break;
            }
        }

        /// <summary>Renderiza a própria janela em PNG via RenderTargetBitmap (funciona minimizada/sem foco).</summary>
        private void RenderShot(string path)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path)) return;
                    var wasMinimized = WindowState != WindowState.Normal;
                    if (wasMinimized) WindowState = WindowState.Normal;
                    UpdateLayout();

                    int w = (int)Math.Max(1, Math.Ceiling(ActualWidth));
                    int h = (int)Math.Max(1, Math.Ceiling(ActualHeight));
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(this);

                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using (var fs = System.IO.File.Create(path))
                        encoder.Save(fs);

                    if (wasMinimized) WindowState = WindowState.Minimized;
                }
                catch { }
            }));
        }
    }
}
