using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace KillerBench.Engine.Automation;

public sealed record MaceCredentialEvidence(int OneDriveReferences, int OutlookReferences,
    int OfficeReferences, int ProjectReferences, int TeamsReferences, bool Complete, string Limitation)
{
    public int RelevantCount(MaceTargets targets) =>
        (targets.HasFlag(MaceTargets.OneDrive) ? OneDriveReferences : 0) +
        (targets.HasFlag(MaceTargets.Outlook) ? OutlookReferences : 0) +
        (targets.HasFlag(MaceTargets.Office) ? OfficeReferences : 0) +
        (targets.HasFlag(MaceTargets.Project) ? ProjectReferences : 0) +
        (targets.HasFlag(MaceTargets.Teams) ? TeamsReferences : 0);
}

public static partial class MaceCredentialReview
{
    private const int MaximumCredentials = 4096;
    private const int MaximumMatchesPerTarget = 128;
    private const int ErrorNotFound = 1168;

    public static MaceCredentialEvidence Read(MaceTargets targets, MaceScope scope,
        CancellationToken token = default)
    {
        new MaceReviewOptions(targets, scope).Validate();
        token.ThrowIfCancellationRequested();
        if (scope != MaceScope.RunningAccount)
            return Incomplete("All-user credential review is unavailable because KillerBench does not load or impersonate other account contexts.");
        if (!OperatingSystem.IsWindows())
            return Incomplete("Windows Credential Manager is unavailable on this platform.");

        try
        {
            return ReadNative(targets, token);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return Incomplete("Credential reference inventory failed: " + error.GetType().Name + ".");
        }
    }

    public static MaceCredentialEvidence Analyze(MaceTargets targets, IEnumerable<string> targetNames,
        bool complete = true, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(targetNames);
        new MaceReviewOptions(targets).Validate();
        token.ThrowIfCancellationRequested();
        int observed = 0;
        var counts = new Dictionary<MaceTargets, int>();
        foreach (string targetName in targetNames)
        {
            token.ThrowIfCancellationRequested();
            observed++;
            if (observed > MaximumCredentials || targetName is null || targetName.Length > 2048 ||
                targetName.IndexOfAny(['\r', '\n', '\0']) >= 0)
                return Incomplete("Credential reference evidence exceeded its privacy or size boundary.");
            MaceTargets match = Match(targetName) & targets;
            foreach (MaceTargets target in Enum.GetValues<MaceTargets>())
            {
                if (target == MaceTargets.None || !match.HasFlag(target)) continue;
                int next = counts.GetValueOrDefault(target) + 1;
                if (next > MaximumMatchesPerTarget)
                    return Incomplete("A selected product exceeded the bounded credential reference count.");
                counts[target] = next;
            }
        }
        if (!complete) return Incomplete("Credential reference inventory was incomplete.");
        int Count(MaceTargets target) => counts.GetValueOrDefault(target);
        return new(Count(MaceTargets.OneDrive), Count(MaceTargets.Outlook), Count(MaceTargets.Office),
            Count(MaceTargets.Project), Count(MaceTargets.Teams), true,
            "Only bounded product-level counts were retained. Credential targets, usernames, persistence types, attributes and secret material were excluded.");
    }

    [SupportedOSPlatform("windows")]
    private static MaceCredentialEvidence ReadNative(MaceTargets targets, CancellationToken token)
    {
        if (!CredEnumerate(null, 0, out int count, out IntPtr credentials))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound) return Analyze(targets, [], token: token);
            throw new Win32Exception(error);
        }
        try
        {
            if (count < 0 || count > MaximumCredentials)
                return Incomplete("Credential reference evidence exceeded its record boundary.");
            var names = new List<string>(count);
            for (int index = 0; index < count; index++)
            {
                token.ThrowIfCancellationRequested();
                IntPtr item = Marshal.ReadIntPtr(credentials, index * IntPtr.Size);
                var credential = Marshal.PtrToStructure<NativeCredential>(item);
                names.Add(Marshal.PtrToStringUni(credential.TargetName) ?? string.Empty);
            }
            return Analyze(targets, names, token: token);
        }
        finally
        {
            CredFree(credentials);
        }
    }

    private static MaceTargets Match(string value)
    {
        string normalized = value.ToUpperInvariant();
        MaceTargets result = MaceTargets.None;
        if (normalized.Contains("ONEDRIVE", StringComparison.Ordinal)) result |= MaceTargets.OneDrive;
        if (normalized.Contains("OUTLOOK", StringComparison.Ordinal)) result |= MaceTargets.Outlook;
        if (normalized.Contains("OFFICE", StringComparison.Ordinal) || normalized.Contains("MICROSOFT365", StringComparison.Ordinal)) result |= MaceTargets.Office;
        if (normalized.Contains("PROJECT", StringComparison.Ordinal)) result |= MaceTargets.Project;
        if (normalized.Contains("TEAMS", StringComparison.Ordinal)) result |= MaceTargets.Teams;
        return result;
    }

    private static MaceCredentialEvidence Incomplete(string reason) =>
        new(0, 0, 0, 0, 0, false, reason + " No credential identity or secret material was retained.");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [LibraryImport("advapi32.dll", EntryPoint = "CredEnumerateW", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredEnumerate(string? filter, uint flags, out int count, out IntPtr credentials);

    [LibraryImport("advapi32.dll")]
    private static partial void CredFree(IntPtr buffer);
}
