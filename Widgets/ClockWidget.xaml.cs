using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI;

namespace LegendBar.Widgets
{
    public sealed partial class ClockWidget : UserControl
    {
        private static readonly HttpClient _http = new();
        private readonly DispatcherQueueTimer _tickTimer;
        private System.Threading.Timer? _syncTimer;
        private TimeSpan _offset = TimeSpan.Zero;

        // Mode state
        private ClockMode? _activeMode = null;
        private bool _isPaused = false;
        private DispatcherQueueTimer? _expandTimer;
        private double _leftWidth = 0;
        private double _rightWidth = 0;
        private bool _expanding = false;
        private bool _pointerInside = false;

        // Color animation
        private SolidColorBrush? _pillBrush;
        private DispatcherQueueTimer? _colorTimer;
        private Color _colorFrom;
        private Color _colorTo;
        private double _colorProgress;
        private static readonly Color ClearColor = Color.FromArgb(0, 0, 0, 0);

        // Mode name brief show timer
        private DispatcherQueueTimer? _modeNameTimer;

        public ClockWidget()
        {
            InitializeComponent();

            _tickTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _tickTimer.Interval = TimeSpan.FromSeconds(1);
            _tickTimer.Tick += (s, e) => UpdateDisplay();
            _tickTimer.Start();
            UpdateDisplay();

            _ = SyncTimeAsync();

            _syncTimer = new System.Threading.Timer(
                async _ => await SyncTimeAsync(),
                null,
                TimeSpan.FromHours(6),
                TimeSpan.FromHours(6));

            Unloaded += (s, e) =>
            {
                _tickTimer.Stop();
                _syncTimer?.Dispose();
            };
        }

        // ── NTP sync ───────────────────────────────────────────────────────

        private async Task SyncTimeAsync()
        {
            try
            {
                var before = DateTime.UtcNow;
                var json = await _http.GetStringAsync("http://worldtimeapi.org/api/ip");
                var after = DateTime.UtcNow;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var datetimeStr = root.GetProperty("datetime").GetString()!;
                var ntpTime = DateTimeOffset.Parse(datetimeStr).UtcDateTime;
                var roundTrip = (after - before) / 2;
                var corrected = ntpTime + roundTrip;
                _offset = corrected - DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Clock] Sync failed: {ex.Message}");
            }
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now + _offset;
            var format = SettingsService.Current.ClockFormat;

            if (_activeMode != null && !string.IsNullOrEmpty(_activeMode.TextColor))
            {
                try
                {
                    var hex = _activeMode.TextColor.TrimStart('#');
                    byte r = Convert.ToByte(hex[0..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);
                    TimeText.Foreground = new SolidColorBrush(
                        Color.FromArgb(255, r, g, b));
                }
                catch { TimeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); }
            }
            else
            {
                TimeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }

            TimeText.Text = now.ToString(format);
        }

        // ── Mode activation ────────────────────────────────────────────────

        public void ActivateMode(ClockMode mode)
        {
            _activeMode = mode;
            _isPaused = false;
            PausePlayIcon.Glyph = "\uE769";

            try
            {
                var hex = mode.Color.TrimStart('#');
                byte r = Convert.ToByte(hex[0..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                AnimateColor(Color.FromArgb(180, r, g, b));
            }
            catch { AnimateColor(Color.FromArgb(180, 0, 180, 216)); }

            ModeNameText.Text = mode.Name;
            ModeNameText.Visibility = Visibility.Visible;
            ShowModeNameBriefly();

            UpdateDisplay();
        }

        public void DeactivateMode()
        {
            _activeMode = null;
            _isPaused = false;
            AnimateColor(ClearColor);
            ModeNameText.Visibility = Visibility.Collapsed;
            ModeNameText.Text = "";
            ModeNameText.Opacity = 0;
            TimeText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            _expanding = false;
            _leftWidth = 0;
            _rightWidth = 0;
            LeftActionsPanel.Visibility = Visibility.Collapsed;
            LeftActionsPanel.Opacity = 0;
            LeftActionsColumn.Width = new GridLength(0);
            RightActionsColumn.Width = new GridLength(0);
            _expandTimer?.Stop();
        }

        private void ShowModeNameBriefly()
        {
            _modeNameTimer?.Stop();
            _modeNameTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _modeNameTimer.Interval = TimeSpan.FromSeconds(3);
            _modeNameTimer.IsRepeating = false;
            _modeNameTimer.Tick += (s, e) =>
            {
                if (!_expanding)
                    ModeNameText.Visibility = Visibility.Collapsed;
                _modeNameTimer?.Stop();
            };
            _modeNameTimer.Start();
        }

        // ── Color animation ────────────────────────────────────────────────

        private void AnimateColor(Color to)
        {
            _colorTimer?.Stop();

            if (_pillBrush == null)
            {
                _pillBrush = new SolidColorBrush(ClearColor);
                PillBackground.Background = _pillBrush;
            }
            else if (PillBackground.Background is SolidColorBrush existing)
            {
                _pillBrush = existing;
            }

            _colorFrom = _pillBrush.Color;
            _colorTo = to;
            _colorProgress = 0;

            _colorTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _colorTimer.Interval = TimeSpan.FromMilliseconds(16);
            _colorTimer.Tick += ColorTimer_Tick;
            _colorTimer.Start();
        }

        private void ColorTimer_Tick(DispatcherQueueTimer s, object e)
        {
            _colorProgress = Math.Min(_colorProgress + 0.05, 1.0);
            double t = _colorProgress < 0.5
                ? 4 * _colorProgress * _colorProgress * _colorProgress
                : 1 - Math.Pow(-2 * _colorProgress + 2, 3) / 2;

            if (_pillBrush != null)
            {
                _pillBrush.Color = Color.FromArgb(
                    (byte)(_colorFrom.A + (_colorTo.A - _colorFrom.A) * t),
                    (byte)(_colorFrom.R + (_colorTo.R - _colorFrom.R) * t),
                    (byte)(_colorFrom.G + (_colorTo.G - _colorFrom.G) * t),
                    (byte)(_colorFrom.B + (_colorTo.B - _colorFrom.B) * t));
            }

            if (_colorProgress >= 1.0)
                _colorTimer?.Stop();
        }

        // ── Hover expand ───────────────────────────────────────────────────

        private void Root_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_activeMode == null) return;
            _pointerInside = true;
            _expanding = true;
            ModeNameText.Text = _activeMode.Name;
            ModeNameText.Visibility = Visibility.Visible;
            LeftActionsPanel.Visibility = Visibility.Visible;
            StartExpandCollapse();
        }

