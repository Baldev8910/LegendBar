using LegendBar.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;
using LegendBar.Popups;

namespace LegendBar.Controls
{
    public sealed partial class PomodoroControl : UserControl
    {
        // State
        private enum PomodoroState { Idle, Focus, Break, Paused }
        private PomodoroState _state = PomodoroState.Idle;
        private bool _isFocus = true;

        // Times (seconds)
        private int _focusSeconds = 45 * 60;
        private int _breakSeconds = 15 * 60;
        private int _remaining = 45 * 60;

        // Timers
        private DispatcherQueueTimer? _timer;
        private DispatcherQueueTimer? _expandTimer;
        private DispatcherQueueTimer? _colorTimer;

        // Animation state
        private double _actionsWidth = 0;
        private const double ActionsExpanded = 52;
        private Color _colorFrom;
        private Color _colorTo;
        private double _colorProgress;

        // Colors
        private SolidColorBrush? _pillBrush;
        private static readonly Color FocusColor = Color.FromArgb(255, 68, 114, 196);
        private static readonly Color BreakColor = Color.FromArgb(255, 50, 170, 100);   
        private static readonly Color PauseColor = Color.FromArgb(255, 196, 68, 68);
        private static readonly Color ClearColor = Color.FromArgb(0, 0, 0, 0);

        // Popup
        private PomodoroPopup? _popup;
        public event Action? PopupOpened;
        public event Action? PopupClosed;

        public int FocusSeconds
        {
            get => _focusSeconds;
            set
            {
                _focusSeconds = value;
                if (_state == PomodoroState.Idle ||
                   (_isFocus && _state == PomodoroState.Paused))
                    _remaining = value;
            }
        }

        public int BreakSeconds
        {
            get => _breakSeconds;
            set
            {
                _breakSeconds = value;
                if (!_isFocus && _state == PomodoroState.Paused)
                    _remaining = value;
            }
        }

        public PomodoroControl()
        {
            InitializeComponent();
            _remaining = _focusSeconds;
            UpdateTimerText();
        }

        // ── Timer logic ────────────────────────────────────────────────────

        public void StartTimer()
        {
            if (_timer == null)
            {
                _timer = DispatcherQueue.CreateTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += Timer_Tick;
            }
            _timer.Start();
            _state = _isFocus ? PomodoroState.Focus : PomodoroState.Break;
            AnimateColor(_isFocus ? FocusColor : BreakColor);
            PausePlayIcon.Glyph = "\uE769";
        }

        private void Timer_Tick(DispatcherQueueTimer sender, object args)
        {
            _remaining--;
            UpdateTimerText();

            if (_remaining <= 0)
            {
                _isFocus = !_isFocus;
                _remaining = _isFocus ? _focusSeconds : _breakSeconds;
                _state = _isFocus ? PomodoroState.Focus : PomodoroState.Break;
                AnimateColor(_isFocus ? FocusColor : BreakColor);
            }
        }

        private void UpdateTimerText()
        {
            int mins = _remaining / 60;
            int secs = _remaining % 60;
            TimerText.Text = $"{mins:D2}:{secs:D2}";
            _popup?.UpdateTimers(_remaining, _isFocus ? _breakSeconds : _focusSeconds, _isFocus);
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

            if (_colorTimer == null)
            {
                _colorTimer = DispatcherQueue.CreateTimer();
                _colorTimer.Interval = TimeSpan.FromMilliseconds(16);
                _colorTimer.Tick += ColorTimer_Tick;
            }
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

        // ── Hover expand/collapse ──────────────────────────────────────────

        private bool _expanding = false;

        private void ExpandActions()
        {
            ActionsPanel.Visibility = Visibility.Visible;
            _expanding = true;
            _expandTimer?.Stop();
            if (_expandTimer == null)
            {
                _expandTimer = DispatcherQueue.CreateTimer();
                _expandTimer.Interval = TimeSpan.FromMilliseconds(16);
                _expandTimer.Tick += ExpandCollapseTimer_Tick;
            }
            _expandTimer.Start();
        }

        private void CollapseActions()
        {
            _expanding = false;
            _expandTimer?.Stop();
            if (_expandTimer == null)
            {
                _expandTimer = DispatcherQueue.CreateTimer();
                _expandTimer.Interval = TimeSpan.FromMilliseconds(16);
                _expandTimer.Tick += ExpandCollapseTimer_Tick;
            }
            _expandTimer.Start();
        }

        private void ExpandCollapseTimer_Tick(DispatcherQueueTimer s, object e)
        {
            if (_expanding)
            {
                _actionsWidth = Math.Min(_actionsWidth + 6, ActionsExpanded);
                PillGrid.ColumnDefinitions[0].Width = new GridLength(_actionsWidth);
                ActionsPanel.Opacity = _actionsWidth / ActionsExpanded;
                if (_actionsWidth >= ActionsExpanded)
                    _expandTimer?.Stop();
            }
            else
            {
                _actionsWidth = Math.Max(_actionsWidth - 6, 0);
                PillGrid.ColumnDefinitions[0].Width = new GridLength(_actionsWidth);
                ActionsPanel.Opacity = _actionsWidth / ActionsExpanded;
                if (_actionsWidth <= 0)
                {
                    ActionsPanel.Visibility = Visibility.Collapsed;
                    ActionsPanel.Opacity = 0;
                    PillGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    _expandTimer?.Stop();
                }
            }
        }

        // ── Hover handlers ─────────────────────────────────────────────────

        private void Root_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_state == PomodoroState.Focus ||
                _state == PomodoroState.Break ||
                _state == PomodoroState.Paused)
                ExpandActions();
        }

