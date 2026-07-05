using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;

namespace LegendBar.Pages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                var v = Package.Current.Id.Version;
                VersionLabel.Text = $"v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
            catch
            {
                // Not running as MSIX (e.g. unpackaged debug run)
                VersionLabel.Text = "v1.1.7.0";
            }
        }
    }
}