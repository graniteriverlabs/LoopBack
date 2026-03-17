using GRL.Logging;
using GRL.VDPWR.LoopBackService.Models;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GRL.VDPWR.LoopBackService.Services
{
    /// <summary>
    /// Service for USB LoopBack testing functionality
    /// </summary>
    public class LoopBackService : ILoopBackService
    {
        private readonly ILoggerService _logger;
        private readonly string _configPath;

        /// <summary>
        /// Initializes a new instance of the LoopBackService
        /// </summary>
        /// <param name="logger">Logger service for diagnostics</param>
        /// <param name="configPath">Path to configuration files directory</param>
        public LoopBackService(ILoggerService logger, string? configPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, "ConfigFiles");
        }

        /// <inheritdoc/>
        public async Task<List<DeviceInfo>> LoadDevicesAsync()
        {
            var devices = new List<DeviceInfo>();
            
            try
            {
                _logger.WriteLog("Loopback Test Device scanning", LogType.Information);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    devices = await LoadWindowsDevicesAsync();
                }
                else
                {
                    devices = await LoadLinuxMacDevicesAsync();
                }
                
                await Task.Delay(2000);
                _logger.WriteLog($"Loopback Test Device scan completed. {devices.Count} device(s) found.", LogType.Information);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error loading GRL USB-LoopBack Tester: {ex.Message}", LogType.Error);
            }
            
            return devices;
        }

        /// <inheritdoc/>
        public async Task<LoopBackTestResult> ExecuteLoopBackTestAsync(int vendorId, int productId, bool useDefaultData = true)
        {
            var result = new LoopBackTestResult();
            UsbDevice? usbDevice = null;
            
            try
            {
                int maxPacketSize = 65536; // Buffer size for transfer
                
                // Find and open device
                foreach (UsbRegistry regDevice in UsbDevice.AllDevices)
                {
                    if (regDevice.Vid == vendorId && regDevice.Pid == productId && regDevice.Open(out usbDevice))
                    {
                        _logger.WriteLog($"Opened Device: {regDevice.Name}", LogType.Information);
                        break;
                    }
                }

                if (usbDevice == null)
                {
                    result.Status = "Device not found!";
                    result.IsSuccess = false;
                    _logger.WriteLog("Device not found!", LogType.Warning);
                    return result;
                }

                _logger.WriteLog($"Opened device: VID=0x{vendorId:X4}, PID=0x{productId:X4}", LogType.Information);

                // Claim USB interface
                if (usbDevice is IUsbDevice wholeUsbDevice)
                {
                    if (!wholeUsbDevice.SetConfiguration(1))
                    {
                        result.Status = "Failed to set USB configuration";
                        result.IsSuccess = false;
                        _logger.WriteLog("Failed to set USB configuration.", LogType.Error);
                        return result;
                    }

                    if (!wholeUsbDevice.ClaimInterface(0))
                    {
                        result.Status = "Failed to claim USB interface";
                        result.IsSuccess = false;
                        _logger.WriteLog("Failed to claim USB interface.", LogType.Error);
                        return result;
                    }
                }

                byte[] writeBuffer = new byte[maxPacketSize];
                byte[] readBuffer = new byte[maxPacketSize];

                if (useDefaultData)
                {
                    // Fill buffer with sequential bytes (0,1,2,...255 repeating)
                    for (int i = 0; i < writeBuffer.Length; i++)
                    {
                        writeBuffer[i] = (byte)i;
                    }
                    _logger.WriteLog("Default Data configuration selected", LogType.Information);
                }
                else
                {
                    PopulateWriteBufferFromJson(ref writeBuffer);
                    _logger.WriteLog("User configured data selected", LogType.Information);
                }

                // Execute the test loop
                var testResults = await ExecuteTestLoop(usbDevice, writeBuffer, readBuffer, maxPacketSize);
                
                result.TransferredData = $"{testResults.TotalBytesWritten} Bytes";
                result.ReceivedData = $"{testResults.PassCount} Bytes";
                result.EffectiveThroughput = testResults.ThroughputMbps > 0 ? $"{testResults.ThroughputMbps:F2} Mbps" : "Error";
                result.Status = "Loopback Test Completed";
                result.IsSuccess = true;
                result.WriteThroughputKBps = testResults.WriteThroughputKBps;
                result.ReadThroughputKBps = testResults.ReadThroughputKBps;
                result.PassedBytes = testResults.PassCount;
                result.FailedBytes = testResults.FailCount;

                // Log results
                _logger.WriteLog($"Write Throughput: {testResults.WriteThroughputKBps:F2} KB/s", LogType.Information);
                _logger.WriteLog($"Read Throughput: {testResults.ReadThroughputKBps:F2} KB/s", LogType.Information);
                _logger.WriteLog($"Failed Bytes: {testResults.FailCount}", LogType.Warning);
                
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error: {ex.Message}", LogType.Error);
                result.Status = "Error";
                result.EffectiveThroughput = "Error";
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                if (usbDevice is IUsbDevice wholeUsbDevice)
                {
                    wholeUsbDevice.ReleaseInterface(0);
                }
                usbDevice?.Close();
            }
            
            return result;
        }

        /// <inheritdoc/>
        public (int VendorId, int ProductId) ParseDeviceId(string deviceId)
        {
            var match = Regex.Match(deviceId, @"VID_0x([A-Fa-f0-9]+)\s+PID_0x([A-Fa-f0-9]+)");
            if (match.Success)
            {
                return (Convert.ToInt32(match.Groups[1].Value, 16), Convert.ToInt32(match.Groups[2].Value, 16));
            }
            return (0, 0);
        }

        #region Private Helper Methods

        /// <summary>
        /// Executes the main test loop for throughput calculation
        /// </summary>
        private async Task<(int TotalBytesWritten, int TotalBytesRead, int PassCount, int FailCount, double ThroughputMbps, double WriteThroughputKBps, double ReadThroughputKBps)> ExecuteTestLoop(UsbDevice usbDevice, byte[] writeBuffer, byte[] readBuffer, int maxPacketSize)
        {
            int totalBytesWritten = 0;
            int totalBytesRead = 0;
            int bytesWritten = 0;
            int bytesRead = 0;
            int passCount = 0;
            int failCount = 0;

            UsbEndpointWriter writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
            UsbEndpointReader reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _logger.WriteLog("Starting write and read operations for 500 iterations...", LogType.Information);

            List<long> elapsedTimeTicks = new List<long>();

            await Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)  // Execute exactly 500 write-read cycles
                {
                    DateTime startTime = DateTime.Now;

                    // Write to USB
                    ErrorCode writeResult = writer.Write(writeBuffer, 5000, out bytesWritten);
                    if (writeResult == ErrorCode.None)
                    {
                        totalBytesWritten += bytesWritten;
                    }
                    
                    // Read from USB
                    ErrorCode readResult = reader.Read(readBuffer, 5000, out bytesRead);
                    int retryCount = 0;

                    // Retry mechanism for read failure
                    while (readResult != ErrorCode.None && retryCount < 2)
                    {
                        readResult = reader.Read(readBuffer, 10_000, out bytesRead);
                        retryCount++;
                    }

                    // If read still fails, log and exit the loop
                    if (readResult != ErrorCode.None)
                    {
                        break;
                    }

                    // Update total bytes read
                    totalBytesRead += bytesRead;

                    // Data verification with bounds check
                    for (int j = 0; j < bytesRead; j++)
                    {
                        if (j < writeBuffer.Length && writeBuffer[j] == readBuffer[j])
                            passCount++;
                        else
                            failCount++;
                    }
                    
                    DateTime endTime = DateTime.Now;
                    elapsedTimeTicks.Add(Math.Abs(startTime.Ticks - endTime.Ticks));
                }
            });
            
            stopwatch.Stop();
            
            double throughputMbps = 0;
            if (elapsedTimeTicks.Count > 0)
            {
                // Compute throughput in Mbps
                long avgTime = (long)elapsedTimeTicks.Average();
                double timeSpanSeconds = new TimeSpan(avgTime).TotalSeconds;
                throughputMbps = timeSpanSeconds > 0 ? (((maxPacketSize * 8 * 2) / 1_000_000.0) / timeSpanSeconds) : 0;
            }

            double writeThroughputKBps = (totalBytesWritten / stopwatch.Elapsed.TotalSeconds) / 1024;
            double readThroughputKBps = (totalBytesRead / stopwatch.Elapsed.TotalSeconds) / 1024;

            return (totalBytesWritten, totalBytesRead, passCount, failCount, throughputMbps, writeThroughputKBps, readThroughputKBps);
        }

        /// <summary>
        /// Loads Windows USB devices using LibUsbDotNet
        /// </summary>
        private async Task<List<DeviceInfo>> LoadWindowsDevicesAsync()
        {
            var devices = new List<DeviceInfo>();
            
            await Task.Run(() =>
            {
                // Get all connected USB devices
                UsbRegDeviceList usbDevices = UsbDevice.AllDevices;

                foreach (UsbRegistry regDevice in usbDevices)
                {
                    if (regDevice.Open(out UsbDevice usbDevice))
                    {
                        try
                        {
                            try
                            {
                                // Read the JSON file
                                LoopBackViewModelInfo? jsonData = GetJsonContent();
                                if (jsonData != null)
                                {
                                    // Log the GRLUSBDeviceType
                                    _logger.WriteLog($"GRLUSBDeviceType: {jsonData?.GRLUSBDeviceType}", LogType.Information);

                                    string deviceName = regDevice.Name;
                                    string deviceID = $"VID_0x{usbDevice.Info.Descriptor.VendorID:X4} PID_0x{usbDevice.Info.Descriptor.ProductID:X4}";

                                    if (!string.IsNullOrEmpty(deviceName) && deviceName.IndexOf($"{jsonData?.GRLUSBDeviceType}", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        devices.Add(new DeviceInfo { DeviceName = deviceName, DeviceID = deviceID });
                                    }
                                }
                                else
                                {
                                    _logger.WriteLog("Error reading JSON: JSON content is null", LogType.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.WriteLog($"Error reading JSON: {ex.Message}", LogType.Error);
                            }
                        }
                        finally
                        {
                            usbDevice.Close();
                        }
                    }
                }
            });

            return devices;
        }

        /// <summary>
        /// Loads USB devices on Linux/Mac using system commands
        /// </summary>
        private async Task<List<DeviceInfo>> LoadLinuxMacDevicesAsync()
        {
            var devices = new List<DeviceInfo>();
            
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            string command = isMac ? "system_profiler SPUSBDataType" : "lsusb";
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = isMac ? "/bin/sh" : "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process == null)
                {
                    _logger.WriteLog("Failed to start process.", LogType.Error);
                    return devices;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                //foreach (string line in output.Split(new[] { '\n' }))
                //{
                //    if (!string.IsNullOrWhiteSpace(line) && line.Contains("GRL USB-LoopBack Tester"))
                //    {
                //        devices.Add(new DeviceInfo { DeviceName = line.Trim(), DeviceID = line.Trim() });
                //    }
                //}
                foreach (string line in output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Contains("227f:0005", StringComparison.OrdinalIgnoreCase))
                    {
                        devices.Add(new DeviceInfo
                        {
                            DeviceName = "GRL Loopback",
                            DeviceID = "227f:0005"
                        });
                    }
                }
            }

            return devices;
        }

        /// <summary>
        /// Reads JSON configuration content
        /// </summary>
        public LoopBackViewModelInfo? GetJsonContent()
        {
            try
            {
                // Navigate to the project root dynamically
                string filePath = Path.Combine(_configPath, "LoopBackViewModelInfo.json");

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    _logger.WriteLog($"File not found: {filePath}", LogType.Error);
                    return null;
                }

                // Read JSON content
                string jsonContent = File.ReadAllText(filePath);

                // Deserialize JSON into an object and return
                return JsonSerializer.Deserialize<LoopBackViewModelInfo>(jsonContent);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error reading JSON file: {ex.Message}", LogType.Error);
                return null; // Return null if an error occurs
            }
        }

        /// <summary>
        /// Populates write buffer with data from JSON configuration
        /// </summary>
        public void PopulateWriteBufferFromJson(ref byte[] writeBuffer)
        {
            try
            {
                // Get the JSON content by calling GetJsonContent
                LoopBackViewModelInfo? jsonData = GetJsonContent();
                StringBuilder sb = new StringBuilder();
                
                if (jsonData != null)
                {
                    // Check if LoopBackData is not null or empty
                    if (!string.IsNullOrEmpty(jsonData.LoopBackData))
                    {
                        // Split the LoopBackData string into individual hex values
                        string[] hexValues = jsonData.LoopBackData.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        // Create a byte array to store the converted hex values
                        byte[] convertedBytes = new byte[hexValues.Length];

                        // Convert each hex value to byte and store it in the convertedBytes array
                        for (int i = 0; i < hexValues.Length; i++)
                        {
                            // Remove "0x" prefix and parse the hex value
                            if (hexValues[i].StartsWith("0x"))
                            {
                                convertedBytes[i] = Convert.ToByte(hexValues[i].Substring(2), 16); // Convert from hex to byte
                            }
                            else
                            {
                                // Log invalid hex format if needed
                                _logger.WriteLog($"Invalid hex format at index {i}: {hexValues[i]}", LogType.Error);
                            }
                        }

                        if (convertedBytes.Length == 0)
                        {
                            _logger.WriteLog("convertedBytes is empty. No data to write.", LogType.Warning);
                            return;
                        }

                        int dataIndex = 0;

                        for (int i = 0; i < writeBuffer.Length; i++)
                        {
                            writeBuffer[i] = convertedBytes[dataIndex];
                            dataIndex = (dataIndex + 1) % convertedBytes.Length; // Wrap around

                            sb.Append($"0x{writeBuffer[i]:X2}");
                            if (i < writeBuffer.Length - 1)
                                sb.Append(", ");
                        }

                        _logger.WriteLog($"LoopBackData is {sb}", LogType.Information);
                    }
                    else
                    {
                        _logger.WriteLog("LoopBackData is empty or null", LogType.Error);
                    }
                }
                else
                {
                    _logger.WriteLog("Error reading JSON: JSON content is null", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Error in PopulateWriteBufferFromJson: {ex.Message}", LogType.Error);
            }
        }

        #endregion
    }
}
