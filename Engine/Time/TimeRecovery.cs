using Microsoft.Win32;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public interface ITimeRecoveryHost
{
    Task<WorkflowCheck> InspectAsync(IProgress<string>? progress, CancellationToken token);
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static class TimeRecovery
{
    public const string Tool = "w32tm.exe";
    public static RepairStep ServiceStartStep() => new("sc.exe", ["start", "W32Time"], false);
    public static RepairStep Step(bool verification) => new(Tool,
        verification ? ["/query", "/status", "/verbose"] : ["/resync"], verification);

    public static WorkflowCheck Interpret(ToolResult result)
    {
        int? Number(string name)
        {
            var fields = Regex.Matches(result.Output, @"(?m)^" + Regex.Escape(name) + @":([^\r\n]*)", RegexOptions.CultureInvariant);
            if (fields.Count != 1) return null;
            var number = Regex.Match(fields[0].Groups[1].Value, @"^[ \t]*([0-9]+)(?:[ \t]*\([^\r\n]*\))?[ \t]*$", RegexOptions.CultureInvariant);
            return number.Success && int.TryParse(number.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : null;
        }
        int? leap = Number("Leap Indicator"), stratum = Number("Stratum"), error = Number("Last Sync Error");
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error) || leap is null or > 3 || stratum is null or > 16 || error is null)
            return new("Time synchronization", CheckState.Unknown, "Synchronization status is incomplete or unrecognized. No resync selected.", Evidence: result);
        if (leap == 3)
            return new("Time synchronization", CheckState.Repairable, "Windows Time reports it is not synchronized. Requesting a resync uses existing configured sources and can adjust the system clock.", RepairKind.TimeSynchronization, result);
        var sources = Regex.Matches(result.Output, @"(?m)^Source:[ \t]*([^\r\n]+)", RegexOptions.CultureInvariant);
        string source = sources.Count == 1 ? sources[0].Groups[1].Value.Trim() : "";
        if (error == 0 && stratum is >= 1 and <= 15 && source.Length > 0 &&
            !source.Equals("Local CMOS Clock", StringComparison.OrdinalIgnoreCase) && !source.Equals("Free-running System Clock", StringComparison.OrdinalIgnoreCase))
            return new("Time synchronization", CheckState.Clear, "Windows Time reports synchronization from " + source + ". Absolute clock accuracy and source trust are not measured.", Evidence: result);
        return new("Time synchronization", CheckState.Attention, "Windows Time does not report a verified external/provider synchronization. Review the configured source and recorded error; settings are preserved.", Evidence: result);
    }

    internal static async Task<WorkflowCheck> CheckAsync(IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Time checks require Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\W32Time\Parameters");
        using var local = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\W32Time\Parameters");
        string? type = (policy?.GetValue("Type") ?? local?.GetValue("Type")) as string;
        if (!CanSynchronize(type)) return new("Time synchronization", CheckState.Attention, "Time synchronization is disabled or its configuration is unrecognized. No configuration change or resync selected.");
        var status = await ExecuteAsync(Step(true), progress, token).ConfigureAwait(false);
        if (!IsServiceNotStarted(status)) return Interpret(status);
        var service = await ServiceRecovery.CheckAsync(RepairKind.WindowsTime, token, true).ConfigureAwait(false);
        if (service.State != CheckState.Repairable)
            return new("Time synchronization", service.State, "Windows Time status is unavailable because the service is stopped. " + service.Detail,
                Evidence: status) { ServiceDependencies = service.ServiceDependencies };
        return new("Time synchronization", CheckState.Repairable,
            "Windows Time status is unavailable because the demand-start service is stopped. Current synchronization cannot be checked until the service runs. The selected recovery can start it, request synchronization from existing configured sources and verify fresh status without changing startup settings.",
            RepairKind.TimeSynchronization, status)
            { ServiceDependencies = service.ServiceDependencies, DemandStartAuthorized = true };
    }

    public static bool IsServiceNotStarted(ToolResult result) => result.ExitCode == unchecked((int)0x80070426) &&
        !result.TimedOut && !result.Truncated;

    public static bool CanSynchronize(string? type) => type is not null &&
        new[] { "NTP", "NT5DS", "AllSync" }.Contains(type, StringComparer.OrdinalIgnoreCase);

    public static bool IsResyncEligible(WorkflowCheck? check)
    {
        if (check is not { State: CheckState.Repairable, Repair: RepairKind.TimeSynchronization, Evidence: not null }) return false;
        var interpreted = Interpret(check.Evidence);
        return interpreted.State == CheckState.Repairable && interpreted.Repair == RepairKind.TimeSynchronization;
    }

    public static void RequirePlanEligible(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        bool stoppedDemandStart = check is
        {
            State: CheckState.Repairable,
            Repair: RepairKind.TimeSynchronization,
            DemandStartAuthorized: true,
            Evidence: not null,
            ServiceDependencies: { Complete: true, Issues.Count: 0 }
        } && IsServiceNotStarted(check.Evidence);
        if (!IsResyncEligible(check) && !stoppedDemandStart)
            throw new InvalidOperationException("Fresh exact unsynchronized or authorized stopped-service evidence is required before Windows Time recovery. " + check.Detail);
    }

    internal static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress, CancellationToken token = default) =>
        ExecuteAsync(step, new WindowsTimeRecoveryHost(), progress, token);

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, ITimeRecoveryHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        bool serviceStart = Matches(step, ServiceStartStep());
        bool resync = Matches(step, Step(false));
        bool verify = Matches(step, Step(true));
        if (!serviceStart && !resync && !verify)
            throw new ArgumentException("Command does not match the Windows Time recovery plan.", nameof(step));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            if (serviceStart)
            {
                progress?.Report("Rechecking Windows Time service and dependencies before start...");
                var service = await ServiceRecovery.CheckAsync(RepairKind.WindowsTime, timeout.Token, true).ConfigureAwait(false);
                if (service.State == CheckState.Clear)
                    throw new RepairStepSkippedException("Windows Time is already running. No service start command was run.", service.Evidence!);
                if (!ServiceRecovery.CanStart(RepairKind.WindowsTime, service))
                    throw new RepairDispatchDeferredException("Windows Time service start was deferred because fresh exact start eligibility was not available. " + service.Detail);
            }
            if (resync)
            {
                progress?.Report("Rechecking Windows Time synchronization before resync...");
                var currentTask = host.InspectAsync(progress, timeout.Token)
                    ?? throw new InvalidOperationException("Windows Time inspection provider returned no task.");
                var current = await currentTask.WaitAsync(TimeSpan.FromSeconds(60), token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Windows Time inspection provider returned no evidence.");
                if (current is { State: CheckState.Clear, Repair: null, Evidence: not null } && Interpret(current.Evidence).State == CheckState.Clear)
                {
                    progress?.Report("Windows Time is already synchronized. No resync command was run.");
                    throw new RepairStepSkippedException("Windows Time is already synchronized. No resync command was run.", current.Evidence);
                }
                if (!IsResyncEligible(current))
                    throw new RepairDispatchDeferredException(
                        "Windows Time resync was deferred because fresh exact unsynchronized evidence was not available. " + current.Detail);
                progress?.Report("Fresh unsynchronized evidence confirmed. Requesting bounded Windows Time resync...");
            }
            var pending = host.RunAsync(step, progress, timeout.Token)
                ?? throw new InvalidOperationException("Windows Time provider returned no command task.");
            return await pending.WaitAsync(TimeSpan.FromSeconds(61), token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Windows Time provider returned no command evidence.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { return new(-1, "", "Time operation timed out. Synchronization is unverified; configured sources were not changed.", false) { TimedOut = true }; }
        catch (TimeoutException)
        {
            timeout.Cancel();
            return new(-1, "", "Time operation timed out. Synchronization is unverified; configured sources were not changed.", false) { TimedOut = true };
        }
    }

    private static bool Matches(RepairStep actual, RepairStep expected) =>
        actual.Tool == expected.Tool && actual.Verification == expected.Verification && actual.Arguments.SequenceEqual(expected.Arguments);
}

public sealed class WindowsTimeRecoveryHost : ITimeRecoveryHost
{
    public Task<WorkflowCheck> InspectAsync(IProgress<string>? progress, CancellationToken token) =>
        TimeRecovery.CheckAsync(progress, token);

    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        NativeTool.RunAsync(step.Tool, step.Arguments, progress: progress, token: token);
}
