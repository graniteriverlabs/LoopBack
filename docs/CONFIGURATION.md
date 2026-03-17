# LoopBack Configuration Files

The source configuration files for the CLI live under `src/LoopBack/ConfigFiles/`.

## Required Files

### LoopBackViewModelInfo.json

**Required for**: Device discovery and custom test data patterns

**Structure**:

```json
{
  "GRLUSBDeviceType": "GRL USB-LoopBack Tester",
  "LoopBackData": "0x00, 0x01, 0x02, ..."
}
```

**Fields**:
- `GRLUSBDeviceType`: Device name filter for USB device scanning
- `LoopBackData`: Comma-separated hex values for custom test patterns (used when `useDefaultData=false`)

## Usage

This configuration file is automatically loaded by the GRL LoopBackService DLL when:
1. Loading devices via `LoadDevicesAsync()`
2. Running tests with custom data via `ExecuteLoopBackTestAsync(vendorId, productId, useDefaultData: false)`

## Build and Runtime

The project links `src/LoopBack/ConfigFiles/LoopBackViewModelInfo.json` into the build output as `ConfigFiles/LoopBackViewModelInfo.json`, which preserves the runtime path expected by the wrapped DLL.