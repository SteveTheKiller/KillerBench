using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>
/// The built-in Windows symptom catalog. Each tree walks the service recipes that can explain a symptom in dependency
/// order, rules each one out or repairs it, and ends at a human hand-off with concrete guidance. Trees only reference
/// recipes in the built-in packs, which the fixtures enforce.
/// </summary>
public static class WindowsSymptomCatalog
{
    public const string CatalogId = "windows.symptoms";

    public static SymptomCatalog Build()
    {
        var symptoms = new List<Symptom>();
        var trees = new List<TriageTree>();

        Add("printing.nothing-prints", "Nothing prints", "Printing",
            "Print jobs never reach the printer or sit in the queue, on every printer or on one printer.",
            ["can't print", "printer not printing", "print queue stuck", "printing does not work"],
            ServiceChain("printing.spooler"),
            "Automated checks cover the Print Spooler service. Next: open the queue for the printer, confirm it is not paused or offline, print a test page from Printer properties, and check the printer's own display and network connection.");

        Add("printing.printer-offline", "Printer shows offline", "Printing",
            "Windows lists the printer as offline although it is powered on.",
            ["printer offline", "offline printer"],
            ServiceChain("printing.spooler"),
            "After the spooler check, clear Use Printer Offline in the queue menu, confirm the printer's IP address matches the port, and ping the printer.");

        Add("network.no-internet", "No Internet access", "Network",
            "The PC reports no Internet or web pages do not load although the adapter shows connected.",
            ["no internet", "can't browse", "web pages won't load", "limited connectivity"],
            ServiceChain("network.dhcp-client", "network.dns-client", "network.location-awareness", "network.list-service", "network.ip-helper"),
            "Automated checks cover the DHCP, DNS, Network Location Awareness, Network List and IP Helper services. Next: run the Network workflow for addresses, gateway reachability and DNS servers, then test another device on the same network.");

        Add("network.wifi-missing", "Wi-Fi networks are missing", "Network",
            "No wireless networks are listed or the Wi-Fi toggle is absent.",
            ["no wifi", "wifi missing", "wireless not showing", "no wireless networks"],
            ServiceChain("network.wireless"),
            "After the WLAN AutoConfig check: confirm the radio is on (hardware switch or Fn key), airplane mode is off, and the wireless adapter is enabled in Device Manager without a problem code.");

        Add("network.wired-auth", "Wired connection will not authenticate", "Network",
            "A wired connection stays at Authenticating or Identifying on an 802.1X network.",
            ["802.1x", "wired authentication", "dot3svc"],
            ServiceChain("network.wired-authentication"),
            "After the Wired AutoConfig check, review the network profile and certificate with the network administrator.");

        Add("network.shares-unreachable", "Network shares are unreachable", "Network",
            "Mapped drives or UNC paths fail although the Internet works.",
            ["mapped drive", "network share", "unc path", "can't access share"],
            ServiceChain("network.workstation", "network.dns-client"),
            "After the SMB client and DNS checks: test the UNC path by IP address, confirm the account's permissions, and check whether SMB1 is expected by the server.");

        Add("network.firewall-off", "Firewall reports it is off or cannot start", "Security",
            "Windows Security reports the firewall is off, or firewall settings will not open.",
            ["firewall off", "firewall won't start", "windows firewall error"],
            ServiceChain("firewall.filtering-engine", "firewall.service"),
            "After the Base Filtering Engine and Firewall service checks, run the Firewall Services workflow to review profile state and managed policy.");

        Add("audio.no-sound", "No sound", "Audio",
            "No audio plays from any application, or the volume icon shows an error.",
            ["no sound", "no audio", "audio not working", "speakers not working"],
            ServiceChain("audio.endpoints", "audio.service"),
            "After the Audio Endpoint Builder and Windows Audio checks: confirm the correct output device is selected and not muted, and check the audio device in Device Manager for a problem code.");

        Add("search.no-results", "Windows Search returns nothing", "Windows services",
            "Start menu or File Explorer search returns no results or the search box does not respond.",
            ["search broken", "start menu search", "search not working", "no search results"],
            ServiceChain("search.service"),
            "After the Windows Search service check, run the Search workflow to review index status; an index rebuild is a separate explicit option.");

        Add("update.stuck", "Windows Update is stuck or failing", "Windows Update",
            "Updates fail with an error code, stay at a percentage, or the update page reports an error.",
            ["windows update stuck", "update failed", "update error", "updates won't install"],
            ServiceChain("update.service", "update.transfers", "update.cryptography", "update.modules-installer", "update.orchestrator", "update.medic-service"),
            "After the update service checks, run the Windows Update workflow for the error code, pending restart, servicing health and data-store recovery options.");

        Add("time.wrong-clock", "Clock is wrong", "Time",
            "The system clock drifts or shows the wrong time and sign-in or web sites complain about certificates.",
            ["wrong time", "clock wrong", "time sync", "certificate date error"],
            null,
            "Run the Time workflow. It checks the Windows Time service, the configured source and the last synchronization, and can resynchronize; source changes are an explicit option. A CMOS battery or a wrong time zone also produces this symptom.");

        Add("backup.fails", "Backup fails", "Backup",
            "Windows Backup, File History or a third-party backup fails with a VSS or snapshot error.",
            ["backup failed", "vss error", "shadow copy error", "file history error"],
            ServiceChain("backup.vss", "backup.software-provider", "backup.windows-backup", "backup.block-level-engine"),
            "After the shadow-copy and backup service checks, run the Backup Services workflow for writer state and recent VSS events.");

        Add("installer.msi-fails", "Programs will not install or uninstall", "Installation",
            "MSI installers fail with error 1719 or a Windows Installer service message.",
            ["error 1719", "windows installer service could not be accessed", "can't install program", "uninstall fails"],
            ServiceChain("installer.service"),
            "After the Windows Installer service check, run the Installer workflow for registration, policy and recent installer events.");

        Add("remote.rdp-fails", "Remote Desktop cannot connect", "Remote access",
            "Remote Desktop connections to this PC fail although the network is up.",
            ["rdp not working", "remote desktop fails", "can't remote in"],
            ServiceChain("remote.desktop-services", "firewall.filtering-engine", "firewall.service"),
            "After the Remote Desktop and firewall service checks, run the Remote Desktop Services workflow for listener state, allowed users and firewall rules.");

        Add("devices.usb-not-detected", "USB devices are not detected", "Devices",
            "Plugging in a USB device does nothing, or devices appear and disappear.",
            ["usb not working", "usb not detected", "device not recognized"],
            ServiceChain("core.plug-and-play-service", "devices.install-service"),
            "After the Plug and Play and Device Install service checks, run the Device Problems workflow for problem codes and driver install history.");

        Add("store.apps-fail", "Store apps will not open or install", "Applications",
            "Microsoft Store apps close immediately, or Store installs never finish.",
            ["store not working", "apps won't open", "appx error", "store install stuck"],
            ServiceChain("applications.appx-deployment-service", "applications.readiness-service", "applications.state-repository-service", "applications.store-install-service", "applications.client-license-service"),
            "After the app platform service checks, run the App Packages workflow for deployment history and package state.");

        Add("bluetooth.missing", "Bluetooth is missing or will not pair", "Devices",
            "The Bluetooth toggle is absent or devices will not pair.",
            ["bluetooth missing", "bluetooth not working", "can't pair"],
            ServiceChain("devices.bluetooth-support"),
            "After the Bluetooth Support service check, run the Bluetooth workflow for radio and device state.");

        // Single-service and short-chain symptoms. Each maps to service recipes in the pack; the hand-off names the focused workflow.
        Service("profile.corrupt-cannot-load", "Profile will not load at sign-in", "Applications", "Sign-in reports the User Profile Service failed or lands on a temporary profile.", ["user profile service failed", "temporary profile", "profile cannot load"], "Run the Profiles workflow for ProfileList state; a .bak entry needs a manual rename.", "profiles.user-profile-service");
        Service("signin.credential-prompts-repeat", "Credential prompts keep repeating", "Security", "Office, OneDrive or Windows keep asking for a password that is correct.", ["password prompt loop", "keeps asking for password", "credential prompt"], "Run the MACE credential review and check the Credential Manager entries for the account.", "security.credential-manager-service", "accounts.web-account-manager-service", "accounts.microsoft-account-sign-in-service");
        Service("signin.group-policy-not-applying", "Group Policy is not applying", "Security", "gpupdate fails or policy settings never arrive on a domain-joined PC.", ["gpupdate fails", "group policy not applying", "policy not updating"], "Run gpresult /h with an administrator account and check domain controller reachability and SYSVOL access.", "core.group-policy-client-service");
        Service("signin.no-user-manager", "Sign-in hangs at Welcome", "Windows services", "Sign-in stalls or apps fail to start because the User Manager service is stopped.", ["stuck at welcome", "user manager service"], "If the service cannot start, review its failure history in the Services workflow.", "core.user-manager-service");
        Service("update.medic-tampered", "Windows Update Medic is disabled", "Windows Update", "The update medic service is missing, disabled or stopped, often after a tweak tool.", ["waasmedic", "update medic disabled"], "Run the focused Windows Update Medic workflow for registration and policy evidence.", "update.medic-service");
        Service("storage.storage-service-down", "Storage settings will not open", "Storage", "Storage settings or disk management fail because the Storage Service is stopped.", ["storage service", "storage settings blank"], "Run the Storage Service workflow.", "storage.service");
        Service("storage.optimize-service-down", "Optimize Drives will not run", "Storage", "Defragment and Optimize Drives reports the service is not running.", ["optimize drives service", "defrag service"], "Run the Optimize Drives Service workflow.", "storage.optimize-drives-service");
        Service("network.file-sharing-inbound-fails", "Other PCs cannot reach this PC's shares", "Network", "Shares hosted here are unreachable although this PC can reach the network.", ["can't access my shares", "file sharing not working", "server service"], "Run the File Sharing workflow for share, firewall rule and discovery evidence.", "network.smb-server", "network.function-discovery-provider-host", "network.function-discovery-resource-publication");
        Service("network.vpn-fails", "VPN will not connect", "Network", "A Windows VPN profile fails to connect or the VPN page errors.", ["vpn not connecting", "vpn error", "rasman"], "Run the Remote Access Connection Manager workflow for profile evidence.", "network.remote-access-service");
        Service("network.proxy-wrong", "Proxy settings are wrong or stuck", "Network", "Browsers report a proxy error or auto-detect never completes.", ["proxy error", "proxy stuck", "wpad"], "Run the Network workflow proxy diagnostics; resetting proxy settings is a separate explicit step.", "network.winhttp-autoproxy-service");
        Service("network.ipv6-broken", "IPv6 or tunnel adapters are broken", "Network", "IPv6 connectivity or port proxy features fail.", ["ipv6 not working", "ip helper", "teredo"], "Run the Network workflow interface diagnostics.", "network.ip-helper");
        Service("network.discovery-off", "Network discovery is off or devices are missing", "Network", "Other PCs and printers are not discovered on the network.", ["network discovery", "can't see other computers"], "Run the Network Connections and Function Discovery workflows; the network profile change is explicit.", "network.connections-service", "network.function-discovery-provider-host", "network.function-discovery-resource-publication");
        Service("security.center-blank", "Windows Security shows nothing", "Security", "The Windows Security app is blank or reports the service is not running.", ["windows security blank", "security center service"], "Run the Security Center workflow for antivirus registrations.", "security.center-service");
        Service("security.smart-card-not-read", "Smart card is not read", "Security", "Smart card sign-in or readers report the service is unavailable.", ["smart card not working", "scardsvr"], "Run the Smart Card workflow.", "devices.smart-card-service");
        Service("security.biometric-missing", "Fingerprint or face sign-in is missing", "Security", "Windows Hello biometric options are unavailable.", ["fingerprint missing", "face recognition missing", "biometric service"], "Run the Biometric workflow; sensor drivers are separate.", "devices.biometric-service");
        Service("display.fonts-broken", "Fonts render wrong or are missing", "Devices", "Text shows boxes, wrong fonts or apps report font errors.", ["fonts broken", "font cache", "missing fonts"], "Run the Font Cache workflow; a cache rebuild is an explicit step.", "display.font-cache-service");
        Service("display.enhancement-service-down", "Brightness or night light controls are missing", "Devices", "Display enhancement features are unavailable.", ["brightness slider missing", "night light missing"], "Run the Display Enhancement workflow.", "display.enhancement-service");
        Service("input.keyboard-mouse-dead", "Keyboard or mouse does not work", "Devices", "Input devices stop working although they have power.", ["keyboard not working", "mouse not working", "hid service"], "Run the Input workflow for device problem codes.", "devices.human-interface-service");
        Service("input.touch-keyboard-missing", "Touch keyboard or handwriting is missing", "Devices", "The touch keyboard does not appear or text input features fail.", ["touch keyboard missing", "text input service"], "Run the Text Input Management workflow.", "input.text-management-service");
        Service("devices.camera-not-working", "Camera does not work", "Devices", "Apps report no camera or the camera cannot start.", ["camera not working", "webcam not detected", "frame server"], "Run the Camera workflow and the Privacy review for camera access.", "devices.camera-frame-server");
        Service("devices.scanner-not-found", "Scanner is not found", "Devices", "Scanning apps cannot find the scanner.", ["scanner not working", "wia service"], "Run the Windows Image Acquisition workflow.", "devices.image-acquisition-service");
        Service("devices.phone-not-detected", "Phone or camera storage is not detected", "Devices", "MTP devices do not appear in File Explorer.", ["phone not showing", "mtp not working"], "Run the Portable Device Enumerator workflow.", "devices.portable-device-enumerator");
        Service("devices.autoplay-broken", "AutoPlay does not appear", "Devices", "Inserting media never prompts and drives do not refresh.", ["autoplay not working", "shell hardware detection"], "Run the Shell Hardware Detection workflow.", "devices.shell-hardware-detection-service");
        Service("devices.pairing-service-down", "Devices will not pair", "Devices", "Pairing wireless devices fails immediately.", ["pairing fails", "device association service"], "Run the Device Association workflow.", "devices.association-service");
        Service("power.service-down", "Power settings are unavailable", "Power", "Power options fail to open or plans cannot change.", ["power service", "power options missing"], "Run the Power Service workflow.", "core.power-service");
        Service("license.not-activated", "Windows is not activated", "Windows", "Activation reports an error or licensing services are stopped.", ["not activated", "activation error", "license"], "Run the Windows Activation workflow for licensing state and sanitized history.", "applications.license-manager-service", "applications.client-license-service");
        Service("remote.winrm-fails", "Remote PowerShell or WinRM fails", "Remote access", "Enter-PSSession or WinRM connections fail.", ["winrm not working", "remote powershell fails"], "Run the Windows Remote Management workflow for listeners and history.", "remote.winrm-service");
        Service("eventlog.stopped", "Event Viewer is empty or fails", "Windows services", "Event Viewer reports the service is unavailable.", ["event log service", "event viewer empty"], "Run the Event Log workflow.", "eventlog.service");
        Service("eventlog.collector-down", "Event forwarding stopped", "Windows services", "Subscriptions no longer collect events.", ["event collector", "event forwarding"], "Run the Windows Event Collector workflow.", "events.collector-service");
        Service("wmi.broken", "WMI queries fail", "Windows management", "Management tools report WMI errors or invalid class.", ["wmi error", "invalid class", "winmgmt"], "Run the WMI workflow; repository salvage is a guarded option.", "wmi.service");
        Service("wer.not-reporting", "Error reporting is not working", "Windows services", "Crashes leave no Windows Error Reporting record.", ["error reporting", "wer service"], "Run the Windows Error Reporting workflow.", "diagnostics.error-reporting-service");
        Service("diagnostics.policy-service-down", "Troubleshooters will not run", "Windows services", "Built-in troubleshooters report the Diagnostic Policy Service is not running.", ["diagnostic policy service", "troubleshooter error"], "Run the Diagnostic Policy Service workflow.", "diagnostics.policy-service");
        Service("tasks.not-running", "Scheduled tasks do not run", "Windows services", "Tasks never start or Task Scheduler cannot connect.", ["task scheduler", "scheduled task not running"], "Run the Scheduled Tasks workflow for task state and missed runs.", "tasks.scheduler-service");
        Service("core.rpc-failures", "RPC server is unavailable", "Windows services", "Tools report the RPC server is unavailable or DCOM errors.", ["rpc server unavailable", "dcom error", "rpc error"], "Run the Core Windows Services workflow.", "core.rpc-endpoint-mapper", "core.remote-procedure-call", "core.dcom-launcher");
        Service("core.background-broker-down", "Apps and notifications fail to start", "Windows services", "Modern apps and notifications fail because broker services are stopped.", ["broker infrastructure", "coremessaging"], "Run the Core Windows Services workflow.", "core.background-tasks-infrastructure", "core.messaging");
        Service("core.network-store-down", "Networking is completely broken", "Windows services", "No adapters function and network services cannot start.", ["nsi service", "network store interface"], "Run the Core Windows Services workflow.", "core.network-store-interface-service");
        Service("notifications.broken", "Notifications and COM events fail", "Windows services", "Notifications never appear or apps report event-system errors.", ["notifications not working", "com+ event system"], "Run the System Notifications workflow.", "notifications.com-event-system", "notifications.system-event-notification");
        Service("shell.start-menu-broken", "Start menu does not open", "Windows services", "Start, search box or settings fail to open.", ["start menu broken", "start menu not opening"], "Run the App Packages workflow for the StateRepository and shell packages.", "applications.state-repository-service");

        return new SymptomCatalog(SymptomCatalog.CurrentSchemaVersion, CatalogId, 1, WindowsPrimitiveLibrary.Platform, "Windows symptoms", symptoms, trees);

        void Service(string id, string title, string area, string description, string[] aliases, string guidance, params string[] recipeIds) =>
            Add(id, title, area, description, aliases, ServiceChain(recipeIds), guidance);

        void Add(string id, string title, string area, string description, string[] aliases, (IReadOnlyList<TriageNode> Nodes, string Root)? chain, string guidance)
        {
            if (chain is { } tree)
            {
                trees.Add(new TriageTree(id, tree.Root, tree.Nodes) { Title = title });
                symptoms.Add(new Symptom(id, title, area, description) { Aliases = aliases, TreeId = id, Guidance = guidance });
            }
            else symptoms.Add(new Symptom(id, title, area, description) { Aliases = aliases, Guidance = guidance });
        }

        (IReadOnlyList<TriageNode> Nodes, string Root)? ServiceChain(params string[] recipeIds)
        {
            var available = WindowsServicePack.SourceRecipes.Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);
            var unknown = recipeIds.Where(id => !available.Contains(id)).ToArray();
            if (unknown.Length > 0) throw new InvalidOperationException("Symptom catalog references recipes outside the service pack: " + string.Join(", ", unknown));
            var known = recipeIds;
            var nodes = new List<TriageNode>();
            for (int i = 0; i < known.Length; i++)
            {
                string nodeId = "r" + (i + 1);
                string next = i + 1 < known.Length ? "r" + (i + 2) : "handoff";
                nodes.Add(new TriageNode(nodeId, TriageNodeKind.Recipe)
                {
                    RecipeId = known[i],
                    Next = new Dictionary<string, string>
                    {
                        [nameof(RecipeRunOutcome.Clear)] = next,
                        [nameof(RecipeRunOutcome.Repaired)] = next,
                        [nameof(RecipeRunOutcome.RepairAvailable)] = "authorize",
                        [nameof(RecipeRunOutcome.RestartRequired)] = "restart",
                        ["default"] = next
                    },
                    Guidance = "Checks whether " + known[i] + " explains the symptom."
                });
            }
            nodes.Add(new TriageNode("authorize", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman, Guidance = "A repair is available. Run again with repairs authorized to apply it." });
            nodes.Add(new TriageNode("restart", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman, Guidance = "Restart Windows, then run this symptom again." });
            nodes.Add(new TriageNode("handoff", TriageNodeKind.Terminal) { Terminal = TriageTerminal.NeedsHuman, Guidance = "The automated service checks did not resolve the symptom." });
            return (nodes, "r1");
        }
    }
}
