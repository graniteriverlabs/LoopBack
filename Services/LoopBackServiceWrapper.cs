using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GRL.VDPWR.LoopBackService.Services;
using GRL.VDPWR.LoopBackService.Models;
using GRL.Logging;
using System.Dynamic;

namespace GrlC2ApiLib
{
    /// <summary>
    /// Wrapper class for GRL LoopBack Service DLL.
    /// Provides a simplified interface for running loopback tests on USB-C devices.
    /// This wrapper can be used in both C2APIScript (console applications) and C2WebApp (web controllers).
    /// 
    /// IMPORTANT: Requires a configuration file at [ConfigPath]\LoopBackViewModelInfo.json
    /// Default config path: [AppDirectory]\ConfigFiles\
    /// See LOOPBACK_CONFIG_GUIDE.md for configuration details.
    /// </summary>
    public class GrlC2LoopBackService : IDisposable
    {
        #region Private Fields
        private readonly ILoopBackService _loopBackService;
        private readonly ILoggerService _logger;
        private readonly string _configPath;
        private bool _isInitialized = false;
        private bool _disposed = false;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the GrlC2LoopBackService wrapper.
        /// </summary>
        /// <param name="logger">Optional logger service. If null, a default console logger will be created.</param>
        /// <param name="configPath">Optional configuration path for device data. 
        /// If null, defaults to [AppDirectory]\ConfigFiles\
        /// Must contain LoopBackViewModelInfo.json file.</param>
        public GrlC2LoopBackService(ILoggerService logger = null, string configPath = null)
        {
            try
            {
                // Create a default logger if none provided
                _logger = logger ?? new ConsoleLoggerService();

                // Set config path (null will use default: AppDirectory\ConfigFiles)
                _configPath = configPath;

                // Log the config path being used
                string effectivePath = configPath ?? Path.Combine(AppContext.BaseDirectory, "ConfigFiles");
                _logger.LogInformation($"Initializing LoopBackService with config path: {effectivePath}");

                // Verify config file exists (optional warning)
                string configFile = Path.Combine(effectivePath, "LoopBackViewModelInfo.json");
                if (!File.Exists(configFile))
                {
                    _logger.LogWarning($"Config file not found: {configFile}. Device loading may fail. See LOOPBACK_CONFIG_GUIDE.md");
                }

                // Initialize the LoopBackService with logger and optional config path
                _loopBackService = new LoopBackService(_logger, configPath);
                _isInitialized = true;

                _logger.LogInformation("LoopBackService initialized successfully.");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                _logger?.LogError("Failed to initialize LoopBackService.", ex);
                throw new InvalidOperationException("Failed to initialize LoopBackService. Ensure all required DLLs and config files are present.", ex);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating whether the service is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the configuration path being used by the service.
        /// Returns the effective path (default if none was specified).
        /// </summary>
        public string ConfigPath => _configPath ?? Path.Combine(AppContext.BaseDirectory, "ConfigFiles");

        /// <summary>
        /// Gets the full path to the config file.
        /// </summary>
        public string ConfigFilePath => Path.Combine(ConfigPath, "LoopBackViewModelInfo.json");

        /// <summary>
        /// Checks if the required config file exists.
        /// </summary>
        public bool ConfigFileExists => File.Exists(ConfigFilePath);
        #endregion

        #region Public Methods

        /// <summary>
        /// Loads all available GRL USB LoopBack devices connected to the system.
        /// Requires LoopBackViewModelInfo.json config file to identify device type.
        /// </summary>
        /// <returns>List of detected devices with VendorId, ProductId, and Name.</returns>
        public async Task<List<DeviceInfo>> GetLoopbackDevices()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("LoopBackService is not initialized.");

            _logger.LogInformation("Loading loopback devices...");
            return await _loopBackService.LoadDevicesAsync();
        }

        /// <summary>
        /// Parses and retrieves the hardware ID of the loopback device.
        /// </summary>
        /// <param name="deviceId">The device ID string to parse.</param>
        /// <returns>The parsed device hardware information.</returns>
        public dynamic GetLoopbackDeviceHardwareId(string deviceId)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("LoopBackService is not initialized.");

            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("DeviceId cannot be null or whitespace.", nameof(deviceId));

            _logger.LogInformation($"Parsing loopback device hardware ID: {deviceId}");
            return _loopBackService.ParseDeviceId(deviceId);
        }

