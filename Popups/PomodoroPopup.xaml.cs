using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class PomodoroPopup : Window
    {
        private int _popupX;
        private int _popupY;

        public event Action<int, int>? TimersChanged;

        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private MicaController? _micaController;

        private int _focusSeconds;
        private int _breakSeconds;

        public event Action<int, int>? StartRequested;

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

        public PomodoroPopup(int focusSeconds, int breakSeconds, int popupX, int popupY)
        {
            InitializeComponent();
            _focusSeconds = focusSeconds;
            _breakSeconds = breakSeconds;
            _popupX = popupX;
            _popupY = popupY;
            _appWindow = GetAppWindow();
            SetupWindow();
            UpdateDisplay();
            SetupScrollWheels();

            bool _loaded = false;
            bool _readyToClose = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded)
                {
                    _loaded = true;
                    var t = DispatcherQueue.CreateTimer();
                    t.Interval = TimeSpan.FromMilliseconds(500);
                    t.IsRepeating = false;
                    t.Tick += (_, _) => { _readyToClose = true; t.Stop(); };
                    t.Start();
                    return;
                }
                if (_readyToClose && e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };
        }

        private void SetupScrollWheels()
        {
            FocusTimeText.PointerWheelChanged += (s, e) =>
            {
                var delta = e.GetCurrentPoint(FocusTimeText).Properties.MouseWheelDelta;
                _focusSeconds = Math.Clamp(
                    _focusSeconds + (delta > 0 ? 60 : -60), 60, 90 * 60);
                UpdateDisplay();
                TimersChanged?.Invoke(_focusSeconds, _breakSeconds); // ← auto-save
                e.Handled = true;
            };

            BreakTimeText.PointerWheelChanged += (s, e) =>
            {
                var delta = e.GetCurrentPoint(BreakTimeText).Properties.MouseWheelDelta;
                _breakSeconds = Math.Clamp(
                    _breakSeconds + (delta > 0 ? 60 : -60), 60, 30 * 60);
                UpdateDisplay();
                TimersChanged?.Invoke(_focusSeconds, _breakSeconds); // ← auto-save
                e.Handled = true;
            };
        }

        public event Action? SkipRequested;

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            SkipRequested?.Invoke();
        }

        private void UpdateDisplay()
        {
            FocusTimeText.Text = $"{_focusSeconds / 60:D2}:{_focusSeconds % 60:D2}";
            BreakTimeText.Text = $"{_breakSeconds / 60:D2}:{_breakSeconds % 60:D2}";
        }

        // Called while timer is running to show live countdown in popup
        public void UpdateTimers(int remaining, int other, bool isFocus)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                FocusTimeText.Text = isFocus
                    ? $"{remaining / 60:D2}:{remaining % 60:D2}"
                    : $"{_focusSeconds / 60:D2}:{_focusSeconds % 60:D2}";
                BreakTimeText.Text = !isFocus
                    ? $"{remaining / 60:D2}:{remaining % 60:D2}"
                    : $"{_breakSeconds / 60:D2}:{_breakSeconds % 60:D2}";
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartRequested?.Invoke(_focusSeconds, _breakSeconds);
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
            _appWindow.MoveAndResize(new RectInt32(
                _popupX, _popupY,
                MonitorHelper.ToPhysical(270),
                MonitorHelper.ToPhysical(320)));
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}