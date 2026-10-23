using System.Text.Json;
using System.Text.Json.Nodes;

internal static class CapabilityOutput
{
    internal static string WithStatus<T>(T value, IEnumerable<RepairCapability> capabilities, JsonSerializerOptions? options = null)
    {
        options ??= Capture.JsonOptions;
        JsonObject node = JsonSerializer.SerializeToNode(value, options)!.AsObject();
        node[options.PropertyNamingPolicy?.ConvertName("NativeCapabilities") ?? "NativeCapabilities"] = JsonSerializer.SerializeToNode(capabilities, options);
        return node.ToJsonString(options);
    }

    internal static void WriteLabels(IEnumerable<RepairCapability> capabilities)
    {
        foreach (var capability in capabilities)
            Console.Error.WriteLine("Native lab: " + capability.Id + ": " + capability.Label);
    }
}
