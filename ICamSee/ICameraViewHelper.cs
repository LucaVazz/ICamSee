using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Storage.Streams;


namespace ICamSee
{
    interface ICameraViewHelper
    {
        MediaCapture MediaCapture { get; }
        Task Initialize(DeviceInformation deviceToUse);
        Task StartView();
        Task ReInitialize();
        Task Deinitialize();
        Task StopView();

        Task SetViewRotationAsync(int rotationDeg);


        bool CanAutoFocus { get; }
        bool IsAutoFocusing { get; }
        double FocusStep { get; }
        double FocusMin { get; }
        double FocusMax { get; }

        bool SetAutoFocus(bool state);
        bool ToggleAutoFocus();

        bool CanFocus { get; }
        bool SetFocus(uint value);

        bool CanZoom { get; }
        double ZoomStep { get; }
        double ZoomMin { get; }
        double ZoomMax { get; }

        bool SetZoom(double value);
        bool ChangeZoomByOneStep(int stepMultiplier = 1);

    }
}
