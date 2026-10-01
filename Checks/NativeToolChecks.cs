using System.Reflection;
using KillerBench.Engine;

internal static class NativeToolChecks
{
    internal static async Task RunAsync()
    {
        var method = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.NativeTool")!
            .GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task<ToolResult> Run(string script, CancellationToken token, IProgress<string>? progress = null) =>
            (Task<ToolResult>)method.Invoke(null, [PrintRecovery.Tool,
                new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script }, false, progress, token])!;

        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        bool started = false;
        var progress = new InlineProgress(text => { if (text.Contains("READY")) { started = true; stop.Cancel(); } });
        try
        {
            await Run("[Console]::Out.WriteLine('READY'); Start-Sleep -Seconds 30", stop.Token, progress).WaitAsync(TimeSpan.FromSeconds(15));
            throw new Exception("Native output wait ignored cancellation");
        }
        catch (OperationCanceledException) when (started) { }
        var output = await Run("[Console]::Out.Write(('x' * 300000)); [Console]::Error.Write('fixture error'); exit 7", CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(15));
        if (output.ExitCode != 7 || !output.Truncated || output.Output.Length != 250000 || output.Error != "fixture error")
            throw new Exception("Native output bounds or stderr/exit capture failed");
        var bounded = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.Infrastructure.BoundedTool")!
            .GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 30"], true);
        var timed = await ((Task<ToolResult>)bounded.Invoke(null, [step, TimeSpan.FromSeconds(1), null, CancellationToken.None])!)
            .WaitAsync(TimeSpan.FromSeconds(10));
        if (!timed.TimedOut || timed.ExitCode == 0) throw new Exception("Native timeout must be explicit and unsuccessful");
        Console.WriteLine("PASS: native cancellation, timeout, output bounds, stderr and exit capture (harmless child processes only).");
    }

    private sealed class InlineProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }
}
