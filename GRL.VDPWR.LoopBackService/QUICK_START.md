# Quick Start Guide - GRL.VDPWR.LoopBackService Integration

## 🚀 5-Minute Setup

### Prerequisites
- Visual Studio 2022 or later
- .NET SDK 8.0+ installed
- .NET Framework 4.7.2+ SDK (for .NET Framework projects)

---

## Step 1: Fix GRL.Logging Dependency ⚠️

**IMPORTANT:** Before using `GRL.VDPWR.LoopBackService`, you must make `GRL.Logging` compatible with .NET Standard 2.0.

### Option A: Quick Fix (Recommended)

Edit `d:\APPS\VDPWR_V2_NET\DecodingEngine\GRL.Logging\GRL.Logging.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <PropertyGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="Serilog.Settings.Configuration" Version="3.4.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Serilog.Settings.Configuration" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  </ItemGroup>
</Project>
```

**Rebuild GRL.Logging:**
```powershell
cd d:\APPS\VDPWR_V2_NET\DecodingEngine\GRL.Logging
dotnet build
```

---

## Step 2: Use in .NET 8 Project (GRL.VDPWR.UI)

### Add Project Reference

Edit `GRL.VDPWR.UI/GRL.VDPWR.UI.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj" />
</ItemGroup>
```

### Update Code

**Before:**
```csharp
using GRL.VDPWR.UI.Services.LoopBack;
using GRL.VDPWR.UI.DataModel.LoopBack.LoopBackViewModel;
```

**After:**
```csharp
using GRL.VDPWR.LoopBackService.Services;
using GRL.VDPWR.LoopBackService.Models;
```

### Register Service (Dependency Injection)

```csharp
// In your DI container setup
services.AddScoped<ILoopBackService>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerService>();
    var configPath = Path.Combine(AppContext.BaseDirectory, "ConfigFiles");
    return new LoopBackService(logger, configPath);
});
```

### Use the Service

```csharp
public class LoopBackViewModel
{
    private readonly ILoopBackService _loopBackService;
    
    public LoopBackViewModel(ILoopBackService loopBackService)
    {
        _loopBackService = loopBackService;
    }
    
    public async Task ScanDevicesAsync()
    {
        var devices = await _loopBackService.LoadDevicesAsync();
        foreach (var device in devices)
        {
            Console.WriteLine($"{device.DeviceName}: {device.DeviceID}");
        }
    }
    
    public async Task RunTestAsync(string deviceId)
    {
        var (vid, pid) = _loopBackService.ParseDeviceId(deviceId);
        var result = await _loopBackService.ExecuteLoopBackTestAsync(vid, pid, true);
        
        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"Throughput: {result.EffectiveThroughput}");
    }
}
```

---

## Step 3: Use in .NET Framework 4.7.2 Web App

### Add to Your Web Project

Edit `YourWebApp.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj">
    <Project>{C8E9F1A0-1234-4567-89AB-CDEF01234567}</Project>
    <Name>GRL.VDPWR.LoopBackService</Name>
  </ProjectReference>
  <ProjectReference Include="..\GRL.Logging\GRL.Logging.csproj">
    <Project>{A544981E-A8E2-405A-A82D-94777D8AC5F5}</Project>
    <Name>GRL.Logging</Name>
  </ProjectReference>
</ItemGroup>
```

### ASP.NET MVC Controller Example

```csharp
using System.Web.Mvc;
using GRL.Logging;
using GRL.VDPWR.LoopBackService.Services;
using GRL.VDPWR.LoopBackService.Models;

public class LoopBackController : Controller
{
    private readonly ILoopBackService _loopBackService;
    
    public LoopBackController()
    {
        var logger = new LoggerService(); // Or inject via DI
        _loopBackService = new LoopBackService(logger);
    }
    
    public async Task<ActionResult> Scan()
    {
        var devices = await _loopBackService.LoadDevicesAsync();
        return Json(devices, JsonRequestBehavior.AllowGet);
    }
    
    public async Task<ActionResult> Test(int vendorId, int productId)
    {
        var result = await _loopBackService.ExecuteLoopBackTestAsync(vendorId, productId, true);
        return Json(result, JsonRequestBehavior.AllowGet);
    }
}
```

### ASP.NET Web API Controller Example

```csharp
using System.Web.Http;
using GRL.Logging;
using GRL.VDPWR.LoopBackService.Services;

[RoutePrefix("api/loopback")]
public class LoopBackApiController : ApiController
{
    private readonly ILoopBackService _loopBackService;
    
    public LoopBackApiController()
    {
        var logger = new LoggerService();
        _loopBackService = new LoopBackService(logger);
    }
    
    [HttpGet]
    [Route("devices")]
    public async Task<IHttpActionResult> GetDevices()
    {
        var devices = await _loopBackService.LoadDevicesAsync();
        return Ok(devices);
    }
    
    [HttpPost]
    [Route("test")]
    public async Task<IHttpActionResult> RunTest([FromBody] TestRequest request)
    {
        var result = await _loopBackService.ExecuteLoopBackTestAsync(
            request.VendorId, 
            request.ProductId, 
            request.UseDefaultData
        );
        return Ok(result);
    }
}

public class TestRequest
{
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public bool UseDefaultData { get; set; }
}
```

