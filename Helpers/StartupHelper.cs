using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace LegendBar.Helpers
{
    public static class StartupHelper
    {
        private const string StartupTaskId = "LegendBarStartupTask";

        public static bool IsStartupEnabled()
        {
            try
            {
                var task = StartupTask.GetAsync(StartupTaskId).AsTask().Result;
                return task.State == StartupTaskState.Enabled;
            }
            catch { return false; }
        }

        public static async void EnableStartup()
        {
            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);
                if (task.State == StartupTaskState.Disabled)
                    await task.RequestEnableAsync();
            }
            catch { }
        }

        public static async void DisableStartup()
        {
            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);
                if (task.State == StartupTaskState.Enabled)
                    task.Disable();
            }
            catch { }
        }
    }
}