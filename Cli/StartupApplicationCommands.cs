using System.Text.Json;
using KillerBench.Engine;

internal static class StartupApplicationCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: startup-apps (read-only startup registrations and boot or logon task review; no disablement)");
            return 2;
        }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var registrationsTask = StartupApplicationDiagnostics.CheckAsync(stop.Token);
            var tasksTask = StartupScheduledTaskDiagnostics.CheckAsync(stop.Token);
            WorkflowCheck[] checks = [await registrationsTask.ConfigureAwait(false), await tasksTask.ConfigureAwait(false)];
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Checks = checks,
                Complete = checks.All(check => check.EffectiveState != CheckState.Unknown),
                Note = "Read-only startup review. Commands, target paths, task actions, arguments, accounts and credentials are excluded. Registered items are not assumed to be unwanted or safe to disable. No task, registry value, file, application, service or policy changed."
            }, Capture.JsonOptions));
            return checks.All(check => check.EffectiveState != CheckState.Unknown) ? 0 : 3;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Startup review stopped."); return 130; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("Startup review unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
