using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GRL.VDPWR.LoopBackService.Models;
using GrlC2ApiLib;

namespace LoopBack.Cli
{
    /// <summary>
    /// Handles interactive console flow for loopback device testing.
    /// </summary>
    internal class LoopBackCli
    {
        private readonly GrlC2LoopBackService _service;
        public LoopBackCli(GrlC2LoopBackService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var devices = await _service.GetLoopbackDevices();
                if (devices == null || devices.Count == 0)
                {
                    Console.WriteLine("No loopback devices found.");
                    return 1;
                }

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return 2;
                    PrintDevices(devices);
                    var selected = PromptDeviceSelection(devices);
                    if (selected == null)
                    {
                        Console.WriteLine("Selection cancelled. Exiting.");
                        return 0;
                    }

                    await ExecuteForDeviceAsync(selected, cancellationToken);

                    Console.WriteLine("\nRun another test? (y/n): ");
                    var again = Console.ReadLine();
                    if (!string.Equals(again, "y", StringComparison.OrdinalIgnoreCase))
                        break;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                return -1;
            }
        }

        private void PrintDevices(List<DeviceInfo> devices)
        {
            Console.WriteLine("\nAvailable Loopback Devices");
            Console.WriteLine("============================");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {devices[i].DeviceName}");
            }
            Console.WriteLine("(Enter number, or press Enter to cancel)");
        }

        private DeviceInfo? PromptDeviceSelection(List<DeviceInfo> devices)
        {
            while (true)
            {
                Console.Write("Select device: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) return null;
                if (int.TryParse(input, out int idx) && idx >= 1 && idx <= devices.Count)
                    return devices[idx - 1];
                Console.WriteLine("Invalid selection. Try again.");
            }
        }

        private async Task ExecuteForDeviceAsync(DeviceInfo device, CancellationToken ct)
        {
            Console.WriteLine($"\nSelected Device: {device.DeviceName}");
            var hw = _service.GetLoopbackDeviceHardwareId(device.DeviceID);
            int vendorId = hw.Item1;
            int productId = hw.Item2;

            int choice = PromptExecutionMode();

            // Currently not passed down; reserved for future extension.
            var result = await _service.StartLoopBackExecutionAsync(vendorId, productId, choice == 1);

            Console.WriteLine("\n=== LoopBack Test Result ===");
            if (result is GRL.VDPWR.LoopBackService.Models.LoopBackTestResult loopBackTestResult)
            {
                PrintSelectedResult(loopBackTestResult);
            }
            else
            {
                // Try to print as dynamic (for future-proofing)
                PrintSelectedResultDynamic(result);
            }
            Console.WriteLine("==============================");

        }

        // Print only EffectiveThroughput, TransferredData, and ReceivedData
        private void PrintSelectedResult(GRL.VDPWR.LoopBackService.Models.LoopBackTestResult result)
        {
            Console.WriteLine($"TransferredData: {result.TransferredData}");
            Console.WriteLine($"ReceivedData: {result.ReceivedData}");
            Console.WriteLine($"EffectiveThroughput: {result.EffectiveThroughput}");
        }

        // For dynamic/fallback cases
        private void PrintSelectedResultDynamic(dynamic result)
        {
            try
            {
                Console.WriteLine($"TransferredData: {result.TransferredData}");
            }
            catch { Console.WriteLine("TransferredData: <unavailable>"); }
            try
            {
                Console.WriteLine($"ReceivedData: {result.ReceivedData}");
            }
            catch { Console.WriteLine("ReceivedData: <unavailable>"); }
            try
            {
                Console.WriteLine($"EffectiveThroughput: {result.EffectiveThroughput}");
            }
            catch { Console.WriteLine("EffectiveThroughput: <unavailable>"); }
        }
        
        private int PromptExecutionMode()
        {
            Console.WriteLine("\n=== Execution Options ===");
            Console.WriteLine("1. Default Data");
            Console.WriteLine("2. Configure Data");
            while (true)
            {
                Console.Write("Choice (1/2): ");
                var input = Console.ReadLine();
                if (int.TryParse(input, out int c) && (c == 1 || c == 2)) return c;
                Console.WriteLine("Invalid choice. Try again.");
            }
        }

        private (int dataSize, int iterations) PromptAdvancedParameters()
        {
            int dataSize = PromptInt("Data size (bytes)", 1024, 1, 1024 * 1024 * 16);
            int iterations = PromptInt("Iterations", 1, 1, 1000);
            Console.WriteLine($"Using DataSize={dataSize}, Iterations={iterations}");
            return (dataSize, iterations);
        }

        private int PromptInt(string label, int defaultValue, int min, int max)
        {
            while (true)
            {
                Console.Write($"{label} [default {defaultValue}]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) return defaultValue;
                if (int.TryParse(input, out int v) && v >= min && v <= max) return v;
                Console.WriteLine($"Enter integer between {min} and {max}.");
            }
        }

        private void PrintObjectDetails(object? obj, string header)
        {
            Console.WriteLine($"--- {header} ---");
            if (obj == null)
            {
                Console.WriteLine("<null>");
                return;
            }
            var t = obj.GetType();
            Console.WriteLine($"Type: {t.FullName}");
            var props = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (props.Length == 0) Console.WriteLine("(No public properties)");
            foreach (var p in props)
            {
                object? value = null;
                try { value = p.GetValue(obj); } catch (Exception ex) { value = $"<error: {ex.Message}>"; }
                Console.WriteLine($"Property {p.Name}: {FormatValue(value)}");
            }
            var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var f in fields)
            {
                object? value = null;
                try { value = f.GetValue(obj); } catch (Exception ex) { value = $"<error: {ex.Message}>"; }
                Console.WriteLine($"Field {f.Name}: {FormatValue(value)}");
            }
        }

        private string FormatValue(object? value)
        {
            if (value == null) return "<null>";
            if (value is string s) return s;
            if (value is System.Collections.IEnumerable e and not string)
            {
                var list = new List<string>();
                int count = 0;
                foreach (var item in e)
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
