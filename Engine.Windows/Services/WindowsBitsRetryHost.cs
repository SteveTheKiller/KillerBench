using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed class WindowsBitsRetryHost : IBitsRetryHost
{
    public string OwnerAccount
    {
        get
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("BITS retry requires Windows.");
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.Name;
        }
    }

    public Task<BitsInventoryResult> ReadAsync(CancellationToken token) => BitsDiagnostics.ReadAsync(false, token);

    public Task<ToolResult> RequestRetryAsync(BitsJobSnapshot expected, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return BoundedTool.RunAsync(CreateStep(expected, OwnerAccount), TimeSpan.FromSeconds(20), token: token);
    }

    public static RepairStep CreateStep(BitsJobSnapshot expected, string owner)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (expected.Id == Guid.Empty || string.IsNullOrWhiteSpace(owner) || expected.Owner != owner ||
            expected.State is not ("Error" or "TransientError") || expected.ErrorCode is null || expected.BytesTransferred is null)
            throw new ArgumentException("An exact errored job owned by the running account is required.", nameof(expected));
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { id = expected.Id, name = expected.Name, owner, total = (expected.BytesTotal ?? ulong.MaxValue).ToString(System.Globalization.CultureInfo.InvariantCulture) })));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; Import-Module BitsTransfer -ErrorAction Stop; " +
            "$e=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + payload + "'))|ConvertFrom-Json; " +
            "$identity=[Security.Principal.WindowsIdentity]::GetCurrent(); try { $owner=$identity.Name } finally { $identity.Dispose() }; " +
            "if($owner -cne $e.owner){throw 'Running identity changed'}; " +
            "$jobs=@(Get-BitsTransfer -JobId ([guid]$e.id) -ErrorAction Stop); if($jobs.Count -ne 1){throw 'Exact job unavailable'}; $j=$jobs[0]; " +
            "if([string]$j.JobId -ne $e.id -or [string]$j.OwnerAccount -cne $e.owner -or [string]$j.DisplayName -cne $e.name -or " +
            "[string]$j.BytesTotal -ne $e.total -or [string]$j.JobState -cnotin @('Error','TransientError')){throw 'Job changed or is no longer eligible'}; " +
            "Resume-BitsTransfer -BitsJob $j -Asynchronous -ErrorAction Stop | Out-Null; 'Retry requested; transfer completion unverified'";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], false);
    }
}
