using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public static class DeleteNotificationDiagnostics
{
    private static readonly Regex StatePattern = new(
        @"^(NTFS|ReFS) DisableDeleteNotify = ([01])(?:\s+\([^\r\n]{1,200}\))?$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Delete-notification inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(15), token: cancellationToken);
            var pending = execute(new("fsutil.exe", ["behavior", "query", "DisableDeleteNotify"], true), deadline.Token)
                ?? throw new InvalidOperationException("Delete-notification provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Delete-notification provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return [Unknown("The delete-notification query exceeded its overall deadline.",
                new(-1, "", "Delete-notification query deadline expired.", false) { TimedOut = true })];
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return [Unknown("Delete-notification state could not be collected.", new(-1, "", error.Message, false))];
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 65_536)
            return [Unknown("The delete-notification query failed or returned incomplete evidence.", evidence)];

        var matches = evidence.Output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => StatePattern.Match(line)).Where(match => match.Success).ToArray();
        if (matches.Length is < 1 or > 2 || matches.Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != matches.Length)
            return [Unknown("The delete-notification response was not recognized or contained duplicate filesystem evidence.", evidence)];

        return matches.OrderBy(match => match.Groups[1].Value, StringComparer.Ordinal).Select(match =>
        {
            string fileSystem = match.Groups[1].Value;
            bool enabled = match.Groups[2].Value == "0";
            return new WorkflowCheck(fileSystem + " delete notifications",
                enabled ? CheckState.Information : CheckState.Attention,
                enabled
                    ? $"{fileSystem} delete notifications are enabled. This permits supported storage devices to receive freed-block notifications, but does not prove that a device accepts or acts on them. No optimization ran and no setting changed."
                    : $"{fileSystem} delete notifications are disabled. Review device support, workload requirements and the setting's management source before changing it. No optimization ran and no setting changed.",
                Evidence: evidence);
        }).ToArray();
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new(
        "Filesystem delete notifications", CheckState.Unknown,
        detail + " TRIM or unmap availability remains unverified. No optimization ran and no setting changed.", Evidence: evidence);
}
