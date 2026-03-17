# Integrating LoopBackService DLL into Other Projects

This guide provides comprehensive, step-by-step instructions for integrating the `LoopBackService` DLL and its dependent DLLs into .NET Framework 4.7.2, .NET Core, or .NET 8 projects. Follow these steps to ensure seamless integration, including reference management, dependency configuration, NuGet package installation, and code updates.

---

## 1. Prerequisites

- Ensure you have access to the following DLLs (from your build output):
  - `GRL.VDPWR.LoopBackService.dll`
  - `GRL.Logging.dll`
  - Any additional dependent DLLs (e.g., `LibUsbDotNet.dll`, `Serilog.dll`)
- Your target project must be one of:
  - .NET Framework 4.7.2
  - .NET Core 3.1+
  - .NET 5/6/7/8

---

## 2. Add Project or DLL References

### A. As a Project Reference (Recommended for Source Access)
1. Right-click your target project in Solution Explorer.
2. Select **Add > Reference...**
3. Click **Projects** and select `GRL.VDPWR.LoopBackService` and `GRL.Logging`.
4. Click **OK**.

### B. As a DLL Reference (Binary Integration)
1. Copy the following DLLs from the build output (e.g., `bin/Debug/netstandard2.0/`):
   - `GRL.VDPWR.LoopBackService.dll`
   - `GRL.Logging.dll`
   - All required third-party DLLs (see below)
2. In your target project, right-click **References** > **Add Reference...**
3. Click **Browse**, select the copied DLLs, and click **Add**.

---

## 3. Install Required NuGet Packages

Your project must reference the same versions of the following NuGet packages as used by `LoopBackService` and `GRL.Logging`:

- `LibUsbDotNet` (version 2.2.29 for .NET Standard compatibility)
- `Serilog` (version 2.12.x for .NET Standard 2.0, or latest for .NET 8)
- `System.Text.Json` (if not already present)

### Install via NuGet Package Manager Console:
```powershell
Install-Package LibUsbDotNet -Version 2.2.29
Install-Package Serilog -Version 2.12.0
Install-Package System.Text.Json
```

Or via Visual Studio NuGet UI:
- Right-click your project > **Manage NuGet Packages** > **Browse** and install the above packages.

---

## 4. Configure Platform Target

- Ensure your project targets a compatible framework:
  - .NET Framework 4.7.2 or higher
  - .NET Core 3.1 or higher
  - .NET 5/6/7/8
- For .NET Framework, ensure `netstandard2.0` compatibility is enabled.

---

## 5. Update Code to Use LoopBackService

### A. Add Using Statements
```csharp
using GRL.VDPWR.LoopBackService.Services;
using GRL.VDPWR.LoopBackService.Models;
```

### B. Instantiate and Use the Service
```csharp
ILoopBackService loopBackService = new LoopBackService(/* dependencies if any */);
// Example usage:
var result = loopBackService.RunLoopBackTest(deviceInfo);
```

- Refer to the `ILoopBackService` interface and `LoopBackService` class for available methods.
- Use the provided models (e.g., `DeviceInfo`, `LoopBackTestResult`) for data exchange.

---

## 6. Configure Logging (If Needed)

If your project does not already use Serilog, configure it as follows:

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Optionally, inject or use LoggerService from GRL.Logging
```

---

## 7. Platform-Specific Notes

- **Windows Only:** `LibUsbDotNet` is Windows-specific. Ensure your application runs on Windows.
- **OS Detection:** The library uses `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` for compatibility.
- **x86/x64:** Match the platform target (x86/x64) of your application with the native dependencies of `LibUsbDotNet`.

---

## 8. Troubleshooting

- **Missing DLLs:** Ensure all dependent DLLs are copied to your output directory.
- **NuGet Version Conflicts:** Align package versions with those used in `LoopBackService`.
- **API Compatibility:** If you encounter missing APIs, ensure your project targets a compatible framework and has all required NuGet packages.

---

## 9. Example: .csproj Reference (for .NET Core/Standard)

```xml
<ItemGroup>
  <ProjectReference Include="..\GRL.VDPWR.LoopBackService\GRL.VDPWR.LoopBackService.csproj" />
  <ProjectReference Include="..\GRL.Logging\GRL.Logging.csproj" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="LibUsbDotNet" Version="2.2.29" />
  <PackageReference Include="Serilog" Version="2.12.0" />
  <PackageReference Include="System.Text.Json" />
</ItemGroup>
```

---

## 10. Additional Resources

- See `README.md`, `ARCHITECTURE.md`, and `QUICK_START.md` in the repository for more details.
- For advanced logging configuration, refer to `LOGGING_COMPATIBILITY_FIX.md`.

---

## 11. Automation and Tooling

- Tools like GitHub Copilot can use this guide to:
  - Add project or DLL references
  - Install required NuGet packages
  - Insert using statements and code samples
  - Update `.csproj` files as shown above

---

## 12. Support

For further assistance, consult the project documentation or contact the maintainers.
