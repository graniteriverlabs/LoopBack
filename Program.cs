using GRL.Logging;
using GRL.VDPWR.LoopBackService.Models;
using GrlC2ApiLib;
using Serilog;
using System;
using System.Collections.Generic;
namespace LoopBack
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var loopBackService = new GrlC2LoopBackService();

            List<DeviceInfo> devices = loopBackService.GetLoopbackDevices().Result;

            if (devices == null || devices.Count == 0)
            {
                Console.WriteLine("No loopback devices found.");
                return;
            }

            // Display available devices
            Console.WriteLine("Available Loopback Devices:");
            Console.WriteLine("============================");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {devices[i].DeviceName}");
            }

            // Prompt user to select a device
            Console.WriteLine("\nEnter the number of the device you want to use:");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int selectedIndex) || selectedIndex < 1 || selectedIndex > devices.Count)
            {
                Console.WriteLine("Invalid selection. Exiting.");
                return;
            }

            DeviceInfo selectedDevice = devices[selectedIndex - 1];
            Console.WriteLine($"\nSelected Device: {selectedDevice.DeviceName}");

            // Execute operations on selected device
            ExecuteDeviceOperations(selectedDevice, loopBackService);
        }
        static void ExecuteDeviceOperations(DeviceInfo device, GrlC2LoopBackService loopBackService)
        {
            Console.WriteLine($"\nExecuting operations on device: {device.DeviceName}");

            var hardwareIdInfo = loopBackService.GetLoopbackDeviceHardwareId(device.DeviceID);

            int vendorId = hardwareIdInfo.Item1;
            int productId = hardwareIdInfo.Item2;

            Console.WriteLine("\n=== Execution Options ===");
            Console.WriteLine("1. Default Data");
            Console.WriteLine("2. Configure Data");
            Console.WriteLine("\nEnter your choice (1 or 2):");

            string? choice = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(choice) || !int.TryParse(choice, out int executionChoice))
            {
                Console.WriteLine("Invalid choice. Using default data.");
                executionChoice = 1;
            }

            dynamic testResult = executionChoice == 2
                ? ExecuteWithConfigureData(device, vendorId, productId, loopBackService)
                : ExecuteWithDefaultData(device, vendorId, productId, loopBackService);

            LoopBackTestResult? loopBackTestResult = testResult as LoopBackTestResult;

            Console.WriteLine("\n=== LoopBack Test Result ===");
            if (loopBackTestResult != null)
            {
                // Use wrapper's formatter first (concise summary)
                try
                {
                    string formatted = loopBackService.FormatTestResult(loopBackTestResult);
                    Console.WriteLine(formatted);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Formatter failed: {ex.Message}");
                }

                // Full dump of all public properties & fields
                PrintObjectDetails(loopBackTestResult, "Detailed Property Dump");
            }
            else
            {
                Console.WriteLine("Result was not a LoopBackTestResult type. Dumping dynamic object:");
                PrintObjectDetails(testResult, "Dynamic Object Dump");
            }
            Console.WriteLine("==============================");
        }

        static dynamic ExecuteWithDefaultData(DeviceInfo device, int vendorId, int productId, GrlC2LoopBackService loopBackService)
        {
            Console.WriteLine("\n--- Executing with Default Data ---");
            return loopBackService.StartLoopBackExecution(vendorId, productId, true);
        }

        static dynamic ExecuteWithConfigureData(DeviceInfo device, int vendorId, int productId, GrlC2LoopBackService loopBackService)
        {
            Console.WriteLine("\n--- Executing with Configure Data ---");
            Console.Write("Data size (bytes) [default 1024]: ");
            string? dataSizeInput = Console.ReadLine();
            int dataSize = int.TryParse(dataSizeInput, out int size) ? size : 1024;

            Console.Write("Number of iterations [default 1]: ");
            string? iterationsInput = Console.ReadLine();
            int iterations = int.TryParse(iterationsInput, out int iter) ? iter : 1;

            Console.WriteLine($"Using DataSize={dataSize}, Iterations={iterations}");
            // Parameters currently not passed to underlying API; extend when available.
            return loopBackService.StartLoopBackExecution(vendorId, productId, false);
        }

        // Generic reflection-based object printer for comprehensive inspection.
        static void PrintObjectDetails(object? obj, string header)
        {
            Console.WriteLine($"--- {header} ---");
            if (obj == null)
            {
                Console.WriteLine("<null>");
                return;
            }
            Type t = obj.GetType();
            Console.WriteLine($"Type: {t.FullName}");

            // Properties
            var props = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (props.Length == 0)
            {
                Console.WriteLine("(No public properties)");
            }
            foreach (var p in props)
            {
                object? value = null;
                try { value = p.GetValue(obj); } catch (Exception ex) { value = $"<error: {ex.Message}>"; }
                Console.WriteLine($"Property {p.Name}: {FormatValue(value)}");
            }

            // Fields
            var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (fields.Length > 0)
            {
                foreach (var f in fields)
                {
                    object? value = null;
                    try { value = f.GetValue(obj); } catch (Exception ex) { value = $"<error: {ex.Message}>"; }
                    Console.WriteLine($"Field {f.Name}: {FormatValue(value)}");
                }
            }
        }

        static string FormatValue(object? value)
        {
            if (value == null) return "<null>";
            if (value is string s) return s;
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var list = new List<string>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= 50) { list.Add("..."); break; }
                    list.Add(item?.ToString() ?? "<null>");
                    count++;
                }
                return "[" + string.Join(", ", list) + "]";
            }
            return value.ToString() ?? "<unprintable>";
        }
    }
}
