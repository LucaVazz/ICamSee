using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace ICamSee
{
    class CameraViewHelper : ICameraViewHelper
    {
        private DeviceInformation VideoDevice;
        private bool IsExternalCamera;
        private bool IsMirroringPreview;

        public MediaCapture MediaCapture { get; private set; }

        public bool CanAutoFocus { get; private set; }
        public bool IsAutoFocusing { get; private set; }
        public bool CanFocus { get; private set; }
        public bool CanZoom { get; private set; }



        /// <summary>
        /// Prepares all components needed for the CameraView for use.
        /// Can throw UnauthorizedAccessException
        /// </summary>
        /// <param name="videoDevice">The VideoDevice which should be used for this View</param>
        /// <returns></returns>
        public async Task InitializeAsync(DeviceInformation videoDevice) {
            if(videoDevice == null) {
                throw new ArgumentNullException("The passed videoDevice is invalid!");
            }

            if (MediaCapture != null) {
                throw new InvalidOperationException("The CameraView is already initilaized!");
            }

            MediaCapture = new MediaCapture();

            MediaCaptureInitializationSettings mediaInitSettings = 
                new MediaCaptureInitializationSettings { VideoDeviceId = videoDevice.Id };
            await MediaCapture.InitializeAsync(mediaInitSettings); // can throw UnauthorizedAccessException

            IsExternalCamera = (videoDevice.EnclosureLocation == null);
            IsMirroringPreview = (
                !IsExternalCamera
                && videoDevice.EnclosureLocation.Panel == Panel.Front
            );

            MediaCapture.SetPreviewMirroring(IsMirroringPreview);

            MediaDeviceControlCapabilities FocusCapabilities =
                MediaCapture.VideoDeviceController.Focus.Capabilities;
            CanFocus       = FocusCapabilities.Supported;
            CanAutoFocus   = MediaCapture.VideoDeviceController.Focus.Capabilities.AutoModeSupported;

            MediaDeviceControlCapabilities ZoomCapabilities =
                MediaCapture.VideoDeviceController.Zoom.Capabilities;
            CanZoom = ZoomCapabilities.Supported;

            await MediaCapture.StartPreviewAsync();
        }

        /// <summary>
        /// Frees up Resources for the View.
        /// Needs to be called after the view was stopped.
        /// </summary>
        public async Task DeactivateAsync() {
            await MediaCapture.StopPreviewAsync();

            MediaCapture.Dispose();
        }

        /// <summary>
        /// Sets the Rotation of the Preview.
        /// </summary>
        /// <param name="rotationDegrees">A Measeure in Degrees between 0 and 360</param>
        /// <returns></returns>
        public async Task SetViewRotationAsync(int rotationDegrees)
        {
            if (rotationDegrees < 0 || rotationDegrees > 360)
            {
                throw new ArgumentOutOfRangeException("rotationDegress");
            }

            if (IsExternalCamera)
            {
                return;                 /* exteranl cameras don't care about the device's rotation */
            }

            // rotation needs to be inverted if the preview is mirrored
            if (IsMirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            var StreamProperties = MediaCapture.VideoDeviceController
                .GetMediaStreamProperties(MediaStreamType.VideoPreview);
            Guid rotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

            StreamProperties.Properties.Add(rotationKey, rotationDegrees);
            await MediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview,
                StreamProperties, null);
        }


        /// <summary>
        /// Captures the Image currently shown by the Preview
        /// </summary>
        /// <returns>A Stream with the capture encoded as PNG</returns>
        public async Task<InMemoryRandomAccessStream> CaptureCurrentImageToStreamAsync() {
            InMemoryRandomAccessStream accessStream = new InMemoryRandomAccessStream();
            ImageEncodingProperties properties = ImageEncodingProperties.CreatePng();

            await MediaCapture.CapturePhotoToStreamAsync(properties, accessStream);
            return accessStream;
        }


        /// <summary>
        /// Sets the state of automatic focusing.
        /// If the Camera doesn't support AutoFocus or an error occurs, an InvalidOperationException will be thrown.
        /// </summary>
        /// <param name="state"></param>
        public void SetAutoFocus(bool state) {
            if(!CanAutoFocus) {
                throw new InvalidOperationException("This VideoDevice doesn't support AutoFocus!");
            }

            bool setWasSuccesfull = MediaCapture.VideoDeviceController.Focus.TrySetAuto(state);

            if (!setWasSuccesfull) {
                throw new InvalidOperationException("An error occured while trying to set AutoFocuse");
            }
        }

        /// <summary>
        /// Sets the Focus-Length of the Camera if supported.
        /// If the camera doesn't support the setting of focus or if an error occurs, an InvalidOperationException 
        /// will be thrown.
        /// </summary>
        /// <param name="value">The Focus-Length, abstracted to a Range between 0 and 100</param>
        public void SetFocus(uint value) {
            if (!CanFocus) {
                throw new InvalidOperationException("This VideoDevice doesn't support setting the Focus!");
            }

            if (value > 100) {
                throw new ArgumentOutOfRangeException("value");
            }

            MediaDeviceControlCapabilities focusCapabilities = MediaCapture.VideoDeviceController.Focus.Capabilities;
            double realStep = focusCapabilities.Step * (int)(
                (focusCapabilities.Max - focusCapabilities.Min) / 100 / focusCapabilities.Step
            );
            double newValue = focusCapabilities.Min + realStep * value;
            bool setWasSuccesfull = MediaCapture.VideoDeviceController.Focus.TrySetValue(newValue);

            if (!setWasSuccesfull) {
                throw new InvalidOperationException("An error occured while trying to set the value of the Focus!");
            }
        }

        /// <summary>
        /// Sets the Zoom-Level of the Camera if supported.
        /// If the camera doesn't support setting a Zoom-Level or if an error occurs, an InvalidOperationException 
        /// will be thrown.
        /// </summary>
        /// <param name="value">The Zoom Level in a Range between 0 and 100</param>
        public void SetZoom(uint value) {
            if (!CanZoom) {
                throw new InvalidOperationException("This VideoDevice doesn't support setting the Zoom!");
            }

            if(value > 100) {
                throw new ArgumentOutOfRangeException("value");
            }

            MediaDeviceControlCapabilities zoomCapabilities = MediaCapture.VideoDeviceController.Zoom.Capabilities;
            double realStep = zoomCapabilities.Step * (int)(
                (zoomCapabilities.Max - zoomCapabilities.Min) / 100 / zoomCapabilities.Step
            );
            double newValue = zoomCapabilities.Min + realStep * value;
            bool setWasSuccesfull = MediaCapture.VideoDeviceController.Zoom.TrySetValue(newValue);

            if (!setWasSuccesfull)
            {
                throw new InvalidOperationException("An error occured while trying to set the value of the Zoom!");
            }
        }

    }
}
