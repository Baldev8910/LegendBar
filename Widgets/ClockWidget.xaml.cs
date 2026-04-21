using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;

namespace LegendBar.Widgets
{
    public sealed partial class ClockWidget : UserControl
    {
        private readonly DispatcherQueueTimer _timer;

        public ClockWidget()
        {
            InitializeComponent();

            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateClock();
            _timer.Start();

            UpdateClock();
        }

        private void UpdateClock()
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm");
        }
    }
}