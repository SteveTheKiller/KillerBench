using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace KillerBench.Engine;

public sealed record OfficeClickToRunProduct(string View, string ProductId, string? Version,
    string? Platform, string? Culture);

public sealed record OfficeClickToRunSnapshot(IReadOnlyList<OfficeClickToRunProduct> Products,
    bool Complete, string? CollectionIssue = null);

public static class OfficeClickToRunInventory
{
    private const string ConfigurationPath = @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration";

    public static OfficeClickToRunSnapshot Read(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Office Click-to-Run inventory requires Windows.");
        var products = new List<OfficeClickToRunProduct>();
        var issues = new List<string>();
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var configuration = root.OpenSubKey(ConfigurationPath);
                if (configuration is null) continue;
                object? rawProducts = configuration.GetValue("ProductReleaseIds");
                if (rawProducts is null) continue;
                if (rawProducts is not string productText || productText.Length > 8192)
                {
                    issues.Add(view + ": ProductReleaseIds has an unsupported value.");
                    continue;
                }
                string? version = ReadText(configuration, "VersionToReport", issues, view);
                string? platform = ReadText(configuration, "Platform", issues, view);
                string? culture = ReadText(configuration, "ClientCulture", issues, view);
                string[] ids = productText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (ids.Length is 0 or > 64 || ids.Any(id => !ValidProductId(id)) ||
                    ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Length)
                {
                    issues.Add(view + ": ProductReleaseIds could not be represented safely.");
                    continue;
                }
                products.AddRange(ids.Select(id => new OfficeClickToRunProduct(view.ToString(), id, version, platform, culture)));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
            {
                issues.Add(view + ": " + error.GetType().Name);
            }
        }
        return new(products.OrderBy(product => product.View, StringComparer.Ordinal)
            .ThenBy(product => product.ProductId, StringComparer.Ordinal).ToArray(), issues.Count == 0,
            issues.Count == 0 ? null : string.Join(" ", issues));
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadText(RegistryKey key, string name, List<string> issues, RegistryView view)
    {
        object? value = key.GetValue(name);
        if (value is null) return null;
        if (value is string text && text.Length is > 0 and <= 512 && !text.Any(char.IsControl)) return text;
        issues.Add(view + ": " + name + " has an unsupported value.");
        return null;
    }

    private static bool ValidProductId(string value) => value.Length is > 0 and <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
