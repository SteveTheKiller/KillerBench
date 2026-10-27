# Network

Network inspects connections, DNS, routes, wireless and wired sign-in, file sharing and remote access. Tools that reach another host use the target and timeout you set, and nothing is changed unless you confirm a repair.

Step-by-step guides: [Fix network and internet problems](fix-network.html).

## Tools in this section

Inspect connections, DNS, routes, wireless, sharing and remote access.

### Network

Inspect adapters, address state and origin, gateways, DNS configuration, effective DNS namespace policy, TCP settings, native Winsock protocol and namespace-provider catalog structure, the IP routing table and Hosts file mappings. Auto-repair can start enabled DNS Client, DHCP Client, SMB client, Network Location Awareness, Network List and IP Helper services and renew connected physical DHCP adapters lacking a usable IPv4 address. Starting network-identification services may recalculate network identity and apply a different existing firewall profile. IP Helper may activate existing IPv6 transition, port-proxy and network-notification features. Service recovery verifies running state, not successful name resolution, route selection, network identification or tunnel operation. Routes, Hosts entries, DNS servers, profiles, firewall rules, Winsock providers, transition configuration, startup types and policies are preserved. Renewal may change leased addresses and interrupt connections. Static settings and virtual adapters are preserved. Exact gateway ICMP responses do not verify Internet reachability.

### Wireless networking

Inspect network devices and WLAN AutoConfig availability. Optional recovery starts only an enabled, stopped service after dependency checks; saved profiles may reconnect automatically. Profiles, credentials, policy, radio settings and startup types are preserved. No adapter reset or driver installation. Verification covers running state, not wireless authentication or connectivity.

### Wired authentication (802.1X)

Inspect network devices and Wired AutoConfig for networks requiring 802.1X. Optional recovery starts only an enabled, stopped service after dependency checks. Existing authentication profiles may activate and change network access. Certificates, credentials, profiles, policy and startup settings are preserved. Verification covers running state only, not authentication or connectivity.

### Network Connections

Review bounded current network profile assignments, exact service-failure history and current Network Connections service readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a network change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing network-management interfaces may resume. Adapters, addresses, routes, DNS settings, connection profiles, credentials, firewall configuration, policy and startup type are preserved. No connection or network setting is changed. Verification covers stable service state only, not adapter operation, address assignment, routing, name resolution or connectivity.

### WinHTTP Auto-Proxy

Review stored WinHTTP defaults, current-account Internet settings, exact service-failure history and current WinHTTP Auto-Proxy readiness. Incomplete evidence takes precedence. Proxy configuration and historical failures cannot authorize service recovery or discovery. A stopped demand-start service can be normal. When this exact tool is selected, optional recovery can start only the stopped enabled service after fresh dependency checks. Existing applications may resume WPAD or PAC discovery under current settings. Proxy definitions, PAC URLs, credentials, network settings, firewall configuration, policy, service permissions and startup type are preserved. No URL is fetched and no proxy or network setting is changed. Verification covers stable service state only, not discovery, reachability, authentication, policy ownership or application use.

### Windows Connection Manager

Review bounded current network profile assignments, exact service-failure history and current Windows Connection Manager readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a connection decision. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Windows may resume automatic connection decisions under existing profiles and policy. Adapters, profiles, credentials, metering, cost, roaming, firewall, VPN and authentication settings and startup type are preserved. No profile, adapter or setting is changed. Verification covers stable service state only, not profile applicability, connection selection, authentication, routing, name resolution or connectivity.

### Network Setup Service

Review bounded current adapter protocol bindings, exact service-failure history and current Network Setup Service readiness. Incomplete evidence takes precedence. Binding state and historical failures cannot authorize service recovery or a setup change. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing pending network setup may resume under Windows policy. Adapters, drivers, driver packages, bindings, addresses, routes, DNS settings, profiles, credentials, firewall configuration, policy and startup type are preserved. KillerBench selects no driver or configuration change. Verification covers stable service state only, not adapter availability, driver suitability, setup completion, address assignment, routing, name resolution or connectivity.

### File sharing (SMB)

Check Windows SMB server availability. Optional recovery starts only an enabled, stopped Server service after fresh dependency checks. Existing file and printer shares may become reachable under their current permissions and firewall policy. No share, permission, SMB setting, firewall rule, credential, policy or startup type is changed. Verification covers stable service state only, not client access or file transfer.

### Remote Access Connection Manager

Review bounded current-account VPN profile configuration, exact service-failure history and current Remote Access Connection Manager readiness without collecting secrets. An empty inventory can be intentional, and profile presence, connection labels or historical failures cannot authorize recovery or a connection attempt. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing dial-up and VPN requests may resume. Connection profiles, credentials, routes, adapters, authentication settings, policy and startup type are preserved. No connection is initiated. Verification covers stable service state only, not profile applicability, server reachability, authentication, tunnel negotiation, route installation, name resolution or connectivity.

### Remote Desktop Services

Review local Remote Desktop enablement, Network Level Authentication and listener port, bounded connection and session history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Historical records cannot authorize service recovery or configuration changes. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. An existing listener may become reachable under current policy, NLA, firewall rules and permissions. Remote Desktop enablement, listeners, certificates, ports, firewall rules, NLA, permissions, sessions, policy, service permissions and startup type are preserved. No listener, firewall rule, account access, certificate or session is created or changed. Verification covers stable service state only, not effective policy, listener binding, firewall access, certificate identity, permissions, authentication, network reachability, connection success or session creation.

### Windows Remote Management

Review bounded WinRM listener configuration, operational history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Disabled listeners or current service concerns request review, while historical records cannot authorize recovery or configuration changes. Optional recovery starts only the enabled, stopped exact service after fresh dependency checks. Existing WS-Management listeners may become reachable under current authentication, certificate, firewall, TrustedHosts and policy settings. Listeners, addresses, ports, certificates, authentication, encryption, TrustedHosts, firewall rules, permissions, quotas, policy, service permissions and startup type are preserved. No listener, firewall rule, trust entry, certificate, credential or remote session is created or changed. Verification covers stable service state only, not effective policy, listener binding, firewall access, certificate validity, authentication, permissions, client reachability or remote command execution.
