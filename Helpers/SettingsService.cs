using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LegendBar.Helpers
{
    public class PinnedItem
    {
        public string Path { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int Order { get; set; } = 0;
    }

    public class AppSettings
    {
        public int SettingsWindowX { get; set; } = 0;
        public int SettingsWindowY { get; set; } = 0;
        public int SettingsWindowWidth { get; set; } = 0;
        public int SettingsWindowHeight { get; set; } = 0;

        public bool IsPinned { get; set; } = false;

        // ── Acrylic / Material ─────────────────────────────────────────────
        public float AcrylicTintOpacity { get; set; } = 0.5f;
        public float AcrylicLuminosityOpacity { get; set; } = 0.5f;

        // Material type: "Acrylic", "MicaAlt", "Mica", "Solid"
        public string MaterialType { get; set; } = "Acrylic";

        // Tint color as hex string e.g. "#141414"
        public string TintColor { get; set; } = "#141414";

        // ── Bar ────────────────────────────────────────────────────────────
        public int BarHeight { get; set; } = 50;

        // ── Animation ─────────────────────────────────────────────────────
        public double ShowDurationMs { get; set; } = 150;
        public double HideDurationMs { get; set; } = 200;
        public int HideDelayMs { get; set; } = 300;

        // ── Misc ───────────────────────────────────────────────────────────
        public string TemperatureUnit { get; set; } = "C";
        public bool LaunchOnStartup { get; set; } = false;

        // ── Widget visibility ──────────────────────────────────────────────
        public bool ShowPinButton { get; set; } = true;
        public bool ShowMediaWidget { get; set; } = true;
        public bool ShowPomodoro { get; set; } = true;
        public bool ShowNotes { get; set; } = true;
        public bool ShowClipboard { get; set; } = true;
        public bool ShowPowerToys { get; set; } = true;
        public bool ShowClock { get; set; } = true;
        public bool ShowDate { get; set; } = true;

        // ── Pins ───────────────────────────────────────────────────────────
        public List<PinnedItem> PinnedItems { get; set; } = new ();

        // ── Clock ──────────────────────────────────────────────────────────
        public string ClockFormat { get; set; } = "HH:mm";

        // ── Weather ──────────────────────────────────────────────────────────
        public bool ShowWeather { get; set; } = true;
    }

    public static class SettingsService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LegendBar", "settings.json");

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private static AppSettings? _current;

        public static AppSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current!;
            }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public static void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
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

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var json = JsonSerializer.Serialize(_current, _jsonOptions);
                File.WriteAllText(_filePath, json);
                System.Diagnostics.Debug.WriteLine($"Settings saved to: {_filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the TintColor hex string to a Windows.UI.Color.
        /// Falls back to #141414 if parsing fails.
        /// </summary>
        public static Windows.UI.Color GetTintColor()
        {
            try
            {
                var hex = Current.TintColor.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex[0..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);
                    return Windows.UI.Color.FromArgb(255, r, g, b);
                }
            }
            catch { }
            return Windows.UI.Color.FromArgb(255, 20, 20, 20);
        }
    }
}