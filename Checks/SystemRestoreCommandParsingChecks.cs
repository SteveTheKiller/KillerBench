using System.IO;
using System.Text.Json;
using KillerBench.Engine;
using KillerBench.Engine.SystemHealth;

internal static class SystemRestoreCommandParsingChecks
{
    public static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "killerbench-restore-args-" + Guid.NewGuid().ToString("N"));
        var host = new RefuseNativeWork();
        var previous = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            int exit = await SystemRestoreCommands.RunAsync(["restore-point", "--execute"], host, root);
            if (exit != 0) throw new InvalidOperationException("A description of --execute must remain a preview: " + output);
            using var result = JsonDocument.Parse(output.ToString());
            if (result.RootElement.GetProperty("outcome").GetString() != "Preview" ||
                result.RootElement.GetProperty("request").GetProperty("description").GetString() != "--execute")
                throw new InvalidOperationException("The command did not preserve the literal description: " + output);
            if (Directory.Exists(root)) throw new InvalidOperationException("A preview created a repair journal folder.");
        }
        finally { Console.SetOut(previous); if (Directory.Exists(root)) Directory.Delete(root, true); }
        Console.WriteLine("PASS restore-point CLI description stays literal (1 selected regression)");
    }

    private sealed class RefuseNativeWork : ISystemRestoreCreationHost
    {
        public bool IsAdministrator => throw new InvalidOperationException("Preview reached native eligibility.");
        public bool? IsSupportedClientOperatingSystem => throw new InvalidOperationException("Preview reached native eligibility.");
        public TimeSpan? MinimumCreationInterval => throw new InvalidOperationException("Preview reached native eligibility.");
        public Task<SystemRestoreInventory> InspectAsync(CancellationToken token) => throw new InvalidOperationException("Preview tried to inspect native restore points.");
        public Task<ToolResult> CreateAsync(SystemRestoreCreationRequest request, IProgress<string>? progress, CancellationToken token) => throw new InvalidOperationException("Preview tried to create a native restore point.");
    }
}
