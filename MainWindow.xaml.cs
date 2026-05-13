using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT;

namespace LegendBar
{
    public sealed partial class MainWindow : Window
    {
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private int WinX => MonitorHelper.WinX;
        private int WinW => MonitorHelper.WinW;
        private int WinY => MonitorHelper.WinY;

        private bool _isPinned = false;
        private bool _blockWindowPos = false;

        // Reminder system
        private ReminderService? _reminderService;
        private AddReminderPopup? _addReminderPopup;
        private ViewRemindersPopup? _viewRemindersPopup;
        private ReminderNotificationPopup? _notificationPopup;

        // Material controllers
        private DesktopAcrylicController? _acrylicController;
        private MicaController? _micaController;
        private SystemBackdropConfiguration? _configurationSource;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate? _wndProc;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private const uint WM_DPICHANGED = 0x02E0;
        private const uint WM_WINDOWPOSCHANGING = 0x0046;
        private const uint WM_POWERBROADCAST = 0x0218;
        private const uint PBT_APMRESUMEAUTOMATIC = 0x0012;
        private const uint PBT_APMRESUMESUSPEND = 0x0007;

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
                if (msg == WM_POWERBROADCAST &&
                   ((uint)wParam == PBT_APMRESUMEAUTOMATIC ||
                    (uint)wParam == PBT_APMRESUMESUSPEND))
                {
                    if (_isPinned)
                    {
                        // Re-register AppBar after wake
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            var h = WinRT.Interop.WindowNative.GetWindowHandle(this);
                            AppBarHelper.Unregister(h);
                            var t = DispatcherQueue.CreateTimer();
                            t.Interval = TimeSpan.FromMilliseconds(1000);
                            t.IsRepeating = false;
                            t.Tick += (s, e) =>
                            {
                                AppBarHelper.Register(h, SettingsService.Current.BarHeight);
                                _appWindow.MoveAndResize(new RectInt32(
                                    WinX, WinY, WinW, SettingsService.Current.BarHeight));
                                t.Stop();
                            };
                            t.Start();
                        });
                    }
                }
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

        // ── Material system ────────────────────────────────────────────────

        private void ClearMaterial()
        {
            _acrylicController?.RemoveAllSystemBackdropTargets();
            _acrylicController = null;
            _micaController?.RemoveAllSystemBackdropTargets();
            _micaController = null;
            SystemBackdrop = null;
        }

        public void ApplyMaterial()
        {
            ClearMaterial();

            // Always reset background first — needed when switching away from Solid
            if (Content is Grid rootGrid)
                rootGrid.Background = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(0, 0, 0, 0)); // transparent

            _configurationSource ??= new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            var s = SettingsService.Current;
            var target = this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();

            switch (s.MaterialType)
            {
                case "Mica":
                    if (MicaController.IsSupported())
                    {
                        _micaController = new MicaController
                        {
                            Kind = MicaKind.Base
                        };
                        _micaController.AddSystemBackdropTarget(target);
                        _micaController.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;

                case "MicaAlt":
                    if (MicaController.IsSupported())
                    {
                        _micaController = new MicaController
                        {
                            Kind = MicaKind.BaseAlt
                        };
                        _micaController.AddSystemBackdropTarget(target);
                        _micaController.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;

                case "Solid":
                    SystemBackdrop = null;
                    if (Content is Grid solidGrid)
                        solidGrid.Background = new SolidColorBrush(SettingsService.GetTintColor());
                    break;

                default: // "Acrylic"
                    _acrylicController = new DesktopAcrylicController
                    {
                        TintColor = SettingsService.GetTintColor(),
                        TintOpacity = s.AcrylicTintOpacity,
                        LuminosityOpacity = s.AcrylicLuminosityOpacity,
                        Kind = DesktopAcrylicKind.Base
                    };
                    _acrylicController.AddSystemBackdropTarget(target);
                    _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
                    break;
            }
        }

        public void SetAcrylicOpacity(float tintOpacity, float luminosityOpacity)
        {
            if (_acrylicController != null)
            {
                _acrylicController.RemoveAllSystemBackdropTargets();
                _acrylicController.TintOpacity = tintOpacity;
                _acrylicController.LuminosityOpacity = luminosityOpacity;
                _acrylicController.TintColor = SettingsService.GetTintColor();
                _acrylicController.AddSystemBackdropTarget(
                    this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _acrylicController.SetSystemBackdropConfiguration(_configurationSource!);
            }
        }

        public void UpdateMaterial()
        {
            ApplyMaterial();
        }

        public void UpdateTintColor()
        {
            if (_acrylicController != null)
            {
                _acrylicController.RemoveAllSystemBackdropTargets();
                _acrylicController.TintColor = SettingsService.GetTintColor();
                _acrylicController.AddSystemBackdropTarget(
                    this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _acrylicController.SetSystemBackdropConfiguration(_configurationSource!);
            }
            else if (SettingsService.Current.MaterialType == "Solid"
                && Content is Grid rootGrid)
            {
                rootGrid.Background = new SolidColorBrush(SettingsService.GetTintColor());
            }
        }

        public void UpdateWidgetVisibility()
        {
            var s = SettingsService.Current;
            if (PinButton != null)
                PinButton.Visibility = s.ShowPinButton
                    ? Visibility.Visible : Visibility.Collapsed;
            if (MediaWidgetContainer != null)
                MediaWidgetContainer.Visibility = s.ShowMediaWidget
                    ? Visibility.Visible : Visibility.Collapsed;
            if (PomodoroWidgetContainer != null)
                PomodoroWidgetContainer.Visibility = s.ShowPomodoro
                    ? Visibility.Visible : Visibility.Collapsed;
            if (PowerToysButton != null)
                PowerToysButton.Visibility = s.ShowPowerToys
                    ? Visibility.Visible : Visibility.Collapsed;
            if (NotesWidgetContainer != null)
                NotesWidgetContainer.Visibility = s.ShowNotes
                    ? Visibility.Visible : Visibility.Collapsed;
            if (ClipboardWidgetContainer != null)
                ClipboardWidgetContainer.Visibility = s.ShowClipboard
                    ? Visibility.Visible : Visibility.Collapsed;
            if (ClockWidgetContainer != null)
                ClockWidgetContainer.Visibility = s.ShowClock
                    ? Visibility.Visible : Visibility.Collapsed;
            if (DateWidgetContainer != null)
                DateWidgetContainer.Visibility = s.ShowDate
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Context Menu ───────────────────────────────────────────────────

        private void RootGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            PinMenuItem.Text = _isPinned ? "Unpin Bar" : "Pin Bar";
            var flyout = FlyoutBase.GetAttachedFlyout(RootGrid) as MenuFlyout;
            if (flyout != null)
            {
                _autoHide?.SetExternalWindowOpen(true);
                flyout.Closed += (s, ev) => _autoHide?.SetExternalWindowOpen(false);
                flyout.ShowAt(RootGrid, new FlyoutShowOptions
                {
                    Position = e.GetPosition(RootGrid),
                    Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
                });
            }
        }

        private void AddReminder_Click(object sender, RoutedEventArgs e) => OpenAddReminder();
        private void ViewReminders_Click(object sender, RoutedEventArgs e) => OpenViewReminders();
        private void PinMenuItem_Click(object sender, RoutedEventArgs e) => PinButton_Click(sender, e);
        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Exit();

        // ── Reminder methods ───────────────────────────────────────────────

        public void OpenAddReminder()
        {
            if (_addReminderPopup != null) return;
            if (_reminderService == null) return;
            if (!_isPinned) _autoHide?.ForceShow();
            _addReminderPopup = new AddReminderPopup(_reminderService);
            _addReminderPopup.Closed += (s, e) =>
            {
                _addReminderPopup = null;
                _autoHide?.SetExternalWindowOpen(false);
            };
            _autoHide?.SetExternalWindowOpen(true);
            _addReminderPopup.Activate();
        }

        public void OpenViewReminders()
        {
            if (_viewRemindersPopup != null) return;
            if (_reminderService == null) return;
            if (!_isPinned) _autoHide?.ForceShow();
            _viewRemindersPopup = new ViewRemindersPopup(_reminderService, this);
            _viewRemindersPopup.Closed += (s, e) =>
            {
                _viewRemindersPopup = null;
                _autoHide?.SetExternalWindowOpen(false);
            };
            _autoHide?.SetExternalWindowOpen(true);
            _viewRemindersPopup.Activate();
        }

        private void ShowReminderNotification(Reminder reminder)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_isPinned) _autoHide?.ForceShow();
                _notificationPopup?.Close();
                _notificationPopup = new ReminderNotificationPopup(reminder, DispatcherQueue);
                _notificationPopup.Closed += (s, e) =>
                {
                    _notificationPopup = null;
                    if (!_isPinned) _autoHide?.SetExternalWindowOpen(false);
                };
                _autoHide?.SetExternalWindowOpen(true);
                _notificationPopup.Activate();
            });
        }

        // ── Pin button ─────────────────────────────────────────────────────

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _isPinned = !_isPinned;

            if (_isPinned)
            {
                _topmostTimer?.Stop();
                var hWndNotTop = WinRT.Interop.WindowNative.GetWindowHandle(this);
                SetWindowPos(hWndNotTop, HWND_NOTOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                _autoHide?.SetPinned(true);

                _blockWindowPos = true;
                _appWindow.MoveAndResize(new RectInt32(
                    WinX, WinY, WinW, SettingsService.Current.BarHeight));
                AppBarHelper.Register(hWnd, SettingsService.Current.BarHeight);
                _blockWindowPos = false;

                var t = DispatcherQueue.CreateTimer();
                t.Interval = TimeSpan.FromMilliseconds(200);
                t.IsRepeating = false;
                t.Tick += (s, ev) =>
                {
                    _appWindow.MoveAndResize(new RectInt32(
                        WinX, WinY, WinW, SettingsService.Current.BarHeight));
                    var hWnd3 = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    var m = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                    DwmExtendFrameIntoClientArea(hWnd3, ref m);
                    int noShadow2 = 2;
                    DwmSetWindowAttribute(hWnd3, 2, ref noShadow2, sizeof(int));
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
                int noShadow1 = 2;
                DwmSetWindowAttribute(hWnd, 2, ref noShadow1, sizeof(int));
                SetPinIcon(true);
                ToolTipService.SetToolTip(PinButton, "Unpin bar");
            }
            else
            {
                AppBarHelper.Unregister(hWnd);
                _topmostTimer?.Start();
                var hWndTop = WinRT.Interop.WindowNative.GetWindowHandle(this);
                SetWindowPos(hWndTop, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                _autoHide?.SetPinned(false);
                _autoHide?.ForceHide();
                SetPinIcon(false);
                ToolTipService.SetToolTip(PinButton, "Pin bar");
            }

            SettingsService.Current.IsPinned = _isPinned;
            SettingsService.Save();
        }

        private AutoHideHelper? _autoHide;
        private AppWindow _appWindow;
        private DispatcherQueueTimer? _topmostTimer;
        private DispatcherQueueTimer? _fullScreenTimer;
        private bool _isFullScreenAppRunning = false;

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

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses SettingsService.Load which may not be trim-safe")]
        public MainWindow()
        {
            InitializeComponent();
            SettingsService.Load();
            MonitorHelper.Initialize();

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

        public void LoadPins()
        {
            PinsPanel.Children.Clear();

            var pins = SettingsService.Current.PinnedItems
                .OrderBy(p => p.Order)
                .ToList();

            foreach (var pin in pins)
            {
                var button = new Button
                {
                    Background = null,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    Width = 18,
                    Height = 18,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var icon = new Image { Width = 16, Height = 16 };
                button.Content = icon;

                ToolTipService.SetToolTip(button, pin.DisplayName);

                var pinPath = pin.Path;
                button.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = pinPath,
                                UseShellExecute = true
                            });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pins] Launch failed: {ex.Message}");
                    }
                };

                _ = LoadPinIconAsync(pin.Path, icon);
                PinsPanel.Children.Add(button);
            }
        }

        public void UpdatePins()
        {
            LoadPins();
        }

        private async Task LoadPinIconAsync(string path, Image target)
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                var thumbnail = await file.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 32);
                var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bmp.SetSourceAsync(thumbnail);
                target.Source = bmp;
            }
            catch { }
        }

        private void SetupWindow()
        {
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

            int darkMode = 1;
            DwmSetWindowAttribute(hWnd, 20, ref darkMode, sizeof(int));
            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));
            int cornerPreference = 1;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            // ← Move DwmExtendFrameIntoClientArea HERE, after all attribute calls
            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            _appWindow.IsShownInSwitchers = false;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);
            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            _appWindow.Show();

            // Apply material from settings
            _configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };
            ApplyMaterial();

            var hWnd2 = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd2, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            SendMessage(hWnd, WM_ACTIVATE, (IntPtr)WA_ACTIVE, IntPtr.Zero);
            InstallWndProc(hWnd);

            _autoHide = new AutoHideHelper(_appWindow, DispatcherQueue,
                SettingsService.Current.BarHeight);

            _topmostTimer = DispatcherQueue.CreateTimer();
            _topmostTimer.Interval = TimeSpan.FromMilliseconds(500);
            _topmostTimer.IsRepeating = true;
            _topmostTimer.Tick += (s, e) =>
            {
                var h = WinRT.Interop.WindowNative.GetWindowHandle(this);
                SetWindowPos(h, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            };
            _topmostTimer.Start();

            // Initialize reminder service
            _reminderService = new ReminderService(DispatcherQueue);
            _reminderService.ReminderFired += ShowReminderNotification;

            // Wire clipboard popup to prevent auto-hide
            ClipboardWidgetContainer.PopupOpened += () => _autoHide?.SetExternalWindowOpen(true);
            ClipboardWidgetContainer.PopupClosed += () => _autoHide?.SetExternalWindowOpen(false);

            NotesWidgetContainer.PopupOpened += () => _autoHide?.SetExternalWindowOpen(true);
            NotesWidgetContainer.PopupClosed += () => _autoHide?.SetExternalWindowOpen(false);

            PomodoroWidgetContainer.OnBeforePopupOpen += () => _topmostTimer?.Stop();

            PomodoroWidgetContainer.PopupOpened += () =>
            {
                _autoHide?.SetExternalWindowOpen(true);
            };
            PomodoroWidgetContainer.PopupClosed += () =>
            {
                _topmostTimer?.Start();
                _autoHide?.SetExternalWindowOpen(false);
            };

            // Apply widget visibility
            UpdateWidgetVisibility();

            SetPinIcon(false);

            // Restore pinned state
            if (SettingsService.Current.IsPinned)
            {
                // Small delay to let window fully initialize first
                var t = DispatcherQueue.CreateTimer();
                t.Interval = TimeSpan.FromMilliseconds(500);
                t.IsRepeating = false;
                t.Tick += (s, e) =>
                {
                    PinButton_Click(this, new RoutedEventArgs());
                    t.Stop();
                };
                t.Start();
            }

            LoadPowerToysIcon();
            LoadPins();
            //_ = WeatherService.InitializeAsync();
            //WeatherWidgetContainer.OpenPopupRequested += WeatherWidget_OpenPopupRequested;
            StartFullScreenWatcher();
            this.Closed += (s, e) =>
            {
                _autoHide?.Dispose();
                _fullScreenTimer?.Stop();
            };
        }

        private async void LoadPowerToysIcon()
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(
                    @"C:\Users\ADMIN\AppData\Local\PowerToys\PowerToys.exe");
                var thumbnail = await file.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.ListView, 32);
                var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bmp.SetSourceAsync(thumbnail);
                PowerToysIcon.Source = bmp;
            }
            catch { }
        }

        private PowerToysPopup? _powerToysPopup;
        // private WeatherPopup? _weatherPopup;

        //private void WeatherWidget_OpenPopupRequested()
        //{
        //    if (_weatherPopup != null) return;
        //    _autoHide?.SetExternalWindowOpen(true);
        //    _weatherPopup = new WeatherPopup();
        //    _weatherPopup.Closed += (s, e) =>
        //    {
        //        _weatherPopup = null;
        //        _autoHide?.SetExternalWindowOpen(false);
        //    };
        //    _weatherPopup.Activate();
        //}

        private void PowerToysButton_Click(object sender, RoutedEventArgs e)
        {
            if (_powerToysPopup != null) return;
            _autoHide?.SetExternalWindowOpen(true);
            _powerToysPopup = new PowerToysPopup();
            _powerToysPopup.Closed += (s, ev) =>
            {
                _powerToysPopup = null;
                _autoHide?.SetExternalWindowOpen(false);
            };
            _powerToysPopup.Activate();
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
            if (_settingsWindow == null)
                _autoHide?.ForceHide();
        }

        public void UpdateAnimationSpeeds(double showMs, double hideMs)
            => _autoHide?.UpdateSpeeds(showMs, hideMs);

        public void UpdateHideDelay(int delayMs)
            => _autoHide?.UpdateHideDelay(delayMs);

        private void StartFullScreenWatcher()
        {
            _fullScreenTimer = DispatcherQueue.CreateTimer();
            _fullScreenTimer.Interval = TimeSpan.FromMilliseconds(2000);
            _fullScreenTimer.IsRepeating = true;
            _fullScreenTimer.Tick += (s, e) =>
            {
                bool isFullScreen = IsFullScreenAppRunning();

                if (isFullScreen && !_isFullScreenAppRunning)
                {
                    _isFullScreenAppRunning = true;
                    _topmostTimer?.Stop();
                    _autoHide?.ForceHide();
                    _autoHide?.SetFullScreenMode(true);
                    System.Diagnostics.Debug.WriteLine("[FullScreen] Full-screen app detected, backing off.");
                }
                else if (!isFullScreen && _isFullScreenAppRunning)
                {
                    _isFullScreenAppRunning = false;
                    _topmostTimer?.Start();
                    _autoHide?.SetFullScreenMode(false);
                    var h = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    SetWindowPos(h, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    System.Diagnostics.Debug.WriteLine("[FullScreen] Full-screen app gone, reinstating.");
                }
            };
            _fullScreenTimer.Start();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;

        private bool IsFullScreenAppRunning()
        {
            try
            {
                var foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero) return false;

                // Ignore our own window
                var ownHwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (foreground == ownHwnd) return false;

                // Ignore desktop window
                var className = new System.Text.StringBuilder(256);
                GetClassName(foreground, className, 256);
                var cn = className.ToString();
                if (cn == "Progman" || cn == "WorkerW" || cn == "Shell_TrayWnd")
                    return false;

                // Get primary monitor bounds
                var primary = MonitorHelper.Primary;
                if (primary == null) return false;

                int screenW = primary.PhysicalBounds.Width;
                int screenH = primary.PhysicalBounds.Height;
                int screenX = primary.PhysicalBounds.Left;
                int screenY = primary.PhysicalBounds.Top;

                // Get foreground window rect
                if (!GetWindowRect(foreground, out RECT r)) return false;

                int winW = r.Right - r.Left;
                int winH = r.Bottom - r.Top;

                // Must cover entire primary monitor
                if (winW < screenW || winH < screenH || r.Left > screenX || r.Top > screenY)
                    return false;

                // Must have no caption/title bar — rules out maximized browsers
                int style = GetWindowLong(foreground, GWL_STYLE);
                if ((style & WS_CAPTION) != 0)
                    return false;

                return true;
            }
            catch { return false; }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
            => OpenSettings();

        private SettingsWindow? _settingsWindow;

        public void OpenSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }
            _topmostTimer?.Stop(); // ← add this
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (s, e) =>
            {
                _settingsWindow = null;
                _topmostTimer?.Start(); // ← and this
                _autoHide?.SetExternalWindowOpen(false);
            };
            _autoHide?.SetExternalWindowOpen(true);
            _settingsWindow.Activate();
        }
    }
}