# Download and setup

KillerBench 0.1.0 is Unreleased. No public KillerBench package or download is available yet. This page describes the current package requirements so you can prepare without downloading an unofficial copy.

## Windows and runtime

- The current publish target is **Windows x64**. Windows 11 x64 is the current native validation baseline. Native ARM64, x86 and Windows Server distributions are not provided.
- The desktop package is **framework-dependent** and requires **.NET Desktop Runtime 10 for Windows x64**. The plain .NET Runtime, ASP.NET Core Runtime and .NET Framework do not supply WPF. An SDK is needed for development, not for ordinary use.
- Use a Windows version supported by [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Runtime support is not a claim that KillerBench repairs are validated on every supported edition or build. Windows 10 coverage remains pending and its supported editions are limited by Microsoft's current lifecycle requirements.
- Obtain the Desktop Runtime only from [Microsoft's .NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), choosing **.NET Desktop Runtime**, **Windows**, **x64**. Follow your organization's software-installation rules.

If `dotnet` is available, `dotnet --list-runtimes` should list `Microsoft.WindowsDesktop.App 10.0.x` for the x64 installation. A runtime installed only for another architecture does not satisfy the x64 app.

## When a package is published

1. Use the official download linked from this website. No KillerBench download link is active yet.
2. Read that package's requirements, license, included notices, signature and checksum information. A filename alone does not establish authenticity.
3. Follow the published package instructions. Keep any accompanying files together; do not assume an installer, updater or shortcut integration is included.
4. Start KillerBench as your normal account and follow [Your first diagnosis](first-run.html). Diagnostic access failures stay visible; elevation is a separate deliberate choice.

Do not bypass Windows security warnings to run an unverified copy. Keep a current backup before selecting any action that changes the machine.

## Current limits

The current build has twelve sections, read-only diagnosis, reports and individually selected repair surfaces. Native repair validation is still in progress. A working UI, successful build or fixture result does not establish repair behavior on a real computer. See [How changes stay safe](safety.html#development-status).

The illustrative console on the landing page is synthetic output. Final application screenshots and the first public package are still pending.

## Source, license and support

Read [About KillerBench](about.html) for source availability, distribution notices, evidence privacy and support information. The [Engine guide](../engine/) is for developers with an authorized source checkout; it does not imply that a public Engine package is available.
