namespace GRL.VDPWR.LoopBackService.Models
{
    /// <summary>
    /// USB Device information
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Human-readable device name
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Device identifier string (typically "VID_0xXXXX PID_0xYYYY" format)
        /// </summary>
        public string DeviceID { get; set; } = string.Empty;
        public string DeviceSerialNo { get; set; } = string.Empty;

    }
}
