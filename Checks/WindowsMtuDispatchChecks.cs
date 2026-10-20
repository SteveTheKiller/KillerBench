using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Windows.Primitives;

internal static class WindowsMtuDispatchChecks
{
    internal static async Task RunAsync()
    {
        const string prefix = "function Set-NetIPInterface { param($InterfaceAlias,$AddressFamily,$NlMtuBytes,$PolicyStore,$ErrorAction) " +
            "if($InterfaceAlias -cne 'Ethernet 2'){throw 'Wrong interface'}; " +
            "if($PolicyStore -eq 'PersistentStore'){[Console]::Error.WriteLine('PERSISTENT_CHANGED')}else{throw 'ACTIVE_WRITE_FAILED'} };";
        Task<ToolResult> Run(string script, CancellationToken token, string[] arguments) =>
            WindowsNetAdapterPrimitives.RunAsync(prefix + script, token, arguments);
        var result = await new WindowsInterfaceMtuSetAction(Run).ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (!result.Dispatched || result.Succeeded || !result.Raw.Contains("PERSISTENT_CHANGED") || !result.Raw.Contains("ACTIVE_WRITE_FAILED"))
            throw new Exception("A partial MTU mutation was reported as never dispatched: dispatched=" + result.Dispatched + ", succeeded=" + result.Succeeded + ", raw=" + result.Raw);
        Console.WriteLine("PASS: MTU first-write success and second-write failure remain dispatched and uncertain.");

        using (var stopped = new CancellationTokenSource())
        {
            int calls = 0;
            Task<ToolResult> StopAfterPersistent(string script, CancellationToken token, string[] arguments)
            {
                calls++;
                stopped.Cancel();
                return Task.FromResult(new ToolResult(0, "Set", "", false));
            }
            var canceled = await new WindowsInterfaceMtuSetAction(StopAfterPersistent).ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, stopped.Token);
            if (!canceled.Dispatched || canceled.Succeeded || calls != 1 || !canceled.Raw.Contains("PersistentStore: completed.") || canceled.Raw.Contains("ActiveStore: dispatch"))
                throw new Exception("Cancellation after the persistent MTU write lost phase evidence or dispatched the next write.");
        }

        foreach (string failure in new[] { "cancel", "timeout", "truncated", "exit" })
        {
            int calls = 0;
            Task<ToolResult> FailActive(string script, CancellationToken token, string[] arguments)
            {
                calls++;
                if (calls == 1) return Task.FromResult(new ToolResult(0, "Set", "", false));
                return failure switch
                {
                    "cancel" => Task.FromException<ToolResult>(new OperationCanceledException()),
                    "timeout" => Task.FromResult(new ToolResult(-1, "", "", false) { TimedOut = true }),
                    "truncated" => Task.FromResult(new ToolResult(0, "Set", "", true)),
                    _ => Task.FromResult(new ToolResult(1, "", "write failed", false))
                };
            }
            var failed = await new WindowsInterfaceMtuSetAction(FailActive).ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (!failed.Dispatched || failed.Succeeded || calls != 2 || !failed.Raw.Contains("PersistentStore: completed.") || !failed.Raw.Contains("ActiveStore: dispatch requested;") || (failure == "timeout" && !failed.TimedOut))
                throw new Exception("MTU " + failure + " lost dispatch uncertainty or phase evidence.");
        }
        int successCalls = 0;
        Task<ToolResult> Succeed(string script, CancellationToken token, string[] arguments)
        {
            successCalls++;
            string expectedStore = successCalls == 1 ? "PersistentStore" : "ActiveStore";
            if (!arguments.SequenceEqual(new[] { "Ethernet 2", "IPv6", "1500", expectedStore })) throw new Exception("MTU phase changed target or order.");
            return Task.FromResult(new ToolResult(0, "Set", "", false));
        }
        var succeeded = await new WindowsInterfaceMtuSetAction(Succeed).ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (!succeeded.Dispatched || !succeeded.Succeeded || successCalls != 2 || !succeeded.Raw.Contains("ActiveStore: completed.")) throw new Exception("Successful MTU phases were not preserved.");
        using (var stopped = new CancellationTokenSource())
        {
            stopped.Cancel();
            var canceled = await new WindowsInterfaceMtuSetAction(Succeed).ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, stopped.Token);
            if (canceled.Dispatched || canceled.Succeeded || successCalls != 2) throw new Exception("Pre-canceled MTU action dispatched a write.");
        }
        Console.WriteLine("PASS: MTU cancellation, timeout, truncation, failure, success, and pre-dispatch refusal retain truthful phase state.");
    }

    private static Dictionary<string, string> Target() => new() { ["name"] = "Ethernet 2", ["family"] = "IPv6", ["mtu"] = "1500" };
}
