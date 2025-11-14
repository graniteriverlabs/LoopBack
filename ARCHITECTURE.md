# LoopBack Architecture Documentation

## Overview

LoopBack is a command-line interface (CLI) application designed to test USB-C loopback functionality on GRL USB devices. The application provides an interactive interface for device enumeration, selection, and execution of loopback tests with comprehensive result reporting.

## Table of Contents

- [System Architecture](#system-architecture)
- [Project Structure](#project-structure)
- [Component Design](#component-design)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)
- [Dependencies](#dependencies)
- [Extension Points](#extension-points)

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        User Interface                        │
│                    (Interactive Console)                     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     CLI Layer (Cli/)                         │
│  ┌──────────────────────────────────────────────────────┐   │
│  │            LoopBackCli                               │   │
│  │  - Device Selection                                  │   │
│  │  - Execution Mode Selection                          │   │
│  │  - Result Presentation                               │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Service Wrapper Layer (Services/)               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      GrlC2LoopBackService (Wrapper)                  │   │
│  │  - Device Discovery                                  │   │
│  │  - Hardware ID Parsing                               │   │
│  │  - Test Execution Orchestration                      │   │
│  │  - Logging Integration                               │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              External DLL Layer (DLL/)                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      GRL.VDPWR.LoopBackService                       │   │
│  │  - Device Communication                              │   │
│  │  - USB Protocol Handling                             │   │
│  │  - Test Execution                                    │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      GRL.Logging                                     │   │
│  │  - Logging Interfaces                                │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   USB Hardware Layer                         │
│              (LibUsbDotNet + USB-C Devices)                  │
└─────────────────────────────────────────────────────────────┘
```

### Layered Architecture

The application follows a **layered architecture** pattern with clear separation of concerns:

1. **Presentation Layer** (`Program.cs`)
   - Entry point and application bootstrapping
   - Logging configuration
   - Resource management

2. **CLI Layer** (`Cli/`)
   - User interaction logic
   - Input validation
   - Output formatting

3. **Service Layer** (`Services/`)
   - Business logic abstraction
   - Wrapper around external DLLs
   - Logging services

4. **External Integration Layer** (`DLL/`)
   - Third-party DLL integration
   - Device communication protocols

## Project Structure

```
LoopBack/
├── Program.cs                          # Application entry point
├── LoopBack.csproj                     # Project configuration
├── LoopBack.sln                        # Solution file
├── README.md                           # Project documentation
├── ARCHITECTURE.md                     # This file
│
├── Cli/                                # CLI interaction layer
│   └── LoopBackCli.cs                  # Interactive console interface
│
├── Services/                           # Service layer
│   ├── LoopBackServiceWrapper.cs       # Main service wrapper
│   └── SerilogLoggerService.cs         # Serilog implementation
│
├── ConfigFiles/                        # Configuration files
│   └── LoopBackViewModelInfo.json      # Device configuration
│
├── DLL/                                # External dependencies
│   ├── GRL.VDPWR.LoopBackService.dll   # Core loopback service
│   └── GRL.Logging.dll                 # Logging interface
│
├── bin/                                # Build output
└── obj/                                # Build intermediates
```

## Component Design

### 1. Program.cs

**Responsibility**: Application bootstrapping and lifecycle management

**Key Functions**:
- Configure Serilog logging
- Initialize service wrapper
- Delegate to CLI layer
- Ensure proper resource disposal

```csharp
Main()
  ├── ConfigureLogging()
  ├── Create GrlC2LoopBackService
  ├── Create LoopBackCli
  ├── await cli.RunAsync()
  └── Cleanup & Exit
```

### 2. LoopBackCli (Cli Layer)

**Responsibility**: User interaction and workflow orchestration

**Key Features**:
- Interactive device selection with retry loop
- Execution mode selection (default/configured)
- Advanced parameter input with validation
- Result presentation (filtered properties)
- Multiple test run capability

**Public Interface**:
```csharp
public async Task<int> RunAsync(CancellationToken)
```

**Internal Methods**:
- `PrintDevices()` - Display available devices
- `PromptDeviceSelection()` - Get user device choice
- `ExecuteForDeviceAsync()` - Run test on selected device
- `PromptExecutionMode()` - Get test mode
- `PromptAdvancedParameters()` - Collect data size/iterations
- `PrintSelectedResult()` - Display filtered results
- `PrintSelectedResultDynamic()` - Fallback for dynamic results

**Exit Codes**:
- `0` - Success
- `1` - No devices found
- `2` - Cancelled by user
- `-1` - Fatal error

### 3. GrlC2LoopBackService (Service Wrapper)

**Responsibility**: Abstraction layer over GRL DLLs

**Key Features**:
- Automatic logger fallback (ConsoleLogger if none provided)
- Configuration file validation
- Initialization state tracking
- Proper resource disposal (IDisposable pattern)
- Async/sync API support

**Public API**:

```csharp
// Constructor
GrlC2LoopBackService(ILoggerService, string configPath)

// Properties
bool IsInitialized
string ConfigPath
string ConfigFilePath
bool ConfigFileExists

// Methods
Task<List<DeviceInfo>> GetLoopbackDevices()
dynamic GetLoopbackDeviceHardwareId(string deviceId)
Task<dynamic> StartLoopBackExecutionAsync(int vid, int pid, bool useDefault)
void Dispose()
```

**Internal Components**:
- `ConsoleLoggerService` - Fallback logger with 200-char limit on info logs

### 4. SerilogLoggerService

**Responsibility**: Structured logging implementation

**Features**:
- Implements `ILoggerService` interface
- Delegates to Serilog static logger
- Provides byte-to-hex string conversion utility

## Data Flow

### Device Discovery and Test Execution Flow

```
┌──────────────┐
│ User starts  │
│     app      │
└──────┬───────┘
       │
       ▼
┌──────────────────────────────┐
│ Program.Main()               │
│  - Configure Serilog         │
│  - Create service wrapper    │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ LoopBackCli.RunAsync()       │
│  - Get device list           │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ GrlC2LoopBackService         │
│  .GetLoopbackDevices()       │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ External DLL                 │
│  .LoadDevicesAsync()         │
│  (reads config JSON)         │
└──────┬───────────────────────┘
       │
       ▼ (returns List<DeviceInfo>)
┌──────────────────────────────┐
│ CLI displays devices         │
│ User selects device          │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ Parse hardware ID            │
│  .GetLoopbackDeviceHardwareId│
└──────┬───────────────────────┘
       │
       ▼ (returns VID/PID tuple)
┌──────────────────────────────┐
│ User selects execution mode  │
│  1. Default data             │
│  2. Configure data           │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ Start test execution         │
│  .StartLoopBackExecutionAsync│
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ External DLL                 │
│  .ExecuteLoopBackTestAsync() │
│  (USB communication)         │
└──────┬───────────────────────┘
       │
       ▼ (returns LoopBackTestResult)
┌──────────────────────────────┐
│ Display filtered results:    │
│  - TransferredData           │
│  - ReceivedData              │
│  - EffectiveThroughput       │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│ Prompt: Run another test?    │
│  Yes → loop back to device   │
│       selection              │
│  No  → Exit                  │
└──────────────────────────────┘
```

## Design Patterns

### 1. Wrapper Pattern
**Location**: `GrlC2LoopBackService`

**Purpose**: Simplify and standardize access to external DLL APIs

**Benefits**:
- Hides complexity of external DLL
- Adds validation and error handling
- Provides initialization state management
- Enables logging integration

### 2. Dependency Injection
**Location**: Throughout application

**Examples**:
- `LoopBackCli` receives `GrlC2LoopBackService` via constructor
- `GrlC2LoopBackService` receives optional `ILoggerService`
- Allows for testing and flexibility

### 3. Dispose Pattern (IDisposable)
**Location**: `GrlC2LoopBackService`

**Implementation**:
```csharp
public void Dispose()
  └── Dispose(bool disposing)
      └── GC.SuppressFinalize(this)
```

**Resources managed**:
- External DLL service instances
- Logger instances

### 4. Strategy Pattern (Implicit)
**Location**: `ILoggerService` implementations

**Implementations**:
- `ConsoleLoggerService` - Simple console output
- `SerilogLoggerService` - Structured logging

### 5. Template Method Pattern
**Location**: `LoopBackCli.RunAsync()`

**Flow**:
1. Load devices
2. Display & select
3. Configure test
4. Execute test
5. Display results
6. Repeat or exit

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `LibUsbDotNet` | 2.2.29 | USB device communication |
| `Serilog` | 2.12.0 | Structured logging framework |
| `Serilog.Sinks.Console` | 4.1.0 | Console output for Serilog |

### External DLLs

| DLL | Purpose |
|-----|---------|
| `GRL.VDPWR.LoopBackService.dll` | Core loopback test functionality |
| `GRL.Logging.dll` | Logging interface definitions |

### Framework
- **.NET 8.0** (net8.0)
- C# 10+ features (nullable reference types, async Main)

## Extension Points

### 1. Custom Loggers
Implement `ILoggerService` interface:
```csharp
public class CustomLogger : ILoggerService
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex);
    void WriteLog(string message, LogType logType, Exception? ex);
    void CloseLogger();
    Task<string> ConvertBytesToString(List<byte> bufData, int printLength);
}
```

### 2. Non-Interactive Mode
Add command-line argument parsing:
- `--device <index>` - Auto-select device
- `--mode <1|2>` - Auto-select execution mode
- `--no-prompt` - Skip confirmations

### 3. Result Output Formats
Extend `LoopBackCli`:
- `--format json` - JSON output
- `--format csv` - CSV output
- `--output <file>` - Write to file

### 4. Batch Testing
Currently infrastructure exists but is unused:
- Extend CLI to support device list input
- Implement parallel test execution
- Aggregate results

### 5. Additional Test Parameters
When underlying DLL supports it:
- Wire `dataSize` and `iterations` parameters
- Add timeout configuration
- Custom throughput thresholds

## Configuration

### LoopBackViewModelInfo.json

**Location**: `ConfigFiles/LoopBackViewModelInfo.json`

**Purpose**: Device identification and configuration

**Usage**:
- Copied to output directory during build
- Read by `GRL.VDPWR.LoopBackService.dll`
- Contains device VID/PID mappings

**Note**: Missing file triggers warning but doesn't prevent initialization

## Error Handling Strategy

### Initialization Errors
- Caught in `GrlC2LoopBackService` constructor
- Logged via logger (if available)
- Wrapped in `InvalidOperationException`
- Prevents service usage

### Runtime Errors
- Device not found → return code 1
- User cancellation → return code 0
- Fatal exception → return code -1
- Test execution errors → logged but don't crash app

### Validation
- Device selection validated against available list
- Execution mode validated (1 or 2)
- Advanced parameters validated (range checks)

## Performance Considerations

### Async/Await Usage
- Device loading: `async Task<List<DeviceInfo>>`
- Test execution: `async Task<dynamic>`
- Main entry point: `async Task<int> Main`

**Benefits**:
- Non-blocking I/O for device communication
- Responsive to cancellation tokens
- Scalable for future batch operations

### Logging Performance
- `ConsoleLoggerService` filters messages >200 chars
- Prevents console overflow with large data dumps
- Structured logging ready for high-volume scenarios

## Security Considerations

### Input Validation
- User selections validated against available ranges
- Device ID validation (null/whitespace checks)
- Parameter range validation

### Resource Management
- Proper disposal of external DLL resources
- Logger cleanup on exit
- No resource leaks via `using` statements

### Configuration
- Config file path validation
- File existence checks before reading
- No sensitive data in config (device metadata only)

## Testing Strategy (Recommended)

### Unit Tests (Not yet implemented)
- CLI input parsing logic
- Service wrapper initialization
- Logger implementations
- Error handling paths

### Integration Tests (Not yet implemented)
- End-to-end device discovery
- Test execution flow
- Configuration loading

### Manual Testing
- Currently primary testing method
- Requires physical USB-C loopback device
- Interactive validation of all UI flows

## Future Enhancements

1. **Configuration Management**
   - Support multiple config files
   - Environment-based configuration
   - Runtime config reload

2. **Enhanced Reporting**
   - Test history/logging
   - Performance metrics over time
   - Export capabilities (JSON, CSV, XML)

3. **Automation Support**
   - Command-line argument parsing
   - Batch mode execution
   - CI/CD integration

4. **Advanced Features**
   - Stress testing mode
   - Comparative analysis
   - Device health monitoring

5. **UI Improvements**
   - Color-coded output
   - Progress indicators
   - Real-time throughput graphs

## Conclusion

The LoopBack architecture emphasizes:
- **Separation of Concerns**: Clear boundaries between UI, business logic, and external integration
- **Extensibility**: Multiple extension points for customization
- **Maintainability**: Clean code structure with well-defined responsibilities
- **Reliability**: Proper error handling and resource management
- **User Experience**: Interactive, guided workflow with validation

The design provides a solid foundation for USB-C loopback testing while remaining flexible for future enhancements.
