using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using KillerBench.Services;

/// <summary>
/// The release certificate's SimpleName, read from an installed signed release. The updater has to hold every
/// signer check to this exact name, not to a fragment of it. The update types are internal to the app, so they
/// are reached by reflection, the same way the version rule is in ReleaseUpdateChecks.
/// </summary>
internal static class UpdateTrustRegressionChecks
{
    private const string ReleasePublisher = "Open Source Developer Stephen Riley";
    private static readonly Assembly App = typeof(SelfUpdate).Assembly;
    private const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run()
    {
        ElevatedHelperHoldsTheInstalledCopyToTheFullPublisherName();
        PublisherChecksUseTheFullName();
        UninstallTakesTheInstallerPublicationLeftovers();
        OnlyAnOfflineRevocationServerEarnsTheCacheOnlyRetry();
        EveryInstallLeaseIsScopedToItsFolder();
        PerUserHelperRecordsTheInstalledCopyBeforeCheckingTheDownload();
        Console.WriteLine("PASS: the update trust checks hold the full publisher name and the install leases are per folder.");
    }

    private static Type AppType(string name) =>
        App.GetType("KillerBench.Services." + name) ?? throw new Exception(name + " exists");

    private static object? Call(string type, string method, params object?[] args)
    {
        MethodInfo target = AppType(type).GetMethods(Statics)
            .Where(m => m.Name == method && m.GetParameters().Length >= args.Length)
            .OrderBy(m => m.GetParameters().Length).First();
        var all = new object?[target.GetParameters().Length];
        args.CopyTo(all, 0);
        try { return target.Invoke(null, all); }
        catch (TargetInvocationException e) when (e.InnerException != null) { throw e.InnerException; }
    }

    private static string Scratch(string name) => Path.Combine(Path.GetTempPath(), "KillerBench.Checks", name + "-" + Guid.NewGuid().ToString("N"));

    private static void ElevatedHelperHoldsTheInstalledCopyToTheFullPublisherName()
    {
        string staged = Scratch("elevated-signer") + ".exe";
        Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
        Call("UpdateImagePublication", "RememberVerified", staged, new string('A', 64), new string('B', 40));
        try
        {
            var start = (System.Diagnostics.ProcessStartInfo)Call("TrustedUpdate", "Publication", staged,
                @"C:\Program Files\KillerBench\KillerBench.exe", "9.0.0", new string('C', 64), 1, 0L, 2, 0L, new string('D', 40))!;
            string body = Unwrap(start.Arguments);

            Require(PlanValue(body, "Signer") == ReleasePublisher, "The elevated plan carries the full publisher name");
            // Start-KillerUpdatedApp (target) 'reopen' $true (original signer) (publisher) ...
            int relaunch = body.IndexOf("Start-KillerUpdatedApp (", StringComparison.Ordinal);
            Require(relaunch > 0, "The elevated helper relaunches through the guard");
            var data = Regex.Matches(body.Substring(relaunch), @"Unicode\.GetString\(\[Convert\]::FromBase64String\('([A-Za-z0-9+/=]*)'\)\)");
            Require(data.Count >= 3, "The relaunch passes its target, signer and publisher");
            Require(Encoding.Unicode.GetString(Convert.FromBase64String(data[2].Groups[1].Value)) == ReleasePublisher,
                "The relaunch holds the installed copy to the full publisher name");
        }
        finally { if (File.Exists(staged + ".recovery.txt")) File.Delete(staged + ".recovery.txt"); }
    }

    private static void PublisherChecksUseTheFullName()
    {
        var incoming = new AssemblyName("KillerBench") { Version = new Version(9, 0, 0, 0) };
        object?[] Verify((bool, string) installed, (bool, string) signed, bool requireInstalled, string? thumb = null) =>
            [incoming, "9.0.0", new Version(1, 0, 0), installed, signed, requireInstalled, thumb];

        try { Call("ReleaseIdentity", "Verify", Verify((false, "(not signed)"), (true, "Stephen Rileyson"), false)); }
        catch (SecurityException e) { Require(e.Message == "The update is not signed by the publisher.", "A name that only contains the publisher is refused: " + e.Message); goto accepted; }
        throw new Exception("A name that only contains the publisher is refused");
    accepted:
        Call("ReleaseIdentity", "Verify", Verify((true, ReleasePublisher), (true, ReleasePublisher), true));

        // An unsigned local build cannot vouch for a publisher, so it takes only the pinned release certificate.
        string pinned = (string)AppType("ReleaseIdentity").GetField("DevelopmentUpdateSigner", Statics)!.GetValue(null)!;
        Call("ReleaseIdentity", "Verify", Verify((false, "(none)"), (true, ReleasePublisher), false, pinned));
        try { Call("ReleaseIdentity", "Verify", Verify((false, "(none)"), (true, ReleasePublisher), false, new string('0', 40))); }
        catch (SecurityException) { return; }
        throw new Exception("An unsigned build accepted a release signed by another certificate");
    }

