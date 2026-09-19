using System.Net.NetworkInformation;

namespace KillerBench.Engine;

public static class NetworkAssessment
{
    public static WorkflowCheck ReadNetworkConfiguration(string name,
        Func<(string[] Addresses, string[] Gateways, string[] Dns)> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        try
        {
            var snapshot = read();
            if (snapshot.Addresses is null || snapshot.Gateways is null || snapshot.Dns is null)
                return new(name, CheckState.Unknown, "Adapter configuration was incomplete. Other interfaces can still be inspected; no repair selected.");
            return NetworkSnapshot.Assess(new(name, snapshot.Addresses, snapshot.Gateways, snapshot.Dns));
        }
        catch (Exception error) when (error is NetworkInformationException or InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(name, CheckState.Unknown, "Adapter configuration could not be read. The interface may have changed during collection or access may be unavailable. Other interfaces can still be inspected; no repair selected.");
        }
    }

    public static WorkflowCheck AssessNetwork(string name, string[] addresses, string[] gateways, string[] dns)
    {
        if (addresses.Length == 0) return new(name, CheckState.Information, "No IP binding. This may be a filter or virtual adapter; it is not treated as a failed connection.");
        if (addresses.Concat(gateways).Concat(dns).Any(a => !System.Net.IPAddress.TryParse(a, out _)))
            return new(name, CheckState.Unknown, "Adapter configuration contains an unrecognized address. Recollect configuration before drawing conclusions; no repair selected.");
        bool usable = addresses.Select(System.Net.IPAddress.Parse).Any(IsUsableHostAddress);
        var findings = new List<string>();
        if (!usable) findings.Add("No usable host address was reported. Check link state and the intended static or DHCP configuration.");
        if (gateways.Length == 0) findings.Add("No default gateway was reported for this adapter. Off-subnet traffic may use another adapter or an explicit route.");
        if (dns.Length == 0) findings.Add("No DNS server was reported for this adapter. Name resolution may use another interface or a local resolver.");
        bool attention = findings.Count > 0;
        return new(name, attention ? CheckState.Attention : CheckState.Clear,
            $"Addresses: {string.Join(", ", addresses)}; gateways: {string.Join(", ", gateways)}; DNS: {string.Join(", ", dns)}. " +
            (attention ? string.Join(" ", findings) + " This can be intentional on isolated or virtual adapters. No configuration changes were selected." : "Local configuration is present; reachability and name resolution are untested."));
    }

    private static bool IsUsableHostAddress(System.Net.IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (System.Net.IPAddress.IsLoopback(address) || address.Equals(System.Net.IPAddress.Any) ||
            address.Equals(System.Net.IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6Multicast) return false;
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length != 4 || (bytes[0] != 0 && bytes[0] < 224 && !(bytes[0] == 169 && bytes[1] == 254));
    }
}
