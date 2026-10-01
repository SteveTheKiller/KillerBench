using System.Diagnostics;
using System.Text.Json;

namespace KillerBench.Engine.Updates;

public sealed record UpdateExecutionContextSnapshot(bool Complete, bool Administrator,
    bool UserInteractive, int SessionId, string? CollectionIssue = null);

public static class UpdateExecutionContextDiagnostics
{
    public static WorkflowCheck Check()
    {
        if (!OperatingSystem.IsWindows())
            return Assess(new(false, false, false, -1, "Windows execution context is unavailable on this platform."));
        try
        {
            using var process = Process.GetCurrentProcess();
            return Assess(new(true, WindowsState.IsAdministrator(), Environment.UserInteractive, process.SessionId));
        }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            return Assess(new(false, false, false, -1, error.GetType().Name));
        }
    }

    public static WorkflowCheck Assess(UpdateExecutionContextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool complete = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) && snapshot.SessionId >= 0;
        CheckState state = !complete ? CheckState.Unknown : snapshot.Administrator && snapshot.UserInteractive && snapshot.SessionId != 0
            ? CheckState.Information : CheckState.Attention;
        string detail = !complete
            ? "The current execution context could not be established. Recollect before attributing an update failure to identity, elevation or session state."
            : $"Current process: {(snapshot.Administrator ? "administrator" : "standard user")}; " +
              $"{(snapshot.UserInteractive && snapshot.SessionId != 0 ? "interactive desktop session" : "noninteractive or session-zero context")}; session {snapshot.SessionId}. " +
              "This describes the current KillerBench process only. It does not prove which identity ran a historical update operation.";
        detail += " No token, credential, group membership or account identifier was retained, and no session or permission changed.";
        return new("Update execution context", state, detail,
            Evidence: new(complete ? 0 : -1, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
    }
}
