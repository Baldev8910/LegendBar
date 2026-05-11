using LegendBar.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace LegendBar.Pages
{
    public sealed partial class AppearancePage : Page
    {
        private MainWindow? _mainWindow;
        private bool _loading = true;

        public AppearancePage()
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

            HeightSlider.Value = s.BarHeight;
            HeightLabel.Text = $"{s.BarHeight}px";

            MaterialCombo.SelectedIndex = s.MaterialType switch
            {
                "Mica" => 1,
                "MicaAlt" => 2,
                "Solid" => 3,
                _ => 0
            };

            TintColorBox.Text = s.TintColor;

            TintSlider.Value = s.AcrylicTintOpacity * 100;
            TintLabel.Text = $"{s.AcrylicTintOpacity * 100:0}%";

            LuminositySlider.Value = s.AcrylicLuminosityOpacity * 100;
            LuminosityLabel.Text = $"{s.AcrylicLuminosityOpacity * 100:0}%";

            UpdateCardVisibility(s.MaterialType);
        }

        private void UpdateCardVisibility(string materialType)
        {
            bool isAcrylic = materialType == "Acrylic";
            bool isSolid = materialType == "Solid";

            TintColorCard.Visibility = (isAcrylic || isSolid)
                ? Visibility.Visible : Visibility.Collapsed;
            AcrylicTintCard.Visibility = isAcrylic
                ? Visibility.Visible : Visibility.Collapsed;
            AcrylicBlurCard.Visibility = isAcrylic
                ? Visibility.Visible : Visibility.Collapsed;

            // Adjust corner radius of material card depending on what follows
            //MaterialCombo.FindName(""); // just to reference — handled by CornerRadius in XAML
        }

        private void HeightSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            int val = (int)e.NewValue;
            HeightLabel.Text = $"{val}px";
            SettingsService.Current.BarHeight = val;
            SettingsService.Save();
            _mainWindow?.UpdateBarHeight(val);
        }

        private void MaterialCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var selected = (MaterialCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                ?? "Acrylic";
            SettingsService.Current.MaterialType = selected;
            SettingsService.Save();
            _mainWindow?.UpdateMaterial();
            UpdateCardVisibility(selected);
        }

        private void TintColorBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;
            var text = TintColorBox.Text.Trim();
            if (!text.StartsWith('#') || text.Length != 7) return;
            try
            {
                Convert.ToByte(text[1..3], 16);
                Convert.ToByte(text[3..5], 16);
                Convert.ToByte(text[5..7], 16);
            }
            catch { return; }
            SettingsService.Current.TintColor = text;
            SettingsService.Save();
            _mainWindow?.UpdateTintColor();
        }

        private void TintSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            float val = (float)(e.NewValue / 100);
            TintLabel.Text = $"{e.NewValue:0}%";
            SettingsService.Current.AcrylicTintOpacity = val;
            SettingsService.Save();
            _mainWindow?.SetAcrylicOpacity(val,
                SettingsService.Current.AcrylicLuminosityOpacity);
        }

        private void LuminositySlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_loading) return;
            float val = (float)(e.NewValue / 100);
            LuminosityLabel.Text = $"{e.NewValue:0}%";
            SettingsService.Current.AcrylicLuminosityOpacity = val;
            SettingsService.Save();
            _mainWindow?.SetAcrylicOpacity(
                SettingsService.Current.AcrylicTintOpacity, val);
        }
    }
}