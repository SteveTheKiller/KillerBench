# Getting started

Run a read-only diagnostic workflow and inspect its typed result in a few minutes.

## Requirements

- Windows x64 for the current native provider; check Microsoft's [.NET 10 Windows requirements](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
- .NET 10 SDK
- A .NET 10 application
- A writable folder for workflow journals

The Engine has no WPF dependency. The desktop interface is a separate host.

## Reference the project

Use an authorized source checkout. Reference the Windows provider project from your .NET 10 console host; its project references also include the shared Engine and Engine.Core:

```xml
<ProjectReference Include="path\to\KillerBench\Engine.Windows\KillerBench.Engine.Windows.csproj" />
```

## Run a focused workflow

```csharp
using KillerBench.Engine.Workflows;
using KillerBench.Engine.Windows;

WindowsEngine.Register();

string journalFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ExampleHost",
    "Workflows");

var progress = new Progress<string>(Console.WriteLine);

WorkflowResult result = await Workflows.RunAsync(
    WorkflowKind.Network,
    autoRepair: false,
    journalFolder,
    progress,
    token: CancellationToken.None);

Console.WriteLine(result.Status);

foreach (WorkflowCheck check in result.Checks)
{
    Console.WriteLine($"{check.State}: {check.Name}");
    Console.WriteLine(check.Detail);
}
```

Call `WindowsEngine.Register()` once at host startup before resolving native providers. Without registration, the platform-neutral Engine reports that no provider is registered. The example uses implicit .NET usings for System, IO and threading.

`autoRepair: false` keeps the run diagnostic-only. The Engine writes a workflow journal even when no repair is attempted.

## Run the broad diagnostic scan

```csharp
WorkflowResult result = await FullSystemScan.RunAsync(
    journalFolder,
    autoRepair: false,
    progress,
    token: CancellationToken.None);
```

The full scan combines common diagnostic areas. It does not prove every possible Windows component healthy, and it does not enable every optional repair.

## Handle cancellation

Pass a `CancellationToken` from your host. The Engine checks cancellation between bounded stages. If a repair may already have reached Windows, the durable journal remains available for reconciliation instead of silently retrying the action.

## Next steps

- Read [Diagnostic workflows](workflows.html) before selecting workflow kinds.
- Read [Repairs and safety](repairs.html) before exposing any repair action.
- Use the [CLI guide](cli.html) to explore diagnostics without writing a host application.
