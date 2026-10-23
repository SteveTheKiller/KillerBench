namespace KillerBench.Engine.Windows.Validation;

public sealed record NativeScenarioMachine(string Product, string Version, string Build, string Architecture);

/// <summary>The OS admission, metadata and service boundaries used by native scenario orchestration.</summary>
public interface INativeScenarioHost
{
    bool IsWindows { get; }
    string MachineName { get; }
    string? DisposableMarker { get; }
    INativeScenarioService Service(string name);
    Task<NativeScenarioMachine> ReadMachineAsync(CancellationToken token);
}
