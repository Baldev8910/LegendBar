using System;
using System.IO;
using System.Text.Json;

namespace LegendBar.Helpers
{
    public class AppSettings
    {
        public float AcrylicTintOpacity { get; set; } = 0.5f;
        public float AcrylicLuminosityOpacity { get; set; } = 0.5f;
        public int BarHeight { get; set; } = 50;
        public double ShowDurationMs { get; set; } = 150;
        public double HideDurationMs { get; set; } = 200;
        public int HideDelayMs { get; set; } = 300;
        public string TemperatureUnit { get; set; } = "C";
        public bool LaunchOnStartup { get; set; } = false;
    }

    public static class SettingsService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LegendBar", "settings.json");

        private static AppSettings? _current;

        public static AppSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current!;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _current = JsonSerializer.Deserialize<AppSettings>(json)
                               ?? new AppSettings();
                }
                else
                {
                    _current = new AppSettings();
                }
            }
            catch
            {
                _current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                var json = JsonSerializer.Serialize(_current,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);

                System.Diagnostics.Debug.WriteLine($"Settings saved to: {_filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            }
        }
    }
}