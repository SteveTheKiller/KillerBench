using System.Globalization;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Counts devices with a problem code through Get-PnpDevice. Value is the count; Raw lists instance id, class and status.</summary>
public sealed class WindowsProblemDevicesProbe : IProbePrimitive
{
    public string Name => "pnp.problem-devices";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Get-PnpDevice -PresentOnly -ErrorAction Stop|Where-Object{$_.Status -ne 'OK'}|ForEach-Object{$_.InstanceId+'|'+$_.Class+'|'+$_.Status+'|'+$_.FriendlyName}";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Device inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-PnpDevice", lines =>
    {
        foreach (string line in lines) if (line.Split('|').Length < 4) return PrimitiveResult.Unavailable("Device inventory line was not recognized.");
        return new PrimitiveResult(true, lines.Length.ToString(CultureInfo.InvariantCulture), string.Join("\n", lines))
        { Detail = lines.Length == 0 ? "Every present device reports OK." : lines.Length + " present device(s) report a problem; the evidence lists instance ids." };
    });
}

/// <summary>Reports one device's status by instance id. Value is the PnP status such as OK, Error or Degraded.</summary>
public sealed partial class WindowsDeviceStatusProbe : IProbePrimitive
{
    [GeneratedRegex("^[A-Za-z]+$")]
    private static partial Regex StatusPattern();

    public const string InstancePattern = @"[A-Za-z0-9&{}_#.\\-]{4,200}";
    public static readonly PrimitiveParameter InstanceParameter = new("instance", true, "Exact device instance id from Device Manager or pnp.problem-devices") { Pattern = InstancePattern };
    public string Name => "pnp.device-status";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [InstanceParameter];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$id) $d=@(Get-PnpDevice -InstanceId $id -ErrorAction Stop); if($d.Count -ne 1){throw 'Exact device not found.'}; [string]$d[0].Status }";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Device inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["instance"]).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-PnpDevice", lines =>
        lines.Length == 1 && StatusPattern().IsMatch(lines[0])
            ? new PrimitiveResult(true, lines[0], lines[0]) { Detail = "The device reports " + lines[0] + "." }
            : PrimitiveResult.Unavailable("Device status output was not recognized."));
}

/// <summary>Restarts one device through pnputil, which re-enumerates it and reloads its driver.</summary>
public sealed class WindowsDeviceRestartAction : IActionPrimitive
{
    public string Name => "pnp.restart-device";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsDeviceStatusProbe.InstanceParameter];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Device changes require Windows.");
        var result = await NativeTool.RunAsync("pnputil.exe", ["/restart-device", parameters["instance"]], progress: progress, token: token).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "pnputil timed out.") { TimedOut = true };
        bool ok = result.ExitCode == 0;
        return new ActionResult(true, ok, raw, ok ? null : "pnputil exited with " + result.ExitCode + ".") { RestartRequired = result.ExitCode == 3010 };
    }
}

/// <summary>Counts formatted volumes without a drive letter. Value is the count; Raw lists disk, partition, size and file system.</summary>
public sealed partial class WindowsUnletteredVolumesProbe : IProbePrimitive
{
    [GeneratedRegex(@"^\d+\|\d+\|[0-9.]+\|[A-Za-z0-9]+$")]
    private static partial Regex VolumeLinePattern();

    public string Name => "volume.unlettered";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Get-Partition -ErrorAction Stop|Where-Object{-not $_.DriveLetter -and $_.Type -in 'Basic','IFS' -and -not $_.IsHidden -and -not $_.IsSystem -and $_.Size -gt 100MB}|ForEach-Object{" +
        "$v=$_|Get-Volume -ErrorAction SilentlyContinue; if($v -and $v.FileSystem){$_.DiskNumber.ToString()+'|'+$_.PartitionNumber.ToString()+'|'+[math]::Round($_.Size/1GB,1).ToString([Globalization.CultureInfo]::InvariantCulture)+'|'+$v.FileSystem}}";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Volume inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-Partition", lines =>
    {
        foreach (string line in lines) if (!VolumeLinePattern().IsMatch(line)) return PrimitiveResult.Unavailable("Volume inventory line was not recognized.");
        return new PrimitiveResult(true, lines.Length.ToString(CultureInfo.InvariantCulture), string.Join("\n", lines))
        { Detail = lines.Length == 0 ? "Every formatted data volume has a drive letter." : lines.Length + " formatted volume(s) have no drive letter; the evidence lists disk and partition numbers." };
    });
}

