using LegendBar.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LegendBar.Pages
{
    public sealed partial class WidgetsPage : Page
    {
        private MainWindow? _mainWindow;
        private bool _loading = true;

        public WidgetsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _mainWindow = e.Parameter as MainWindow;
            LoadSettings();
            _loading = false;
        }

        private void LoadSettings()
        {
            var s = SettingsService.Current;
            PinButtonToggle.IsOn = s.ShowPinButton;
            MediaToggle.IsOn = s.ShowMediaWidget;
            PomodoroToggle.IsOn = s.ShowPomodoro;
            PowerToysToggle.IsOn = s.ShowPowerToys;
            NotesToggle.IsOn = s.ShowNotes;
            ClipboardToggle.IsOn = s.ShowClipboard;
            ClockToggle.IsOn = s.ShowClock;
            DateToggle.IsOn = s.ShowDate;
        }

        private void PinButtonToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowPinButton = PinButtonToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void MediaToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowMediaWidget = MediaToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void PomodoroToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowPomodoro = PomodoroToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void PowerToysToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowPowerToys = PowerToysToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void NotesToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowNotes = NotesToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void ClipboardToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowClipboard = ClipboardToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void ClockToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowClock = ClockToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }

        private void DateToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_loading) return;
            SettingsService.Current.ShowDate = DateToggle.IsOn;
            SettingsService.Save();
            _mainWindow?.UpdateWidgetVisibility();
        }
    }
}