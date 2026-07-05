using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LegendBar.Popups;

namespace LegendBar.Widgets
{
    public sealed partial class NotesWidget : UserControl
    {
        private NotesPopup? _popup;

        public event System.Action? PopupOpened;
        public event System.Action? PopupClosed;

        public NotesWidget()
        {
            InitializeComponent();
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_popup != null) return;
            PopupOpened?.Invoke();
            _popup = new NotesPopup();
            _popup.Closed += (s, ev) =>
            {
                _popup = null;
                PopupClosed?.Invoke();
            };
            _popup.Activate();
        }
    }
}