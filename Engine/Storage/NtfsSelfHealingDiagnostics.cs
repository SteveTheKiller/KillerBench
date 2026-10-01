using System.Globalization;
using System.Text.RegularExpressions;

namespace KillerBench.Engine.Storage;

public static class NtfsSelfHealingDiagnostics
{
    private static readonly Regex StatePattern = new(
        @"^Self healing state on ([A-Za-z]:) is: 0x([0-9A-Fa-f]{1,8})$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static async Task<WorkflowCheck> CheckAsync(string volume, CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("NTFS self-healing inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(15), token: cancellationToken);
            var pending = execute(new("fsutil.exe", ["repair", "query", volume], true), deadline.Token)
                ?? throw new InvalidOperationException("NTFS self-healing provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("NTFS self-healing provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(volume, evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown(volume, "The self-healing query exceeded its overall deadline.",
                new(-1, "", "NTFS self-healing query deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown(volume, "The self-healing state could not be collected.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(string volume, ToolResult evidence, CancellationToken token = default)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 65_536)
            return Unknown(volume, "The self-healing query failed or returned incomplete evidence.", evidence);

        var lines = evidence.Output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = lines.Select(line => StatePattern.Match(line)).Where(match => match.Success).ToArray();
        if (matches.Length != 1 || !matches[0].Groups[1].Value.Equals(volume, StringComparison.OrdinalIgnoreCase) ||
            !uint.TryParse(matches[0].Groups[2].Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint state))
            return Unknown(volume, "The self-healing response was not recognized or did not match the selected volume.", evidence);

        return state switch
        {
            0x01 => Result(CheckState.Clear, "General online NTFS repair is enabled.", state),
            0x09 => Result(CheckState.Clear, "Online NTFS repair is enabled with warnings for potential data loss.", state),
            0x00 => Result(CheckState.Attention, "Online NTFS self-healing is disabled. Review the setting and its management source before changing it.", state),
            _ => Unknown(volume, $"The provider reported unsupported self-healing flags 0x{state:X}.", evidence)
        };

        WorkflowCheck Result(CheckState checkState, string detail, uint flags) => new(
            "NTFS self-healing: " + volume, checkState,
            detail + $" Reported flags: 0x{flags:X}. This is configuration evidence, not proof of filesystem health or successful repair. No repair was initiated and no setting changed.",
            Evidence: evidence);
    }

    private static WorkflowCheck Unknown(string volume, string detail, ToolResult evidence) => new(
        "NTFS self-healing: " + volume, CheckState.Unknown,
        detail + " Filesystem health remains unverified. No repair was initiated and no setting changed.", Evidence: evidence);
}
