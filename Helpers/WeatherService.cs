using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Networking.Connectivity;

namespace LegendBar.Helpers
{
    public static class WeatherService
    {
        private static readonly HttpClient _http = new();
        private static WeatherData? _current;
        private static System.Threading.Timer? _refreshTimer;

        public static event Action<WeatherData>? WeatherUpdated;
        public static WeatherData? Current => _current;

        private static bool _wasOffline = false;

        private static void StartNetworkWatcher()
        {
            NetworkInformation.NetworkStatusChanged += async _ =>
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                bool isOnline = profile?.GetNetworkConnectivityLevel()
                                == NetworkConnectivityLevel.InternetAccess;

                if (isOnline && _wasOffline)
                {
                    System.Diagnostics.Debug.WriteLine("[Weather] Internet restored — refreshing.");
                    _wasOffline = false;
                    await RefreshAsync();
                }
                else if (!isOnline)
                {
                    _wasOffline = true;
                    System.Diagnostics.Debug.WriteLine("[Weather] Internet lost.");
                }
            };
        }

        // ── Entry point ────────────────────────────────────────────────────
        public static async Task InitializeAsync()
        {
            // 1 — Load cache instantly so widget shows something immediately
            var cached = WeatherCache.Load();
            if (cached != null)
            {
                var parsed = ParseWeatherJson(cached.RawJson, cached.Latitude, cached.Longitude);
                if (parsed != null)
                {
                    _current = parsed;
                    WeatherUpdated?.Invoke(_current);
                }
            }

            // 2 — Fetch fresh data in background
            await RefreshAsync();

            // 3 — Refresh every 30 minutes
            _refreshTimer = new System.Threading.Timer(
                async _ => await RefreshAsync(),
                null,
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30));

