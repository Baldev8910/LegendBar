using CommunityToolkit.WinUI.Lottie;
using LegendBar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LegendBar.Widgets
{
    public sealed partial class WeatherWidget : UserControl
    {
        public event Action? OpenPopupRequested;

        public WeatherWidget()
        {
            InitializeComponent();

            // Show current data if already available
            if (WeatherService.Current != null)
                UpdateDisplay(WeatherService.Current);

            // Subscribe to future updates
            WeatherService.WeatherUpdated += OnWeatherUpdated;

            Unloaded += (s, e) =>
                WeatherService.WeatherUpdated -= OnWeatherUpdated;
        }

        private void OnWeatherUpdated(WeatherData data)
        {
            DispatcherQueue.TryEnqueue(() => UpdateDisplay(data));
        }

        private void UpdateDisplay(WeatherData data)
        {
            var unit = SettingsService.Current.TemperatureUnit == "F" ? "°F" : "°C";
            TempText.Text = $"{(int)Math.Round(data.Temperature)}{unit}";

            var iconName = WeatherService.GetIconFileName(data.WeatherCode, data.IsDay);
            var iconPath = $"ms-appx:///Assets/Weather/{iconName}.json";
            var fullPath = Path.Combine(
                AppContext.BaseDirectory, "Assets", "Weather", $"{iconName}.json");

            if (File.Exists(fullPath))
            {
                // Lottie file exists — show animation
                WeatherPlayer.Visibility = Visibility.Visible;
                EmojiText.Visibility = Visibility.Collapsed;

                var source = new LottieVisualSource();
                source.UriSource = new Uri(iconPath);
                WeatherPlayer.Source = source;
            }
            else
            {
                // Fallback to emoji
                WeatherPlayer.Visibility = Visibility.Collapsed;
                EmojiText.Visibility = Visibility.Visible;
                EmojiText.Text = WeatherService.GetWeatherEmoji(data.WeatherCode);
            }
        }

        private void RootButton_Click(object sender, RoutedEventArgs e)
        {
            OpenPopupRequested?.Invoke();
        }
    }
}