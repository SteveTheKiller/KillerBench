using System.Text.Json;

namespace KillerBench.Engine;

public sealed record BitsJobSnapshot(Guid Id, string Name, string State, string Error,
    string? Owner, int? ErrorCode, ulong? BytesTotal, ulong? BytesTransferred);
public sealed record BitsInventoryResult(string? Scope, bool Complete, IReadOnlyList<BitsJobSnapshot> Jobs, ToolResult Evidence);

public static class BitsDiagnostics
{
    public static async Task<WorkflowCheck> CheckAsync(bool allUsers = false, CancellationToken token = default) =>
        Interpret((await ReadAsync(allUsers, token).ConfigureAwait(false)).Evidence, token);

    public static async Task<BitsInventoryResult> ReadAsync(bool allUsers = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("BITS inspection requires Windows.");
        string script = "$ErrorActionPreference='Stop'; Import-Module BitsTransfer -ErrorAction Stop; " +
            "$jobs=@(Get-BitsTransfer " + (allUsers ? "-AllUsers " : "") + "-ErrorAction Stop | Select-Object -First 257); " +
            "$rows=@($jobs | Select-Object -First 256 | ForEach-Object { [pscustomobject]@{id=[string]$_.JobId;name=[string]$_.DisplayName;state=[string]$_.JobState;error=[string]$_.ErrorDescription;owner=[string]$_.OwnerAccount;errorCode=[int]$_.InternalErrorCode;bytesTotal=[string]$_.BytesTotal;bytesTransferred=[string]$_.BytesTransferred} }); " +
            "[pscustomobject]@{scope='" + (allUsers ? "AllUsers" : "CurrentAccount") + "';complete=($jobs.Count -le 256);jobs=$rows}|ConvertTo-Json -Depth 4 -Compress";
        try
        {
            return Parse(await BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false), token);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { return new(allUsers ? "AllUsers" : "CurrentAccount", false, [], new(-1, "", error.Message, false)); }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        var inventory = Parse(evidence, token);
        if (inventory.Scope is null) return new("BITS transfer jobs", CheckState.Unknown, "Transfer-job coverage is unavailable or incomplete. Jobs are preserved; no recovery selected from this snapshot.", Evidence: evidence);
        int failed = inventory.Jobs.Count(job => job.State is "Error" or "TransientError");
        var rows = inventory.Jobs.Take(8).Select(job => job.Id + " | " + job.Name.Replace('\r', ' ').Replace('\n', ' ') + " | " + job.State +
            (job.Owner is null ? " | owner/progress not included in saved evidence" : " | owner " + job.Owner + " | error 0x" + unchecked((uint)job.ErrorCode!.Value).ToString("X8", System.Globalization.CultureInfo.InvariantCulture) +
                " | " + job.BytesTransferred + "/" + (job.BytesTotal?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown") + " bytes") +
            (job.Error.Length == 0 ? "" : " | " + job.Error.Replace('\r', ' ').Replace('\n', ' ')));
        return new("BITS transfer jobs", !inventory.Complete ? CheckState.Unknown : failed > 0 ? CheckState.Attention : CheckState.Information,
            $"Scope: {inventory.Scope}. {inventory.Jobs.Count} retained jobs; {failed} report Error or TransientError.\n" + string.Join("\n", rows) +
            (inventory.Jobs.Count > 8 ? "\nAdditional jobs remain in raw evidence." : "") +
            (!inventory.Complete ? "\nJob limit reached; coverage is incomplete." : "") +
            "\nRunning-account scope excludes other owners. Queue names do not establish Windows Update ownership. Suspended jobs may be intentional; queued/connecting jobs are not proven stuck from one observation. Review errors with the owning application. No jobs are resumed, completed, canceled, deleted or reassigned.", Evidence: evidence);
    }

    public static BitsInventoryResult Parse(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        BitsInventoryResult Unknown() => new(null, false, [], evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Unique(root)) return Unknown();
            string? scope = root.GetProperty("scope").GetString();
            if (scope is not ("AllUsers" or "CurrentAccount")) return Unknown();
            bool complete = root.GetProperty("complete").GetBoolean();
            var jobs = root.GetProperty("jobs");
            if (jobs.ValueKind != JsonValueKind.Array || jobs.GetArrayLength() > 256) return Unknown();
            var identities = new HashSet<Guid>();
            var rows = new List<BitsJobSnapshot>();
            foreach (var job in jobs.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(job) || !Guid.TryParse(job.GetProperty("id").GetString(), out var id) || id == Guid.Empty || !identities.Add(id)) return Unknown();
                string? name = job.GetProperty("name").GetString(), state = job.GetProperty("state").GetString(), error = job.GetProperty("error").GetString();
                if (name is null || name.Length > 1024 || error is null || error.Length > 8192) return Unknown();
                if (state is not ("Queued" or "Connecting" or "Transferring" or "Suspended" or "Error" or "TransientError" or "Transferred" or "Acknowledged" or "Cancelled")) return Unknown();
                string? owner = null;
                int? savedError = null;
                ulong? savedTotal = null, savedTransferred = null;
                if (new[] { "owner", "errorCode", "bytesTotal", "bytesTransferred" }.Any(field => job.TryGetProperty(field, out _)))
                {
                    owner = job.GetProperty("owner").GetString();
                    if (string.IsNullOrWhiteSpace(owner) || owner.Length > 1024 || owner.Any(char.IsControl) ||
                        !job.GetProperty("errorCode").TryGetInt32(out int errorCode) ||
                        !ulong.TryParse(job.GetProperty("bytesTotal").GetString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ulong total) ||
                        !ulong.TryParse(job.GetProperty("bytesTransferred").GetString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out ulong transferred) ||
                        (total != ulong.MaxValue && transferred > total)) return Unknown();
                    savedError = errorCode;
                    savedTotal = total == ulong.MaxValue ? null : total;
                    savedTransferred = transferred;
                }
                rows.Add(new(id, name, state, error, owner, savedError, savedTotal, savedTransferred));
            }
            return new(scope, complete, rows.AsReadOnly(), evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return Unknown(); }
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
}
