using Microsoft.Win32;

namespace LegendBar.Helpers
{
    public static class StartupHelper
    {
        private const string AppName = "LegendBar";
        private const string RegistryKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            return key?.GetValue(AppName) != null;
        }

        public static void EnableStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            var exePath = System.Diagnostics.Process.GetCurrentProcess()
                                .MainModule!.FileName;
            key?.SetValue(AppName, exePath);
        }

        public static void DisableStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            key?.DeleteValue(AppName, false);
        }
    }
}