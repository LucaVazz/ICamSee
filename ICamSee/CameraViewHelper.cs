using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace ICamSee {
    class CameraViewHelper : ICameraViewHelper {
        public MediaCapture MediaCapture { get; private set; }
        DeviceInformation CurrentDevice;
        bool IsMirroringPreview;

        public async Task Initialize(DeviceInformation deviceToUse)
        {
            if (MediaCapture == null) {
                // Create MediaCapture and initialize it:
                MediaCapture = new MediaCapture();
            }

            var mediaInitSettings = new MediaCaptureInitializationSettings {
                VideoDeviceId = deviceToUse.Id
            };

            await MediaCapture.InitializeAsync(mediaInitSettings);
               
            // Set if the cameras live-feed should get mirrored:
            // Figure out where the camera is located
            if (  deviceToUse.EnclosureLocation == null
                || deviceToUse.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown
            ) {
                // It's probably external if we can't get location-info
                IsMirroringPreview = false;
            } else {
                // Only mirror the preview if the camera is on the front panel
                IsMirroringPreview = (deviceToUse.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
            }

            CurrentDevice = deviceToUse;
        }

        public async Task StartView()
        {
            await MediaCapture.StartPreviewAsync();
        }

        public async Task ReInitialize()
        {
            await Initialize(CurrentDevice);
        }

        public async Task Deinitialize()
        {
            if (MediaCapture != null) {
                MediaCapture.Dispose();
                MediaCapture = null;
            }
        }

        public async Task StopView()
        {
            await MediaCapture.StopPreviewAsync();
        }

        #region Capabilities
        public bool CanAutoFocus {
            get {
                return MediaCapture.VideoDeviceController.Focus.Capabilities.AutoModeSupported;
            }
        }

        public bool CanFocus {
            get {
                return MediaCapture.VideoDeviceController.Focus.Capabilities.Supported;
            }
        }

        public double FocusStep { get { return FocusCapabilities.Step; } }

        public double FocusMin { get { return FocusCapabilities.Min; } }

        public double FocusMax { get { return FocusCapabilities.Max; } }

        MediaDeviceControlCapabilities FocusCapabilities {
            get {
                return MediaCapture.VideoDeviceController.Focus.Capabilities;
            }
        }

        public bool CanZoom {
            get {
                return ZoomCapabilities.Supported;
            }
        }

        public double ZoomStep { get { return ZoomCapabilities.Step; } }

        public double ZoomMin { get { return ZoomCapabilities.Min; } }

        public double ZoomMax { get { return ZoomCapabilities.Max; } }

        MediaDeviceControlCapabilities ZoomCapabilities {
            get {
                return MediaCapture.VideoDeviceController.Zoom.Capabilities;
            }
        }
        #endregion

        #region Public State
        public bool IsAutoFocusing {
            get {
                bool isAuto;
                if (MediaCapture.VideoDeviceController.Focus.TryGetAuto(out isAuto)) {
                    return isAuto;
                } else {
                    return false;
                }
            }
        }
        #endregion

        #region Change Video Device Properties
        public bool ToggleAutoFocus()
        {
            bool isAutoFocusenabled = false;
            if (MediaCapture.VideoDeviceController.Focus.TryGetAuto(out isAutoFocusenabled)) {
                return SetAutoFocus(!isAutoFocusenabled);
            } else {
                return false;
            }
        }

        public bool SetAutoFocus(bool state)
        {
            return MediaCapture.VideoDeviceController.Focus.TrySetAuto(state);
        }

        public bool SetFocus(uint value)
        {
            throw new NotImplementedException();  // TODO: Implement SetFocus for a slider
        }

        public async Task SetViewRotationAsync(int rotationDeg)
        {
            var props = MediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);

            var rotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
            props.Properties[rotationKey] = rotationDeg;

            await MediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Triggers the video device to change the zoom to the specified step.
        /// </summary>
        /// <returns>Indicates if the zoom-level was succesfully changed</returns>
        public bool SetZoom(double value)
        {
            if (value > ZoomMax) {
                return false;
            } else if (value < ZoomMin) {
                return false;
            } else {
                return MediaCapture.VideoDeviceController.Zoom.TrySetValue(value);
            }
        }

        /// <summary>
        /// Triggers the video device to change the zoom by one step.
        /// </summary>
        /// <returns>Indicates if the zoom-level was succesfully changed</returns>
        public bool ChangeZoomByOneStep(int stepMultiplier = 1)
        {
            double current;
            if (MediaCapture.VideoDeviceController.Zoom.TryGetValue(out current)) {
                double next = current + ZoomStep * stepMultiplier;
                return SetZoom(next);
            } else {
                return false;
            }
        }
        #endregion

    }
}
