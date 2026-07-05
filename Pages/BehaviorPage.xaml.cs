using LegendBar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace LegendBar.Pages
{
    public sealed partial class BehaviorPage : Page
    {
        private MainWindow? _mainWindow;
        private bool _loading = true;

        public BehaviorPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _mainWindow = e.Parameter as MainWindow;
            LoadSettings();
            _loading = false;
        }

        private void LoadSettings()
        {
            var s = SettingsService.Current;

            StartupToggle.IsOn = StartupHelper.IsStartupEnabled();

            // Load monitor mode
            MonitorModeCombo.SelectedIndex = s.BarMonitorMode switch
            {
                "Primary" => 1,
                "Custom" => 2,
                _ => 0
            };

            // Populate monitor list
            MonitorIndexCombo.Items.Clear();
            for (int i = 0; i < MonitorHelper.Monitors.Count; i++)
            {
                var m = MonitorHelper.Monitors[i];
                var label = m.IsPrimary
                    ? $"Monitor {i + 1} (Primary) — {m.PhysicalBounds.Width}×{m.PhysicalBounds.Height}"
                    : $"Monitor {i + 1} — {m.PhysicalBounds.Width}×{m.PhysicalBounds.Height}";
                MonitorIndexCombo.Items.Add(new ComboBoxItem { Content = label, Tag = i });
            }
            MonitorIndexCombo.SelectedIndex = Math.Clamp(
                s.BarMonitorIndex, 0, MonitorHelper.Monitors.Count - 1);

            MonitorIndexCard.Visibility = s.BarMonitorMode == "Custom"
                ? Visibility.Visible : Visibility.Collapsed;

            ShowSpeedSlider.Value = s.ShowDurationMs;
            ShowSpeedLabel.Text = $"{s.ShowDurationMs:0}ms";

            HideSpeedSlider.Value = s.HideDurationMs;
            HideSpeedLabel.Text = $"{s.HideDurationMs:0}ms";

            HideDelaySlider.Value = s.HideDelayMs;
            HideDelayLabel.Text = $"{s.HideDelayMs:0}ms";

            CelsiusRadio.IsChecked = s.TemperatureUnit == "C";
            FahrenheitRadio.IsChecked = s.TemperatureUnit == "F";
        }

        private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            if (StartupToggle.IsOn)
                StartupHelper.EnableStartup();
            else
                StartupHelper.DisableStartup();
        }

        private void ShowSpeedSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            ShowSpeedLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.ShowDurationMs = e.NewValue;
            SettingsService.Save();
            _mainWindow?.UpdateAnimationSpeeds(
                e.NewValue, SettingsService.Current.HideDurationMs);
        }

        private void HideSpeedSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            HideSpeedLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.HideDurationMs = e.NewValue;
            SettingsService.Save();
            _mainWindow?.UpdateAnimationSpeeds(
                SettingsService.Current.ShowDurationMs, e.NewValue);
        }

        private void HideDelaySlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            HideDelayLabel.Text = $"{e.NewValue:0}ms";
            SettingsService.Current.HideDelayMs = (int)e.NewValue;
            SettingsService.Save();
            _mainWindow?.UpdateHideDelay((int)e.NewValue);
        }

        private void TempUnit_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            string unit = CelsiusRadio.IsChecked == true ? "C" : "F";
            SettingsService.Current.TemperatureUnit = unit;
            SettingsService.Save();
        }

        private void MonitorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var tag = (MonitorModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            SettingsService.Current.BarMonitorMode = tag;
            SettingsService.Save();
            MonitorIndexCard.Visibility = tag == "Custom"
                ? Visibility.Visible : Visibility.Collapsed;
            _mainWindow?.UpdateMonitorMode();
        }

        private void MonitorIndexCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var index = (MonitorIndexCombo.SelectedItem as ComboBoxItem)?.Tag is int i ? i : 0;
            SettingsService.Current.BarMonitorIndex = index;
            SettingsService.Save();
            _mainWindow?.UpdateMonitorMode();
        }
    }
}