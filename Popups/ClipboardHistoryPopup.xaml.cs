using LegendBar.Helpers;
using LegendBar.Widgets;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class ClipboardHistoryPopup : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;

        private readonly List<ClipboardEntry> _history;
        private AppWindow _appWindow;

        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;

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

        public ClipboardHistoryPopup(List<ClipboardEntry> history)
        {
            InitializeComponent();
            _history = history;
            _appWindow = GetAppWindow();
            SetupWindow();
            PopulateItems();

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded)
                {
                    if (e.WindowActivationState != WindowActivationState.Deactivated)
                        _loaded = true;
                    return;
                }
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };
        }

        private void SetupWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            try
            {
                int style = GetWindowLong(hWnd, GWL_STYLE);
                SetWindowLong(hWnd, GWL_STYLE, style & ~WS_CAPTION);
            }
            catch { /* suppress Win32 SEH */ }

            _appWindow.IsShownInSwitchers = false;

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            //ExtendsContentIntoTitleBar = true;
            //SetTitleBar(null);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.SetDragRectangles(Array.Empty<RectInt32>()); 
            _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            // Move DwmExtendFrameIntoClientArea AFTER title bar setup
            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));
            int cornerPreference = 2;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = 0,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            int borderColor = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
            DwmSetWindowAttribute(hWnd, 34, ref borderColor, sizeof(int));

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
                    _acrylicController = new DesktopAcrylicController
                    {
                        TintColor = Windows.UI.Color.FromArgb(255, 20, 20, 20),
                        TintOpacity = SettingsService.Current.AcrylicTintOpacity,
                        LuminosityOpacity = SettingsService.Current.AcrylicLuminosityOpacity,
                        Kind = DesktopAcrylicKind.Base
                    };
                    _acrylicController.AddSystemBackdropTarget(target);
                    _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
                    break;
            }

            var primary = MonitorHelper.Primary;
            int rightEdge = (primary?.LogicalBounds.Left ?? 0) +
                            (primary?.LogicalBounds.Width ?? 1920);
            int popupX = MonitorHelper.ToPhysical(rightEdge - 420 - 24);
            int popupY = MonitorHelper.ToPhysical(SettingsService.Current.BarHeight + 8);
            int popupW = MonitorHelper.ToPhysical(420);
            int popupH = MonitorHelper.ToPhysical(480);
            _appWindow.MoveAndResize(new RectInt32(popupX, popupY, popupW, popupH));
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            ItemsPanel.Children.Clear();
            EmptyText.Visibility = Visibility.Visible;
            ClearAllButton.IsEnabled = false;
        }

        private void PopulateItems()
        {
            if (_history.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            foreach (var entry in _history)
            {
                bool isImage = entry.IsImage && entry.Thumbnail != null;
                var btn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = new SolidColorBrush(
                        Windows.UI.Color.FromArgb(34, 255, 255, 255)),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(6),
                    Padding = isImage ? new Thickness(0) : new Thickness(10, 8, 10, 8)
                };

                if (entry.IsImage)
                {
                    if (entry.Thumbnail != null)
                    {
                        btn.Content = new Border
                        {
                            CornerRadius = new CornerRadius(6),
                            Child = new Image
                            {
                                Source = entry.Thumbnail,
                                MaxHeight = 100,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                            }
                        };
                    }
                    else
                    {
                        btn.Content = new TextBlock
                        {
                            Text = entry.Text,
                            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(230, 255, 255, 255)),
                            FontSize = 12,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            MaxLines = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            TextAlignment = TextAlignment.Left
                        };
                    }
                }
                else
                {
                    btn.Content = new TextBlock
                    {
                        Text = entry.Text,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(230, 255, 255, 255)),
                        FontSize = 12,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxLines = 1
                    };
                }

                var captured = entry;
                btn.Click += async (s, e) =>
                {
                    var data = new DataPackage();
                    if (captured.IsImage)
                    {
                        // Can't re-copy bitmap without the original stream — just close
                        this.Close();
                        return;
                    }
                    data.SetText(captured.Text ?? "");
                    Clipboard.SetContent(data);
                    this.Close();
                };

                ItemsPanel.Children.Add(btn);
            }
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}