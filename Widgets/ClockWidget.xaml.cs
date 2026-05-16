using LegendBar.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LegendBar.Widgets
{
    public sealed partial class ClockWidget : UserControl
    {
        private static readonly HttpClient _http = new();
        private readonly DispatcherQueueTimer _tickTimer;
        private System.Threading.Timer? _syncTimer;
        private TimeSpan _offset = TimeSpan.Zero;

        public ClockWidget()
        {
            InitializeComponent();

            // 1 — Start 1-second tick immediately using local time
            _tickTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _tickTimer.Interval = TimeSpan.FromSeconds(1);
            _tickTimer.Tick += (s, e) => UpdateDisplay();
            _tickTimer.Start();
            UpdateDisplay();

            // 2 — Sync with WorldTimeAPI on startup
            _ = SyncTimeAsync();

            // 3 — Re-sync every 6 hours
            _syncTimer = new System.Threading.Timer(
                async _ => await SyncTimeAsync(),
                null,
                TimeSpan.FromHours(6),
                TimeSpan.FromHours(6));

            // 4 — Clean up on unload
            Unloaded += (s, e) =>
            {
                _tickTimer.Stop();
                _syncTimer?.Dispose();
            };
        }

        private async Task SyncTimeAsync()
        {
            try
            {
                var before = DateTime.UtcNow;
                var json = await _http.GetStringAsync("http://worldtimeapi.org/api/ip");
                var after = DateTime.UtcNow;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Parse the datetime string from the API
                var datetimeStr = root.GetProperty("datetime").GetString()!;
                var ntpTime = DateTimeOffset.Parse(datetimeStr).UtcDateTime;

                // Account for network round-trip — use midpoint
                var roundTrip = (after - before) / 2;
                var corrected = ntpTime + roundTrip;

                // Calculate offset between NTP time and local clock
                _offset = corrected - DateTime.UtcNow;

                System.Diagnostics.Debug.WriteLine(
                    $"[Clock] Synced. Offset: {_offset.TotalMilliseconds:0}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Clock] Sync failed: {ex.Message}");
                // Keep existing offset — falls back to local time if _offset is Zero
            }
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now + _offset;
            var format = SettingsService.Current.ClockFormat;
            TimeText.Text = now.ToString(format);
        }
    }
}