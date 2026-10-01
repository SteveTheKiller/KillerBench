using System.Text.RegularExpressions;

namespace KillerBench.Engine.Backup;

public static class VssWriters
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("VSS writer inspection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(new("vssadmin.exe", ["list", "writers"], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false), token);
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "VSS writers";
        WorkflowCheck Unknown() => new(name, CheckState.Unknown,
            "Writer status is unavailable, incomplete or uses an unsupported output format. English vssadmin fields are required for interpretation. Raw output is retained; no writer recovery selected.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 1_000_000)
            return Unknown();
        var lines = evidence.Output.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length != 0).ToArray();
        int index = 0;
        while (index < lines.Length && (lines[index].StartsWith("vssadmin ", StringComparison.Ordinal) || lines[index].StartsWith("(C) Copyright", StringComparison.Ordinal))) index++;
        var rows = new List<string>();
        var identities = new HashSet<Guid>();
        bool attention = false;
        while (index < lines.Length)
        {
            token.ThrowIfCancellationRequested();
            if (rows.Count >= 256 || lines.Length - index < 5) return Unknown();
            string title = lines[index++];
            if (!title.StartsWith("Writer name: '", StringComparison.Ordinal) || !title.EndsWith('\'')) return Unknown();
            string writerName = title[14..^1];
            if (string.IsNullOrWhiteSpace(writerName) || writerName.Length > 512 || writerName.Any(char.IsControl)) return Unknown();
            string writer = lines[index++], instance = lines[index++], state = lines[index++], error = lines[index++];
            if (!writer.StartsWith("Writer Id: ", StringComparison.Ordinal) || !Guid.TryParse(writer[11..], out var writerId) || writerId == Guid.Empty ||
                !instance.StartsWith("Writer Instance Id: ", StringComparison.Ordinal) || !Guid.TryParse(instance[20..], out var instanceId) || instanceId == Guid.Empty || !identities.Add(instanceId)) return Unknown();
            var match = Regex.Match(state, @"^State: \[([0-9]{1,2})\] (.+)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int code) || code is < 1 or > 15 ||
                !error.StartsWith("Last error: ", StringComparison.Ordinal) || error.Length <= 12) return Unknown();
            string reportedError = error[12..];
            attention |= code != 1 || reportedError != "No error";
            string category = code == 1 ? "Stable" : code <= 5 ? "Waiting for backup operation" : "Writer reports failure";
            rows.Add(writerName + " | " + writerId + " | instance " + instanceId + " | " + category + " [" + code + "] | " + reportedError);
        }
        if (rows.Count == 0) return Unknown();
        return new(name, attention ? CheckState.Attention : CheckState.Information,
            rows.Count + " writers reported.\n" + string.Join("\n", rows.Take(8)) +
            (rows.Count > 8 ? "\nAdditional writer records remain in raw output." : "") +
            "\nThis is a point-in-time writer observation, not verification of a backup or restore. Waiting states can be part of an active backup. Review failed writer identities with the backup/application owner; no services, writers or snapshots are reset.", Evidence: evidence);
    }
}
