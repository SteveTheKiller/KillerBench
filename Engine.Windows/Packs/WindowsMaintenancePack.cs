using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Procedures and targeted repairs that need explicit confirmation and, mostly, an option naming the exact target.</summary>
public static class WindowsMaintenancePack
{
    public const string PackId = "windows.maintenance";

    private static readonly IReadOnlyDictionary<string, string> None = new Dictionary<string, string>();
    private const string ProcedureNote = "This is a procedure rather than a diagnosed fault; it runs only when explicitly confirmed.";

    public static RecipePack Build() => new(RecipePack.CurrentSchemaVersion, PackId, 1, WindowsPrimitiveLibrary.Platform, "Windows maintenance procedures and targeted repairs",
    [
        new RecipeDefinition("shell.quick-access-reset", 1, "Reset Quick Access history", "Windows",
            "The AutomaticDestinations folder holds no jump-list files immediately after the reset; Explorer recreates them",
            new ProbeSpec("file.count", new Dictionary<string, string> { ["folder"] = "recent-automatic-destinations" }, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear) { Detail = "No jump-list files exist; there is nothing to reset." },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "Jump-list files exist. " + ProcedureNote }
            ])) { Name = "Jump-list files" })
        {
            Symptoms = ["shell.quick-access-broken"],
            Action = new ActionSpec("file.retain-folder", new Dictionary<string, string> { ["folder"] = "recent-automatic-destinations" })
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Moves the current account's jump-list files to a retained folder under the local KillerBench data directory. Pinned Quick Access entries and recent-file history are rebuilt from scratch." },
            Risk = RecipeRisk.Persistent,
            RequiresExplicitOptions = true,
            Limitations = "Corruption is not detectable in advance; the reset is requested, not diagnosed."
        },
        new RecipeDefinition("printing.spool-clear", 1, "Clear the print spool", "Printing",
            "The spool folder is empty and the Print Spooler is running afterwards; physical printing is untested",
            new ProbeSpec("file.count", new Dictionary<string, string> { ["folder"] = "spool-printers" }, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear) { Detail = "The spool folder is empty." },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "Spool files exist. " + ProcedureNote }
            ])) { Name = "Spool files" })
        {
            Symptoms = ["printing.spooler-crashes"],
            Action = new ActionSpec("spooler.clear-spool", None)
                { Timeout = TimeSpan.FromSeconds(60), Effects = "Stops the Print Spooler, moves every spool file to a retained folder under the local KillerBench data directory, and starts the spooler. Every queued job disappears from its queue." },
            Verify = new ProbeSpec("service.running", new Dictionary<string, string> { ["name"] = "Spooler" }, ExpectSpec.ClearWhenEquals("Running", ProbeVerdict.Attention)) { Name = "Spooler running" },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "A driver that crashes the spooler will crash it again on the next job; the PrintService events in the Printing workflow name the driver."
        },
        new RecipeDefinition("network.mtu-set", 1, "Set an interface MTU", "Network",
            "The interface reports the requested MTU afterwards; path MTU and application behavior are untested",
            new ProbeSpec("netip.mtu", new Dictionary<string, string> { ["name"] = "${interface}", ["family"] = "${family}" }, new ExpectSpec(
            [
                new ExpectClause("less-than", "1280", ProbeVerdict.Repairable) { Detail = "The MTU is below 1280 bytes, which breaks IPv6 and many VPN paths." },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "The MTU is readable. " + ProcedureNote }
            ])) { Name = "Interface MTU" })
        {
            Symptoms = ["network.mtu-black-hole"],
            Options =
            [
                new RecipeOption("interface", "Exact interface alias") { Pattern = WindowsNetAdapterPrimitives.AdapterNamePattern },
                new RecipeOption("family", "IPv6 or IPv4") { Pattern = "IPv6|IPv4", Default = "IPv6", Required = false },
                new RecipeOption("mtu", "MTU in bytes, 1280 through 9000") { Pattern = "12[8-9][0-9]|1[3-9][0-9]{2}|[2-8][0-9]{3}|9000", Default = "1500", Required = false }
            ],
            Action = new ActionSpec("netip.mtu-set", new Dictionary<string, string> { ["name"] = "${interface}", ["family"] = "${family}", ["mtu"] = "${mtu}" })
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Sets the interface MTU in the active and persistent stores." },
            Verify = new ProbeSpec("netip.mtu", new Dictionary<string, string> { ["name"] = "${interface}", ["family"] = "${family}" }, ExpectSpec.ClearWhenEquals("${mtu}", ProbeVerdict.Attention)) { Name = "Interface MTU" },
            Risk = RecipeRisk.Persistent,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "The prior MTU is journaled in the detect evidence; there is no automatic rollback."
        },
        new RecipeDefinition("devices.problem-devices", 1, "Devices reporting a problem", "Devices",
            "Present devices whose PnP status is not OK are listed; drivers are not changed",
            new ProbeSpec("pnp.problem-devices", None, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "Present devices report a problem. Restarting one with the device restart recipe and its instance id reloads the driver; a persistent code needs a driver package from the vendor." }
            ])) { Name = "Problem devices" })
        {
            Symptoms = ["devices.problem-code"],
            Limitations = "Hidden and absent devices are not listed."
        },
        new RecipeDefinition("devices.restart-device", 1, "Restart one device", "Devices",
            "The named device reports OK after the restart; driver correctness is untested",
            new ProbeSpec("pnp.device-status", new Dictionary<string, string> { ["instance"] = "${instance}" }, new ExpectSpec(
            [
                new ExpectClause("equals", "OK", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "The device reports a problem; a restart re-enumerates it and reloads its driver." }
            ])) { Name = "Device status" })
        {
            Symptoms = ["devices.problem-code"],
            Options = [new RecipeOption("instance", "Exact device instance id") { Pattern = WindowsDeviceStatusProbe.InstancePattern }],
            Action = new ActionSpec("pnp.restart-device", new Dictionary<string, string> { ["instance"] = "${instance}" })
                { Timeout = TimeSpan.FromSeconds(60), Effects = "Runs pnputil /restart-device for the exact instance. The device disappears briefly; a restart of Windows may be required." },
            Verify = new ProbeSpec("pnp.device-status", new Dictionary<string, string> { ["instance"] = "${instance}" }, ExpectSpec.ClearWhenEquals("OK", ProbeVerdict.Attention)) { Name = "Device status" },
            Risk = RecipeRisk.Reversible,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            MayRequireRestart = true,
            Limitations = "Restarting a storage or display device can interrupt the session; choose the instance carefully."
        },
        new RecipeDefinition("storage.unlettered-volumes", 1, "Formatted volumes without a drive letter", "Storage",
            "Data partitions with a file system and no letter are listed; nothing is changed",
            new ProbeSpec("volume.unlettered", None, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "A formatted volume has no drive letter, so it does not appear in File Explorer. The assign letter recipe can give it one with the disk, partition and letter options." }
            ])) { Name = "Unlettered volumes" })
        {
            Symptoms = ["storage.usb-drive-not-mounting"],
            Limitations = "Recovery, EFI and hidden partitions are excluded on purpose; an unformatted disk is not listed."
        },
        new RecipeDefinition("storage.assign-letter", 1, "Assign a drive letter", "Storage",
            "The exact partition reports a drive letter afterwards; file access is untested",
            new ProbeSpec("volume.unlettered", None, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear) { Detail = "Every formatted data volume already has a letter." },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "A formatted volume lacks a letter. " + ProcedureNote }
            ])) { Name = "Unlettered volumes" })
        {
            Symptoms = ["storage.usb-drive-not-mounting"],
            Options =
            [
                new RecipeOption("disk", "Disk number from the unlettered volumes evidence") { Pattern = "[0-9]{1,3}" },
                new RecipeOption("partition", "Partition number from the unlettered volumes evidence") { Pattern = "[0-9]{1,3}" },
                new RecipeOption("letter", "Free drive letter D through Z") { Pattern = "[D-Zd-z]" }
            ],
            Action = new ActionSpec("volume.assign-letter", new Dictionary<string, string> { ["disk"] = "${disk}", ["partition"] = "${partition}", ["letter"] = "${letter}" })
                { Timeout = TimeSpan.FromSeconds(30), Effects = "Runs Set-Partition -NewDriveLetter for the exact disk and partition. A letter already in use is refused." },
            Risk = RecipeRisk.Reversible,
            RequiresElevation = true,
            RequiresExplicitOptions = true,
            Limitations = "Verification only confirms no unlettered data volume remains; a second unlettered volume reports Attention."
        },
        new RecipeDefinition("apps.package-reregister", 1, "Re-register an installed app package", "Applications",
            "The exact package reports Ok for the current account afterwards; app behavior is untested",
            new ProbeSpec("appx.status", new Dictionary<string, string> { ["package"] = "${package}" }, new ExpectSpec(
            [
                new ExpectClause("equals", "Ok", ProbeVerdict.Repairable) { Detail = "The package reports Ok; re-registration is still possible when the app fails to launch. " + ProcedureNote },
                new ExpectClause("any", "", ProbeVerdict.Repairable) { Detail = "The package reports a problem; re-registering it from its install location rebuilds the registration." }
            ])) { Name = "Package status" })
        {
            Symptoms = ["store.package-corrupt"],
            Options = [new RecipeOption("package", "Exact package full name from Get-AppxPackage") { Pattern = WindowsAppxStatusProbe.FullNamePattern }],
            Action = new ActionSpec("appx.reregister", new Dictionary<string, string> { ["package"] = "${package}" })
                { Timeout = TimeSpan.FromMinutes(2), Effects = "Runs Add-AppxPackage -Register on the package's AppxManifest.xml for the current account. Files are not downloaded or replaced." },
            Verify = new ProbeSpec("appx.status", new Dictionary<string, string> { ["package"] = "${package}" }, ExpectSpec.ClearWhenEquals("Ok", ProbeVerdict.Attention)) { Name = "Package status" },
            Risk = RecipeRisk.Persistent,
            RequiresExplicitOptions = true,
            Limitations = "Only the current account's registration changes; a damaged install location needs a reinstall from the Store."
        },
        new RecipeDefinition("power.wake-timers", 1, "Active wake timers", "Power",
            "powercfg wake timers only; battery drain from other causes is not measured",
            new ProbeSpec("powercfg.wake-timers", None, new ExpectSpec(
            [
                new ExpectClause("equals", "0", ProbeVerdict.Clear),
                new ExpectClause("any", "", ProbeVerdict.Attention) { Detail = "Active wake timers can wake the PC from sleep and drain the battery. The evidence names their owners; scheduled tasks and update maintenance are the usual sources." }
            ])) { Name = "Wake timers" })
        {
            Symptoms = ["power.battery-drains"],
            RequiresElevation = true,
            Limitations = "Wake timers are one cause among several; a battery report covers the rest."
        }
    ]);
}
