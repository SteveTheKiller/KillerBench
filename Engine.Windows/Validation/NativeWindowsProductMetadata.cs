using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Windows.Validation;

/// <summary>Reads the current Windows product identity without trusting the legacy registry ProductName.</summary>
public static class NativeWindowsProductMetadata
{
    public const string Unavailable = "Windows (product metadata unavailable)";

    public static async Task<string> ReadAsync(CancellationToken token = default)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(token);
        bounded.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var result = await NativeTool.RunAsync(@"WindowsPowerShell\v1.0\powershell.exe",
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
                 "$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); Get-CimInstance Win32_OperatingSystem | Select-Object Caption,ProductType,Version,BuildNumber | ConvertTo-Json -Compress"],
                token: bounded.Token).ConfigureAwait(false);
            return FromProvider(result);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return Unavailable; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or PlatformNotSupportedException)
        { return Unavailable; }
    }

    public static string FromProvider(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error) ||
            string.IsNullOrWhiteSpace(result.Output) || result.Output.Length > 4096) return Unavailable;
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("Caption", out var caption) || caption.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("ProductType", out var productType) || !productType.TryGetInt32(out int type) || type is < 1 or > 3 ||
                !root.TryGetProperty("Version", out var version) || version.ValueKind != JsonValueKind.String ||
                !System.Version.TryParse(version.GetString(), out var parsedVersion) ||
                !root.TryGetProperty("BuildNumber", out var build) || build.ValueKind != JsonValueKind.String ||
                !int.TryParse(build.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsedBuild) ||
                parsedBuild <= 0 || parsedVersion.Build != parsedBuild) return Unavailable;
            string? name = caption.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || name.Any(char.IsControl)) return Unavailable;
            // ProductType distinguishes workstation, domain controller and server even when they share a build.
            // Caption comes from the current OS provider, which identifies Windows 11 correctly and can be localized.
            return name;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return Unavailable; }
    }
}