    private static void UninstallTakesTheInstallerPublicationLeftovers()
    {
        foreach (string marker in new[] { ".install-", ".previous-", ".failed-" })
        {
            string folder = Scratch("leftovers");
            Directory.CreateDirectory(folder);
            try
            {
                string name = "KillerBench.exe" + marker + "0123456789abcdef0123456789abcdef";
                File.WriteAllText(Path.Combine(folder, name), "old image");
                File.WriteAllText(Path.Combine(folder, "KillerBench.exe" + marker + "notaguid"), "unrelated");
                var found = (string[])Call("ExecutableDeployment", "UninstallLeftovers", folder, "KillerBench.exe")!;
                Require(found.Length == 1 && found[0] == name, "Uninstall takes the " + marker + " leftover and nothing else");
            }
            finally { Directory.Delete(folder, true); }
        }
    }

    private static void OnlyAnOfflineRevocationServerEarnsTheCacheOnlyRetry()
    {
        foreach (var (first, retry, trusted) in new[]
        {
            (0u, 0u, true), (0x80092013u, 0u, true), (0x80092013u, 0x800B0101u, false),
            (0x800B010Cu, 0u, false), (0x80096010u, 0u, false),
        })
        {
            var calls = new List<(uint revocation, uint flags)>();
            Func<uint, uint, uint> native = (revocation, flags) => { calls.Add((revocation, flags)); return calls.Count == 1 ? first : retry; };
            bool result = (bool)Call("CodeSignature", "VerifyTrust", native)!;
            Require(result == trusted, $"Trust result for {first:X} then {retry:X}");
            // WTD_REVOKE_WHOLECHAIN first; the retry is WTD_REVOKE_NONE with cache-only retrieval.
            Require(calls[0] == (1u, 0x80u), "The first check asks for whole-chain revocation");
            Require(calls.Count == (first == 0x80092013u ? 2 : 1), "Only an offline revocation server retries");
            if (calls.Count == 2) Require(calls[1] == (0u, 0x1010u), "The retry is cache-only");
        }
    }

    private static void EveryInstallLeaseIsScopedToItsFolder()
    {
        // The update helpers lock Global\KillerTools-Install-KillerBench-<folder hash>. An install that takes
        // the bare product lock would not exclude them.
        string? root = AppContext.BaseDirectory;
        while (root != null && !File.Exists(Path.Combine(root, "KillerBench.csproj"))) root = Path.GetDirectoryName(root);
        Require(root != null, "The repository root is found");
        foreach (string file in Directory.GetFiles(root!, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root!, file);
            if (relative.StartsWith("obj" + Path.DirectorySeparatorChar) || relative.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || relative.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                || relative.StartsWith("Checks" + Path.DirectorySeparatorChar) || file.EndsWith("InstallerExitCodes.cs", StringComparison.Ordinal)) continue;
            foreach (Match call in Regex.Matches(File.ReadAllText(file), @"InstallerExitCodes\.Acquire\(([^()]*(\([^()]*\))?[^()]*)\)"))
                Require(call.Groups[1].Value.Contains(','), relative + " takes an unscoped install lease: " + call.Value);
        }
    }

    private static void PerUserHelperRecordsTheInstalledCopyBeforeCheckingTheDownload()
    {
        // The app has already closed when the helper starts. A download that fails its check must still find
        // the installed copy's identity recorded, or recovery refuses to reopen it.
        string path = Scratch("capture-order") + ".exe";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Call("UpdateImagePublication", "RememberVerified", path, new string('A', 64), new string('B', 40));
        try
        {
            MethodInfo script = AppType("UpdateImagePublication").GetMethod("Script", Statics)!;
            var arguments = script.GetParameters().Select(p => p.Name switch
            {
                "download" => path,
                "target" => @"C:\Apps\KillerBench.exe",
                "elevated" => (object)false,
                "releasesUrl" => "https://example.invalid/releases",
                "displayName" => "KillerBench",
                "originalHash" => new string('C', 64),
                _ => p.HasDefaultValue ? p.DefaultValue : null,
            }).ToArray();
            string text = (string)script.Invoke(null, arguments)!;
            int function = text.IndexOf("function Invoke-UpdateImagePublication", StringComparison.Ordinal);
            int capture = text.IndexOf("& $ops.CaptureOriginal $plan", function, StringComparison.Ordinal);
            int downloadCheck = text.IndexOf("$plan.Download", function, StringComparison.Ordinal);
            Require(function > 0 && capture > 0 && downloadCheck > 0, "The publication function captures and checks");
            Require(capture < downloadCheck, "The installed copy's identity is captured before the download is opened");
        }
        finally { if (File.Exists(path + ".recovery.txt")) File.Delete(path + ".recovery.txt"); }
    }

    // Reverses ImmutablePowerShell.Arguments: the script travels gzip-compressed inside Base64.
    private static string Unwrap(string arguments)
    {
        var payload = Regex.Match(arguments, @"FromBase64String\('([A-Za-z0-9+/=]+)'\)");
        Require(payload.Success, "The helper carries its script as Base64");
        using var gzip = new GZipStream(new MemoryStream(Convert.FromBase64String(payload.Groups[1].Value)), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    // Plan values are written as Base64 so no quoting can break out of them.
    private static string PlanValue(string script, string name)
    {
        var value = Regex.Match(script, "(?m)^" + Regex.Escape(name) + @" = \[Text\.Encoding\]::UTF8\.GetString\(\[Convert\]::FromBase64String\('([A-Za-z0-9+/=]*)'\)\)");
        Require(value.Success, "The plan sets " + name);
        return Encoding.UTF8.GetString(Convert.FromBase64String(value.Groups[1].Value));
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
