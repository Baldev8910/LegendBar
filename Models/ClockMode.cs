using System;

namespace LegendBar.Models
{
    public class ClockMode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#00B4D8";
        public string TextColor { get; set; } = "#FFFFFF";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsDaily { get; set; } = true;
        public DateTime? OneTimeDate { get; set; } = null;
        public bool IsActive { get; set; } = false;
    }
}