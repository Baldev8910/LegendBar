using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Graphics;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using System.Runtime.InteropServices;

namespace LegendBar
{
    public sealed partial class SettingsPopup : Window
    {
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

        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;

        private readonly MainWindow _mainWindow;
        private bool _loading = true;
        private AppWindow _appWindow;

        public SettingsPopup(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _appWindow = GetAppWindow();

            SetupWindow();
            LoadSettings();
            _loading = false;

            // Close when loses focus
            this.Activated += SettingsPopup_Activated;
        }

        private void SetupWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

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
            _appWindow.TitleBar.SetDragRectangles(new RectInt32[] { });

            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            // Remove window border and shadow BEFORE showing
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));

            int cornerPreference = 1;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

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

            // Position and show LAST
            _appWindow.MoveAndResize(new RectInt32(1920 - 340, 50, 320, 520));
        }

        private void SettingsPopup_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                this.Close();
            }
        }

        private void LoadSettings()
        {
            var s = SettingsService.Current;

            TintSlider.Value = s.AcrylicTintOpacity * 100;
            TintLabel.Text = $"{s.AcrylicTintOpacity * 100:0}%";

            LuminositySlider.Value = s.AcrylicLuminosityOpacity * 100;
            LuminosityLabel.Text = $"{s.AcrylicLuminosityOpacity * 100:0}%";

            ShowSpeedSlider.Value = s.ShowDurationMs;
            ShowSpeedLabel.Text = $"{s.ShowDurationMs:0}ms";

            HideSpeedSlider.Value = s.HideDurationMs;
            HideSpeedLabel.Text = $"{s.HideDurationMs:0}ms";

            HideDelaySlider.Value = s.HideDelayMs;
            HideDelayLabel.Text = $"{s.HideDelayMs:0}ms";

            HeightSlider.Value = s.BarHeight;
            HeightLabel.Text = $"{s.BarHeight}px";

            CelsiusRadio.IsChecked = s.TemperatureUnit == "C";
            FahrenheitRadio.IsChecked = s.TemperatureUnit == "F";

            StartupToggle.IsOn = StartupHelper.IsStartupEnabled();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var defaults = new AppSettings();
            SettingsService.Current.AcrylicTintOpacity = defaults.AcrylicTintOpacity;
            SettingsService.Current.AcrylicLuminosityOpacity = defaults.AcrylicLuminosityOpacity;
            SettingsService.Current.BarHeight = defaults.BarHeight;
            SettingsService.Current.ShowDurationMs = defaults.ShowDurationMs;
            SettingsService.Current.HideDurationMs = defaults.HideDurationMs;
            SettingsService.Current.HideDelayMs = defaults.HideDelayMs;
            SettingsService.Current.TemperatureUnit = defaults.TemperatureUnit;
            SettingsService.Save();
            LoadSettings();
            _mainWindow.SetAcrylicOpacity(defaults.AcrylicTintOpacity,
                defaults.AcrylicLuminosityOpacity);
            _mainWindow.UpdateBarHeight(defaults.BarHeight);
        }

        private void HeightSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            int val = (int)e.NewValue;
            HeightLabel.Text = $"{val}px";
            SettingsService.Current.BarHeight = val;
            SettingsService.Save();
            _mainWindow.UpdateBarHeight(val);
        }

        private void TintSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            float val = (float)(e.NewValue / 100);
            TintLabel.Text = $"{e.NewValue:0}%";
            SettingsService.Current.AcrylicTintOpacity = val;
            SettingsService.Save();
            _mainWindow.SetAcrylicOpacity(val,
                SettingsService.Current.AcrylicLuminosityOpacity);
        }

        private void LuminositySlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            float val = (float)(e.NewValue / 100);
            LuminosityLabel.Text = $"{e.NewValue:0}%";
            SettingsService.Current.AcrylicLuminosityOpacity = val;
            SettingsService.Save();
            _mainWindow.SetAcrylicOpacity(
                SettingsService.Current.AcrylicTintOpacity, val);
        }

        private void ShowSpeedSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            ShowSpeedLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.ShowDurationMs = e.NewValue;
            SettingsService.Save();
            _mainWindow.UpdateAnimationSpeeds(
                e.NewValue, SettingsService.Current.HideDurationMs);
        }

        private void HideSpeedSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            HideSpeedLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.HideDurationMs = e.NewValue;
            SettingsService.Save();
            _mainWindow.UpdateAnimationSpeeds(
                SettingsService.Current.ShowDurationMs, e.NewValue);
        }

        private void HideDelaySlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            HideDelayLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.HideDelayMs = (int)e.NewValue;
            SettingsService.Save();
            _mainWindow.UpdateHideDelay((int)e.NewValue);
        }

        private void TempUnit_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            string unit = CelsiusRadio.IsChecked == true ? "C" : "F";
            SettingsService.Current.TemperatureUnit = unit;
            SettingsService.Save();
        }

        private void Startup_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            if (StartupToggle.IsOn)
                StartupHelper.EnableStartup();
            else
                StartupHelper.DisableStartup();
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}