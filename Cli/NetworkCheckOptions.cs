using System.Globalization;

internal static class NetworkCheckOptions
{
    internal static bool TryTimeout(string[] args, int requiredCount, out TimeSpan timeout)
    {
        timeout = TimeSpan.FromSeconds(10);
        if (args.Length == requiredCount) return true;
        if (args.Length != requiredCount + 2 || args[requiredCount] != "--timeout-seconds" ||
            !int.TryParse(args[requiredCount + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) || seconds is < 1 or > 60)
            return false;
        timeout = TimeSpan.FromSeconds(seconds);
        return true;
    }
}
