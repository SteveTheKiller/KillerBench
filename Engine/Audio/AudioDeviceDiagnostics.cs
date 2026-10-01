namespace KillerBench.Engine.Audio;

public static class AudioDeviceDiagnostics
{
    public static Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default) =>
        DeviceDiagnostics.CheckAsync(DeviceCategory.Audio, token);

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result) =>
        DeviceDiagnostics.Interpret(result, DeviceCategory.Audio);
}
