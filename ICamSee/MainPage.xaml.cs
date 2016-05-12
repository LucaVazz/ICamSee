using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.ApplicationModel;



namespace ICamSee
{
    public sealed partial class MainPage : Page
    {
        private readonly DisplayRequest _displayRequest = new DisplayRequest(); 
            // used later to prevent the display from turning off while on this page
        
        private MediaCapture _mediaCapture;

        // app states:
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _externalCamera;
        private bool _mirroringPreview;

        // rotation stuff
        private readonly DisplayInformation _displayInformation 
            = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation 
            = DisplayOrientations.Portrait;
        private readonly SimpleOrientationSensor _orientationSensor 
            = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation 
            = SimpleOrientation.NotRotated;
        private static readonly Guid RotationKey 
            = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        // handles minimizing/restoring of the app's window
        private readonly SystemMediaTransportControls _systemMediaControls 
            = SystemMediaTransportControls.GetForCurrentView();


        public MainPage()
        {
            this.InitializeComponent();
            this.InitializeCameraAsync();

            // register lifecycle-events
            Application.Current.Suspending  += Application_Suspending;
            Application.Current.Resuming    += Application_Resuming;
        }

        #region Initialisation and Deinitialisation
        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                // Get available devices for capturing pictures
                var allVideoDevices = 
                    await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                // Get the camera
                DeviceInformation cameraDevice = allVideoDevices.ElementAt(2);
                    //.FirstOrDefault();

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found.");
                    return;
                }

                // Create MediaCapture and initialize it
                _mediaCapture = new MediaCapture();
                
                var mediaInitSettings = new MediaCaptureInitializationSettings {
                    VideoDeviceId = cameraDevice.Id
                };

                try
                {
                    await _mediaCapture.InitializeAsync(mediaInitSettings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                    await new MessageDialog("The app was denied access to your camera", ":/")
                        .ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                // start the live preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null 
                        || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // It's probably external if we can't get location-info
                        _externalCamera = true;
                    }
                    else
                    {
                        _externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        _mirroringPreview = (
                            cameraDevice.EnclosureLocation.Panel == 
                                Windows.Devices.Enumeration.Panel.Front
                        );
                    }

                    await StartPreviewAsync();                    
                }
            }
        }

        private async Task StartPreviewAsync()
        {
            // request to prevent device-sleeping
            _displayRequest.RequestActive();

            // make the preview-control visible
            this.capturePreview.Visibility  = Visibility.Visible;
            this.noiseImage.Visibility      = Visibility.Collapsed;

            // set the preview up
            this.capturePreview.Source = _mediaCapture;
            this.capturePreview.FlowDirection = 
                _mirroringPreview   ? FlowDirection.RightToLeft 
                                    : FlowDirection.LeftToRight;

            // Start the preview
            try
            {
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when starting the preview: {0}", ex.ToString());
            }

            // activate orientation-awarness
            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task StopPreviewAsync()
        {
            try
            {
                _isPreviewing = false;
                await _mediaCapture.StopPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping the preview: {0}", ex.ToString());
            }

            // necessary because of the possibility of cross-calls from non-ui threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // clean the UI
                this.capturePreview.Source = null;
                this.capturePreview.Visibility = Visibility.Collapsed;
                this.noiseImage.Visibility = Visibility.Visible;

                // allow the device to sleep again
                _displayRequest.RequestRelease();
            });
        }

        private async Task CleanupCameraAsync()
        {
            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }
        #endregion


        #region handle changes in the device's orientation
        private void RegisterOrientationEventHandlers()
        {
            // subscribe to "orientation changed"-events
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            _displayOrientation = _displayInformation.CurrentOrientation;
        }

        private void UnregisterOrientationEventHandlers()
        {
            // unsubscribe
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, 
            SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup 
                && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private static int ConvertDeviceOrientationToDegrees(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return 270;
                case SimpleOrientation.NotRotated:
                default:
                    return 0;
            }
        }

        private async Task SetPreviewRotationAsync()
        {
            // external cameras aren't effected by the device's orientation
            if (_externalCamera)
                return;

            _displayOrientation = _displayInformation.CurrentOrientation;

            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // rotation needs to be inverted if the preview is mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // add rotation metadata to the preview stream
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }
        #endregion


        #region all of them lifecycle
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // we only care if this page is currently active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                // the system shall wait until we tell it specifically that we are finished
                //   (becasue the clenaup happens async)
                var deferral = e.SuspendingOperation.GetDeferral();

                UnregisterOrientationEventHandlers();
                _systemMediaControls.PropertyChanged -= SystemMediaControls_PropertyChanged;
                await CleanupCameraAsync();

                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // we only care if this page is currently active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                RegisterOrientationEventHandlers();
                _systemMediaControls.PropertyChanged += SystemMediaControls_PropertyChanged;

                await InitializeCameraAsync();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RegisterOrientationEventHandlers();
            _systemMediaControls.PropertyChanged += SystemMediaControls_PropertyChanged;
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            UnregisterOrientationEventHandlers();
            _systemMediaControls.PropertyChanged -= SystemMediaControls_PropertyChanged;
            await CleanupCameraAsync();
        }

        // handle when the app's window gets minimzed / restored
        private async void SystemMediaControls_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // we only care if this page is currently active
                if (args.Property == SystemMediaTransportControlsProperty.SoundLevel && Frame.CurrentSourcePageType == typeof(MainPage))
                {
                    // if the sound gets muted, the window is getting minimized
                    if (sender.SoundLevel == SoundLevel.Muted)
                    {
                        await CleanupCameraAsync();
                    }
                    else if (!_isInitialized)
                    {
                        await InitializeCameraAsync();
                    }
                }
            });
        }
        #endregion

    }

}