        private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_activeMode == null) return;

            var pos = e.GetCurrentPoint(PillGrid).Position;
            if (pos.X >= 0 && pos.Y >= 0 &&
                pos.X <= PillGrid.ActualWidth &&
                pos.Y <= PillGrid.ActualHeight)
                return;

            _pointerInside = false;
            _expanding = false;
            StartExpandCollapse();
        }

        private void StartExpandCollapse()
        {
            _expandTimer?.Stop();
            _expandTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _expandTimer.Interval = TimeSpan.FromMilliseconds(16);
            _expandTimer.Tick += ExpandCollapseTimer_Tick;
            _expandTimer.Start();
        }

        private double LeftExpanded => Math.Max(ModeNameText.ActualWidth + 16, 60);
        private double RightExpanded => Math.Max(ModeNameText.ActualWidth + 16, 60);

        private void ExpandCollapseTimer_Tick(DispatcherQueueTimer s, object e)
        {
            if (_expanding)
            {
                _leftWidth = Math.Min(_leftWidth + 6, LeftExpanded);
                _rightWidth = Math.Min(_rightWidth + 6, RightExpanded);

                LeftActionsColumn.Width = new GridLength(_leftWidth);
                RightActionsColumn.Width = new GridLength(_rightWidth);

                LeftActionsPanel.Opacity = _leftWidth / LeftExpanded;
                ModeNameText.Opacity = (_rightWidth / RightExpanded) * 0.85;

                if (_leftWidth >= LeftExpanded && _rightWidth >= RightExpanded)
                    _expandTimer?.Stop();
            }
            else
            {
                _leftWidth = Math.Max(_leftWidth - 6, 0);
                _rightWidth = Math.Max(_rightWidth - 6, 0);

                LeftActionsColumn.Width = new GridLength(_leftWidth);
                RightActionsColumn.Width = new GridLength(_rightWidth);

                LeftActionsPanel.Opacity = _leftWidth / LeftExpanded;
                ModeNameText.Opacity = (_rightWidth / RightExpanded) * 0.85;

                if (_leftWidth <= 0 && _rightWidth <= 0)
                {
                    LeftActionsPanel.Visibility = Visibility.Collapsed;
                    ModeNameText.Visibility = Visibility.Collapsed;
                    LeftActionsColumn.Width = new GridLength(0);
                    RightActionsColumn.Width = new GridLength(0);
                    _expandTimer?.Stop();
                }
            }
        }

        // ── Button handlers ────────────────────────────────────────────────

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeMode == null) return;
            _expanding = false;
            StartExpandCollapse();

            var t = DispatcherQueue.GetForCurrentThread().CreateTimer();
            t.Interval = TimeSpan.FromMilliseconds(300);
            t.IsRepeating = false;
            t.Tick += (s, ev) =>
            {
                t.Stop();
                ModeService?.Deactivate();
            };
            t.Start();
        }

        private void PausePlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeMode == null) return;
            _isPaused = !_isPaused;
            PausePlayIcon.Glyph = _isPaused ? "\uE768" : "\uE769";
            ModeService?.SetPaused(_isPaused);

            if (_isPaused)
            {
                AnimateColor(Color.FromArgb(180, 196, 68, 68));
            }
            else
            {
                try
                {
                    var hex = _activeMode.Color.TrimStart('#');
                    byte r = Convert.ToByte(hex[0..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);
                    AnimateColor(Color.FromArgb(180, r, g, b));
                }
                catch { AnimateColor(Color.FromArgb(180, 0, 180, 216)); }
            }

            if (_pointerInside)
            {
                _expanding = true;
                StartExpandCollapse();
            }
        }

        // ── Context menu (left click) ──────────────────────────────────────

        private void TimeText_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(TimeText);
            if (!point.Properties.IsLeftButtonPressed) return;

            var flyout = new MenuFlyout();

            var addItem = new MenuFlyoutItem
            {
                Text = "Add Mode",
                Icon = new FontIcon { Glyph = "\uE710" }
            };
            addItem.Click += (s, ev) => AddModeRequested?.Invoke();

            var viewItem = new MenuFlyoutItem
            {
                Text = "View Modes",
                Icon = new FontIcon { Glyph = "\uE8A1" }
            };
            viewItem.Click += (s, ev) => ViewModesRequested?.Invoke();

            flyout.Items.Add(addItem);
            flyout.Items.Add(viewItem);
            flyout.ShowAt(TimeText);
        }

        // ── Public events for MainWindow wiring ────────────────────────────

        public event Action? AddModeRequested;
        public event Action? ViewModesRequested;
        public ModeService? ModeService { get; set; }
    }
}