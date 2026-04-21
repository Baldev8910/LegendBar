using System;
using LegendBar.Models;

namespace LegendBar.Models
{
    public class ReminderViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string DateTimeDisplay { get; set; } = "";
        public string RepeatDisplay { get; set; } = "";
        public Reminder OriginalReminder { get; set; } = null!;

        public static ReminderViewModel FromReminder(Reminder r) => new()
        {
            Id = r.Id,
            Title = r.Title,
            DateTimeDisplay = r.DateTime.ToString("HH:mm  —  ddd, MMM d, yyyy"),
            RepeatDisplay = r.Repeat switch
            {
                RepeatType.Daily => "🔁 Daily",
                RepeatType.Weekly => "🔁 Weekly",
                RepeatType.Monthly => "🔁 Monthly",
                _ => "one-time"
            },
            OriginalReminder = r
        };
    }
}