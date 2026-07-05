using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class AddModePopup : Window
    {
        private readonly ModeService _modeService;
        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private ClockMode? _editMode = null;

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

        // Constructor for Add
        public AddModePopup(ModeService modeService)
        {
            InitializeComponent();
            _modeService = modeService;
            _appWindow = GetAppWindow();
            SetupWindow();
            WireEvents();

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded) { _loaded = true; return; }
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };
        }

        // Constructor for Edit
        public AddModePopup(ModeService modeService, ClockMode editMode)
            : this(modeService)
        {
            _editMode = editMode;
            LoadEditMode(editMode);
        }

        private void LoadEditMode(ClockMode mode)
        {
            NameBox.Text = mode.Name;

            try
            {
                var hex = mode.Color.TrimStart('#');
                byte r = Convert.ToByte(hex[0..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                ModePicker.Color = Color.FromArgb(255, r, g, b);
            }
            catch { }

            StartTimePicker.Time = mode.StartTime;
            EndTimePicker.Time = mode.EndTime;
            RepeatBox.SelectedIndex = mode.IsDaily ? 0 : 1;

            if (!mode.IsDaily && mode.OneTimeDate.HasValue)
            {
                OneTimeDatePicker.Visibility = Visibility.Visible;
                OneTimeDatePicker.Date = mode.OneTimeDate.Value;
            }

            try
            {
                var hex = mode.TextColor.TrimStart('#');
                byte r = Convert.ToByte(hex[0..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                TextColorPicker.Color = Color.FromArgb(255, r, g, b);
            }
            catch { }
        }

        private void WireEvents()
        {
            RepeatBox.SelectionChanged += (s, e) =>
            {
                OneTimeDatePicker.Visibility =
                    RepeatBox.SelectedIndex == 1
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            };

            StartTimePicker.TimeChanged += (s, e) => ValidateOverlap();
            EndTimePicker.TimeChanged += (s, e) => ValidateOverlap();
        }

        private void ValidateOverlap()
        {
            var start = StartTimePicker.Time;
            var end = EndTimePicker.Time;

            if (end <= start)
            {
                OverlapWarning.Text = "End time must be after start time.";
                OverlapWarning.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
                return;
            }

            bool isDaily = RepeatBox.SelectedIndex == 0;
            bool hasOverlap = isDaily &&
                _modeService.HasOverlap(start, end, _editMode?.Id);

            if (hasOverlap)
            {
                OverlapWarning.Text = "This time range overlaps with an existing mode.";
                OverlapWarning.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
            }
            else
            {
                OverlapWarning.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text)) return;

            var c = ModePicker.Color;
            var color = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            var tc = TextColorPicker.Color;
            var textColor = $"#{tc.R:X2}{tc.G:X2}{tc.B:X2}";

            bool isDaily = RepeatBox.SelectedIndex == 0;

            if (_editMode != null)
            {
                _editMode.Name = NameBox.Text.Trim();
                _editMode.Color = color;
                _editMode.TextColor = textColor;
                _editMode.StartTime = StartTimePicker.Time;
                _editMode.EndTime = EndTimePicker.Time;
                _editMode.IsDaily = isDaily;
                _editMode.OneTimeDate = !isDaily
                    ? OneTimeDatePicker.Date.DateTime
                    : null;
                _modeService.Update(_editMode);
            }
            else
            {
                var mode = new ClockMode
                {
                    Name = NameBox.Text.Trim(),
                    Color = color,
                    TextColor = textColor,
                    StartTime = StartTimePicker.Time,
                    EndTime = EndTimePicker.Time,
                    IsDaily = isDaily,
                    OneTimeDate = !isDaily
                        ? OneTimeDatePicker.Date.DateTime
                        : null
                };
                _modeService.Add(mode);
            }

            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
                (primary?.LogicalBounds.Width ?? 1920) / 2 - 200);
            int popupY = MonitorHelper.ToPhysical(SettingsService.Current.BarHeight + 8);
            int popupW = MonitorHelper.ToPhysical(400);
            int popupH = MonitorHelper.ToPhysical(600);
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