            // 4 — Watch for network restoration
            StartNetworkWatcher();

        }

        public static async Task RefreshAsync()
        {
            try
            {
                var (lat, lon) = await GetLocationAsync();
                if (lat == 0 && lon == 0) return;

                var unit = SettingsService.Current.TemperatureUnit == "F"
                    ? "&temperature_unit=fahrenheit" : "";

                var url = $"https://api.open-meteo.com/v1/forecast" +
                    $"?latitude={lat}&longitude={lon}" +
                    $"&current=temperature_2m,apparent_temperature,relative_humidity_2m," +
                    $"weather_code,wind_speed_10m,wind_direction_10m," +
                    $"precipitation,cloud_cover,visibility,uv_index,is_day" +
                    $"&daily=weather_code,temperature_2m_max,temperature_2m_min," +
                    $"sunrise,sunset,precipitation_sum,uv_index_max" +
                    $"&forecast_days=7&timezone=auto{unit}";

                var response = await _http.GetStringAsync(url);
                WeatherCache.Save(response, lat, lon);

                var data = ParseWeatherJson(response, lat, lon);
                if (data == null) return;

                // Reverse geocode city name
                data.CityName = await GetCityNameAsync(lat, lon);

                _current = data;
                WeatherUpdated?.Invoke(_current);

                System.Diagnostics.Debug.WriteLine(
                    $"[Weather] Refreshed at {DateTime.Now:HH:mm:ss} — {data.Temperature}°, {data.CityName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Weather] Refresh error: {ex.Message}");
            }
        }

        // ── Location ───────────────────────────────────────────────────────
        private static async Task<(double lat, double lon)> GetLocationAsync()
        {
            // Stage 1 — Windows Location API
            try
            {
                var access = await Geolocator.RequestAccessAsync();
                if (access == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator
                    {
                        DesiredAccuracy = PositionAccuracy.Default,
                        MovementThreshold = 500
                    };
                    var position = await geolocator.GetGeopositionAsync(
                        maximumAge: TimeSpan.FromMinutes(10),
                        timeout: TimeSpan.FromSeconds(5));

                    var lat = position.Coordinate.Point.Position.Latitude;
                    var lon = position.Coordinate.Point.Position.Longitude;
                    System.Diagnostics.Debug.WriteLine($"[Weather] Location via Windows API: {lat}, {lon}");
                    return (lat, lon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Weather] Windows Location failed: {ex.Message}");
            }

            // Stage 2 — IP fallback
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://ip-api.com/json");
                request.Headers.Add("User-Agent", "LegendBar/1.0");
                var ipResponse = await _http.SendAsync(request);
                var json = await ipResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("status").GetString() == "success")
                {
                    var lat = root.GetProperty("lat").GetDouble();
                    var lon = root.GetProperty("lon").GetDouble();
                    System.Diagnostics.Debug.WriteLine($"[Weather] Location via IP: {lat}, {lon}");
                    return (lat, lon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Weather] IP location failed: {ex.Message}");
            }

            return (0, 0);
        }

        // ── Reverse geocoding ──────────────────────────────────────────────
        private static async Task<string> GetCityNameAsync(double lat, double lon)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse" +
                    $"?lat={lat}&lon={lon}&format=json";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "LegendBar/1.0");
                var response = await _http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var address = doc.RootElement.GetProperty("address");

                // Try city → town → village → county in order
                foreach (var key in new[] { "city", "town", "village", "county" })
                {
                    if (address.TryGetProperty(key, out var val))
                        return val.GetString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Weather] Geocode error: {ex.Message}");
            }
            return "";
        }

        // ── Parsing ────────────────────────────────────────────────────────
        private static WeatherData? ParseWeatherJson(string json, double lat, double lon)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var current = root.GetProperty("current");
                var daily = root.GetProperty("daily");

                var data = new WeatherData
                {
                    Latitude = lat,
                    Longitude = lon,
                    FetchedAt = DateTime.Now,
                    Temperature = current.GetProperty("temperature_2m").GetDouble(),
                    ApparentTemperature = current.GetProperty("apparent_temperature").GetDouble(),
                    Humidity = current.GetProperty("relative_humidity_2m").GetDouble(),
                    WeatherCode = current.GetProperty("weather_code").GetInt32(),
                    WindSpeed = current.GetProperty("wind_speed_10m").GetDouble(),
                    WindDirection = current.GetProperty("wind_direction_10m").GetDouble(),
                    Precipitation = current.GetProperty("precipitation").GetDouble(),
                    CloudCover = current.GetProperty("cloud_cover").GetDouble(),
                    Visibility = current.GetProperty("visibility").GetDouble(),
                    UvIndex = current.GetProperty("uv_index").GetDouble(),
                    IsDay = current.GetProperty("is_day").GetInt32() == 1,
                };

                // Parse 7-day forecast
                var times = daily.GetProperty("time");
                var codes = daily.GetProperty("weather_code");
                var maxTemps = daily.GetProperty("temperature_2m_max");
                var minTemps = daily.GetProperty("temperature_2m_min");
                var sunrises = daily.GetProperty("sunrise");
                var sunsets = daily.GetProperty("sunset");
                var precip = daily.GetProperty("precipitation_sum");
                var uvMax = daily.GetProperty("uv_index_max");

                for (int i = 0; i < times.GetArrayLength(); i++)
                {
                    data.Forecast.Add(new WeatherDayForecast
                    {
                        Date = DateTime.Parse(times[i].GetString()!),
                        WeatherCode = codes[i].GetInt32(),
                        TempMax = maxTemps[i].GetDouble(),
                        TempMin = minTemps[i].GetDouble(),
                        Sunrise = sunrises[i].GetString() ?? "",
                        Sunset = sunsets[i].GetString() ?? "",
                        PrecipitationSum = precip[i].ValueKind != JsonValueKind.Null
                                           ? precip[i].GetDouble() : 0,
                        UvIndexMax = uvMax[i].ValueKind != JsonValueKind.Null
                                           ? uvMax[i].GetDouble() : 0,
                    });
                }

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Weather] Parse error: {ex.Message}");
                return null;
            }
        }

        // ── Icon mapping ───────────────────────────────────────────────────
        public static string GetIconFileName(int weatherCode, bool isDay)
        {
            return weatherCode switch
            {
                0 => isDay ? "clear-day" : "clear-night",
                1 => isDay ? "clear-day" : "clear-night",
                2 => isDay ? "partly-cloudy-day" : "partly-cloudy-night",
                3 => "overcast",
                45 or 48 => "fog",
                51 or 53 or 55 => "drizzle",
                56 or 57 => "sleet",
                61 or 63 or 65 => "rain",
                66 or 67 => "sleet",
                71 or 73 or 75 => "snow",
                77 => "snow",
                80 or 81 or 82 => "rain",
                85 or 86 => "snow",
                87 or 88 => "hail",
                95 => "thunderstorms",
                96 or 99 => "thunderstorms-rain",
                _ => "not-available"
            };
        }

        public static string GetConditionText(int weatherCode)
        {
            return weatherCode switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 => "Foggy",
                48 => "Icy fog",
                51 => "Light drizzle",
                53 => "Drizzle",
                55 => "Heavy drizzle",
                56 => "Freezing drizzle",
                57 => "Heavy freezing drizzle",
                61 => "Light rain",
                63 => "Rain",
                65 => "Heavy rain",
                66 => "Freezing rain",
                67 => "Heavy freezing rain",
                71 => "Light snow",
                73 => "Snow",
                75 => "Heavy snow",
                77 => "Snow grains",
                80 => "Light showers",
                81 => "Showers",
                82 => "Heavy showers",
                85 => "Snow showers",
                86 => "Heavy snow showers",
                95 => "Thunderstorm",
                96 => "Thunderstorm with hail",
                99 => "Thunderstorm with heavy hail",
                _ => "Unknown"
            };
        }

        public static string WindDirectionToCompass(double degrees)
        {
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return dirs[(int)Math.Round(degrees / 45) % 8];
        }

        public static string GetWeatherEmoji(int weatherCode)
        {
            return weatherCode switch
            {
                0 or 1 => "☀️",
                2 => "⛅",
                3 => "☁️",
                45 or 48 => "🌫️",
                51 or 53 or 55 or 61 or 63 or 65 or 80 or 81 or 82 => "🌧️",
                56 or 57 or 66 or 67 => "🌨️",
                71 or 73 or 75 or 77 or 85 or 86 => "❄️",
                87 or 88 => "🌨️",
                95 or 96 or 99 => "⛈️",
                _ => "🌡️"
            };
        }
    }
}