        /// <summary>
        /// Strongly typed helper to extract VendorId and ProductId from deviceId.
        /// </summary>
        /// <param name="deviceId">Raw device identifier.</param>
        /// <returns>Tuple (VendorId, ProductId)</returns>
        public (int VendorId, int ProductId) GetVendorProductIds(string deviceId)
        {
            var dyn = GetLoopbackDeviceHardwareId(deviceId);
            try
            {
                int vid = dyn.Item1;
                int pid = dyn.Item2;
                return (vid, pid);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to parse Vendor/Product IDs from '{deviceId}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Starts the loopback test execution asynchronously and returns the underlying result object.
        /// </summary>
        /// <param name="vendorId">Vendor ID (VID) of the device.</param>
        /// <param name="productId">Product ID (PID) of the device.</param>
        /// <param name="useDefaultData">True to use default data set, false to use configured data.</param>
        /// <returns>Dynamic result object returned by the underlying service (e.g., LoopBackTestResult).</returns>
        public async Task<dynamic> StartLoopBackExecutionAsync(int vendorId, int productId, bool useDefaultData)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("LoopBackService is not initialized.");

            _logger.LogInformation($"Starting loopback test (VID={vendorId}, PID={productId}, DefaultData={useDefaultData})");
            var result = await _loopBackService.ExecuteLoopBackTestAsync(vendorId, productId, useDefaultData);
            return result;
        }

        /// <summary>
        /// Synchronous convenience wrapper for <see cref="StartLoopBackExecutionAsync"/>.
        /// </summary>
        public dynamic StartLoopBackExecution(int vendorId, int productId, bool useDefaultData)
        {
            return StartLoopBackExecutionAsync(vendorId, productId, useDefaultData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs a loopback test on the specified device.
        /// </summary>
        /// <param name="deviceInfo">Device information including VID, PID, and port details.</param>
        /// <returns>Test result containing success status, data, and any errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when deviceInfo is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when service is not initialized.</exception>
        //public LoopBackTestResult RunLoopBackTest(DeviceInfo deviceInfo)
        //{
        //    if (!_isInitialized)
        //        throw new InvalidOperationException("LoopBackService is not initialized.");

        //    if (deviceInfo == null)
        //        throw new ArgumentNullException(nameof(deviceInfo), "DeviceInfo cannot be null.");

        //    try
        //    {
        //        return _loopBackService.RunLoopBackTest(deviceInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Return a failed result with error details
        //        return new LoopBackTestResult
        //        {
        //            Success = false,
        //            ErrorMessage = $"Test execution failed: {ex.Message}",
        //            Exception = ex
        //        };
        //    }
        //}

        /// <summary>
        /// Runs a loopback test asynchronously on the specified device.
        /// </summary>
        /// <param name="deviceInfo">Device information including VID, PID, and port details.</param>
        /// <returns>Task containing the test result.</returns>
        //public async Task<LoopBackTestResult> RunLoopBackTestAsync(DeviceInfo deviceInfo)
        //{
        //    return await Task.Run(() => RunLoopBackTest(deviceInfo));
        //}

        /// <summary>
        /// Creates a DeviceInfo object with the specified parameters.
        /// </summary>
        /// <param name="vendorId">USB Vendor ID (VID).</param>
        /// <param name="productId">USB Product ID (PID).</param>
        /// <param name="portNumber">Port number (optional).</param>
        /// <param name="serialNumber">Device serial number (optional).</param>
        /// <returns>Configured DeviceInfo object.</returns>
        //public DeviceInfo CreateDeviceInfo(int vendorId, int productId, int? portNumber = null, string serialNumber = null)
        //{
        //    var deviceInfo = new DeviceInfo
        //    {
        //        VendorId = vendorId,
        //        ProductId = productId
        //    };

        //    if (portNumber.HasValue)
        //        deviceInfo.PortNumber = portNumber.Value;

        //    if (!string.IsNullOrEmpty(serialNumber))
        //        deviceInfo.SerialNumber = serialNumber;

        //    return deviceInfo;
        //}

        ///// <summary>
        ///// Validates device information before running a test.
        ///// </summary>
        ///// <param name="deviceInfo">Device information to validate.</param>
        ///// <returns>True if valid, false otherwise.</returns>
        //public bool ValidateDeviceInfo(DeviceInfo deviceInfo)
        //{
        //    if (deviceInfo == null)
        //        return false;

        //    if (deviceInfo.VendorId <= 0 || deviceInfo.ProductId <= 0)
        //        return false;

        //    return true;
        //}

        /// <summary>
        /// Runs multiple loopback tests sequentially on different devices.
        /// </summary>
        /// <param name="deviceInfoList">List of devices to test.</param>
        /// <returns>Dictionary mapping device info to test results.</returns>
        //public Dictionary<DeviceInfo, LoopBackTestResult> RunBatchTests(List<DeviceInfo> deviceInfoList)
        //{
        //    if (!_isInitialized)
        //        throw new InvalidOperationException("LoopBackService is not initialized.");

        //    if (deviceInfoList == null || !deviceInfoList.Any())
        //        throw new ArgumentException("Device list cannot be null or empty.", nameof(deviceInfoList));

        //    var results = new Dictionary<DeviceInfo, LoopBackTestResult>();

        //    foreach (var device in deviceInfoList)
        //    {
        //        try
        //        {
        //            var result = RunLoopBackTest(device);
        //            results[device] = result;
        //        }
        //        catch (Exception ex)
        //        {
        //            results[device] = new LoopBackTestResult
        //            {
        //                Success = false,
        //                ErrorMessage = $"Batch test failed: {ex.Message}",
        //                Exception = ex
        //            };
        //        }
        //    }

        //    return results;
        //}

        /// <summary>
        /// Runs multiple loopback tests asynchronously on different devices.
        /// </summary>
        /// <param name="deviceInfoList">List of devices to test.</param>
        /// <returns>Task containing dictionary mapping device info to test results.</returns>
        //public async Task<Dictionary<DeviceInfo, LoopBackTestResult>> RunBatchTestsAsync(List<DeviceInfo> deviceInfoList)
        //{
        //    return await Task.Run(() => RunBatchTests(deviceInfoList));
        //}

        /// <summary>
        /// Disposes resources used by the LoopBackService.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helper Methods


        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the LoopBackService.
        /// </summary>
        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        /// <summary>
        /// Protected dispose method.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                if (_loopBackService is IDisposable disposable) disposable.Dispose();
                if (_logger is IDisposable loggerDisposable) loggerDisposable.Dispose();
            }

            _disposed = true;
            _isInitialized = false;
        }

        #endregion
    }

    /// <summary>
    /// Simple console logger service for when no logger is provided.
    /// </summary>
    internal class ConsoleLoggerService : ILoggerService
    {
        public void LogInformation(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {message}");
        }

        public void LogError(string message, Exception? ex = null)
        {
            Console.WriteLine($"[ERROR] {message}");
            if (ex != null)
                Console.WriteLine($"Exception: {ex.Message}");
        }

        public void WriteLog(string message, LogType logType, Exception? ex = null)
        {
            switch (logType)
            {
                case LogType.Information:
                    LogInformation(message);
                    break;
                case LogType.Warning:
                    LogWarning(message);
                    break;
                case LogType.Error:
                    LogError(message, ex);
                    break;
                default:
                    Console.WriteLine(message);
                    break;
            }
        }

        public void CloseLogger()
        {
            // No-op for console logger
        }

        public Task<string> ConvertBytesToString(List<byte> bufData, int printLength = -1)
        {
            if (bufData == null || !bufData.Any())
                return Task.FromResult(string.Empty);

            int length = printLength > 0 ? Math.Min(printLength, bufData.Count) : bufData.Count;
            return Task.FromResult(BitConverter.ToString(bufData.Take(length).ToArray()).Replace("-", " "));
        }
    }
}
