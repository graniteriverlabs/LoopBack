using GRL.VDPWR.LoopBackService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GRL.VDPWR.LoopBackService.Services
{
    /// <summary>
    /// Interface for LoopBack USB testing service
    /// </summary>
    public interface ILoopBackService
    {
        /// <summary>
        /// Loads available USB devices that match the LoopBack criteria
        /// </summary>
        /// <returns>List of available devices</returns>
        Task<List<DeviceInfo>> LoadDevicesAsync();

        /// <summary>
        /// Executes the USB loopback test
        /// </summary>
        /// <param name="vendorId">USB Vendor ID</param>
        /// <param name="productId">USB Product ID</param>
        /// <param name="useDefaultData">Whether to use default sequential data or JSON configured data</param>
        /// <returns>Test result with throughput and status information</returns>
        Task<LoopBackTestResult> ExecuteLoopBackTestAsync(int vendorId, int productId, bool useDefaultData = true);

        /// <summary>
        /// Parses device ID string to extract VID and PID
        /// </summary>
        /// <param name="deviceId">Device ID string in format "VID_0xXXXX PID_0xYYYY"</param>
        /// <returns>Tuple containing VendorId and ProductId</returns>
        (int VendorId, int ProductId) ParseDeviceId(string deviceId);
    }
}