/// <summary>Assigns a free drive letter to one partition identified by disk and partition number.</summary>
public sealed class WindowsAssignDriveLetterAction : IActionPrimitive
{
    public string Name => "volume.assign-letter";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
        [new("disk", true, "Disk number") { Pattern = "[0-9]{1,3}" }, new("partition", true, "Partition number") { Pattern = "[0-9]{1,3}" }, new("letter", true, "Drive letter D through Z") { Pattern = "[D-Zd-z]" }];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => true;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([int]$disk,[int]$partition,[string]$letter) $p=Get-Partition -DiskNumber $disk -PartitionNumber $partition -ErrorAction Stop; if($p.DriveLetter){'AlreadyLettered';exit 0}; " +
        "if(Get-Volume -DriveLetter $letter -ErrorAction SilentlyContinue){throw 'Drive letter is in use.'}; " + MutationBoundary.Script + "Set-Partition -DiskNumber $disk -PartitionNumber $partition -NewDriveLetter $letter -ErrorAction Stop; 'Assigned' }";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Volume changes require Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["disk"], parameters["partition"], parameters["letter"].ToUpperInvariant()).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Set-Partition timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return MutationBoundary.Failed(result, raw, "Set-Partition failed: " + result.Error.Trim());
        string text = MutationBoundary.Output(result);
        return text == "AlreadyLettered" ? new ActionResult(false, true, raw, "The partition already has a drive letter; nothing changed.")
            : new ActionResult(true, text == "Assigned", raw, text == "Assigned" ? null : "Unexpected output.");
    }
}

/// <summary>Reports the status of one installed app package for the current account. Value is the package status such as Ok.</summary>
public sealed partial class WindowsAppxStatusProbe : IProbePrimitive
{
    [GeneratedRegex("^[A-Za-z, ]+$")]
    private static partial Regex PackageStatusPattern();

    public const string FullNamePattern = @"[A-Za-z0-9][A-Za-z0-9._-]{2,255}";
    public static readonly PrimitiveParameter FullNameParameter = new("package", true, "Exact package full name from Get-AppxPackage") { Pattern = FullNamePattern };
    public string Name => "appx.status";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [FullNameParameter];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $p=@(Get-AppxPackage -Package $name -ErrorAction Stop); if($p.Count -ne 1){throw 'Exact package not found for this account.'}; [string]$p[0].Status }";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Package inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["package"]).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-AppxPackage", lines =>
        lines.Length == 1 && PackageStatusPattern().IsMatch(lines[0])
            ? new PrimitiveResult(true, lines[0], lines[0]) { Detail = "The package reports " + lines[0] + "." }
            : PrimitiveResult.Unavailable("Package status output was not recognized."));
}

/// <summary>Re-registers one installed app package for the current account from its existing install location.</summary>
public sealed class WindowsAppxReregisterAction : IActionPrimitive
{
    public string Name => "appx.reregister";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsAppxStatusProbe.FullNameParameter];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => false;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $p=@(Get-AppxPackage -Package $name -ErrorAction Stop); if($p.Count -ne 1){throw 'Exact package not found for this account.'}; " +
        "$m=Join-Path $p[0].InstallLocation 'AppxManifest.xml'; if(-not (Test-Path $m)){throw 'AppxManifest.xml is missing from the install location.'}; " +
        MutationBoundary.Script + "Add-AppxPackage -DisableDevelopmentMode -Register $m -ErrorAction Stop; 'Registered' }";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Package changes require Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["package"]).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Add-AppxPackage timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return MutationBoundary.Failed(result, raw, "Add-AppxPackage failed: " + result.Error.Trim());
        return new ActionResult(true, MutationBoundary.Output(result) == "Registered", raw);
    }
}
