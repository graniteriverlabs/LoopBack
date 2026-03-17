# GRL.VDPWR.LoopBackService

A **.NET Standard 2.0** class library for USB LoopBack testing, compatible with:
- ✅ .NET Framework 4.7.2+
- ✅ .NET Core 2.0+
- ✅ .NET 5, 6, 7, 8+

## Overview

This library provides a cross-platform USB LoopBack testing service that can be shared across different .NET implementations. It was extracted from the GRL.VDPWR.UI project to enable reusability in both legacy .NET Framework applications and modern .NET Core/8 projects.

## Project Structure

```
GRL.VDPWR.LoopBackService/
├── Models/
│   ├── DeviceInfo.cs              # USB device information
│   ├── LoopBackTestResult.cs      # Test execution results
│   └── LoopBackViewModelInfo.cs   # Configuration model
├── Services/
│   ├── ILoopBackService.cs        # Service interface
│   └── LoopBackService.cs         # Service implementation
└── GRL.VDPWR.LoopBackService.csproj
```

## Dependencies

### NuGet Packages
- **LibUsbDotNet** (3.0.102) - USB device communication
- **System.Text.Json** (8.0.0) - JSON configuration parsing
- **System.Runtime.InteropServices.RuntimeInformation** (4.3.0) - OS detection

### Project References
- **GRL.Logging** - Logging abstraction layer

## Key Features

### 1. **Cross-Platform Device Detection**
- Windows: Uses LibUsbDotNet's `UsbDevice.AllDevices`
- Linux: Uses `lsusb` command
- macOS: Uses `system_profiler SPUSBDataType`

### 2. **USB LoopBack Testing**
- Configurable test data (default sequential or JSON-based)
- 500-iteration write-read cycles
- Throughput calculation (Mbps, KB/s)
- Data verification with pass/fail counts
- Automatic retry mechanism for failed reads

### 3. **Configuration Support**
- JSON-based device type filtering
- Custom test data patterns via `LoopBackViewModelInfo.json`
- Configurable buffer sizes (default: 65536 bytes)

## Usage

### Basic Integration

#### 1. Add Project Reference

**For .NET 8 / .NET Core projects:**
```xml
<ItemGroup>
  <ProjectReference Include="..\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj" />
</ItemGroup>
```

**For .NET Framework 4.7.2 projects:**
```xml
<ItemGroup>
  <ProjectReference Include="..\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj" />
</ItemGroup>
```

#### 2. Register Service (Dependency Injection)

```csharp
using GRL.VDPWR.LoopBackService.Services;
using Microsoft.Extensions.DependencyInjection;

// In your DI configuration
services.AddScoped<ILoopBackService>(sp => 
{
    var logger = sp.GetRequiredService<ILoggerService>();
    var configPath = Path.Combine(AppContext.BaseDirectory, "ConfigFiles");
    return new LoopBackService(logger, configPath);
});
```

#### 3. Use the Service

```csharp
using GRL.VDPWR.LoopBackService.Models;
using GRL.VDPWR.LoopBackService.Services;

// Inject the service
public class MyController
{
    private readonly ILoopBackService _loopBackService;
    
    public MyController(ILoopBackService loopBackService)
    {
        _loopBackService = loopBackService;
    }
    
    public async Task RunTest()
    {
        // Scan for devices
        List<DeviceInfo> devices = await _loopBackService.LoadDevicesAsync();
        
        if (devices.Any())
        {
            // Parse device IDs
            var (vendorId, productId) = _loopBackService.ParseDeviceId(devices[0].DeviceID);
            
            // Execute loopback test
            LoopBackTestResult result = await _loopBackService.ExecuteLoopBackTestAsync(
                vendorId, 
                productId, 
                useDefaultData: true
            );
            
            // Check results
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Throughput: {result.EffectiveThroughput}");
            Console.WriteLine($"Passed: {result.PassedBytes}, Failed: {result.FailedBytes}");
        }
    }
}
```

### Configuration File

Create `ConfigFiles/LoopBackViewModelInfo.json`:

```json
{
  "GRLUSBDeviceType": "GRL USB-LoopBack Tester",
  "LoopBackData": "0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07"
}
```

## API Reference

### ILoopBackService

#### Methods

##### `Task<List<DeviceInfo>> LoadDevicesAsync()`
Scans for available USB devices matching the configured device type.

**Returns:** List of discovered devices with names and IDs.

##### `Task<LoopBackTestResult> ExecuteLoopBackTestAsync(int vendorId, int productId, bool useDefaultData = true)`
Executes the USB loopback test.

**Parameters:**
- `vendorId` - USB Vendor ID (hex)
- `productId` - USB Product ID (hex)
- `useDefaultData` - Use sequential bytes (true) or JSON config (false)

**Returns:** Test results with throughput metrics and verification counts.

##### `(int VendorId, int ProductId) ParseDeviceId(string deviceId)`
Parses device ID string to extract VID and PID.

**Parameters:**
- `deviceId` - Device ID in format `"VID_0xXXXX PID_0xYYYY"`

**Returns:** Tuple with parsed vendor and product IDs.

## Migration Guide

### From GRL.VDPWR.UI to GRL.VDPWR.LoopBackService

**Old namespace:**
```csharp
using GRL.VDPWR.UI.Services.LoopBack;
using GRL.VDPWR.UI.DataModel.LoopBack.LoopBackViewModel;
```

**New namespace:**
```csharp
using GRL.VDPWR.LoopBackService.Services;
using GRL.VDPWR.LoopBackService.Models;
```

**Changes:**
- `DeviceInfo` moved to `GRL.VDPWR.LoopBackService.Models`
- `LoopBackTestResult` moved to `GRL.VDPWR.LoopBackService.Models`
- `LoopBackViewModelInfo` moved to `GRL.VDPWR.LoopBackService.Models`
- OS detection changed from `OperatingSystem.IsWindows()` to `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` for .NET Standard 2.0 compatibility

## Platform-Specific Notes

### Windows
- Requires LibUSB drivers installed for the USB device
- Uses `[SupportedOSPlatform("windows")]` attribute for Windows-specific code

### Linux
- Requires `libusb` package installed
- May need `udev` rules for non-root access

### macOS
- Requires libusb via Homebrew: `brew install libusb`
- May require system extension approval

## Testing

The service performs:
1. **500 write-read cycles** per test execution
2. **Byte-level verification** of received data
3. **Automatic retry** (up to 2 times) on read failures
4. **Throughput calculation** in both Mbps and KB/s

## License

© 2025 Granite River Labs. All rights reserved.

## Version History

- **2.0.0.4** (2025-03-12) - Initial extraction to .NET Standard 2.0 library
