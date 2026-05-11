using System;
using System.Collections.Generic;
using System.Linq;

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

        // For Weekly — which days to repeat (e.g. [1,3,5] = Mon,Wed,Fri)
        public List<int> DaysOfWeek { get; set; } = new();

        // For Monthly — which day of month (1-31)
        public int DayOfMonth { get; set; } = 1;

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
                RepeatType.Weekly => GetNextWeeklyTrigger(now),
                RepeatType.Monthly => GetNextMonthlyTrigger(now),
                _ => DateTime
            };
        }
        private DateTime GetNextWeeklyTrigger(DateTime now)
        {
            if (DaysOfWeek.Count == 0) return DateTime;
            var time = DateTime.TimeOfDay;
            for (int i = 1; i <= 7; i++)
            {
                var candidate = now.Date.AddDays(i).Add(time);
                if (DaysOfWeek.Contains((int)candidate.DayOfWeek))
                    return candidate;
            }
            return DateTime;
        }

        // For Monthly — specific dates selected on the calendar
        public List<DateTime> SpecificDates { get; set; } = new();
        
        private DateTime GetNextMonthlyTrigger(DateTime now)
        {
            var time = DateTime.TimeOfDay;
            if (SpecificDates.Count > 0)
            {
                // Find the next specific date that hasn't passed yet
                var upcoming = SpecificDates
                    .Select(d => d.Date.Add(time))
                    .Where(d => d > now)
                    .OrderBy(d => d)
                    .FirstOrDefault();
                if (upcoming != default) return upcoming;
                // All dates passed — find earliest next year
                return SpecificDates
                    .Select(d => d.Date.AddYears(1).Add(time))
                    .OrderBy(d => d)
                    .First();
            }
            // Fallback to old DayOfMonth behavior
            var candidate = new DateTime(now.Year, now.Month,
                Math.Min(DayOfMonth, System.DateTime.DaysInMonth(now.Year, now.Month)))
                .Add(time);
            if (candidate > now) return candidate;
            var next = now.AddMonths(1);
            return new DateTime(next.Year, next.Month,
                Math.Min(DayOfMonth, System.DateTime.DaysInMonth(next.Year, next.Month)))
                .Add(time);
        }
    }
}