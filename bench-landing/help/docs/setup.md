# Download and setup

KillerBench 0.1.0 is Unreleased. No public KillerBench package or download is available yet. This page describes the current package requirements so you can prepare without downloading an unofficial copy.

## Windows and runtime

- The current publish target is **Windows x64**. Windows 11 x64 is the current native validation baseline. Native ARM64, x86 and Windows Server distributions are not provided.
- KillerBench is packaged two ways. The installer is **framework-dependent** and requires **.NET Desktop Runtime 10 for Windows x64**. The plain .NET Runtime, ASP.NET Core Runtime and .NET Framework do not supply WPF. The portable edition carries its own runtime and needs no separate install. The command-line tool `killerbench-cli.exe` needs the .NET 10 Runtime for Windows x64. An SDK is needed for development, not for ordinary use.
- Use a Windows version supported by [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Runtime support is not a claim that KillerBench repairs are validated on every supported edition or build. Windows 10 coverage remains pending and its supported editions are limited by Microsoft's current lifecycle requirements.
- Obtain the Desktop Runtime only from [Microsoft's .NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), choosing **.NET Desktop Runtime**, **Windows**, **x64**. Follow your organization's software-installation rules.

If `dotnet` is available, `dotnet --list-runtimes` should list `Microsoft.WindowsDesktop.App 10.0.x` for the x64 installation. A runtime installed only for another architecture does not satisfy the x64 installer package.

## When a package is published

1. Use the official download linked from this website. No KillerBench download link is active yet.
2. Read that package's requirements, [GPLv3 license](../license.txt), Corresponding Source instructions, included third-party notices, signature and checksum information. A filename alone does not establish authenticity.
3. Run the installer for the current account, or for all users after an elevation prompt. It adds a Start menu shortcut, an optional desktop shortcut and an Add or Remove Programs entry. The portable edition runs without installation.
4. Start KillerBench as your normal account and follow [Your first diagnosis](first-run.html). Diagnostic access failures stay visible; elevation is a separate deliberate choice.

Do not bypass Windows security warnings to run an unverified copy. Keep a current backup before selecting any action that changes the machine.

At startup KillerBench checks GitHub for a newer stable release and asks before installing it. The download must match the published SHA-256 checksum. Turn the check off with **Check for updates on startup** in About.

## Current limits

The current build has twelve sections, read-only diagnosis, reports and individually selected repair surfaces. Native repair validation is still in progress. A working UI, successful build or fixture result does not establish repair behavior on a real computer. See [How changes stay safe](safety.html#development-status).

The illustrative console on the landing page is synthetic output. Final application screenshots and the first public package are still pending.

## Source, license and support

KillerBench is licensed under GPLv3. Source-delivery instructions and third-party font permissions remain pending for the first public package. Read [About KillerBench](about.html) for source availability, distribution notices, evidence privacy and support information. The [Engine guide](../engine/) is for developers with an authorized source checkout; it does not imply that a public Engine package is available.
