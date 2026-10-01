namespace KillerBench.Engine.Devices;

public sealed record DriverReference(string HardwareId, string Architecture, string WindowsProduct,
    int MinimumBuild, int MaximumBuild, string Vendor, string DriverVersion, string DownloadUrl)
{
    internal bool IsValid() => ValidText(HardwareId, 512) && HardwareId == HardwareId.Trim() &&
        Architecture is "x86" or "x64" or "arm64" &&
        WindowsProduct is "client" or "server" && MinimumBuild > 0 && MaximumBuild >= MinimumBuild &&
        ValidText(Vendor, 256) && ValidText(DriverVersion, 128) && ValidText(DownloadUrl, 2048) &&
        Uri.TryCreate(DownloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.UserInfo.Length == 0 && !string.IsNullOrWhiteSpace(uri.Host);

    internal static bool ValidText(string? text, int maximum) => !string.IsNullOrWhiteSpace(text) &&
        text.Length <= maximum && !text.Any(char.IsControl);

    public bool Matches(string hardwareId, string architecture, string windowsProduct, int build) =>
        string.Equals(HardwareId, hardwareId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Architecture, architecture, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(WindowsProduct, windowsProduct, StringComparison.OrdinalIgnoreCase) &&
        build >= MinimumBuild && build <= MaximumBuild;
}
