using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;
using System.IO;
using Windows.Data.Json;
using System.Threading.Tasks;
using Windows.Foundation;

namespace LegendBar.Popups
{
    public sealed partial class PowerToysPopup : Window
    {
        private FileSystemWatcher? _settingsWatcher;

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

        private void InitSettingsWatcher()
        {
            var watchPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "PowerToys");

            _settingsWatcher = new FileSystemWatcher(watchPath, "settings.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _settingsWatcher.Changed += (s, e) =>
            {
                Task.Delay(200).ContinueWith(_ =>
                    DispatcherQueue.TryEnqueue(() => ApplyPowerToysVisibility()));
            };

            this.Closed += (s, e) => _settingsWatcher.Dispose();
        }

        private void ApplyPowerToysVisibility()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "PowerToys", "settings.json"
            );

            if (!File.Exists(path)) return;

            try
            {
                var json = JsonObject.Parse(File.ReadAllText(path));
                var enabled = json["enabled"].GetObject();

                bool Get(string key) =>
                    enabled.ContainsKey(key) && enabled[key].GetBoolean();

                AdvancedPasteSection.Visibility = Get("AdvancedPaste") ? Visibility.Visible : Visibility.Collapsed;
                AlwaysOnTopSection.Visibility = Get("AlwaysOnTop") ? Visibility.Visible : Visibility.Collapsed;
                ColorPickerSection.Visibility = Get("ColorPicker") ? Visibility.Visible : Visibility.Collapsed;
                CommandPaletteSection.Visibility = Get("CmdPal") ? Visibility.Visible : Visibility.Collapsed;
                CropAndLockSection.Visibility = Get("CropAndLock") ? Visibility.Visible : Visibility.Collapsed;
                FancyZonesSection.Visibility = Get("FancyZones") ? Visibility.Visible : Visibility.Collapsed;
                MouseHighlighterSection.Visibility = Get("MouseHighlighter") ? Visibility.Visible : Visibility.Collapsed;
                PeekSection.Visibility = Get("Peek") ? Visibility.Visible : Visibility.Collapsed;
                PowerToysRunSection.Visibility = Get("PowerToys Run") ? Visibility.Visible : Visibility.Collapsed;
                ScreenRulerSection.Visibility = Get("Measure Tool") ? Visibility.Visible : Visibility.Collapsed;
                ShortcutGuideSection.Visibility = Get("Shortcut Guide") ? Visibility.Visible : Visibility.Collapsed;
                TextExtractorSection.Visibility = Get("TextExtractor") ? Visibility.Visible : Visibility.Collapsed;
                WorkspacesSection.Visibility = Get("Workspaces") ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { /* file still being written, skip this cycle */ }
        }

        public PowerToysPopup()
        {
            InitializeComponent();
            _appWindow = GetAppWindow();
            SetupWindow();
            ApplyPowerToysVisibility();
            InitSettingsWatcher();

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded) { _loaded = true; return; }
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

            MainStackPanel.Measure(new Size(480, double.PositiveInfinity));
            int contentHeight = (int)MainStackPanel.DesiredSize.Height - 50;
            var primary = MonitorHelper.Primary;
            int rightEdge = (primary?.LogicalBounds.Left ?? 0) +
                            (primary?.LogicalBounds.Width ?? 1920);
            int popupX = rightEdge - 480 - 24;
            int popupY = SettingsService.Current.BarHeight + 8;
            _appWindow.MoveAndResize(new RectInt32(popupX, popupY, 480, contentHeight));
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}