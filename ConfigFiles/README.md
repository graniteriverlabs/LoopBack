# ConfigFiles Folder

This folder contains configuration files required by the GRL LoopBack Service.

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

This configuration file is automatically loaded by the LoopBackService DLL when:
1. Loading devices via `LoadDevicesAsync()` - uses `GRLUSBDeviceType` to filter devices
2. Running tests with custom data via `ExecuteLoopBackTestAsync(vendorId, productId, useDefaultData: false)` - uses `LoopBackData`

## Default Behavior

If `useDefaultData=true` (default), the `LoopBackData` field is ignored and a sequential 0x00-0xFF pattern is used instead.

## Deployment

- **Console Applications**: This file is automatically copied to the output directory (bin\Debug or bin\Release)
- **Web Applications**: Ensure this folder exists in the web application root or specify custom path

For more details, see `LOOPBACK_CONFIG_GUIDE.md` in the project root.
