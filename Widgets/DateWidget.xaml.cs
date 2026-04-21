using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;

namespace LegendBar.Widgets
{
    public sealed partial class DateWidget : UserControl
    {
        private readonly DispatcherQueueTimer _timer;

        public DateWidget()
        {
            InitializeComponent();

            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += (s, e) => UpdateDate();
            _timer.Start();

            UpdateDate();
        }

        private void UpdateDate()
        {
            DateText.Text = DateTime.Now.ToString("dddd, MMMM d");
        }
    }
}