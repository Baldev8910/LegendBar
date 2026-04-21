using System;
using System.IO;
using System.Text.Json;

namespace LegendBar.Helpers
{
    public class WeatherCacheData
    {
        public string RawJson { get; set; } = "";
        public DateTime CachedAt { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public static class WeatherCache
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TopBar", "weather_cache.json");

        public static void Save(string rawJson, double lat, double lon)
        {
            try
            {
                var cache = new WeatherCacheData
                {
                    RawJson = rawJson,
                    CachedAt = DateTime.Now,
                    Latitude = lat,
                    Longitude = lon
                };

                var json = JsonSerializer.Serialize(cache,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);

                System.Diagnostics.Debug.WriteLine(
                    $"Weather cached at {cache.CachedAt:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache save error: {ex.Message}");
            }
        }

        public static WeatherCacheData? Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return null;

                var json = File.ReadAllText(_filePath);
                var cache = JsonSerializer.Deserialize<WeatherCacheData>(json);

                if (cache == null) return null;

                // Only use cache if less than 1 hour old
                if ((DateTime.Now - cache.CachedAt).TotalHours > 1)
                {
                    System.Diagnostics.Debug.WriteLine("Cache expired.");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"Cache loaded from {cache.CachedAt:HH:mm:ss}");
                return cache;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache load error: {ex.Message}");
                return null;
            }
        }

        public static bool IsValid()
        {
            var cache = Load();
            return cache != null;
        }
    }
}