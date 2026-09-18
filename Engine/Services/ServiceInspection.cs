namespace KillerBench.Engine;

public static class ServiceInspection
{
    public static Task<IReadOnlyList<WorkflowCheck>> ReadAsync(IEnumerable<RepairKind> scopes, CancellationToken token = default, IProgress<string>? progress = null) =>
        ReadAsync(scopes, ServiceRecovery.CheckAsync, token, progress);

    public static async Task<IReadOnlyList<WorkflowCheck>> ReadAsync(IEnumerable<RepairKind> scopes,
        Func<RepairKind, CancellationToken, Task<WorkflowCheck>> inspect, CancellationToken token = default, IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(inspect);
        progress = progress is null ? null : new DiagnosticProgress(progress);
        var selected = new HashSet<RepairKind>();
        var ordered = new List<RepairKind>();
        int count = 0;
        foreach (var kind in scopes)
        {
            token.ThrowIfCancellationRequested();
            if (++count > 128) throw new ArgumentException("At most 128 scope entries are supported.", nameof(scopes));
            if (!Enum.IsDefined(kind)) throw new ArgumentException("Unknown repair scope.", nameof(scopes));
            if (ServiceRecovery.ServiceName(kind) is not null && selected.Add(kind)) ordered.Add(kind);
        }
        var checks = new List<WorkflowCheck>();
        foreach (var kind in ordered)
        {
            token.ThrowIfCancellationRequested();
            progress?.Report("Inspecting " + ServiceRecovery.ServiceName(kind) + "...");
            token.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                var pending = inspect(kind, deadline.Token) ?? throw new InvalidOperationException("Inspection provider returned no operation.");
                var check = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Inspection provider returned no observation.");
                token.ThrowIfCancellationRequested();
                if (check.Evidence is { } evidence && (evidence.TimedOut || evidence.Truncated || evidence.ExitCode != 0 || !string.IsNullOrWhiteSpace(evidence.Error)))
                    check = check with
                    {
                        State = CheckState.Unknown,
                        Detail = "Service evidence is incomplete or collection failed. The current service state is unconfirmed."
                    };
                checks.Add(check with
                {
                    Repair = null,
                    State = check.State == CheckState.Repairable ? CheckState.Attention : check.State,
                    Detail = check.State == CheckState.Repairable
                        ? ReadOnlyRepairableDetail(check)
                        : check.Detail
                });
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                checks.Add(new(kind.ToString(), CheckState.Unknown, "Read-only service inspection did not complete. Other service checks continue."));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or TimeoutException or InvalidOperationException)
            {
                checks.Add(new(kind.ToString(), CheckState.Unknown, "Read-only service inspection was unavailable or incomplete. Other service checks continue."));
            }
            var observed = checks[^1];
            string detail = observed.Detail.Length <= 2000 ? observed.Detail : observed.Detail[..2000] + " (see full result)";
            progress?.Report(ServiceRecovery.ServiceName(kind) + ": " + observed.EffectiveState + ". " + detail);
        }
        token.ThrowIfCancellationRequested();
        return checks.AsReadOnly();
    }

    private static string ReadOnlyRepairableDetail(WorkflowCheck check) =>
        check.ServiceDependencies?.TargetStartup == 2
            ? "Automatic service is stopped. This read-only inspection did not start it. A focused recovery workflow can start it after fresh dependency and eligibility checks without changing startup settings."
            : "Service is stopped, which can be normal when idle. This read-only inspection did not start it. Run an elevated recovery workflow if a start is needed.";
}
