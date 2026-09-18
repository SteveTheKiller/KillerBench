using System.Diagnostics;
using KillerBench.Engine;

internal static class WindowsBitsRetryChecks
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows()) return;
        string owner = new WindowsBitsRetryHost().OwnerAccount;
        var job = new BitsJobSnapshot(Guid.NewGuid(), "Fixture ' $(throw 'injection')", "Error", "", owner, -1, 100, 0);
        foreach (string state in new[] { "Error", "Suspended", "Transferred" })
        {
            string script = WindowsBitsRetryHost.CreateStep(job, owner).Arguments.Last();
            // Replace all transfer entry points before evaluating the production command.
            string fixture = "function Import-Module { }; function Get-BitsTransfer { [pscustomobject]@{JobId=$e.id;OwnerAccount=$e.owner;DisplayName=$e.name;BytesTotal=$e.total;JobState='" + state + "'} }; " +
                "function Resume-BitsTransfer($BitsJob,[switch]$Asynchronous) { if(-not $Asynchronous -or $BitsJob.JobId -ne $e.id){throw 'Invalid fixture request'}; $global:called=$true }; ";
            var start = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, PrintRecovery.Tool))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", fixture + script + "; if(-not $global:called){throw 'Missing fixture request'}" }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
            using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try { await process.WaitForExitAsync(limit.Token); }
            catch { if (!process.HasExited) process.Kill(true); throw; }
            string stdout = await output, stderr = await error;
            if (state == "Error" ? process.ExitCode != 0 || !stdout.Contains("Retry requested") || stderr.Length != 0 : process.ExitCode == 0 || !stderr.Contains("no longer eligible"))
                throw new Exception("BITS command fixture failed for " + state + ": " + stderr);
        }
        Console.WriteLine("PASS: Windows BITS command executes with substituted transfer functions, preserves literal names and rejects changed states; no real transfers touched.");
    }
}
