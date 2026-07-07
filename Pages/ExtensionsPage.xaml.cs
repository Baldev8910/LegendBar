using LegendBar.Helpers;
using LegendBar.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace LegendBar.Pages
{
    public sealed partial class ExtensionsPage : Page
    {
        private List<ExtensionCatalogEntry> _catalog = new();

        public ExtensionsPage()
        {
            InitializeComponent();
            Loaded += ExtensionsPage_Loaded;
        }

        private async void ExtensionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingRing.Visibility = Visibility.Visible;
            _catalog = await ExtensionCatalogService.FetchCatalogAsync();
            LoadingRing.Visibility = Visibility.Collapsed;
            BuildList();
        }

        private void BuildList()
        {
            ExtensionsListPanel.Children.Clear();

            if (_catalog.Count == 0)
            {
                ExtensionsListPanel.Children.Add(new TextBlock
                {
                    Text = "Couldn't load the extensions list. Check your internet connection.",
                    Opacity = 0.7,
                    Margin = new Thickness(4, 8, 4, 8)
                });
                return;
            }

            foreach (var entry in _catalog)
            {
                ExtensionsListPanel.Children.Add(BuildCard(entry));
            }
        }

        private Border BuildCard(ExtensionCatalogEntry entry)
        {
            bool installed = ExtensionCatalogService.IsInstalled(entry);
            bool pendingRemoval = ExtensionCatalogService.IsPendingRemoval(entry);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel { Spacing = 2 };
            textStack.Children.Add(new TextBlock
            {
                Text = $"{entry.Name}  ·  v{entry.Version}",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            textStack.Children.Add(new TextBlock
            {
                Text = entry.Description,
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(textStack, 0);

            var actionButton = new Button
            {
                Content = pendingRemoval ? "Pending removal (restart)" : (installed ? "Remove" : "Install"),
                IsEnabled = !pendingRemoval,
                VerticalAlignment = VerticalAlignment.Center
            };
            actionButton.Click += async (s, e) => await OnActionClicked(entry, actionButton, installed);
            Grid.SetColumn(actionButton, 1);

            grid.Children.Add(textStack);
            grid.Children.Add(actionButton);

            return new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, 8),
                Child = grid
            };
        }

        private async System.Threading.Tasks.Task OnActionClicked(ExtensionCatalogEntry entry, Button button, bool wasInstalled)
        {
            button.IsEnabled = false;
            button.Content = wasInstalled ? "Removing..." : "Installing...";

            bool success = wasInstalled
                ? ExtensionCatalogService.Remove(entry)
                : await ExtensionCatalogService.InstallAsync(entry);

            if (success)
            {
                var dialog = new ContentDialog
                {
                    Title = wasInstalled ? "Extension queued for removal" : "Extension installed",
                    Content = wasInstalled
                        ? "This extension will be fully removed the next time you restart LegendBar."
                        : "Restart LegendBar for this change to take effect.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Something went wrong",
                    Content = wasInstalled
                        ? "Couldn't remove the extension file."
                        : "Couldn't install the extension. Check your internet connection.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }

            BuildList(); // Refresh so the button flips to Install/Remove correctly
        }
    }
}