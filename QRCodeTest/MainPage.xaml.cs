using System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QRCodeTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            qrScanner.QrScanned += QrScanner_QrScanned;
            qrScanner.ScanAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            await qrScanner?.StopScanAsync();
        }

        private async void QrScanner_QrScanned(object sender, QrScanner.QrScannerEventArgs e)
        {
            await new MessageDialog(e.Data).ShowAsync();
        }
    }
}
