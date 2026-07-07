using LegendBar.Helpers;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;

namespace LegendBar
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private MicaController? _micaController;
        private SystemBackdropConfiguration? _configurationSource;
        private AppWindow _appWindow;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Search index — maps search terms to nav items
        private readonly List<(string Term, string Page)> _searchIndex = new()
        {
            ("appearance", "Appearance"), ("material", "Appearance"),
            ("acrylic", "Appearance"), ("mica", "Appearance"),
            ("tint", "Appearance"), ("color", "Appearance"),
            ("bar height", "Appearance"), ("height", "Appearance"),
            ("widgets", "Widgets"), ("media", "Widgets"),
            ("clock", "Widgets"), ("date", "Widgets"),
            ("player", "Widgets"),
            ("behavior", "Behavior"), ("startup", "Behavior"),
            ("launch", "Behavior"), ("animation", "Behavior"),
            ("speed", "Behavior"), ("hide", "Behavior"),
            ("delay", "Behavior"), ("temperature", "Behavior"),
            ("celsius", "Behavior"), ("fahrenheit", "Behavior"),
            ("about", "About"), ("version", "About"),
            ("monitor", "Behavior"), ("display", "Behavior"),
            ("span", "Behavior"), ("screen", "Behavior"),
            ("github", "About"), ("reset", "About"),
            ("pins", "Pins"), ("pin", "Pins"),
            ("extensions", "Extensions"), ("plugins", "Extensions"),
            ("install", "Extensions"), ("download", "Extensions"),
            ("drag", "Extensions"), ("drop", "Extensions"),
            ("launch", "Pins"), ("shortcut", "Pins"),
            ("app", "Pins"), ("file", "Pins"),
        };

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _appWindow = this.AppWindow;

            SetupWindow();
            SetupMica();
            RestoreWindowPosition();

            NavView.SelectedItem = AppearanceItem;
            ContentFrame.Navigate(typeof(Pages.AppearancePage),
                _mainWindow, new DrillInNavigationTransitionInfo());

            this.Closed += (s, e) =>
            {
                SaveWindowPosition();
                _micaController?.RemoveAllSystemBackdropTargets(); // ← add this
                _micaController = null;
            };
        }

        private void SetupWindow()
        {
            _appWindow.IsShownInSwitchers = true;

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

            _appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            int darkMode = 1;
            DwmSetWindowAttribute(hWnd, 20, ref darkMode, sizeof(int));
        }

        private void SetupMica()
        {
            _configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            if (MicaController.IsSupported())
            {
                _micaController = new MicaController { Kind = MicaKind.Base };
                _micaController.AddSystemBackdropTarget(
                    this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);
            }

            this.Activated += (s, e) =>
            {
                _configurationSource!.IsInputActive =
                    e.WindowActivationState != WindowActivationState.Deactivated;
            };
        }

        private void RestoreWindowPosition()
        {
            var s = SettingsService.Current;
            if (s.SettingsWindowWidth > 0 && s.SettingsWindowHeight > 0)
            {
                _appWindow.MoveAndResize(new RectInt32(
                    s.SettingsWindowX, s.SettingsWindowY,
                    s.SettingsWindowWidth, s.SettingsWindowHeight));
            }
            else
            {
                // Default — center on primary monitor
                var primary = MonitorHelper.Primary;
                int screenW = primary?.LogicalBounds.Width ?? 1920;
                int screenH = primary?.LogicalBounds.Height ?? 1080;
                int screenX = primary?.LogicalBounds.Left ?? 0;
                int screenY = primary?.LogicalBounds.Top ?? 0;
                _appWindow.MoveAndResize(new RectInt32(
                    screenX + (screenW - 900) / 2,
                    screenY + (screenH - 600) / 2,
                    900, 600));
            }
        }

        private void SaveWindowPosition()
        {
            SettingsService.Current.SettingsWindowX = _appWindow.Position.X;
            SettingsService.Current.SettingsWindowY = _appWindow.Position.Y;
            SettingsService.Current.SettingsWindowWidth = _appWindow.Size.Width;
            SettingsService.Current.SettingsWindowHeight = _appWindow.Size.Height;
            SettingsService.Save();
        }

        private void NavView_ItemInvoked(NavigationView sender,
            NavigationViewItemInvokedEventArgs args)
        {
            var tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag?.ToString();
            NavigateTo(tag);
        }

        private void NavigateTo(string? tag)
        {
            Type? pageType = tag switch
            {
                "Appearance" => typeof(Pages.AppearancePage),
                "Widgets" => typeof(Pages.WidgetsPage),
                "Behavior" => typeof(Pages.BehaviorPage),
                "Pins" => typeof(Pages.PinsPage),
                "Extensions" => typeof(Pages.ExtensionsPage),
                "About" => typeof(Pages.AboutPage),
                _ => null
            };

            if (pageType == null) return;
            if (ContentFrame.CurrentSourcePageType == pageType) return;

            ContentFrame.Navigate(pageType, _mainWindow,
                new DrillInNavigationTransitionInfo());
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var query = sender.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(query))
            {
                sender.ItemsSource = null;
                return;
            }

            var results = _searchIndex
                .Where(x => x.Term.Contains(query))
                .Select(x => $"{x.Term} — {x.Page}")
                .Distinct()
                .ToList();

            sender.ItemsSource = results;
        }
        
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var chosen = args.SelectedItem?.ToString();
            if (chosen == null) return;

            var page = chosen.Split('—').LastOrDefault()?.Trim();
            NavigateTo(page);

            // Update nav selection
            var item = page switch
            {
                "Appearance" => AppearanceItem,
                "Widgets" => WidgetsItem,
                "Behavior" => BehaviorItem,
                "Pins" => PinsItem,
                "Extensions" => ExtensionsItem,
                "About" => AboutItem,
                _ => null
            };
            if (item != null) NavView.SelectedItem = item;

            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
    }
}