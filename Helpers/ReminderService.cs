using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using LegendBar.Models;

namespace LegendBar.Helpers
{
    public class ReminderService
    {
        // CA1869 — cache JsonSerializerOptions instance
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _filePath;
        private readonly List<Reminder> _reminders;
        private readonly DispatcherQueue _dispatcherQueue;
        private DispatcherQueueTimer? _preciseTimer;

        public event Action<Reminder>? ReminderFired;

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public ReminderService(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LegendBar");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "reminders.json");

            _reminders = Load();
            ScheduleNext();
        }

        public List<Reminder> GetAll() => _reminders;

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public void Add(Reminder reminder)
        {
            _reminders.Add(reminder);
            Save();
            ScheduleNext(); // Reschedule in case new reminder is sooner
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        public void Remove(Guid id)
        {
            _reminders.RemoveAll(r => r.Id == id);
            Save();
            ScheduleNext(); // Reschedule after removal
        }

        private void ScheduleNext()
        {
            _preciseTimer?.Stop();

            var now = DateTime.Now;
            DateTime? nearest = null;

            foreach (var reminder in _reminders)
            {
                if (!reminder.IsActive) continue;
                var next = reminder.GetNextTrigger();
                if (next == null || next.Value < now) continue;
                if (nearest == null || next.Value < nearest.Value)
                    nearest = next.Value;
            }

            if (nearest == null) return;

            var delay = nearest.Value - now;
            if (delay.TotalMilliseconds <= 0) return;

            System.Diagnostics.Debug.WriteLine(
                $"[Reminder] Next reminder in {delay.TotalSeconds:F0}s at {nearest.Value:HH:mm:ss}");

            if (_preciseTimer == null)
            {
                _preciseTimer = _dispatcherQueue.CreateTimer();
                _preciseTimer.IsRepeating = false;
                _preciseTimer.Tick += (s, e) =>
                {
                    FireDueReminders();
                    ScheduleNext();
                };
            }
            _preciseTimer.Interval = delay;
            _preciseTimer.Start();
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        private void FireDueReminders()
        {
            var now = DateTime.Now;
            foreach (var reminder in _reminders.ToList())
            {
                if (!reminder.IsActive) continue;
                var next = reminder.GetNextTrigger();
                if (next == null) continue;

                // Fire if within 5 seconds of scheduled time
                if (Math.Abs((now - next.Value).TotalSeconds) <= 5)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Reminder] Firing: {reminder.Title}");
                    ReminderFired?.Invoke(reminder);

                    if (reminder.Repeat == RepeatType.OneTime)
                    {
                        reminder.IsActive = false;
                        Save();
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        private List<Reminder> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return [];
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Reminder>>(json, _jsonOptions) ?? [];
            }
            catch { return []; }
        }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
            "Uses JsonSerializer which may not be trim-safe")]
        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_reminders, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Reminder] Save failed: {ex.Message}");
            }
        }
    }
}