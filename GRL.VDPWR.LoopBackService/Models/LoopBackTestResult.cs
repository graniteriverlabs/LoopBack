namespace GRL.VDPWR.LoopBackService.Models
{
    /// <summary>
    /// Result object for USB LoopBack test execution
    /// </summary>
    public class LoopBackTestResult
    {
        /// <summary>
        /// Test execution status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Amount of data transferred to the device
        /// </summary>
        public string TransferredData { get; set; } = string.Empty;

        /// <summary>
        /// Amount of data successfully received back from device
        /// </summary>
        public string ReceivedData { get; set; } = string.Empty;

        /// <summary>
        /// Calculated effective throughput in Mbps
        /// </summary>
        public string EffectiveThroughput { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the test completed successfully
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if test failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Write throughput in KB/s
        /// </summary>
        public double WriteThroughputKBps { get; set; }

        /// <summary>
        /// Read throughput in KB/s
        /// </summary>
        public double ReadThroughputKBps { get; set; }

        /// <summary>
        /// Number of bytes that passed verification
        /// </summary>
        public int PassedBytes { get; set; }

        /// <summary>
        /// Number of bytes that failed verification
        /// </summary>
        public int FailedBytes { get; set; }

        /// <summary>
        /// Total test iterations/passes completed
        /// </summary>
        public int TotalPasses { get; set; }

        /// <summary>
        /// Total test iterations that encountered errors
        /// </summary>
        public int TotalFailures { get; set; }
    }
}
