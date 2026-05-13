using CommunityToolkit.WinUI.Lottie;
using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using WinRT;

namespace LegendBar
{
    public sealed partial class WeatherPopup : Window
    {
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

        public WeatherPopup()
        {
            InitializeComponent();
            _appWindow = GetAppWindow();
            SetupWindow();

            var data = WeatherService.Current;
            if (data != null)
                PopulateData(data);

            WeatherService.WeatherUpdated += OnWeatherUpdated;

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded) { _loaded = true; return; }
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };

            this.Closed += (s, e) =>
                WeatherService.WeatherUpdated -= OnWeatherUpdated;
        }

        private void OnWeatherUpdated(WeatherData data)
        {
            DispatcherQueue.TryEnqueue(() => PopulateData(data));
        }

        private void PopulateData(WeatherData data)
        {
            var unit = SettingsService.Current.TemperatureUnit == "F" ? "°F" : "°C";
            bool isF = SettingsService.Current.TemperatureUnit == "F";

            // Header
            CityText.Text = string.IsNullOrEmpty(data.CityName) ? "Unknown location" : data.CityName;
            DateTimeText.Text = data.FetchedAt.ToString("dddd, MMMM d · HH:mm");

            // Main temp
            TempText.Text = $"{(int)Math.Round(data.Temperature)}{unit}";
            ConditionText.Text = WeatherService.GetConditionText(data.WeatherCode);
            FeelsLikeText.Text = $"Feels like {(int)Math.Round(data.ApparentTemperature)}{unit}";

            // Main icon
            SetWeatherIcon(MainWeatherIcon, MainWeatherEmoji,
                data.WeatherCode, data.IsDay, 72);

            // Conditions
            HumidityText.Text = $"{data.Humidity:0}%";
            WindText.Text = $"{data.WindSpeed:0.0} km/h {WeatherService.WindDirectionToCompass(data.WindDirection)}";
            UvText.Text = $"{data.UvIndex:0.0}";
            PrecipText.Text = $"{data.Precipitation:0.0} mm";
            VisibilityText.Text = $"{data.Visibility / 1000:0.0} km";
            CloudText.Text = $"{data.CloudCover:0}%";

            // Sunrise/Sunset from today's forecast
            if (data.Forecast.Count > 0)
            {
                var today = data.Forecast[0];
                SunriseText.Text = FormatTime(today.Sunrise);
                SunsetText.Text = FormatTime(today.Sunset);
            }

            // 7-day forecast
            ForecastPanel.Children.Clear();
            foreach (var day in data.Forecast)
            {
                var card = BuildForecastCard(day, isF, unit);
                ForecastPanel.Children.Add(card);
            }

            // Resize window after content populates
            DispatcherQueue.TryEnqueue(() =>
            {
                MainPanel.Measure(new Size(360, double.PositiveInfinity));
                int height = Math.Min((int)MainPanel.DesiredSize.Height + 40, 700);
                var primary = MonitorHelper.Primary;
                int rightEdge = (primary?.LogicalBounds.Left ?? 0) +
                                (primary?.LogicalBounds.Width ?? 1920);
                int popupX = rightEdge - 360 - 24;
                int popupY = SettingsService.Current.BarHeight + 8;
                _appWindow.MoveAndResize(new RectInt32(popupX, popupY, 360, height));
            });
        }

        private void SetWeatherIcon(AnimatedVisualPlayer player,
            TextBlock emoji, int code, bool isDay, double size)
        {
            var iconName = WeatherService.GetIconFileName(code, isDay);
            var fullPath = Path.Combine(
                AppContext.BaseDirectory, "Assets", "Weather", $"{iconName}.json");

            if (File.Exists(fullPath))
            {
                player.Visibility = Visibility.Visible;
                emoji.Visibility = Visibility.Collapsed;
                var src = new LottieVisualSource();
                src.UriSource = new Uri($"ms-appx:///Assets/Weather/{iconName}.json");
                player.Source = src;
            }
            else
            {
                player.Visibility = Visibility.Collapsed;
                emoji.Visibility = Visibility.Visible;
                emoji.Text = WeatherService.GetWeatherEmoji(code);
            }
        }

        private FrameworkElement BuildForecastCard(WeatherDayForecast day,
            bool isF, string unit)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 10, 10, 10),
                MinWidth = 70
            };

            var stack = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Day name
            var dayText = new TextBlock
            {
                Text = day.Date.Date == DateTime.Today
                    ? "Today"
                    : day.Date.ToString("ddd"),
                FontSize = 11,
                Foreground = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(180, 255, 255, 255)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Icon
            var iconPlayer = new AnimatedVisualPlayer
            {
                Width = 32,
                Height = 32,
                AutoPlay = true,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var emojiText = new TextBlock
            {
                FontSize = 24,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            SetWeatherIcon(iconPlayer, emojiText, day.WeatherCode, true, 32);

            // Temp range
            var tempText = new TextBlock
            {
                Text = $"{(int)Math.Round(day.TempMax)}/{(int)Math.Round(day.TempMin)}{unit}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(220, 255, 255, 255)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Precipitation
            var precipText = new TextBlock
            {
                Text = $"{day.PrecipitationSum:0.0}mm",
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 255, 255, 255)),
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(dayText);
            stack.Children.Add(iconPlayer);
            stack.Children.Add(emojiText);
            stack.Children.Add(tempText);
            stack.Children.Add(precipText);
            card.Child = stack;

            return card;
        }

        private static string FormatTime(string isoDateTime)
        {
            if (DateTime.TryParse(isoDateTime, out var dt))
                return dt.ToString("HH:mm");
            return "--:--";
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
            int borderColor = unchecked((int)0xFFFFFFFE);
            DwmSetWindowAttribute(hWnd, 34, ref borderColor, sizeof(int));

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

            // Initial position — will be resized in PopulateData
            var primary = MonitorHelper.Primary;
            int rightEdge = (primary?.LogicalBounds.Left ?? 0) +
                            (primary?.LogicalBounds.Width ?? 1920);
            int popupX = rightEdge - 360 - 24;
            int popupY = SettingsService.Current.BarHeight + 8;
            _appWindow.MoveAndResize(new RectInt32(popupX, popupY, 360, 200));
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}