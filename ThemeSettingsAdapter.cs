using LegendBar.Extensibility;
using LegendBar.Helpers;

namespace LegendBar
{
    public class ThemeSettingsAdapter : IThemeSettingsProvider
    {
        public string MaterialType => SettingsService.Current.MaterialType;
        public double AcrylicTintOpacity => SettingsService.Current.AcrylicTintOpacity;
        public double AcrylicLuminosityOpacity => SettingsService.Current.AcrylicLuminosityOpacity;
    }
}