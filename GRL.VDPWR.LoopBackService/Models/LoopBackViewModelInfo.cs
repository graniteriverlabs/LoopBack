namespace GRL.VDPWR.LoopBackService.Models
{
    /// <summary>
    /// Configuration model for LoopBack test data
    /// </summary>
    public class LoopBackViewModelInfo
    {
        /// <summary>
        /// GRL USB Device Type identifier
        /// </summary>
        public string GRLUSBDeviceType { get; set; } = string.Empty;

        /// <summary>
        /// LoopBack test data configuration (comma-separated hex values)
        /// </summary>
        public string LoopBackData { get; set; } = string.Empty;
    }
}
