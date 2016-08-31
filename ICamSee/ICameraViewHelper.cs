using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;


namespace ICamSee
{
    interface ICameraViewHelper
    {
        void SetVideoDeviceAsync(DeviceInformation videoDevice);

        MediaCapture MediaCapture { get; }
        void InitializeAsync();
        void DeactivateAsync();

        void LoadVideoDeviceUserSettingsOrDefault();
        void SaveVideoDeviceUserSettings();

        Task SetViewRotationAsync(int rotationDeg);
        void SetViewMirroring(bool mirror);


        IMediaCaptureEnhancer Enhancements { get; }


        IAsyncAction CaptureCurrentImageToStreamAsync(ImageEncodingProperties type,
            IRandomAccessStream stream);


        bool CanAutoFocus { get; }
        bool IsAutoFocusing { get; }
        Task SetAutoFocusAsync(bool state);

        uint Focus { get; }
        Task SetFocusAsync(uint value);

        bool CanZoom { get; }
        uint Zoom { get; }
        Task SetZoomAsync(uint value);
    }
}
