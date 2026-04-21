using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Interop;

namespace LegendBar
{
    public sealed partial class MainWindow : Window
    {
        // All layout values come from MonitorHelper — nothing hardcoded
        private int WinX => MonitorHelper.WinX;
        private int WinW => MonitorHelper.WinW;
        private int WinY => MonitorHelper.WinY;

        private bool _isPinned = false;
        private bool _blockWindowPos = false;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate? _wndProc;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private const uint WM_DPICHANGED = 0x02E0;
        private const uint WM_WINDOWPOSCHANGING = 0x0046;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4;

        private void InstallWndProc(IntPtr hWnd)
        {
            _wndProc = (hWnd2, msg, wParam, lParam) =>
            {
                if (msg == WM_DPICHANGED)
                    return IntPtr.Zero;
                if (msg == WM_WINDOWPOSCHANGING && _blockWindowPos)
                    return IntPtr.Zero;
                return CallWindowProc(_oldWndProc, hWnd2, msg, wParam, lParam);
            };
            _oldWndProc = GetWindowLongPtr(hWnd, GWLP_WNDPROC);
            SetWindowLongPtr(hWnd, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProc));
        }

        private void SetPinIcon(bool pinned)
        {
            var path = pinned
                ? "ms-appx:///Assets/Pins/pinned.svg"
                : "ms-appx:///Assets/Pins/unpin.svg";
            PinIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                new Uri(path));
            PinIcon.Opacity = pinned ? 1.0 : 0.5;
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _isPinned = !_isPinned;

            if (_isPinned)
            {
                _topmostTimer?.Stop();
                _autoHide?.SetPinned(true);

                // Block shell from repositioning during AppBar registration
                _blockWindowPos = true;
                _appWindow.MoveAndResize(new RectInt32(
                    WinX, WinY, WinW,
                    (int)(SettingsService.Current.BarHeight)));
                AppBarHelper.Register(hWnd, (int)(SettingsService.Current.BarHeight));
                _blockWindowPos = false;

                // Re-assert position after shell settles
                var t = DispatcherQueue.CreateTimer();
                t.Interval = TimeSpan.FromMilliseconds(200);
                t.IsRepeating = false;
                t.Tick += (s, ev) =>
                {
                    _appWindow.MoveAndResize(new RectInt32(
                        WinX, WinY, WinW,
                        (int)(SettingsService.Current.BarHeight)));
                    var hWnd3 = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var m = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                    DwmExtendFrameIntoClientArea(hWnd3, ref m);
                    t.Stop();
                };
                t.Start();

                var margins = new MARGINS
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1
                };
                DwmExtendFrameIntoClientArea(hWnd, ref margins);

                SetPinIcon(true);
                ToolTipService.SetToolTip(PinButton, "Unpin bar");
            }
            else
            {
                AppBarHelper.Unregister(hWnd);
                _topmostTimer?.Start();
                _autoHide?.SetPinned(false);
                _autoHide?.ForceHide();

                SetPinIcon(false);
                ToolTipService.SetToolTip(PinButton, "Pin bar");
            }
        }

        private SettingsPopup? _settingsPopup;
        private AutoHideHelper? _autoHide;
        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private DispatcherQueueTimer? _topmostTimer;

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_ACTIVATE = 0x0006;
        private const uint WA_ACTIVE = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        public MainWindow()
        {
            InitializeComponent();
            SettingsService.Load();

            // Detect monitors FIRST before anything else
            MonitorHelper.Initialize();

            // Apply dynamic XAML margin for primary monitor offset
            if (ContentGrid != null)
                ContentGrid.Margin = new Thickness(MonitorHelper.PrimaryOffsetX, 0, 0, 0);

            _appWindow = GetAppWindowForCurrentWindow();
            SetupWindow();
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private void SetupWindow()
        {
            _topmostTimer = DispatcherQueue.CreateTimer();
            _topmostTimer.Interval = TimeSpan.FromSeconds(1);
            _topmostTimer.Tick += (s, e) =>
            {
                var h = WinRT.Interop.WindowNative.GetWindowHandle(this);
                SetWindowPos(h, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            };
            _topmostTimer.Start();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            int savedHeight = SettingsService.Current.BarHeight;
            _appWindow.MoveAndResize(new RectInt32(WinX, WinY, WinW, savedHeight));

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsResizable = false;
                presenter.IsAlwaysOnTop = true;
                presenter.SetBorderAndTitleBar(false, false);
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                _appWindow.TitleBar.SetDragRectangles(Array.Empty<RectInt32>());
            }

            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            int darkMode = 1;
            DwmSetWindowAttribute(hWnd, 20, ref darkMode, sizeof(int));

            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));

            int cornerPreference = 1;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            _appWindow.IsShownInSwitchers = false;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);
            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            _appWindow.Show();

            _configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            _acrylicController = new DesktopAcrylicController
            {
                TintColor = Windows.UI.Color.FromArgb(255, 20, 20, 20),
                TintOpacity = SettingsService.Current.AcrylicTintOpacity,
                LuminosityOpacity = SettingsService.Current.AcrylicLuminosityOpacity,
                Kind = DesktopAcrylicKind.Base
            };

            _acrylicController.AddSystemBackdropTarget(
                this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _acrylicController.SetSystemBackdropConfiguration(_configurationSource);

            var hWnd2 = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd2, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            SendMessage(hWnd, WM_ACTIVATE, (IntPtr)WA_ACTIVE, IntPtr.Zero);

            InstallWndProc(hWnd);

            _autoHide = new AutoHideHelper(_appWindow, DispatcherQueue,
                SettingsService.Current.BarHeight);

            SetPinIcon(false);
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        public void UpdateBarHeight(int height)
        {
            _appWindow.MoveAndResize(new RectInt32(WinX, WinY, WinW, height));
            _autoHide?.UpdateBarHeight(height);

            if (_settingsPopup == null)
                _autoHide?.ForceHide();
            else
                _appWindow.MoveAndResize(new RectInt32(WinX, WinY, WinW, height));
        }

        public void SetAcrylicOpacity(float tintOpacity, float luminosityOpacity)
        {
            if (_acrylicController != null)
            {
                _acrylicController.RemoveAllSystemBackdropTargets();
                _acrylicController.TintOpacity = tintOpacity;
                _acrylicController.LuminosityOpacity = luminosityOpacity;
                _acrylicController.AddSystemBackdropTarget(
                    this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _acrylicController.SetSystemBackdropConfiguration(_configurationSource!);
            }
        }

        public void UpdateAnimationSpeeds(double showMs, double hideMs)
        {
            _autoHide?.UpdateSpeeds(showMs, hideMs);
        }

        public void UpdateHideDelay(int delayMs)
        {
            _autoHide?.UpdateHideDelay(delayMs);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        public void OpenSettings()
        {
            if (_settingsPopup != null) return;
            _settingsPopup = new SettingsPopup(this);
            _settingsPopup.Closed += (s, e) =>
            {
                _settingsPopup = null;
                _autoHide?.SetExternalWindowOpen(false);
            };
            _autoHide?.SetExternalWindowOpen(true);
            _settingsPopup.Activate();
        }
    }
}