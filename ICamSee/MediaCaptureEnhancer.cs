using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Devices;

namespace ICamSee
{
    class MediaCaptureEnhancer : IMediaCaptureEnhancer
    {
        private MediaCapture ModifiedCapture;


        enum EnahnceTechnique
        {
            VideoEffect,
            DeviceControl
        }

        private EnahnceTechnique BrightnessCorrectionTechnique;
        private EnahnceTechnique ContrastCorrectionTechnique;
        private EnahnceTechnique ExposureCorrectionTechnique;
        private EnahnceTechnique HueCorrectionTechnique;
        private EnahnceTechnique WhitebalanceCorrectionTechnique;
        public bool CanCompensateBacklight { get; }
        public bool CanHdr { get; }
        public bool CanAutoWhitebalance { get; }

        /* -50 <= value <= +50 */
        public int BrightnessCorrection { get; }
        public int ContrastCorrection { get; }
        public int ExposureCorrection { get; }
        public int HueCorrection { get; }
        public int WhitebalanceCorrection { get; }
        /* 0 <= value <= 100 */
        public uint BacklightCompesnation { get; }
        /* value := bool */
        public bool IsAutoWhitebalancing { get; }
        public bool IsHdr { get; }



        /// <summary></summary>
        /// <param name="capture">The MediaCapture-Object whose Captures will be enhanced.</param>
        public MediaCaptureEnhancer(MediaCapture capture)
        {
            this.ModifiedCapture = capture;

            VideoDeviceController device = capture.VideoDeviceController;

            this.BrightnessCorrectionTechnique   = DetermineTechnique(device.Brightness);
            this.BrightnessCorrection         = DetermineDefaultValue(device.Brightness);

            this.ContrastCorrectionTechnique     = DetermineTechnique(device.Contrast);
            this.ContrastCorrection           = DetermineDefaultValue(device.Contrast);

            this.ExposureCorrectionTechnique     = DetermineTechnique(device.Exposure);
            this.ExposureCorrection           = DetermineDefaultValue(device.Exposure);

            this.HueCorrectionTechnique          = DetermineTechnique(device.Hue);
            this.HueCorrection                = DetermineDefaultValue(device.Hue);

            this.WhitebalanceCorrectionTechnique = DetermineTechnique(device.WhiteBalance);
            this.WhitebalanceCorrection       = DetermineDefaultValue(device.WhiteBalance);

            this.CanCompensateBacklight = device.BacklightCompensation.Capabilities.Supported;
            this.BacklightCompesnation        = (uint)DetermineDefaultValue(device.BacklightCompensation);

            this.CanHdr = device.HdrVideoControl.Supported;
            this.IsHdr = device.HdrVideoControl.Mode.Equals(HdrVideoMode.On);

            this.CanAutoWhitebalance = device.WhiteBalance.Capabilities.AutoModeSupported;
            bool autoWhitebalancingActivated;
            device.WhiteBalance.TryGetAuto(out autoWhitebalancingActivated);
            this.IsAutoWhitebalancing = autoWhitebalancingActivated;                                    
        }

        /// <summary>
        /// Determines if the MediaDevice supports the DeviceControl or if otherwise a VideoEffect
        /// needs to be used.
        /// </summary>
        /// <param name="controlToCheck">A DeviceControl from the VideoDeviceController</param>
        /// <returns></returns>
        private EnahnceTechnique DetermineTechnique(MediaDeviceControl controlToCheck)
        {
            return (controlToCheck.Capabilities.Supported)
                ? EnahnceTechnique.DeviceControl
                : EnahnceTechnique.VideoEffect
            ;
        }

        private int DetermineDefaultValue(MediaDeviceControl controlToCheck)
        {
            return (int)(controlToCheck.Capabilities.Default);
        }



        public async Task SetBrightnessCorrectionAsync(int value)
        {
            throw new NotImplementedException();
        }

        public async Task SetContrastCorrectionAsync(int value)
        {
            throw new NotImplementedException();
        }

        public async Task SetExposureCorrectionAsync(int value)
        {
            throw new NotImplementedException();
        }

        public async Task SetHueCorrectionAsync(int value)
        {
            throw new NotImplementedException();
        }

        public async Task SetWhitebalacneCorrectionAsync(int value)
        {
            throw new NotImplementedException();
        }


        public async Task SetBacklightCompensationAsync(uint value)
        {
            throw new NotImplementedException();
        }

        public async Task SetAutoWhitebalanceAsync(bool state)
        {
            throw new NotImplementedException();
        }

        public async Task SetHdrAsync(bool state)
        {
            throw new NotImplementedException();
        }
    }
}
