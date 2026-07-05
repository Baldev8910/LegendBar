using LegendBar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace LegendBar.Pages
{
    public sealed partial class PinsPage : Page
    {
        private MainWindow? _mainWindow;

        public PinsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _mainWindow = e.Parameter as MainWindow;
            RebuildList();
        }

        private void RebuildList()
        {
            var pins = SettingsService.Current.PinnedItems
                .OrderBy(p => p.Order)
                .ToList();

            PinCountLabel.Text = $"{pins.Count} / 10 pins used";
            AddPinButton.IsEnabled = pins.Count < 10;

            PinListPanel.Children.Clear();

            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                var isFirst = i == 0;
                var isLast = i == pins.Count - 1;

                var cornerRadius = (isFirst && isLast) ? new CornerRadius(8)
                    : isFirst ? new CornerRadius(8, 8, 0, 0)
                    : isLast ? new CornerRadius(0, 0, 8, 8)
                    : new CornerRadius(0);

                var border = new Border
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = cornerRadius,
                    Padding = new Thickness(16, 12, 16, 12)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Icon
                var icon = new Image
                {
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = LoadIconAsync(pin.Path, icon);
                Grid.SetColumn(icon, 0);

                // Name + path
                var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                nameStack.Children.Add(new TextBlock
                {
                    Text = pin.DisplayName,
                    FontSize = 13
                });
                nameStack.Children.Add(new TextBlock
                {
                    Text = pin.Path,
                    FontSize = 11,
                    Opacity = 0.5,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(nameStack, 1);

                // Move Up button
                var upButton = new Button
                {
                    Content = new FontIcon { Glyph = "\uE70E", FontSize = 11 },
                    Background = null,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = !isFirst
                };
                upButton.Click += (s, e) => MovePin(pin.Order, -1);
                Grid.SetColumn(upButton, 2);

                // Move Down button
                var downButton = new Button
                {
                    Content = new FontIcon { Glyph = "\uE70D", FontSize = 11 },
                    Background = null,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = !isLast
                };
                downButton.Click += (s, e) => MovePin(pin.Order, 1);
                Grid.SetColumn(downButton, 3);

                // Remove button
                var removeButton = new Button
                {
                    Content = new FontIcon { Glyph = "\uE74D", FontSize = 11 },
                    Background = null,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 200, 80, 80))
                };
                removeButton.Click += (s, e) => RemovePin(pin.Path);
                Grid.SetColumn(removeButton, 4);

                grid.Children.Add(icon);
                grid.Children.Add(nameStack);
                grid.Children.Add(upButton);
                grid.Children.Add(downButton);
                grid.Children.Add(removeButton);

                border.Child = grid;
                PinListPanel.Children.Add(border);
            }
        }

        private async Task LoadIconAsync(string path, Image target)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                var thumbnail = await file.GetThumbnailAsync(
                    Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 32);
                var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bmp.SetSourceAsync(thumbnail);
                target.Source = bmp;
            }
            catch { }
        }

        private async void AddPinButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow!);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var pins = SettingsService.Current.PinnedItems;
            if (pins.Count >= 10) return;

            // Avoid duplicates
            if (pins.Any(p => p.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)))
                return;

            pins.Add(new PinnedItem
            {
                Path = file.Path,
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(file.Path),
                Order = pins.Count
            });

            SettingsService.Save();
            RebuildList();
            _mainWindow?.LoadPins();
        }

        private void RemovePin(string path)
        {
            var pins = SettingsService.Current.PinnedItems;
            pins.RemoveAll(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

            // Re-index order
            for (int i = 0; i < pins.Count; i++)
                pins[i].Order = i;

            SettingsService.Save();
            RebuildList();
            _mainWindow?.LoadPins();
        }

        private void MovePin(int order, int direction)
        {
            var pins = SettingsService.Current.PinnedItems;
            var pin = pins.FirstOrDefault(p => p.Order == order);
            var swap = pins.FirstOrDefault(p => p.Order == order + direction);
            if (pin == null || swap == null) return;

            pin.Order += direction;
            swap.Order -= direction;

            SettingsService.Save();
            RebuildList();
            _mainWindow?.LoadPins();
        }
    }
}