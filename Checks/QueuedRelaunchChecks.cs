using System.Diagnostics;
using System.IO;
using System.Reflection;
using KillerBench.Services;

internal static class QueuedRelaunchChecks
{
    internal static void Run(string scratch)
    {
        string folder = Path.Combine(scratch, "queued-relaunch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string download = Path.Combine(folder, "download.exe");
        File.WriteAllBytes(download, [1]);
        UpdateImagePublication.RememberVerified(download, new string('A', 64), new string('B', 40));
        string target = Path.Combine(folder, "app", new string('x', 120), "KillerBench.exe");
        string overrides = string.Join("\n", Enumerable.Range(0, 100).Select(i => "$fixture" + i + " = 'operation " + i + "'"));
        foreach (string script in new[]
        {
            UpdateImagePublication.Script(download, target, true, "https://example.invalid", "Fixture", "1.0.0", "HKLM", overrides: overrides),
            UpdateImagePublication.Installer(download, "/S", true, target, Path.Combine(folder, "stage"), "https://example.invalid", overrides)
        })
        {
            int length = ImmutablePowerShell.Arguments(script).Length;
            if (length > 28_000) throw new Exception("Update helper lost its command margin: " + length);
            Console.WriteLine("PASS: update helper with long target and operation overrides uses " + length + " command characters.");
        }
        // Reflection only obtains the emitted command artifact from the executable assembly.
        // Assertions execute that command with substituted native boundaries in the script fixture.
        typeof(KillerBench.App).Assembly.GetType("KillerBench.Services.UpdateImagePublication", throwOnError: true)!
            .GetMethod("RememberVerified", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [download, new string('A', 64), new string('B', 40)]);
        var publication = typeof(KillerBench.App).Assembly.GetType("KillerBench.Services.TrustedUpdate", throwOnError: true)!
            .GetMethod("Publication", BindingFlags.Static | BindingFlags.NonPublic)!;
        using var owner = Process.GetCurrentProcess();
        object[] publicationArguments = [download, target, "1.0.0", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(new byte[] { 1, 2, 3 })), owner.Id,
            owner.StartTime.ToUniversalTime().Ticks, owner.Id, owner.StartTime.ToUniversalTime().Ticks, new string('A', 40)];
        var recovery = (ProcessStartInfo)publication.Invoke(null, publicationArguments)!;
        string recoveryArguments = Path.Combine(folder, "recovery-arguments.txt");
        File.WriteAllText(recoveryArguments, recovery.Arguments);
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-ExecutionPolicy"); start.ArgumentList.Add("Bypass"); start.ArgumentList.Add("-File");
        start.ArgumentList.Add(Path.GetFullPath(Path.Combine("Checks", "Test-QueuedRelaunch.ps1")));
        start.ArgumentList.Add("-SourceRoot"); start.ArgumentList.Add(Environment.CurrentDirectory);
        start.ArgumentList.Add("-WorkRoot"); start.ArgumentList.Add(folder);
        start.ArgumentList.Add("-RecoveryArgumentsPath"); start.ArgumentList.Add(recoveryArguments);
        start.Environment["PSModulePath"] = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\Modules");
        using var process = Process.Start(start) ?? throw new Exception("Queued relaunch fixture did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000)) { process.Kill(true); throw new Exception("Queued relaunch fixture timed out."); }
        if (process.ExitCode != 0) throw new Exception("Queued relaunch fixture failed: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
        string evidence = output.GetAwaiter().GetResult();
        if (!evidence.Contains("PASS:", StringComparison.Ordinal)) throw new Exception("Queued relaunch fixture selected no checks.");
        Console.Write(evidence);
    }
}
