using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.System.Display;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.ApplicationModel;


namespace ICamSee
{
    public sealed partial class MainPage : Page
    {      
        // App States:
        bool IsPreviewing;

        // Helpers:
        IVideoDevicesManager Devices = new VideoDevicesManager();
        ICameraViewHelper CameraView = new CameraViewHelper();


        public MainPage()
        {
            InitializeComponent();

            // Register lifecycle-events:
            Application.Current.Suspending  += Application_Suspending;
            Application.Current.Resuming    += Application_Resuming;

            start();
        }

        public async void start()
        {
            var defDevice = await Devices.GetDefaultDeviceAsync();
            ChangeVideoDeviceAsync(defDevice);
        }


        private async void ChangeVideoDeviceAsync(DeviceInformation videoDevice)
        {
            await Deinitialize();

            await InitializeCameraAsync(videoDevice);
        }

        private async Task InitializeCameraAsync(DeviceInformation deviceToUse)
        {
            try {
                await CameraView.Initialize(deviceToUse);
            }
            catch (UnauthorizedAccessException ex) {
                Debug.WriteLine("The app was denied access to the camera: {0}", ex.ToString());
                await new MessageDialog(
                    "The app couldn't access the camera because of an I/O-Error or a Security Violation.", ":("
                ).ShowAsync();
            }
            catch (Exception ex) {
                Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}",
                    deviceToUse.Id, ex.ToString());
                await new MessageDialog("An error occoured while trying to initialize the camera.", ":(")
                    .ShowAsync();
            }

            if (CameraView.CanFocus
                && CameraView.CanAutoFocus
                && CameraView.SetAutoFocus(true)
            ) {
                ToggleAutoFocusButton.Visibility = Visibility.Visible;
                ToggleAutoFocusButton.IsChecked = true;
            } else {
                ToggleAutoFocusButton.Visibility = Visibility.Collapsed;
            }

            if (CameraView.CanZoom) {
                ZoomCommandbar.Visibility = Visibility.Visible;
            } else {
                ZoomCommandbar.Visibility = Visibility.Collapsed;
            }

            CapturePreview.Source = CameraView.MediaCapture;
            /*capturePreview.FlowDirection = 
                IsMirroringPreview ? FlowDirection.RightToLeft 
                                    : FlowDirection.LeftToRight;
            */
            await CameraView.StartView();
            IsPreviewing = true;
        }

        private async Task Deinitialize()
        {
            if (IsPreviewing) {
                CapturePreview.Source = null;
                CapturePreview.Visibility = Visibility.Collapsed;
                NoiseImage.Visibility = Visibility.Visible;

                CameraView.StopView();

                IsPreviewing = false;
            }

            try
            {
                await CameraView.Deinitialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping the view: {0}", ex.ToString());
            }
        }

        #region Lifecycle Events
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // The system shall wait until we tell it specifically that we are finished (becasue the clenaup happens async)
            var deferral = e.SuspendingOperation.GetDeferral();

            await Deinitialize();

            deferral.Complete();
        }

        private async void Application_Resuming(object sender, object o)
        {
            await CameraView.ReInitialize();
        }
        #endregion


        #region Click Events
        private async void CameraSwitchOpener_Click(object sender, RoutedEventArgs e)
        {
            CameraList.Visibility = Visibility.Collapsed;
            CameraListLoadIndicator.Visibility = Visibility.Visible;

            var videoDevicesCollection = await Devices.GetAllAvailableDevicesAsync();
            CameraList.ItemsSource = videoDevicesCollection;

            CameraListLoadIndicator.Visibility = Visibility.Collapsed;
            CameraList.Visibility = Visibility.Visible;
        }

        private void CameraList_Click(object sender, ItemClickEventArgs e)
        {
            ChangeVideoDeviceAsync((DeviceInformation)e.ClickedItem);
        }

        private async void ToggleAutoFocus_Click(object sender, RoutedEventArgs e)
        {
            if(!CameraView.ToggleAutoFocus()) {
                await new MessageDialog("An error occured while trying to toggle the Auto-Focus.", ":/")
                    .ShowAsync();
            }
        }

        private async void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if(!CameraView.ChangeZoomByOneStep(310)) {
                await new MessageDialog("An error occured while trying to change the zoom level.", ":/")
                    .ShowAsync();
            }
        }

        private async void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (!CameraView.ChangeZoomByOneStep(-310)) {
                await new MessageDialog("An error occured while trying to change the zoom level.", ":/")
                    .ShowAsync();
            }
        }
        #endregion
    }

}
