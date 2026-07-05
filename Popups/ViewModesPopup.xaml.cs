using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class ViewModesPopup : Window
    {
        private readonly ModeService _modeService;
        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;

        public event Action<ClockMode>? ModeActivated;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        public ViewModesPopup(ModeService modeService)
        {
            InitializeComponent();
            _modeService = modeService;
            _appWindow = GetAppWindow();
            SetupWindow();
            LoadModes();

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded) { _loaded = true; return; }
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };
        }

        private void LoadModes()
        {
            ModesList.Children.Clear();
            var modes = _modeService.GetAll();

            if (modes.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var mode in modes)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(
                        Color.FromArgb(40, 255, 255, 255)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Color dot
                try
                {
                    var hex = mode.Color.TrimStart('#');
                    byte r = Convert.ToByte(hex[0..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);

                    var dot = new Ellipse
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(Color.FromArgb(255, r, g, b)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(dot, 0);
                    grid.Children.Add(dot);
                }
                catch { }

                // Name + time
                var textStack = new StackPanel
                {
                    Spacing = 2,
                    VerticalAlignment = VerticalAlignment.Center
                };
                textStack.Children.Add(new TextBlock
                {
                    Text = mode.Name,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                });
                textStack.Children.Add(new TextBlock
                {
                    Text = $"{mode.StartTime:hh\\:mm} - {mode.EndTime:hh\\:mm} · {(mode.IsDaily ? "Daily" : mode.OneTimeDate?.ToString("MMM d") ?? "One-time")}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
                });
                Grid.SetColumn(textStack, 1);
                grid.Children.Add(textStack);

                // Action buttons
                var actions = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Edit button
                var editBtn = new Button
                {
                    Content = new FontIcon { Glyph = "\uE70F", FontSize = 12 },
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6),
                    Tag = mode
                };
                editBtn.Click += EditMode_Click;

                // Activate button
                var activateBtn = new Button
                {
                    Content = new FontIcon { Glyph = "\uE768", FontSize = 12 },
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6),
                    Tag = mode
                };
                activateBtn.Click += ActivateMode_Click;

                // Delete button
                var deleteBtn = new Button
                {
                    Content = new FontIcon
                    {
                        Glyph = "\uE74D",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 80, 80))
                    },
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6),
                    Tag = mode.Id
                };
                deleteBtn.Click += DeleteMode_Click;

                actions.Children.Add(editBtn);
                actions.Children.Add(activateBtn);
                actions.Children.Add(deleteBtn);

                Grid.SetColumn(actions, 2);
                grid.Children.Add(actions);

                card.Child = grid;
                ModesList.Children.Add(card);
            }
        }

        private void EditMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ClockMode mode)
            {
                this.Close();
                var editPopup = new AddModePopup(_modeService, mode);
                editPopup.Activate();
            }
        }

        private void ActivateMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ClockMode mode)
            {
                ModeActivated?.Invoke(mode);
                this.Close();
            }
        }

        private void DeleteMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                _modeService.Remove(id);
                LoadModes();
            }
        }

        private void AddMode_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            var addPopup = new AddModePopup(_modeService);
            addPopup.Activate();
        }

        private void SetupWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            try
            {
                int style = GetWindowLong(hWnd, GWL_STYLE);
                SetWindowLong(hWnd, GWL_STYLE, style & ~WS_CAPTION);
            }
            catch { }

            _appWindow.IsShownInSwitchers = false;

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.SetDragRectangles(Array.Empty<RectInt32>());

            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));
            int cornerPreference = 2;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            _configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            var target = this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
            switch (SettingsService.Current.MaterialType)
            {
                case "Mica":
                    if (MicaController.IsSupported())
                    {
                        var mica = new MicaController { Kind = MicaKind.Base };
                        mica.AddSystemBackdropTarget(target);
                        mica.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;
                case "MicaAlt":
                    if (MicaController.IsSupported())
                    {
                        var mica = new MicaController { Kind = MicaKind.BaseAlt };
                        mica.AddSystemBackdropTarget(target);
                        mica.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;
                default:
                    if (DesktopAcrylicController.IsSupported())
                    {
                        _acrylicController = new DesktopAcrylicController
                        {
                            TintColor = Windows.UI.Color.FromArgb(255, 20, 20, 20),
                            TintOpacity = SettingsService.Current.AcrylicTintOpacity,
                            LuminosityOpacity = SettingsService.Current.AcrylicLuminosityOpacity,
                            Kind = DesktopAcrylicKind.Base
                        };
                        _acrylicController.AddSystemBackdropTarget(target);
                        _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;
            }

            var primary = MonitorHelper.Primary;
            int centerX = MonitorHelper.ToPhysical(
                (primary?.LogicalBounds.Left ?? 0) +
                (primary?.LogicalBounds.Width ?? 1920) / 2 - 180);
            int popupY = MonitorHelper.ToPhysical(SettingsService.Current.BarHeight + 8);
            int popupW = MonitorHelper.ToPhysical(360);
            int popupH = MonitorHelper.ToPhysical(500);
            _appWindow.MoveAndResize(new RectInt32(centerX, popupY, popupW, popupH));
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}