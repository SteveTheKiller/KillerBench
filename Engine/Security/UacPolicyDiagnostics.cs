using Microsoft.Win32;

namespace KillerBench.Engine.Security;

public sealed record UacPolicySnapshot(int? EnableLua, int? AdministratorPrompt,
    int? StandardUserPrompt, int? SecureDesktop, bool Complete, string Detail);

public static class UacPolicyDiagnostics
{
    private const string Scope = " Machine UAC configuration only. Registry presence does not identify whether local policy, domain policy or MDM owns the effective value. Tokens, credentials, accounts, consent history and application manifests are not inspected. No elevation or policy setting was changed.";

    public static IReadOnlyList<WorkflowCheck> Check()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("UAC policy collection requires Windows.");
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key is null) return Interpret(new(null, null, null, null, false, "The UAC configuration key is unavailable."));
            return Interpret(new(Read(key, "EnableLUA"), Read(key, "ConsentPromptBehaviorAdmin"),
                Read(key, "ConsentPromptBehaviorUser"), Read(key, "PromptOnSecureDesktop"), true,
                "Read the four exact machine UAC configuration values."));
        }
        catch (Exception error) when (error is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return Interpret(new(null, null, null, null, false, "UAC configuration could not be read: " + error.Message));
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(UacPolicySnapshot? snapshot)
    {
        static WorkflowCheck Unknown(string name, string detail) => new(name, CheckState.Unknown, detail + Scope);
        if (snapshot is null || !snapshot.Complete)
            return [Unknown("User Account Control", (snapshot?.Detail ?? "UAC evidence is unavailable.") + " ")];
        if (!Valid(snapshot.EnableLua, 0, 1) || !Valid(snapshot.AdministratorPrompt, 0, 5) ||
            snapshot.StandardUserPrompt is not (0 or 1 or 3) || !Valid(snapshot.SecureDesktop, 0, 1))
            return [Unknown("User Account Control", "One or more UAC values are missing or unrecognized. ")];

        var checks = new List<WorkflowCheck>(3);
        bool enabled = snapshot.EnableLua == 1;
        checks.Add(new("User Account Control", enabled ? CheckState.Clear : CheckState.Attention,
            enabled ? "UAC is enabled." + Scope :
            "UAC is disabled. Administrative applications can run without the normal elevation boundary, and changing this value requires separate policy review and a restart." + Scope));

        string admin = snapshot.AdministratorPrompt switch
        {
            0 => "elevate without prompting",
            1 => "request credentials on the secure desktop",
            2 => "request consent on the secure desktop",
            3 => "request credentials on the interactive desktop",
            4 => "request consent on the interactive desktop",
            _ => "request consent for non-Windows binaries"
        };
        bool adminAttention = snapshot.AdministratorPrompt == 0 || snapshot.SecureDesktop == 0;
        checks.Add(new("Administrator elevation prompts", adminAttention ? CheckState.Attention : CheckState.Information,
            "Configured behavior: " + admin + ". Prompt desktop: " + (snapshot.SecureDesktop == 1 ? "secure desktop" : "interactive desktop") + ". " +
            (snapshot.AdministratorPrompt == 0 ? "Administrator approval mode can elevate without an interactive prompt. " : "") +
            (snapshot.SecureDesktop == 0 ? "Elevation prompts are not isolated on the secure desktop. " : "") +
            "Review intended effective policy before changing this behavior." + Scope));

        string standard = snapshot.StandardUserPrompt switch
        {
            0 => "automatically deny elevation requests",
            1 => "request credentials on the secure desktop",
            _ => "request credentials on the interactive desktop"
        };
        bool userAttention = snapshot.StandardUserPrompt == 3 || snapshot.StandardUserPrompt == 1 && snapshot.SecureDesktop == 0;
        checks.Add(new("Standard-user elevation prompts", userAttention ? CheckState.Attention : CheckState.Information,
            "Configured behavior: " + standard + ". Prompt desktop: " + (snapshot.SecureDesktop == 1 ? "secure desktop" : "interactive desktop") + ". " +
            (userAttention ? "Credential entry outside the secure desktop warrants effective-policy review." : "") + Scope));
        return checks.AsReadOnly();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static int? Read(RegistryKey key, string name) => key.GetValue(name) switch
    {
        int value => value,
        _ => null
    };

    private static bool Valid(int? value, int minimum, int maximum) => value is int number && number >= minimum && number <= maximum;
}
