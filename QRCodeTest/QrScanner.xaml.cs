using System;
using System.Collections.Generic;
using System.Linq;
using ZXing;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.Media.Capture;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using Windows.Media;
using Windows.Graphics.Imaging;

namespace QRCodeTest
{

    public partial class QrScanner : UserControl
    {

        public enum CameraLoadedState
        {
            Unloaded,
            Initializing,
            Loaded,
        };

        public class QrScannerEventArgs : EventArgs
        {
            public QrScannerEventArgs(string s, byte[] b)
            {
                Data = s;
                Bytes = b;
            }
            public string Data;
            public byte[] Bytes;
        }
        public event EventHandler<QrScannerEventArgs> QrScanned;

        MediaCapture mediaCapture = null;

        DispatcherTimer timer;
        BarcodeReader reader;

        private VideoEncodingProperties previewProperties;

        CameraLoadedState _cameraState = CameraLoadedState.Unloaded;
        bool _scanStarted = false;


        public QrScanner()
        {
            InitializeComponent();
        }

        async Task CameraInitializedAsync()
        {
            if (this.mediaCapture != null)
            {
                this.PreviewControl.Source = mediaCapture;
                previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                await mediaCapture.StartPreviewAsync();
                int w = (int)previewProperties.Width, h = (int)previewProperties.Height;
                var buf = new byte[w * h * 3];

                reader = new BarcodeReader();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (this.mediaCapture != null)
                    {
                        _cameraState = CameraLoadedState.Loaded;
                        if (_scanStarted)
                        {
                            StartScanner();
                        }
                    }
                });
            }
        }

        public async void ScanAsync()
        {
            await LoadAsync();

            _scanStarted = true;
            if (_cameraState == CameraLoadedState.Loaded)
            {
                StartScanner();
            }
        }

        void StartScanner()
        {
            tickCount = 0;
            this.timer.Start();
        }

        public void StopScan()
        {
            _scanStarted = false;
            if (this.timer != null && this.timer.IsEnabled)
            {
                this.timer.Stop();
            }
        }

        int tickCount = 0;
        bool focusing_ = false;

        private async void Timer_Tick(object sender, object e)
        {
            if (_cameraState == CameraLoadedState.Loaded)
            {
                var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

                var frame = await this.mediaCapture.GetPreviewFrameAsync(videoFrame);
                var res = reader.Decode(frame.SoftwareBitmap);
                if (res != null && res.Text != null)
                {
                    var handlers = QrScanned;
                    if (handlers != null)
                    {
                        byte[] bytes = null;
                        List<byte[]> byteList = res.ResultMetadata[ResultMetadataType.BYTE_SEGMENTS] as List<byte[]>;
                        if (byteList != null)
                        {
                            if (byteList.Count > 1)
                            {
                                int totalLength = 0;
                                foreach (byte[] b in byteList)
                                {
                                    totalLength += b.Length;
                                }
                                bytes = new byte[totalLength];

                                totalLength = 0;
                                foreach (byte[] b in byteList)
                                {
                                    Array.Copy(b, 0, bytes, totalLength, b.Length);
                                    totalLength += b.Length;
                                }
                            }
                            else
                            {
                                bytes = byteList.FirstOrDefault();
                            }
                        }

                        handlers(this, new QrScannerEventArgs(res.Text, bytes));
                    }
                }

                if (this.tickCount % 5 == 0)
                {
                        if (!focusing_ && mediaCapture != null)
                        {
                            focusing_ = true;
                            await this.mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
                            focusing_ = false;
                        }
                }

                if (this.tickCount % 50 == 0)
                {

                }

                this.tickCount++;
            }
        }

        public async Task LoadAsync()
        {
            _cameraState = CameraLoadedState.Initializing;

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            mediaCapture.VideoDeviceController.FocusControl.Configure(new Windows.Media.Devices.FocusSettings
            {
                Mode = Windows.Media.Devices.FocusMode.Auto
            });

            await CameraInitializedAsync();

            if (this.timer == null)
            {
                this.timer = new DispatcherTimer();
                this.timer.Interval = TimeSpan.FromMilliseconds(100);
                this.timer.Tick += Timer_Tick;
            }
        }
    }
}