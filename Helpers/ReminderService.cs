using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using LegendBar.Models;

namespace LegendBar.Helpers
{
    public class ReminderService
    {
        private readonly string _filePath;
        private readonly DispatcherTimer _timer;
        private List<Reminder> _reminders;

        public event Action<Reminder>? ReminderFired;

        public ReminderService()
        {
            // Save reminders in AppData
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TopBar");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "reminders.json");

            _reminders = Load();

            // Check every 30 seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += CheckReminders;
            _timer.Start();
        }

        public List<Reminder> GetAll() => _reminders;

        public void Add(Reminder reminder)
        {
            _reminders.Add(reminder);
            Save();
        }

        public void Remove(Guid id)
        {
            _reminders.RemoveAll(r => r.Id == id);
            Save();
        }

        private readonly HashSet<Guid> _firedToday = new();

        private void CheckReminders(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"[{now:HH:mm:ss}] Checking {_reminders.Count} reminders...");

            foreach (var reminder in _reminders)
            {
                System.Diagnostics.Debug.WriteLine($"  Reminder: '{reminder.Title}' | Active: {reminder.IsActive} | DateTime: {reminder.DateTime:HH:mm:ss}");

                if (!reminder.IsActive) continue;

                var next = reminder.GetNextTrigger();
                System.Diagnostics.Debug.WriteLine($"  Next trigger: {next}");

                if (next == null) continue;

                bool isDue = now >= next.Value;
                bool notFiredYet = !_firedToday.Contains(reminder.Id);
                bool notTooLate = (now - next.Value).TotalMinutes <= 2;

                System.Diagnostics.Debug.WriteLine($"  isDue: {isDue} | notFiredYet: {notFiredYet} | notTooLate: {notTooLate}");

                if (isDue && notFiredYet && notTooLate)
                {
                    _firedToday.Add(reminder.Id);
                    ReminderFired?.Invoke(reminder);

                    if (reminder.Repeat == RepeatType.OneTime)
                    {
                        reminder.IsActive = false;
                        Save();
                    }
                }
            }
        }

        private List<Reminder> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<Reminder>();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Reminder>>(json)
                       ?? new List<Reminder>();
            }
            catch { return new List<Reminder>(); }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_reminders,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}