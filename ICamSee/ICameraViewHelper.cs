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
        MediaCapture MediaCapture { get; }
        Task InitializeAsync(DeviceInformation videoDevice);
        Task DeactivateAsync();

        Task SetViewRotationAsync(int rotationDeg);


        Task<InMemoryRandomAccessStream> CaptureCurrentImageToStreamAsync();


        bool CanAutoFocus { get; }
        bool IsAutoFocusing { get; }
        void SetAutoFocus(bool state);

        bool CanFocus { get; }
        void SetFocus(uint value);

        bool CanZoom { get; }
        void SetZoom(uint value);
    }
}
