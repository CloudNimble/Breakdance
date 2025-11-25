# CloudNimble.Breakdance.Azurite

A test harness for Azurite (Azure Storage Emulator) that integrates seamlessly with the Breakdance testing framework. Provides in-memory Azure Storage emulation with automatic lifecycle management and support for parallel test execution.

## Features

- **In-Memory by Default**: No disk I/O or file cleanup required
- **Parallel Test Safe**: Dynamic port allocation prevents conflicts
- **Multi-Service Support**: Blob, Queue, and Table services
- **Automatic Lifecycle**: Start/stop managed through test hooks
- **Zero Configuration**: Works out of the box with sensible defaults
- **Flexible**: Override settings for custom scenarios

## Requirements

- **Node.js**: Required to run Azurite (npm must be in PATH)
- **.NET 8.0+**: Target framework support for net8.0, net9.0, net10.0

## Installation

```bash
dotnet add package Breakdance.Azurite
```

## Quick Start

### Basic Usage

```csharp
using CloudNimble.Breakdance.Azurite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MyStorageTests : AzuriteTestBase
{
    [TestMethod]
    public async Task TestBlobStorage()
    {
        // Azurite is already running!
        var connectionString = ConnectionString;

        var blobClient = new BlobServiceClient(connectionString);
        var container = blobClient.GetBlobContainerClient("test");
        await container.CreateAsync();

        // ... your test code ...
    }
}
```

### Blob Service Only

```csharp
[TestClass]
public class BlobTests : AzuriteTestBase
{
    // Only start the Blob service
    protected override AzuriteServiceType Services => AzuriteServiceType.Blob;

    [TestMethod]
    public void TestBlobs()
    {
        BlobEndpoint.Should().NotBeNullOrEmpty();
        QueueEndpoint.Should().BeNull(); // Not started
    }
}
```

### Custom Configuration

```csharp
[TestClass]
public class CustomAzuriteTests : AzuriteTestBase
{
    protected override bool SilentMode => false; // Show access logs
    protected override int StartupTimeoutSeconds => 60; // Longer timeout
    protected override int? ExtentMemoryLimitMB => 100; // Limit memory

    [TestMethod]
    public void MyTest() { /* ... */ }
}
```

### Disk Persistence (Optional)

```csharp
[TestClass]
public class DiskPersistedTests : AzuriteTestBase
{
    protected override bool UseInMemoryPersistence => false;
    protected override string Location => @"C:\temp\azurite-data";

    [TestMethod]
    public void TestWithDiskStorage() { /* ... */ }
}
```

## Available Properties

| Property | Description |
|----------|-------------|
| `Azurite` | The `AzuriteInstance` object |
| `BlobEndpoint` | HTTP URL for Blob service |
| `QueueEndpoint` | HTTP URL for Queue service |
| `TableEndpoint` | HTTP URL for Table service |
| `BlobPort` | Port number for Blob service |
| `QueuePort` | Port number for Queue service |
| `TablePort` | Port number for Table service |
| `ConnectionString` | Azure Storage connection string |

## Configurable Options

| Option | Default | Description |
|--------|---------|-------------|
| `Services` | `AzuriteServiceType.All` | Which services to start |
| `UseInMemoryPersistence` | `true` | Use in-memory storage |
| `SilentMode` | `true` | Disable access logs |
| `StartupTimeoutSeconds` | `30` | Startup timeout |
| `ExtentMemoryLimitMB` | `null` | Memory limit (unlimited) |
| `Location` | `null` | Disk storage path |

## Parallel Test Execution

The library automatically handles port allocation to support multiple test instances running simultaneously:

```csharp
// These can run in parallel without conflicts
[TestClass]
public class ParallelTest1 : AzuriteTestBase { /* ... */ }

[TestClass]
public class ParallelTest2 : AzuriteTestBase { /* ... */ }

[TestClass]
public class ParallelTest3 : AzuriteTestBase { /* ... */ }
```

Each test class gets its own Azurite instance with unique ports (starting from 11000).

## Lifecycle Hooks

Choose the appropriate lifecycle for your tests:

```csharp
// Per-test (default)
public override void TestSetup() { /* Azurite starts */ }
public override void TestTearDown() { /* Azurite stops */ }

// Per-class
public override void ClassSetup() { /* Azurite starts once */ }
public override void ClassTearDown() { /* Azurite stops once */ }

// Per-assembly
public override void AssemblySetup() { /* Azurite starts once */ }
public override void AssemblyTearDown() { /* Azurite stops once */ }
```

Async versions are also available:
- `TestSetupAsync()` / `TestTearDownAsync()`
- `ClassSetupAsync()` / `ClassTearDownAsync()`
- `AssemblySetupAsync()` / `AssemblyTearDownAsync()`

## Advanced Usage

### Direct Instance Control

```csharp
var config = new AzuriteConfiguration
{
    Services = AzuriteServiceType.Blob | AzuriteServiceType.Queue,
    InMemoryPersistence = true,
    Silent = true,
    BlobPort = 15000 // Specific port
};

var azurite = new AzuriteInstance(config);
await azurite.StartAsync();

// Use azurite.BlobEndpoint, azurite.ConnectionString, etc.

await azurite.StopAsync();
azurite.Dispose();
```

### Port Manager

```csharp
var portManager = new PortManager();

// Allocate a single port
var port = portManager.GetAvailablePort();

// Allocate multiple ports
var ports = portManager.GetAvailablePorts(3);

// Release when done
portManager.ReleasePorts(ports);
```

## Troubleshooting

### "npm is not installed"

Install Node.js from https://nodejs.org/ and ensure `npm` is in your PATH.

### "Azurite failed to start"

1. Check `Azurite.StandardOutput` and `Azurite.StandardError` for diagnostics
2. Increase `StartupTimeoutSeconds` if startup is slow
3. Verify Node.js and npm are properly installed

### Port Conflicts

If you get port conflicts with manually specified ports, use dynamic allocation (don't set `BlobPort`, `QueuePort`, or `TablePort`).

## License

MIT License - see LICENSE file for details

## Contributing

Contributions welcome! Please submit issues and pull requests to the [Breakdance repository](https://github.com/CloudNimble/Breakdance).