---

## Step 4: Use in .NET Core Console App

### Create New Console App (Optional)

```powershell
cd d:\APPS\VDPWR_V2_NET
dotnet new console -n LoopBackTestConsole
cd LoopBackTestConsole
```

### Add Project References

Edit `LoopBackTestConsole.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\DecodingEngine\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj" />
    <ProjectReference Include="..\DecodingEngine\GRL.Logging\GRL.Logging.csproj" />
  </ItemGroup>
</Project>
```

### Console App Code

```csharp
using GRL.Logging;
using GRL.VDPWR.LoopBackService.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("GRL USB LoopBack Tester\n");
        
        // Initialize service
        var logger = new LoggerService();
        var loopBackService = new LoopBackService(logger);
        
        // Scan for devices
        Console.WriteLine("Scanning for devices...");
        var devices = await loopBackService.LoadDevicesAsync();
        
        if (devices.Count == 0)
        {
            Console.WriteLine("No devices found!");
            return;
        }
        
        // Display devices
        Console.WriteLine($"\nFound {devices.Count} device(s):");
        for (int i = 0; i < devices.Count; i++)
        {
            Console.WriteLine($"  [{i}] {devices[i].DeviceName}");
            Console.WriteLine($"      {devices[i].DeviceID}");
        }
        
        // Select device
        Console.Write("\nSelect device [0]: ");
        string? input = Console.ReadLine();
        int selectedIndex = string.IsNullOrEmpty(input) ? 0 : int.Parse(input);
        
        var selectedDevice = devices[selectedIndex];
        var (vendorId, productId) = loopBackService.ParseDeviceId(selectedDevice.DeviceID);
        
        // Run test
        Console.WriteLine($"\nTesting device: {selectedDevice.DeviceName}");
        Console.WriteLine("Running 500 iterations...\n");
        
        var result = await loopBackService.ExecuteLoopBackTestAsync(vendorId, productId, true);
        
        // Display results
        Console.WriteLine("=== Test Results ===");
        Console.WriteLine($"Status: {result.Status}");
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Transferred: {result.TransferredData}");
        Console.WriteLine($"Received: {result.ReceivedData}");
        Console.WriteLine($"Throughput: {result.EffectiveThroughput}");
        Console.WriteLine($"Write Speed: {result.WriteThroughputKBps:F2} KB/s");
        Console.WriteLine($"Read Speed: {result.ReadThroughputKBps:F2} KB/s");
        Console.WriteLine($"Passed Bytes: {result.PassedBytes}");
        Console.WriteLine($"Failed Bytes: {result.FailedBytes}");
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }
}
```

---

## Step 5: Configuration File Setup

Create `ConfigFiles/LoopBackViewModelInfo.json` in your application's output directory:

```json
{
  "GRLUSBDeviceType": "GRL USB-LoopBack Tester",
  "LoopBackData": "0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F"
}
```

### Ensure File is Copied to Output

Add to your project file:

```xml
<ItemGroup>
  <None Update="ConfigFiles\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Step 6: Build & Test

### Build the Solution

```powershell
cd d:\APPS\VDPWR_V2_NET\DecodingEngine
dotnet build
```

### Run Tests

**Console App:**
```powershell
cd LoopBackTestConsole
dotnet run
```

**Web App:**
```powershell
cd YourWebApp
dotnet run
# OR
msbuild /t:Build
```

---

## Troubleshooting

### Issue: "Could not load file or assembly 'GRL.Logging'"

**Solution:** Ensure GRL.Logging is multi-targeted (Step 1).

### Issue: "Device not found"

**Solutions:**
1. Install LibUSB driver on Windows
2. Check USB connection
3. Verify device name in `LoopBackViewModelInfo.json`

### Issue: "Configuration file not found"

**Solution:** Ensure JSON file is copied to output directory (Step 5).

---

## Next Steps

1. ✅ Complete Step 1 (Fix GRL.Logging)
2. ✅ Test in your .NET 8 project
3. ✅ Test in your .NET Framework 4.7.2 project
4. ✅ Test in a console app
5. 📦 Optional: Package as NuGet for easier distribution

---

## Support & Documentation

- **Full Documentation:** See `README.md`
- **Architecture Guide:** See `ARCHITECTURE.md`
- **Logging Fix:** See `LOGGING_COMPATIBILITY_FIX.md`

## Summary

You now have a **fully reusable** USB LoopBack testing service that works across:
- ✅ .NET Framework 4.7.2+
- ✅ .NET Core 2.0+
- ✅ .NET 5, 6, 7, 8+

**Single codebase, multiple platforms!** 🎉
