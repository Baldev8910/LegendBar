using LegendBar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;

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
    }
}