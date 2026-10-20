using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Windows.Primitives;

internal static class WindowsPowerShellBindingChecks
{
    internal static async Task RunAsync()
    {
        string[] identities = ["Ethernet", "Ethernet 2", "Ethernet (Lab)", "Ethernet #2", @"PCI\VEN_1234&DEV_5678\1", "Adapter';throw 'CODE_EXECUTED", "Adapter$(throw 'CODE_EXECUTED')"];
        foreach (string identity in identities)
        {
            var result = await WindowsNetAdapterPrimitives.RunAsync(
                "& { param([string]$identity) [Console]::Out.Write($identity) }", CancellationToken.None, identity);
            if (result.ExitCode != 0 || result.Output != identity)
                throw new Exception("PowerShell data binding changed selected identity: " + identity + "; observed=" + result.Output + "; error=" + result.Error);
        }
        var multiple = await WindowsNetAdapterPrimitives.RunAsync(
            "& { param([string]$name,[string]$family,[int]$mtu) $name+'|'+$family+'|'+$mtu }", CancellationToken.None, "Ethernet 2", "IPv6", "1500");
        if (multiple.ExitCode != 0 || multiple.Output.Trim() != "Ethernet 2|IPv6|1500") throw new Exception("Multiple PowerShell data arguments changed: " + multiple.Output + "; error=" + multiple.Error);
        var noArguments = await WindowsNetAdapterPrimitives.RunAsync("'NoArguments'", CancellationToken.None);
        if (noArguments.ExitCode != 0 || noArguments.Output.Trim() != "NoArguments") throw new Exception("Argument-free PowerShell command changed.");

        foreach (string name in identities.Take(4))
        {
            string prefix = "$selected='" + name + "';" +
                "function Get-NetAdapter { param($Name,[switch]$Physical,$ErrorAction) [pscustomobject]@{Count=1;Name=$Name;InterfaceGuid='11111111-1111-1111-1111-111111111111';AdminStatus='Down'} };" +
                "function Enable-NetAdapter { param($InputObject,$Confirm,$ErrorAction) if($InputObject.Name -cne $selected -or $InputObject.InterfaceGuid -ne '11111111-1111-1111-1111-111111111111'){throw 'Wrong target'} };" +
                "function Get-NetAdapterPowerManagement { param($Name,$ErrorAction) [pscustomobject]@{Name=$Name;AllowComputerToTurnOffDevice='Enabled'} };" +
                "function Set-NetAdapterPowerManagement { param($InputObject,$AllowComputerToTurnOffDevice,$ErrorAction) if($InputObject.Name -cne $selected){throw 'Wrong target'} };";
            Task<KillerBench.Engine.Infrastructure.ToolResult> Run(string script, CancellationToken token, string[] arguments) =>
                WindowsNetAdapterPrimitives.RunAsync(prefix + script, token, arguments);
            var parameters = new Dictionary<string, string> { ["name"] = name };
            var enabled = await new WindowsAdapterEnableAction(Run).ExecuteAsync(parameters, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (!enabled.Succeeded) throw new Exception("Exact adapter enable failed for " + name + ": " + enabled.Raw);
            var power = await new WindowsAdapterPowerSavingDisableAction(Run).ExecuteAsync(parameters, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (!power.Succeeded) throw new Exception("Exact adapter power change failed for " + name + ": " + power.Raw);
        }
        Console.WriteLine("PASS: real PowerShell identity binding and substituted exact adapter actions.");

        foreach (bool powerSaving in new[] { false, true })
        {
            string prefix = "function Get-NetAdapter { param($Name,[switch]$Physical,$ErrorAction) [pscustomobject]@{Count=1;Name='Ethernet';AdminStatus='Down'} };" +
                "function Get-NetAdapterPowerManagement { param($Name,$ErrorAction) [pscustomobject]@{Name='Ethernet';AllowComputerToTurnOffDevice='Enabled'} };" +
                "function Enable-NetAdapter { throw 'MUTATION_REACHED' }; function Set-NetAdapterPowerManagement { throw 'MUTATION_REACHED' };";
            Task<KillerBench.Engine.Infrastructure.ToolResult> Run(string script, CancellationToken token, string[] arguments) =>
                WindowsNetAdapterPrimitives.RunAsync(prefix + script, token, arguments);
            IActionPrimitive action = powerSaving ? new WindowsAdapterPowerSavingDisableAction(Run) : new WindowsAdapterEnableAction(Run);
            var rejected = await action.ExecuteAsync(new Dictionary<string, string> { ["name"] = "Ethernet 2" }, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (rejected.Succeeded || rejected.Raw.Contains("MUTATION_REACHED")) throw new Exception("A mismatched returned adapter identity reached mutation: " + action.Name);
        }
        Console.WriteLine("PASS: mismatched returned adapter identities refused before substituted mutation.");
    }
}
