using LegendBar.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LegendBar.Helpers
{
    public class ModeService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LegendBar", "modes.json");

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private List<ClockMode> _modes = new();
        private ClockMode? _activeMode = null;
        private readonly DispatcherQueue _dispatcherQueue;

        private DispatcherQueueTimer? _startTimer;
        private DispatcherQueueTimer? _endTimer;

        public event Action<ClockMode>? ModeActivated;
        public event Action? ModeDeactivated;

        public ClockMode? ActiveMode => _activeMode;

        public ModeService(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            Load();
            ScheduleNextMode();
        }

        // ── Load / Save ────────────────────────────────────────────────────

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _modes = JsonSerializer.Deserialize<List<ClockMode>>(json, _jsonOptions)
                             ?? new List<ClockMode>();
                }
                else
                {
                    _modes = new List<ClockMode>();
                }
            }
            catch
            {
                _modes = new List<ClockMode>();
            }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var json = JsonSerializer.Serialize(_modes, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModeService] Save failed: {ex.Message}");
            }
        }

        // ── CRUD ───────────────────────────────────────────────────────────

        public List<ClockMode> GetAll() => _modes.ToList();

        public void Add(ClockMode mode)
        {
            _modes.Add(mode);
            Save();
            ScheduleNextMode();
        }

        public void Update(ClockMode mode)
        {
            var existing = _modes.FirstOrDefault(m => m.Id == mode.Id);
            if (existing == null) return;
            var index = _modes.IndexOf(existing);
            _modes[index] = mode;
            Save();
            ScheduleNextMode();
        }

        public void Remove(Guid id)
        {
            var mode = _modes.FirstOrDefault(m => m.Id == id);
            if (mode == null) return;
            if (_activeMode?.Id == id)
                Deactivate();
            _modes.Remove(mode);
            Save();
            ScheduleNextMode();
        }

        // ── Overlap detection ──────────────────────────────────────────────

        public bool HasOverlap(TimeSpan start, TimeSpan end, Guid? excludeId = null)
        {
            foreach (var mode in _modes)
            {
                if (excludeId.HasValue && mode.Id == excludeId.Value) continue;
                if (!mode.IsDaily) continue;
                if (start < mode.EndTime && end > mode.StartTime)
                    return true;
            }
            return false;
        }

        // ── Activation ─────────────────────────────────────────────────────

        public void Activate(ClockMode mode)
        {
            if (_activeMode != null)
                Deactivate();

            _activeMode = mode;
            _activeMode.IsActive = true;
            Save();

            _dispatcherQueue.TryEnqueue(() => ModeActivated?.Invoke(_activeMode));

            var now = DateTime.Now;
            var endToday = now.Date + mode.EndTime;
            if (endToday <= now) endToday = endToday.AddDays(1);
            var msUntilEnd = (endToday - now).TotalMilliseconds;

            _endTimer?.Stop();
            _endTimer = _dispatcherQueue.CreateTimer();
            _endTimer.Interval = TimeSpan.FromMilliseconds(msUntilEnd);
            _endTimer.IsRepeating = false;
            _endTimer.Tick += (s, e) =>
            {
                _endTimer?.Stop();
                Deactivate();
                ScheduleNextMode();
            };
            _endTimer.Start();
        }

        public void Deactivate()
        {
            if (_activeMode == null) return;
            _activeMode.IsActive = false;
            _activeMode = null;
            Save();
            _endTimer?.Stop();
            _dispatcherQueue.TryEnqueue(() => ModeDeactivated?.Invoke());
        }

        public void ManualActivate(ClockMode mode)
        {
            Activate(mode);
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                _endTimer?.Stop();
                _startTimer?.Stop();
            }
            else
            {
                if (_activeMode != null)
                    Activate(_activeMode);
                else
                    ScheduleNextMode();
            }
        }

        // ── Scheduling ─────────────────────────────────────────────────────

        private void ScheduleNextMode()
        {
            _startTimer?.Stop();

            var now = DateTime.Now;
            var todayTime = now.TimeOfDay;

            foreach (var mode in _modes)
            {
                if (mode.IsDaily)
                {
                    if (todayTime >= mode.StartTime && todayTime < mode.EndTime)
                    {
                        if (_activeMode?.Id != mode.Id)
                            Activate(mode);
                        return;
                    }
                }
                else if (mode.OneTimeDate.HasValue)
                {
                    var modeDate = mode.OneTimeDate.Value.Date;
                    if (modeDate == now.Date &&
                        todayTime >= mode.StartTime && todayTime < mode.EndTime)
                    {
                        if (_activeMode?.Id != mode.Id)
                            Activate(mode);
                        return;
                    }
                }
            }

            ClockMode? nextMode = null;
            double minMs = double.MaxValue;

            foreach (var mode in _modes)
            {
                double msUntilStart;

                if (mode.IsDaily)
                {
                    var startToday = now.Date + mode.StartTime;
                    if (startToday <= now)
                        startToday = startToday.AddDays(1);
                    msUntilStart = (startToday - now).TotalMilliseconds;
                }
                else if (mode.OneTimeDate.HasValue)
                {
                    var startDateTime = mode.OneTimeDate.Value.Date + mode.StartTime;
                    if (startDateTime <= now) continue;
                    msUntilStart = (startDateTime - now).TotalMilliseconds;
                }
                else continue;

                if (msUntilStart < minMs)
                {
                    minMs = msUntilStart;
                    nextMode = mode;
                }
            }

            if (nextMode == null) return;

            _startTimer = _dispatcherQueue.CreateTimer();
            _startTimer.Interval = TimeSpan.FromMilliseconds(minMs);
            _startTimer.IsRepeating = false;
            _startTimer.Tick += (s, e) =>
            {
                _startTimer?.Stop();
                Activate(nextMode);
            };
            _startTimer.Start();

            System.Diagnostics.Debug.WriteLine(
                $"[ModeService] Next mode '{nextMode.Name}' in {minMs / 1000:0}s");
        }
    }
}