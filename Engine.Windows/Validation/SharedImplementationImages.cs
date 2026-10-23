using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace KillerBench.Engine.Windows.Validation;

/// <summary>Reads exact shared implementation images from disk or a host's embedded publication copies.</summary>
public static class SharedImplementationImages
{
    public const string ResourcePrefix = "KillerBench.SharedImplementation.";

    [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "An empty bundled assembly location uses the exact embedded publication image.")]
    public static Stream Open(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!string.IsNullOrEmpty(assembly.Location)) return File.OpenRead(assembly.Location);
        string resource = ResourcePrefix + assembly.GetName().Name + ".dll";
        Stream stream = Assembly.GetEntryAssembly()?.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException("The bundled host does not retain a required shared implementation image: " + assembly.GetName().Name);
        try
        {
            using var image = new PEReader(stream, PEStreamOptions.LeaveOpen);
            MetadataReader metadata = image.GetMetadataReader();
            var definition = metadata.GetAssemblyDefinition();
            if (metadata.GetString(definition.Name) != assembly.GetName().Name || definition.Version != assembly.GetName().Version ||
                metadata.GetGuid(metadata.GetModuleDefinition().Mvid) != assembly.ManifestModule.ModuleVersionId)
                throw new InvalidDataException("The retained shared implementation image does not match the loaded assembly.");
            stream.Position = 0;
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }
}
