using LegendBar.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT;

namespace LegendBar
{
    public sealed partial class NotesPopup : Window
    {
        private AppWindow _appWindow;
        private DesktopAcrylicController? _acrylicController;
        private SystemBackdropConfiguration? _configurationSource;
        private MicaController? _micaController;

        private static readonly string _notesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LegendBar", "notes.md");

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        public NotesPopup()
        {
            InitializeComponent();
            _appWindow = GetAppWindow();
            SetupWindow();
            InitWebView();

            bool _loaded = false;
            this.Activated += (s, e) =>
            {
                if (!_loaded) { _loaded = true; return; }
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                    this.Close();
            };
        }

        private async void InitWebView()
        {
            try
            {
                await PreviewView.EnsureCoreWebView2Async(null);
                PreviewView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    // Auto-save when content changes
                    var markdown = e.TryGetWebMessageAsString();
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_notesPath)!);
                        File.WriteAllText(_notesPath, markdown);
                    }
                    catch { }
                };

                var editorPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets", "Editor", "editor.html");
                PreviewView.Source = new Uri(editorPath);
                PreviewView.NavigationCompleted += PreviewView_NavigationCompleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notes] WebView2 init failed: {ex.Message}");
            }
        }

        private async void PreviewView_NavigationCompleted(
            Microsoft.UI.Xaml.Controls.WebView2 sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            try
            {
                var tint = SettingsService.GetTintColor();
                var bgColor = $"#{tint.R:X2}{tint.G:X2}{tint.B:X2}";

                var content = File.Exists(_notesPath)
                    ? File.ReadAllText(_notesPath)
                    : "";
                content = content
                    .Replace("\\", "\\\\")
                    .Replace("`", "\\`")
                    .Replace("$", "\\$");

                await PreviewView.ExecuteScriptAsync($"setContent(`{content}`)");
                await PreviewView.ExecuteScriptAsync($"window.setBackground('{bgColor}')");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notes] NavigationCompleted error: {ex.Message}");
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_notesPath}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void SetupWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            try
            {
                int style = GetWindowLong(hWnd, GWL_STYLE);
                SetWindowLong(hWnd, GWL_STYLE, style & ~WS_CAPTION);
            }
            catch { /* suppress Win32 SEH */ }

            _appWindow.IsShownInSwitchers = false;

            var presenter = _appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.SetDragRectangles(Array.Empty<RectInt32>());

            ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;

            var margins = new MARGINS
            {
                cxLeftWidth = -1,
                cxRightWidth = -1,
                cyTopHeight = -1,
                cyBottomHeight = -1
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            int noShadow = 2;
            DwmSetWindowAttribute(hWnd, 2, ref noShadow, sizeof(int));
            int marginValue = 0;
            DwmSetWindowAttribute(hWnd, 3, ref marginValue, sizeof(int));
            int cornerPreference = 2;
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            _configurationSource = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            var target = this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
            switch (SettingsService.Current.MaterialType)
            {
                case "Mica":
                    if (MicaController.IsSupported())
                    {
                        var mica = new MicaController { Kind = MicaKind.Base };
                        mica.AddSystemBackdropTarget(target);
                        mica.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;
                case "MicaAlt":
                    if (MicaController.IsSupported())
                    {
                        var mica = new MicaController { Kind = MicaKind.BaseAlt };
                        mica.AddSystemBackdropTarget(target);
                        mica.SetSystemBackdropConfiguration(_configurationSource);
                    }
                    break;
                default:
                    _acrylicController = new DesktopAcrylicController
                    {
                        TintColor = Windows.UI.Color.FromArgb(255, 20, 20, 20),
                        TintOpacity = SettingsService.Current.AcrylicTintOpacity,
                        LuminosityOpacity = SettingsService.Current.AcrylicLuminosityOpacity,
                        Kind = DesktopAcrylicKind.Base
                    };
                    _acrylicController.AddSystemBackdropTarget(target);
                    _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
                    break;
            }

            var primary = MonitorHelper.Primary;
            int rightEdge = (primary?.LogicalBounds.Left ?? 0) +
                            (primary?.LogicalBounds.Width ?? 1920);
            int popupX = rightEdge - 420 - 24;
            int popupY = SettingsService.Current.BarHeight + 8;
            _appWindow.MoveAndResize(new RectInt32(popupX, popupY, 420, 460));
        }

        private async void MathPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await PreviewView.ExecuteScriptAsync("window.showMathPreview()");
            }
            catch { }
        }

        private AppWindow GetAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}