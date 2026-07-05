using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class ReminderNotificationPopup : Window
    {
        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private MicaController? _micaController;

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

        public ReminderNotificationPopup(Reminder reminder, DispatcherQueue dispatcherQueue)
        {
            InitializeComponent();
            _appWindow = GetAppWindow();
            TitleText.Text = reminder.Title;
            SetupWindow();
            FadeIn();
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
            int cornerPreference = 2; // DWMWCP_ROUND
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

            // Center below clock — center of primary monitor
            var primary = MonitorHelper.Primary;
            int centerX = (primary?.LogicalBounds.Left ?? 0) +
                          (primary?.LogicalBounds.Width ?? 1920) / 2 - 140;
            int popupY = SettingsService.Current.BarHeight + 8;
            _appWindow.MoveAndResize(new RectInt32(centerX, popupY, 280, 130));

            // Force topmost via Win32
            SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0,
                0x0001 | 0x0002 | 0x0010);
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private void FadeIn()
        {
            var fadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, NotificationBorder);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            storyboard.Children.Add(fadeIn);
            storyboard.Begin();
        }

        private void FadeOutAndClose()
        {
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, NotificationBorder);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            storyboard.Children.Add(fadeOut);
            storyboard.Completed += (s, e) => this.Close();
            storyboard.Begin();
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            FadeOutAndClose();
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}