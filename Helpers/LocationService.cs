using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace LegendBar.Helpers
{
    public class LocationInfo
    {
        public string City { get; set; } = "Nagpur";
        public double Latitude { get; set; } = 21.15;
        public double Longitude { get; set; } = 79.08;
        public string Country { get; set; } = "India";
    }

    public static class LocationService
    {
        private static readonly HttpClient _httpClient = new();
        private static LocationInfo? _cached;

        public static async Task<LocationInfo> GetLocationAsync()
        {
            if (_cached != null) return _cached;

            try
            {
                // Step 1 — Try Windows Location API (WiFi based, accurate)
                var accessStatus = await Geolocator.RequestAccessAsync();

                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator
                    {
                        DesiredAccuracy = PositionAccuracy.Default
                    };

                    var position = await geolocator.GetGeopositionAsync(
                        maximumAge: TimeSpan.FromMinutes(5),
                        timeout: TimeSpan.FromSeconds(10));

                    double lat = position.Coordinate.Point.Position.Latitude;
                    double lon = position.Coordinate.Point.Position.Longitude;

                    System.Diagnostics.Debug.WriteLine(
                        $"Windows Location API: ({lat}, {lon})");

                    // Step 2 — Reverse geocode to get city name
                    string city = await ReversGeocodeAsync(lat, lon);

                    _cached = new LocationInfo
                    {
                        City = city,
                        Latitude = lat,
                        Longitude = lon,
                        Country = "India"
                    };

                    System.Diagnostics.Debug.WriteLine(
                        $"Location detected: {_cached.City} ({lat}, {lon})");

                    return _cached;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Location access denied: {accessStatus}. Falling back to IP.");
                    return await GetLocationFromIpAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows Location error: {ex.Message}");
                return await GetLocationFromIpAsync();
            }
        }

        private static async Task<string> ReversGeocodeAsync(double lat, double lon)
        {
            try
            {
                // Use Open-Meteo geocoding to get city name from coordinates
                var url = $"https://nominatim.openstreetmap.org/reverse" +
                          $"?lat={lat}&lon={lon}&format=json";

                _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("TopBar/1.0");
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var address = json.RootElement.GetProperty("address");

                // Try city, then town, then county
                if (address.TryGetProperty("city", out var city))
                    return city.GetString() ?? "Unknown";
                if (address.TryGetProperty("town", out var town))
                    return town.GetString() ?? "Unknown";
                if (address.TryGetProperty("county", out var county))
                    return county.GetString() ?? "Unknown";

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static async Task<LocationInfo> GetLocationFromIpAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("http://ip-api.com/json");
                var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                _cached = new LocationInfo
                {
                    City = root.GetProperty("city").GetString() ?? "Nagpur",
                    Latitude = root.GetProperty("lat").GetDouble(),
                    Longitude = root.GetProperty("lon").GetDouble(),
                    Country = root.GetProperty("country").GetString() ?? "India"
                };

                System.Diagnostics.Debug.WriteLine(
                    $"IP Location: {_cached.City} ({_cached.Latitude}, {_cached.Longitude})");

                return _cached;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("IP location failed. Using Nagpur fallback.");
                _cached = new LocationInfo();
                return _cached;
            }
        }
    }
}