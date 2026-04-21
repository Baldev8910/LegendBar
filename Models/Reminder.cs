using System;

namespace LegendBar.Models
{
    public enum RepeatType
    {
        OneTime,
        Daily,
        Weekly,
        Monthly
    }

    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public DateTime DateTime { get; set; }
        public RepeatType Repeat { get; set; } = RepeatType.OneTime;
        public bool IsActive { get; set; } = true;

        // Calculate next trigger time based on repeat type
        public DateTime? GetNextTrigger()
        {
            if (!IsActive) return null;

            var now = DateTime.Now;

            // Always return DateTime for one-time — let the service decide if it's too late
            if (Repeat == RepeatType.OneTime) return DateTime;

            if (DateTime > now) return DateTime;

            return Repeat switch
            {
                RepeatType.Daily => DateTime.AddDays(
                    Math.Ceiling((now - DateTime).TotalDays)),
                RepeatType.Weekly => DateTime.AddDays(
                    Math.Ceiling((now - DateTime).TotalDays / 7) * 7),
                RepeatType.Monthly => DateTime.AddMonths(
                    (int)Math.Ceiling((now - DateTime).TotalDays / 30)),
                _ => DateTime
            };
        }
    }
}