        private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CollapseActions();
        }

        // ── Button handlers ────────────────────────────────────────────────

        private void TimerButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"TimerButton_Click fired, state={_state}");
            if (_state == PomodoroState.Idle || _state == PomodoroState.Paused)
                OpenPopup();
        }

        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            if (_state == PomodoroState.Focus || _state == PomodoroState.Break)
            {
                _timer?.Stop();
                _state = PomodoroState.Paused;
                PausePlayIcon.Glyph = "\uE768";
                AnimateColor(PauseColor);
            }
            else if (_state == PomodoroState.Paused)
            {
                _timer?.Start();
                _state = _isFocus ? PomodoroState.Focus : PomodoroState.Break;
                PausePlayIcon.Glyph = "\uE769";
                AnimateColor(_isFocus ? FocusColor : BreakColor);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _remaining = _focusSeconds;
            _isFocus = true;
            _state = PomodoroState.Idle;
            UpdateTimerText();
            CollapseActions();
            AnimateColor(ClearColor);
        }

        // ── Popup ──────────────────────────────────────────────────────────

        public Action? OnBeforePopupOpen;

        private void OpenPopup()
        {
            if (_popup != null) return;
            OnBeforePopupOpen?.Invoke();
            PopupOpened?.Invoke();

            var primary = MonitorHelper.Primary;
            int primaryLeft = primary?.LogicalBounds.Left ?? 0;
            int primaryWidth = primary?.LogicalBounds.Width ?? 1920;
            int popupX = primaryLeft + primaryWidth - 490;
            int popupY = SettingsService.Current.BarHeight + 8;

            _popup = new PomodoroPopup(_focusSeconds, _breakSeconds, popupX, popupY);
            _popup.StartRequested += (focusSecs, breakSecs) =>
            {
                _focusSeconds = focusSecs;
                _breakSeconds = breakSecs;
                _remaining = _isFocus ? focusSecs : breakSecs;
                _popup?.Close();
                StartTimer();
            };
            _popup.Closed += (s, e) =>
            {
                _popup = null;
                PopupClosed?.Invoke();
            };
    
            _popup.TimersChanged += (focusSecs, breakSecs) =>
            {
                _focusSeconds = focusSecs;
                _breakSeconds = breakSecs;
                if (_state == PomodoroState.Idle)
                {
                    _remaining = focusSecs;
                    UpdateTimerText();
                }
                else if (_state == PomodoroState.Paused && _isFocus)
                {
                    _remaining = focusSecs;
                    UpdateTimerText();
                }
                else if (_state == PomodoroState.Paused && !_isFocus)
                {
                    _remaining = breakSecs;
                    UpdateTimerText();
                }
            };

            _popup.SkipRequested += () =>
            {
                _isFocus = !_isFocus;
                _remaining = _isFocus ? _focusSeconds : _breakSeconds;
                _state = _isFocus ? PomodoroState.Focus : PomodoroState.Break;
                UpdateTimerText();
                AnimateColor(_isFocus ? FocusColor : BreakColor);
                PausePlayIcon.Glyph = "\uE769";
                if (_state == PomodoroState.Focus || _state == PomodoroState.Break)
                    _timer?.Start();
            };
            _popup.Activate();
            System.Diagnostics.Debug.WriteLine($"Popup activated, position: {popupX}, {popupY}");

        }

        public void UpdatePopupTimers(int remaining, int other, bool isFocus)
        {
            _popup?.UpdateTimers(remaining, other, isFocus);
        }
    }
}