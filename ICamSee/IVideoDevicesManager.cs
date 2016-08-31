using System.Threading.Tasks;
using Windows.Devices.Enumeration;


namespace ICamSee
{
    interface IVideoDevicesManager
    {
        Task<DeviceInformationCollection> GetAllAvailableDevicesAsync();
        Task<DeviceInformation> GetDefaultDeviceAsync();
        void SetUserDefaultDevice();
    }
}
