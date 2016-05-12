using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading.Tasks;         // Used to implement asynchronous methods
using Windows.Devices.Enumeration;    // Used to enumerate cameras on the device
using Windows.Devices.Sensors;        // Orientation sensor is used to rotate the camera preview
using Windows.Graphics.Display;       // Used to determine the display orientation
using Windows.Graphics.Imaging;       // Used for encoding captured images
using Windows.Media;                  // Provides SystemMediaTransportControls
using Windows.Media.Capture;          // MediaCapture APIs
using Windows.Media.MediaProperties;  // Used for photo and video encoding
using Windows.Storage;                // General file I/O
using Windows.Storage.FileProperties; // Used for image file encoding
using Windows.Storage.Streams;        // General file I/O
using Windows.System.Display;         // Used to keep the screen awake during preview and capture
using Windows.UI.Core;                // Used for updating UI from within async operations
using System.Diagnostics;
using Windows.UI.Popups;


namespace ICamSee
{
    public sealed partial class MainPage : Page
    {
        private readonly DisplayRequest _displayRequest = new DisplayRequest(); 
            // prevent the display from turning off while on this page

        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _isRecording;
        private bool _externalCamera;
        private bool _mirroringPreview;

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;

        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");


        public MainPage()
        {
            this.InitializeComponent();

            this.InitializeCameraAsync();
        }


        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                // Get available devices for capturing pictures
                var allVideoDevices = 
                    await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                // Get the camera
                DeviceInformation cameraDevice = allVideoDevices.FirstOrDefault();

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

        private async void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, 
            SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup 
                && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
                if (_isPreviewing)
                {
                    await SetPreviewRotationAsync();
                }
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

    }

}
