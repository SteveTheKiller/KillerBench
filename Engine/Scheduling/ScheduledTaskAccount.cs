using System.Text;
using System.Text.Json;

namespace KillerBench.Engine.Scheduling;

public sealed record ScheduledTaskAccountResult(string RequestedAccount, string? Sid, string Detail, ToolResult Evidence);

public static class ScheduledTaskAccount
{
    public static async Task<ScheduledTaskAccountResult> ResolveAsync(ScheduledTaskPrincipal principal, CancellationToken token = default)
    {
        principal.Validate();
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(principal.UserId));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$account=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encoded + "')); " +
            "if($account -match '^S-1-'){ $sid=[Security.Principal.SecurityIdentifier]::new($account); " +
            "$null=$sid.Translate([Security.Principal.NTAccount]) } else { " +
            "$sid=([Security.Principal.NTAccount]::new($account)).Translate([Security.Principal.SecurityIdentifier]) }; " +
            "[pscustomobject]@{account=$account; sid=$sid.Value}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        return Interpret(principal, result);
    }

    public static ScheduledTaskAccountResult Interpret(ScheduledTaskPrincipal principal, ToolResult evidence)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows account review requires Windows.");
        principal.Validate();
        const string unavailable = "Account identity unresolved. No logon or execution permission established.";
        if (evidence.ExitCode != 0 || evidence.Truncated || evidence.TimedOut || !string.IsNullOrWhiteSpace(evidence.Error))
            return new(principal.UserId, null, unavailable, evidence);
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            if (root.EnumerateObject().Count() != 2 || root.GetProperty("account").GetString() != principal.UserId)
                return new(principal.UserId, null, unavailable, evidence);
            string? value = root.GetProperty("sid").GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 184) return new(principal.UserId, null, unavailable, evidence);
            var sid = new System.Security.Principal.SecurityIdentifier(value);
            if (principal.UserId.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase) &&
                !sid.Equals(new System.Security.Principal.SecurityIdentifier(principal.UserId)))
                return new(principal.UserId, null, "Resolved SID differs from the requested identity. Review cannot proceed.", evidence);
            if (principal.Kind == ScheduledTaskAccountKind.InteractiveUser && sid.Value == "S-1-5-18")
                return new(principal.UserId, null, "The account resolves to Local System. Select Local System and its privileges explicitly.", evidence);
            return new(principal.UserId, sid.Value, "Name/SID translation only. Account type, enabled state, logon rights and executable access are not verified.", evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        { return new(principal.UserId, null, unavailable, evidence); }
    }
}
