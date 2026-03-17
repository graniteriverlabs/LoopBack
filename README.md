# LoopBack

A command-line interface (CLI) application for testing USB-C loopback functionality on GRL USB devices. This tool provides an interactive interface for device enumeration, selection, and execution of comprehensive loopback tests with detailed result reporting.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
- [Output](#output)
- [Architecture](#architecture)
- [Development](#development)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Features

- 🔌 **Automatic Device Discovery** - Detects all connected GRL USB loopback devices
- 🎯 **Interactive Selection** - User-friendly device selection interface
- ⚙️ **Flexible Test Modes**:
  - Default data mode (pre-configured test parameters)
  - Configure data mode (custom data size and iterations)
- 📊 **Detailed Results** - Displays transferred data, received data, and effective throughput
- 🔄 **Multiple Test Runs** - Execute sequential tests without restarting the application
- 📝 **Structured Logging** - Serilog integration with configurable output
- ✅ **Input Validation** - Comprehensive validation with user-friendly error messages
- 🛡️ **Error Handling** - Graceful error handling and recovery

## Requirements

### Runtime Requirements

- **Operating System**: Windows (PowerShell 5.1+)
- **.NET Runtime**: .NET 8.0 or higher
- **Hardware**: GRL USB-C loopback device(s)
- **Drivers**: LibUSB drivers for USB device communication

### Build Requirements

- **SDK**: .NET 8.0 SDK
- **IDE** (optional): Visual Studio 2022 or VS Code

## Installation

### 1. Clone the Repository

```powershell
git clone https://github.com/deepak-grl/LoopBack.git
cd LoopBack
```

### 2. Restore Dependencies

```powershell
dotnet restore
```

### 3. Build the Project

```powershell
dotnet build --configuration Release
```

### 4. Configuration File

Ensure `src/LoopBack/ConfigFiles/LoopBackViewModelInfo.json` exists and contains proper device configuration:

```json
{
  "DeviceMappings": [
    {
      "VendorId": "0x04B4",
      "ProductId": "0x1234",
      "DeviceName": "GRL USB-C Loopback Device"
    }
  ]
}
```

The configuration file is automatically copied to the output directory during build as `ConfigFiles/LoopBackViewModelInfo.json`.

## Usage

### Running the Application

**From Project Directory:**
```powershell
dotnet run
```

**From Build Output:**
```powershell
cd bin\Release\net8.0
.\LoopBack.exe
```

### Interactive Workflow

1. **Device Selection**
   ```
   Available Loopback Devices
   ============================
   1. GRL USB-C Loopback Device
   2. GRL USB-C Loopback Device (Port 2)
   (Enter number, or press Enter to cancel)
   Select device: 1
   ```

2. **Execution Mode Selection**
   ```
   === Execution Options ===
   1. Default Data
   2. Configure Data
   Choice (1/2): 2
   ```

3. **Advanced Parameters** (if Configure Data mode selected)
   ```
   Data size (bytes) [default 1024]: 2048
   Iterations [default 1]: 5
   ```

4. **View Results**
   ```
   === LoopBack Test Result ===
   TransferredData: 32768000 Bytes
   ReceivedData: 32768000 Bytes
   EffectiveThroughput: 193.83 Mbps
   ==============================
   ```

5. **Repeat or Exit**
   ```
   Run another test? (y/n): n
   ```

### Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success - test completed or user cancelled |
| `1` | No loopback devices found |
| `2` | Operation cancelled by user (Ctrl+C) |
| `-1` | Fatal error occurred |

## Configuration

### LoopBackViewModelInfo.json

**Source Location**: `src/LoopBack/ConfigFiles/LoopBackViewModelInfo.json`

**Runtime Location**: `ConfigFiles/LoopBackViewModelInfo.json`

**Purpose**: Maps USB Vendor/Product IDs to device names for recognition and display.

**Structure**:
```json
{
  "DeviceMappings": [
    {
      "VendorId": "string",
      "ProductId": "string", 
      "DeviceName": "string"
    }
  ]
}
```

**Note**: If the configuration file is missing, a warning is logged but the application will still attempt to enumerate devices.

### Logging Configuration

Logging is configured in `src/LoopBack/Program.cs` using Serilog:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Customization Options**:
- Change minimum log level (Debug, Information, Warning, Error)
- Add file sinks for persistent logging
- Customize output template
- Add enrichers (machine name, thread ID, etc.)

## Output

### Test Result Properties

The application displays three key metrics from each loopback test:

1. **TransferredData**: Total bytes transferred to the device
   - Format: `{value} Bytes`
   - Example: `32768000 Bytes`

2. **ReceivedData**: Total bytes received from the device
   - Format: `{value} Bytes`
   - Example: `32768000 Bytes`

3. **EffectiveThroughput**: Calculated throughput rate
   - Format: `{value} Mbps`
   - Example: `193.83 Mbps`

### Sample Output

```
[15:23:45 INF] Initializing LoopBackService with config path: D:\DEV\LoopBack\bin\Debug\net8.0\ConfigFiles
[15:23:45 INF] LoopBackService initialized successfully.
[15:23:45 INF] Loading loopback devices...

Available Loopback Devices
============================
1. GRL USB-C Loopback Device

Select device: 1

Selected Device: GRL USB-C Loopback Device
[15:23:48 INF] Parsing loopback device hardware ID: USB\VID_04B4&PID_1234\1234567890

=== Execution Options ===
1. Default Data
2. Configure Data
Choice (1/2): 1

[15:23:50 INF] Starting loopback test (VID=1204, PID=4660, DefaultData=True)

=== LoopBack Test Result ===
TransferredData: 32768000 Bytes
ReceivedData: 32768000 Bytes
EffectiveThroughput: 193.83 Mbps
==============================

Run another test? (y/n): n
```

## Architecture

The application follows a layered architecture with clear separation of concerns:

- **Presentation Layer** (`Program.cs`) - Entry point and bootstrapping
- **CLI Layer** (`Cli/LoopBackCli.cs`) - Interactive user interface
- **Service Layer** (`Services/`) - Business logic and DLL abstraction
- **External Integration** (`DLL/`) - GRL proprietary libraries

For detailed architecture documentation, see [ARCHITECTURE.md](ARCHITECTURE.md).

### Key Components

```
src/LoopBack/Program.cs
  └── LoopBackCli (User interaction)
        └── GrlC2LoopBackService (Service wrapper)
        └── lib/GRL.VDPWR.LoopBackService.dll (External DLL)
                    └── LibUsbDotNet (USB communication)
```

## Development

### Project Structure

```
LoopBack/
├── lib/                           # External dependencies
├── src/
│   └── LoopBack/
│       ├── Program.cs            # Application entry point
│       ├── Cli/
│       │   └── LoopBackCli.cs    # CLI interface
│       ├── Services/
│       │   ├── LoopBackServiceWrapper.cs
│       │   └── SerilogLoggerService.cs
│       └── ConfigFiles/
│           └── LoopBackViewModelInfo.json
```

### Building from Source

```powershell
# Debug build
dotnet build

# Release build
dotnet build --configuration Release

# Clean build
dotnet clean
dotnet build
```

### Adding Custom Logger

Implement the `ILoggerService` interface:

```csharp
public class CustomLogger : ILoggerService
{
    public void LogInformation(string message) { /* ... */ }
    public void LogWarning(string message) { /* ... */ }
    public void LogError(string message, Exception? ex = null) { /* ... */ }
    public void WriteLog(string message, LogType logType, Exception? ex = null) { /* ... */ }
    public void CloseLogger() { /* ... */ }
    public Task<string> ConvertBytesToString(List<byte> bufData, int printLength = -1) { /* ... */ }
}
```

Then inject it:
```csharp
var service = new GrlC2LoopBackService(new CustomLogger());
```

### Extending Test Parameters

Currently, data size and iterations are collected but not passed to the underlying API. To enable:

1. Update `GrlC2LoopBackService.StartLoopBackExecutionAsync` signature
2. Pass parameters to `_loopBackService.ExecuteLoopBackTestAsync()`
3. Ensure external DLL supports these parameters

## Troubleshooting

### No Devices Found

**Symptom**: "No loopback devices found."

**Solutions**:
1. Verify USB device is connected and powered
2. Check Windows Device Manager for driver issues
3. Install LibUSB drivers if missing
4. Verify `LoopBackViewModelInfo.json` has correct VID/PID
5. Run application as Administrator

### Configuration File Not Found

**Symptom**: Warning about missing config file

**Solutions**:
1. Ensure `src/LoopBack/ConfigFiles/LoopBackViewModelInfo.json` exists
2. Verify build action is set to "Copy to Output Directory"
3. Manually copy config file to output directory

### Build Errors

**Symptom**: Missing DLL references

**Solutions**:
1. Ensure `DLL/` folder contains required files:
   - `GRL.VDPWR.LoopBackService.dll`
   - `GRL.Logging.dll`
2. Clean and rebuild solution
3. Restore NuGet packages: `dotnet restore`

### Test Execution Fails

**Symptom**: Exception during test execution

**Solutions**:
1. Check device connection and drivers
2. Review log output for specific errors
3. Try different execution mode (default vs configure)
4. Ensure device is not in use by another application
5. Restart device and retry

### Logger Issues

**Symptom**: No log output or excessive logging

**Solutions**:
1. Check Serilog configuration in `Program.cs`
2. Verify minimum log level setting
3. Console logger filters messages >200 characters
4. Use file sink for complete logs

## Dependencies

### NuGet Packages

- **LibUsbDotNet** (2.2.29) - USB device communication
- **Serilog** (2.12.0) - Structured logging
- **Serilog.Sinks.Console** (4.1.0) - Console output

### External Libraries

- **GRL.VDPWR.LoopBackService.dll** - Core loopback functionality
- **GRL.Logging.dll** - Logging interface definitions

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

[Specify your license here]

## Support

For issues, questions, or contributions:
- **Repository**: https://github.com/deepak-grl/LoopBack
- **Issues**: https://github.com/deepak-grl/LoopBack/issues

## Acknowledgments

- GRL (Granite River Labs) for the USB-C loopback service libraries
- LibUsbDotNet community for USB communication support
- Serilog project for structured logging

---

**Version**: 1.0.0  
**Last Updated**: November 14, 2025  
**Target Framework**: .NET 8.0
