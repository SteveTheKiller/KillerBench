using System.Text.Json;

namespace KillerBench.Engine.Monitoring;

public static class ServicingPhaseDiagnostics
{
    public static WorkflowCheck Summarize(IReadOnlyList<WorkflowCheck> checks, DateTimeOffset capturedUtc)
    {
        ArgumentNullException.ThrowIfNull(checks);
        if (capturedUtc == default)
            return Result(CheckState.Unknown, "The snapshot time is unavailable, so a live servicing state could not be summarized.");

        var processCheck = checks.SingleOrDefault(check => check.Name == "Active servicing processes");
        if (!TryProcesses(processCheck, out var activeGroups))
            return Result(CheckState.Unknown, "The bounded servicing process snapshot is unavailable or invalid, so a live servicing state could not be summarized.");

        if (TryRecentSetupMode(checks, capturedUtc, out string setupMode, out long setupAgeSeconds))
            return Result(CheckState.Information,
                $"WINDOWS SETUP / {setupMode.ToUpperInvariant()}. The latest classified Panther activity is {setupAgeSeconds} seconds old.");

        if (TryRecentUpdate(checks, capturedUtc, out WindowsUpdateTransition update, out long updateAgeSeconds))
            return Result(update.EventId is 20 or 212 or 214 ? CheckState.Attention : CheckState.Information,
                $"WINDOWS UPDATE / {update.State.ToUpperInvariant()}. The latest recognized transition is {updateAgeSeconds} seconds old.");

        string[] setup = Active(activeGroups, "Windows11InstallationAssistant", "setup", "SetupHost", "SetupPrep");
        string[] components = Active(activeGroups, "TiWorker", "TrustedInstaller", "dism");
        string[] orchestration = Active(activeGroups, "wuaulcore", "MoUsoCoreWorker", "UsoClient", "WaaSMedicAgent");
        var signals = new List<string>();
        if (setup.Length > 0) signals.Add("WINDOWS SETUP / ACTIVE (" + string.Join(", ", setup) + ")");
        if (components.Length > 0) signals.Add("COMPONENT SERVICING / ACTIVE (" + string.Join(", ", components) + ")");
        if (orchestration.Length > 0) signals.Add("UPDATE ORCHESTRATION / ACTIVE (" + string.Join(", ", orchestration) + ")");
        if (TryInstaller(checks, out bool installerActive) && installerActive)
            signals.Add("WINDOWS INSTALLER / ACTIVE");
        if (signals.Count > 0)
            return Result(CheckState.Information, string.Join("; ", signals) + ".");

        if (TryRestart(checks, out bool restartPending) && restartPending)
            return Result(CheckState.Information, "RESTART / PENDING. No supported servicing process is active in this snapshot.");

        return Result(CheckState.Information,
            "IDLE / NO SUPPORTED SERVICING PROCESS OBSERVED. Background or between-sample activity can still exist.");
    }

    private static bool TryProcesses(WorkflowCheck? check, out IReadOnlyList<ServicingProcessGroup> groups)
    {
        groups = [];
        if (check is not { State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence }) return false;
        try
        {
            var snapshot = JsonSerializer.Deserialize<ServicingProcessSnapshot>(evidence.Output, Capture.JsonOptions);
            if (snapshot?.Groups is null || snapshot.Groups.Count != ProcessActivityDiagnostics.Names.Length ||
                snapshot.Groups.Any(group => group.Count is < 0 or > 256) ||
                snapshot.Groups.Select(group => group.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ProcessActivityDiagnostics.Names.Length ||
                ProcessActivityDiagnostics.Names.Any(name => !snapshot.Groups.Any(group => group.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
                return false;
            groups = snapshot.Groups;
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool TryRecentSetupMode(IReadOnlyList<WorkflowCheck> checks, DateTimeOffset capturedUtc,
        out string mode, out long ageSeconds)
    {
        mode = "";
        ageSeconds = 0;
        var check = checks.SingleOrDefault(item => item.Name == "Windows Setup activity");
        if (check is not { State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence }) return false;
        try
        {
            var snapshot = JsonSerializer.Deserialize<SetupActivitySnapshot>(evidence.Output, Capture.JsonOptions);
            var classified = snapshot?.Logs?.Where(log => log.Mode.Length > 0)
                .OrderByDescending(log => log.LastWriteUtc).FirstOrDefault();
            if (snapshot is not { Complete: true } || classified is null ||
                classified.Mode is not ("Compatibility Scan" or "PreDownload / Package" or "In-Place Upgrade" or "Finalize" or "Windows Setup"))
                return false;
            TimeSpan age = capturedUtc - classified.LastWriteUtc;
            if (age < TimeSpan.Zero || age > TimeSpan.FromSeconds(30)) return false;
            mode = classified.Mode;
            ageSeconds = (long)age.TotalSeconds;
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool TryInstaller(IReadOnlyList<WorkflowCheck> checks, out bool active)
    {
        active = false;
        var check = checks.SingleOrDefault(item => item.Name == "Installer activity summary");
        if (check is not { State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence }) return false;
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("processCount", out var countValue) || !countValue.TryGetInt32(out int count) || count is < 0 or > 128 ||
                !root.TryGetProperty("marker", out var marker) || marker.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return false;
            active = count > 0 || marker.GetBoolean();
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool TryRecentUpdate(IReadOnlyList<WorkflowCheck> checks, DateTimeOffset capturedUtc,
        out WindowsUpdateTransition transition, out long ageSeconds)
    {
        transition = null!;
        ageSeconds = 0;
        var check = checks.SingleOrDefault(item => item.Name == "Windows Update live activity");
        if (!WindowsUpdateActivityDiagnostics.TryLatestTransition(check, out transition)) return false;
        TimeSpan age = capturedUtc - transition.TimeUtc;
        if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(2)) return false;
        ageSeconds = (long)age.TotalSeconds;
        return true;
    }

    private static bool TryRestart(IReadOnlyList<WorkflowCheck> checks, out bool pending)
    {
        pending = false;
        var check = checks.SingleOrDefault(item => item.Name == "Restart");
        if (check is not { State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence }) return false;
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True)
                return false;
            string[] names = ["componentServicing", "windowsUpdate", "pendingFileRenames", "computerRename", "updateExecutable"];
            foreach (string name in names)
            {
                if (!root.TryGetProperty(name, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return false;
                pending |= value.GetBoolean();
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static string[] Active(IReadOnlyList<ServicingProcessGroup> groups, params string[] names) =>
        [.. names.Where(name => groups.Any(group => group.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && group.Count > 0))];

    private static WorkflowCheck Result(CheckState state, string detail) => new("Live servicing state", state,
        detail + " This is a point-in-time signal summary, not a progress percentage, completion claim, stall diagnosis or root-cause finding. No action was requested.");
}
