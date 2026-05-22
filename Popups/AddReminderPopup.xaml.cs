using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;

namespace LegendBar.Popups
{
    public sealed partial class AddReminderPopup : Window
    {
        private readonly List<DateTime> _selectedMonthlyDates = new();
        public DateTimeOffset Today { get; } = DateTimeOffset.Now.Date;

        private readonly ReminderService _reminderService;
        private AppWindow _appWindow;

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

        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private MicaController? _micaController;

        private readonly List<int> _selectedDays = new();
        private readonly List<Button> _dayButtons = new();

        private void MonthlyCalendar_SelectedDatesChanged(CalendarView sender,
            CalendarViewSelectedDatesChangedEventArgs args)
        {
            foreach (var d in args.AddedDates)
                _selectedMonthlyDates.Add(d.UtcDateTime.Date);
            foreach (var d in args.RemovedDates)
                _selectedMonthlyDates.RemoveAll(x => x.Date == d.UtcDateTime.Date);
        }

        public AddReminderPopup(ReminderService reminderService)
        {
            InitializeComponent();
            _reminderService = reminderService;
            _appWindow = GetAppWindow();
            SetupWindow();

            // Default date/time to now + 1 hour
            var nowPlusHour = DateTimeOffset.Now.AddHours(1);
            DatePicker.Date = nowPlusHour;
            HourBox.Value = nowPlusHour.Hour;
            MinuteBox.Value = DateTimeOffset.Now.Minute;

            HourBox.PointerWheelChanged += (s, e) =>
            {
                var delta = e.GetCurrentPoint(HourBox).Properties.MouseWheelDelta;
                HourBox.Value = Math.Clamp(HourBox.Value + (delta > 0 ? 1 : -1), 0, 23);
                e.Handled = true;
            };

            MinuteBox.PointerWheelChanged += (s, e) =>
            {
                var delta = e.GetCurrentPoint(MinuteBox).Properties.MouseWheelDelta;
                MinuteBox.Value = Math.Clamp(MinuteBox.Value + (delta > 0 ? 1 : -1), 0, 59);
                e.Handled = true;
            };

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded)
                {
                    _loaded = true;
                    return; // ignore the first activation event
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
                        TintColor = SettingsService.GetTintColor(),
                        TintOpacity = SettingsService.Current.AcrylicTintOpacity,
                        LuminosityOpacity = SettingsService.Current.AcrylicLuminosityOpacity,
                        Kind = DesktopAcrylicKind.Base
                    };
                    _acrylicController.AddSystemBackdropTarget(target);
                    _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
                    break;
            }

            var primary = MonitorHelper.Primary;
            int centerX = MonitorHelper.ToPhysical(
                (primary?.LogicalBounds.Left ?? 0) +
                (primary?.LogicalBounds.Width ?? 1920) / 2 - 210);
            int popupY = MonitorHelper.ToPhysical(SettingsService.Current.BarHeight + 8);
            int popupW = MonitorHelper.ToPhysical(420);
            int popupH = MonitorHelper.ToPhysical(500);
            _appWindow.MoveAndResize(new RectInt32(centerX, popupY, popupW, popupH));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text)) return;
            var repeat = RepeatBox.SelectedIndex switch
            {
                1 => RepeatType.Daily,
                2 => RepeatType.Weekly,
                3 => RepeatType.Monthly,
                _ => RepeatType.OneTime
            };

            if (repeat != RepeatType.Monthly && DatePicker.Date == null) return;
            if (repeat == RepeatType.Monthly && _selectedMonthlyDates.Count == 0) return;

            var date = repeat == RepeatType.Monthly
                ? _selectedMonthlyDates[0].Date
                : DatePicker.Date!.Value.Date;
            var time = new TimeSpan((int)HourBox.Value, (int)MinuteBox.Value, 0);
            var dateTime = date + time;

            var reminder = new Reminder
            {
                Title = TitleBox.Text.Trim(),
                DateTime = dateTime,
                Repeat = repeat,
                IsActive = true,
                DaysOfWeek = repeat == RepeatType.Weekly ? new List<int>(_selectedDays) : new(),
                DayOfMonth = 1,
                SpecificDates = repeat == RepeatType.Monthly ? new List<DateTime>(_selectedMonthlyDates) : new()
            };

            _reminderService.Add(reminder);
            this.Close();
        }

        private void RepeatBox_Changed(object _sender, SelectionChangedEventArgs _e)
        {
            if (WeeklyPanel == null || MonthlyPanel == null) return;
            bool isWeekly = RepeatBox.SelectedIndex == 2;
            bool isMonthly = RepeatBox.SelectedIndex == 3;
            WeeklyPanel.Visibility = isWeekly ? Visibility.Visible : Visibility.Collapsed;
            MonthlyPanel.Visibility = isMonthly ? Visibility.Visible : Visibility.Collapsed;

            var primary = MonitorHelper.Primary;
            int centerX = MonitorHelper.ToPhysical(
                (primary?.LogicalBounds.Left ?? 0) +
                (primary?.LogicalBounds.Width ?? 1920) / 2 - 210);
            int popupY = MonitorHelper.ToPhysical(SettingsService.Current.BarHeight + 8);
            int height = isMonthly ? 780 : 500;
            _appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                centerX, popupY,
                MonitorHelper.ToPhysical(420),
                MonitorHelper.ToPhysical(height)));
        }

        private void DayButton_Click(object sender, RoutedEventArgs _e)
        {
            if (sender is not Button btn) return;
            int day = int.Parse(btn.Tag.ToString()!);

            if (_selectedDays.Contains(day))
            {
                _selectedDays.Remove(day);
                btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(34, 255, 255, 255));
            }
            else
            {
                _selectedDays.Add(day);
                btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 68, 114, 196));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}