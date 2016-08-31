using System.Threading.Tasks;


namespace ICamSee
{
    interface IMediaCaptureEnhancer
    {
        int BrightnessCorrection { get; }
        Task SetBrightnessCorrectionAsync(int value);

        int ContrastCorrection { get; }
        Task SetContrastCorrectionAsync(int value);

        int ExposureCorrection { get; }
        Task SetExposureCorrectionAsync(int value);

        int HueCorrection { get; }
        Task SetHueCorrectionAsync(int value);

        int WhitebalanceCorrection { get; }
        Task SetWhitebalacneCorrectionAsync(int value);


        bool CanCompensateBacklight { get; }
        uint BacklightCompesnation { get; }
        Task SetBacklightCompensationAsync(uint value);

        bool CanHdr { get; }
        bool IsHdr { get; }
        Task SetHdrAsync(bool state);

        bool CanAutoWhitebalance { get; }
        bool IsAutoWhitebalancing { get; }
        Task SetAutoWhitebalanceAsync(bool state);
    }
}
