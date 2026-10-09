# Fix network and internet problems

Use this when a computer has no internet, cannot reach a server or name, or keeps dropping its connection. KillerBench checks the adapter, address, gateway, DNS and firewall in order, and only renews a lease or starts a service when fresh evidence proves it is needed.

## Check the connection

1. Open **Network** and choose **Network**.
2. To test something specific, fill in **Hostname lookup (optional)**, or **TCP destination (optional)** and **TCP port**. Leave them blank to check only the local configuration. No application data is sent.
3. Set **Timeout per lookup or connection (seconds)** if the network is slow.
4. Leave **Auto-repair confirmed issues** off and press **GO**.
5. Read the report. It separates a missing address, a missing or unreachable gateway, DNS configuration problems, proxy settings, firewall profile mismatches and a failed lookup or connection, and gives the next step for each.

A reply from the gateway does not prove internet access, and a failed lookup through your configured resolver does not mean the site is down.

## Repair what the report found

1. Run **Network** again with **Auto-repair confirmed issues** turned on. It can start enabled DNS Client, DHCP Client, SMB client, Network Location Awareness, Network List and IP Helper services, and renew connected physical DHCP adapters that lack a usable IPv4 address.
2. Expect the connection to drop briefly during a renewal. Static addresses and virtual adapters are never changed.
3. For one bad cached name, use the DNS cache flush for that exact host instead of clearing everything.

Routes, Hosts entries, DNS servers, firewall rules, Winsock providers and network profiles are always preserved.

## Wireless, wired sign-in and proxies

- **Wireless networking** checks the adapter and WLAN AutoConfig, and can start the service if it is stopped.
- **Wired authentication (802.1X)** does the same for Wired AutoConfig on networks that need sign-in at the port.
- **WinHTTP Auto-Proxy** shows stored proxy settings for the machine and the current account. KillerBench never fetches a PAC URL or changes proxy settings during diagnosis.

## From the command line

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

See [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) and [dns-cache](cli.html#cmd-dns-cache).

## Related

- [Network section](network.html)
- [Fix printing problems](fix-printing.html)
- [How changes stay safe](safety.html)
