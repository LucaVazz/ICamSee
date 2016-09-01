using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage;

namespace ICamSee
{
    /// <summary>
    /// A class to ease some tasks while managing and selecting the avaiable VideoDevices.
    /// </summary>
    class VideoDevicesManager : IVideoDevicesManager
    {
        /// <summary>
        /// Searches for VideoDevices avaialbe for use
        /// </summary>
        /// <returns>all available DeviceInforamtions inside a Collection</returns>
        public async Task<DeviceInformationCollection> GetAllAvailableDevicesAsync()
        {
            return await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        }

        /// <summary>
        /// Determinies the VideoDevice to use by default. If the user has specified
        /// a Device he personally wants to use by default, its Info is returned.
        /// Otherwise or if this Device is unavailable, the System's default Device is used.
        /// If no VideoDevices are available, an InvalidOperationException will be thrown.
        /// </summary>
        /// <returns>The DeviceInformaton about the Device</returns>
        public async Task<DeviceInformation> GetDefaultDeviceAsync()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("UserDefault")) {
                string UserDefaultId = (string)localSettings.Values["UserDefault"];

                DeviceInformation UserDefaultDevice = (await GetAllAvailableDevicesAsync())
                    .First(info => info.Id == UserDefaultId);   // could throw InvalidOperationException

                return UserDefaultDevice;
            }

            DeviceInformation SystemDefault = (await GetAllAvailableDevicesAsync())
                .First();   // could throw InvalidOperationException
            return SystemDefault;
        }

        /// <summary>
        /// Stores the given VideoDevice as the User's default choice (via its ID) 
        /// inside ApplicationData.LocalSettings.
        /// </summary>
        /// <param name="device"></param>
        public void SetUserDefaultDevice(DeviceInformation device)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values["UserDefault"] = device.Id;
        }
    }
}
