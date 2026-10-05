using System.Text.Json;

namespace KillerBench.Engine.Time;

public interface ITimeConfigurationHost
{
    Task<TimeEnvironment> InspectAsync(CancellationToken token);
    Task ApplyAsync(TimeOptions options, CancellationToken token);
    Task<bool> VerifyAsync(TimeOptions options, CancellationToken token);
}

public sealed record TimeConfigurationResult(string Status, string Journal)
{
    public TimeEnvironment? Before { get; init; }
    public TimeEnvironment? After { get; init; }
    public string Detail { get; init; } = "";
}

public static partial class TimeConfiguration
{
    [System.Text.RegularExpressions.GeneratedRegex(@"(?m)^Source:[ \t]*([^\r\n]+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex SourceLineRegex();

    public static async Task<bool> VerifyAsync(TimeOptions options, Func<CancellationToken, Task<TimeEnvironment>>? inspect = null,
        Func<CancellationToken, Task<ToolResult>>? queryStatus = null,
        CancellationToken token = default)
    {
        var validated = TimePlanning.Create(options, new(null, null, null, false));
        if (validated.Source == TimeSourceMode.Existing) return false;
        var retained = new TimeOptions(validated.Source, validated.Peers);
        var collector = inspect ?? TimeEnvironmentCollector.ReadAsync;
        token.ThrowIfCancellationRequested();
        var before = await collector(token).ConfigureAwait(false);
        if (!MatchesStoredConfiguration(retained, before)) return false;
        token.ThrowIfCancellationRequested();
        var status = await (queryStatus ?? (ct => TimeRecovery.ExecuteAsync(TimeRecovery.Step(true), null, ct)))(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var after = await collector(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (before != after || !MatchesStoredConfiguration(retained, after)) return false;
        return retained.Source == TimeSourceMode.CustomPeers
            ? MatchesCustomSynchronization(retained, after, status)
            : MatchesDomainSynchronization(retained, after, status);
    }

    public static async Task<bool> VerifyCustomAsync(TimeOptions options, Func<CancellationToken, Task<TimeEnvironment>>? inspect = null,
        Func<CancellationToken, Task<ToolResult>>? queryStatus = null,
        CancellationToken token = default)
    {
        if (options.Source != TimeSourceMode.CustomPeers) return false;
        return await VerifyAsync(options, inspect, queryStatus, token: token).ConfigureAwait(false);
    }

    public static bool MatchesCustomSynchronization(TimeOptions options, TimeEnvironment current, ToolResult status)
    {
        if (options.Source != TimeSourceMode.CustomPeers || !MatchesStoredConfiguration(options, current) ||
            TimeRecovery.Interpret(status).State != CheckState.Clear) return false;
        var sources = SourceLineRegex().Matches(status.Output);
        if (sources.Count != 1) return false;
        string source = sources[0].Groups[1].Value.Trim();
        return options.Peers is not null && options.Peers.Contains(source, StringComparer.OrdinalIgnoreCase);
    }

    public static bool MatchesDomainSynchronization(TimeOptions options, TimeEnvironment current, ToolResult status) =>
        options.Source == TimeSourceMode.DomainHierarchy && current.DomainJoined == true &&
        current.DomainController == false && current.PolicyManaged == false &&
        MatchesStoredConfiguration(options, current) && TimeRecovery.Interpret(status).State == CheckState.Clear;

    public static bool MatchesStoredConfiguration(TimeOptions options, TimeEnvironment current)
    {
        var plan = TimePlanning.Create(options, current);
        if (!plan.ReviewPassed || plan.Source == TimeSourceMode.Existing) return false;
        if (plan.Source == TimeSourceMode.DomainHierarchy)
            return string.Equals(current.LocalMode, "NT5DS", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(current.LocalMode, "NTP", StringComparison.OrdinalIgnoreCase) || current.LocalPeers is null) return false;
        var actual = current.LocalPeers.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var expected = plan.Peers.Select(peer => peer + ",0x8").ToArray();
        return actual.Length == expected.Length && actual.Order(StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(expected.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    }

    public static RepairStep ConfigurationStep(TimeOptions options)
    {
        var plan = TimePlanning.Create(options, new(null, null, null, false));
        return plan.Source switch
        {
            TimeSourceMode.DomainHierarchy => new(TimeRecovery.Tool, ["/config", "/syncfromflags:domhier", "/update"], false),
            TimeSourceMode.CustomPeers => new(TimeRecovery.Tool,
                ["/config", "/manualpeerlist:" + string.Join(" ", plan.Peers.Select(peer => peer + ",0x8")), "/syncfromflags:manual", "/update"], false),
            _ => throw new ArgumentException("Keeping the current source does not require a configuration command.", nameof(options))
        };
    }

    public static async Task<TimeConfigurationResult> RunAsync(TimeOptions options, string folder,
        ITimeConfigurationHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        var validated = TimePlanning.Create(options, new(null, null, null, false));
        if (validated.Source == TimeSourceMode.Existing) throw new ArgumentException("Use the existing time workflow to keep configured sources.");
        var retained = new TimeOptions(validated.Source, validated.Peers);
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(folder);
        using var gate = new FileStream(Path.Combine(folder, "time.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(folder, "pending-time.json");
        if (File.Exists(pending)) throw new InvalidOperationException("An earlier time change is unresolved. No retry started.");
        string journal = Path.Combine(folder, "time-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var output = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        TimeEnvironment? before = null;
        TimeEnvironment? after = null;
        TimeConfigurationResult Result(string status, string detail) => new(status, journal)
            { Before = before, After = after, Detail = detail };
        void Record(string stage, object? evidence = null)
        {
            output.Write(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Stage = stage, TimeUtc = DateTimeOffset.UtcNow, Evidence = evidence }) + "\n"));
            output.Flush(true);
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        var operationToken = deadline.Token;
        bool prepared = false;
        bool preparationCompleted = false;
        bool applyDispatched = false;
        void ClosePreparedRecord()
        {
            if (!prepared || !File.Exists(pending)) return;
            using var record = RecoveryRecordFiles.OpenLocked(pending);
            RecoveryRecordFiles.Archive(record, journal + ".prepared.json");
            prepared = false;
        }
        Record("Started");
        try
        {
            var first = await host.InspectAsync(operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            var second = await host.InspectAsync(operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            before = second;
            Record("Before", second);
            if (first != second || !TimePlanning.Create(retained, second).ReviewPassed)
            { Record("Prerequisites blocked or changed"); return Result("Blocked", "Prerequisites are unavailable, blocked or changed between checks. No configuration command started."); }
            operationToken.ThrowIfCancellationRequested();
            using (var marker = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(marker, new
                {
                    Schema = 1,
                    Machine = Environment.MachineName,
                    StartedUtc = DateTimeOffset.UtcNow,
                    Options = retained,
                    Before = second,
                    Journal = journal
                }, Capture.JsonOptions);
                marker.Flush(true);
            }
            prepared = true;
            preparationCompleted = true;
            Record("Prepared");
            using var pendingRecord = RecoveryRecordFiles.OpenLocked(pending);
            operationToken.ThrowIfCancellationRequested();
            var final = await host.InspectAsync(operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            Record("Final eligibility", final);
            if (final != second || !TimePlanning.Create(retained, final).ReviewPassed)
            {
                Record("Final eligibility blocked");
                RecoveryRecordFiles.Archive(pendingRecord, journal + ".prepared.json");
                prepared = false;
                return Result("Blocked", "Prerequisites changed after durable preparation. No configuration command started.");
            }
            applyDispatched = true;
            await host.ApplyAsync(retained, operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            bool verified = await host.VerifyAsync(retained, operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            after = await host.InspectAsync(operationToken).WaitAsync(operationToken).ConfigureAwait(false);
            Record("After", after);
            if (!verified || !MatchesStoredConfiguration(retained, after))
            { Record("Verification incomplete"); return Result("Needs review", "The selected configuration and synchronization were not both verified. Pending evidence is retained; no automatic retry."); }
            operationToken.ThrowIfCancellationRequested();
            Record("Verified");
            RecoveryRecordFiles.Archive(pendingRecord, Path.Combine(folder, "verified-time-" + Guid.NewGuid().ToString("N") + ".json"));
            prepared = false;
            return Result("Verified", "Selected configuration and reported synchronization verified. Absolute clock accuracy is not measured.");
        }
        catch (OperationCanceledException)
        {
            if (!applyDispatched) ClosePreparedRecord();
            Record(applyDispatched || !preparationCompleted
                ? "Stopped; state unverified"
                : "Stopped before configuration; no change requested");
            return Result(token.IsCancellationRequested ? "Stopped" : applyDispatched ? "Timed out" : "Blocked",
                applyDispatched || !preparationCompleted ? "Operation ended before verification completed. Any pending record is retained."
                    : "Operation ended before configuration started. The prepared no-change record was archived.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException)
        {
            if (!applyDispatched) ClosePreparedRecord();
            Record(applyDispatched || !preparationCompleted
                ? "Failed; state unverified"
                : "Final eligibility unavailable; no change requested", error.Message);
            return Result(!applyDispatched && preparationCompleted ? "Blocked" : "Needs review", error.Message);
        }
    }
}
