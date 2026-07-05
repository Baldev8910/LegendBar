using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LegendBar
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LegendBar", "crash.log");
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now} [AppDomain]: {e.ExceptionObject}\n\n");
                }
                catch { }
            };

            InitializeComponent();

            this.UnhandledException += (s, e) =>
            {
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LegendBar", "crash.log");
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now}: {e.Exception}\n\n");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                e.SetObserved();
                System.Diagnostics.Debug.WriteLine($"[App] Unobserved task exception: {e.Exception}");
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LegendBar", "crash.log");
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!); ;
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now} [Task]: {e.Exception}\n\n");
                }
                catch { }
            };
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
