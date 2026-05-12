using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Dispatching;

namespace LegendBar.Widgets
{
    public sealed partial class ClipboardWidget : UserControl
    {
        private DispatcherQueueTimer? _debounceTimer;

        private const int MaxItems = 10;
        private readonly List<ClipboardEntry> _history = new();
        private ClipboardHistoryPopup? _popup;

        public event Action? PopupOpened;
        public event Action? PopupClosed;

        public ClipboardWidget()
        {
            InitializeComponent();
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            this.Unloaded += (s, e) => Clipboard.ContentChanged -= Clipboard_ContentChanged;
        }

        private void Clipboard_ContentChanged(object? sender, object e)
        {
            if (_debounceTimer == null)
            {
                _debounceTimer = DispatcherQueue.CreateTimer();
                _debounceTimer.Interval = TimeSpan.FromMilliseconds(300);
                _debounceTimer.IsRepeating = false;
                _debounceTimer.Tick += Debounce_Tick;
            }
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void Debounce_Tick(DispatcherQueueTimer s, object ev)
        {
            _debounceTimer?.Stop();
            try
            {
                var content = Clipboard.GetContent();
                var entry = new ClipboardEntry();

                if (content.Contains(StandardDataFormats.Text))
                {
                    entry.Text = await content.GetTextAsync();
                    entry.IsImage = false;
                }
                else if (content.Contains(StandardDataFormats.Bitmap))
                {
                    entry.IsImage = true;
                    var streamRef = await content.GetBitmapAsync();
                    var stream = await streamRef.OpenReadAsync();
                    var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bmp.SetSourceAsync(stream);
                    entry.Thumbnail = bmp;
                }
                else return;

                if (_history.Count > 0
                    && !_history[0].IsImage
                    && !entry.IsImage
                    && _history[0].Text == entry.Text) return;

                _history.Insert(0, entry);
                if (_history.Count > MaxItems)
                    _history.RemoveAt(_history.Count - 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Clipboard] ContentChanged error: {ex}");
            }
        }

        private void ClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_popup != null) return;
            PopupOpened?.Invoke();
            _popup = new ClipboardHistoryPopup(_history);
            _popup.Closed += (s, ev) =>
            {
                _popup = null;
                PopupClosed?.Invoke();
            };
            _popup.Activate();
        }
    }

    public class ClipboardEntry
    {
        public string? Text { get; set; }
        public bool IsImage { get; set; }
        public Microsoft.UI.Xaml.Media.Imaging.BitmapImage? Thumbnail { get; set; }
    }

}