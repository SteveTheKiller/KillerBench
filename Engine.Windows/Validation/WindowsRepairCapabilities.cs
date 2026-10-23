using System.Security.Cryptography;
using System.Text;

namespace KillerBench.Engine.Windows.Validation;

/// <summary>Supplies the Windows catalogs to the platform-neutral capability inventory.</summary>
public static class WindowsRepairCapabilities
{
    private static readonly string[] ExplicitCommands =
    [
        "restore-point", "defender-scan", "bits-retry", "update-store-reset", "search-index-rebuild", "previous-windows",
        "event-channel-enable", "msi-repair", "installer-registration-repair", "task-register", "mace-package-plan",
        "dns-cache", "dhcp-renew", "name-rename", "storage-repair", "volume-optimize", "printer-repair", "print-job-cancel",
        "default-printer-set", "time-configure", "profiles", "orca-cache-reset", "cleanup", "privacy-clipboard-history",
        "privacy-clipboard-history-undo", "privacy-setting", "privacy-setting-undo", "bitlocker-backup", "bitlocker-resume",
        "bitlocker-escrow", "bitlocker-protector-add", "bitlocker-decrypt", "bitlocker-auto-unlock", "bitlocker-encrypt",
        "firewall-enable", "update-plan"
    ];

    public static IReadOnlyList<ExplicitRepairCapability> Explicit { get; } = Array.AsReadOnly(ExplicitCommands
        .Select(command => new ExplicitRepairCapability("command." + command, 1, command,
            "Only the selected command's documented verification scope; complete native scenario acceptance remains required.",
            [command, "desktop/" + command]))
        .Concat(new[]
        {
            CreateExplicit("repair-dism", "repair/dism", "desktop/repair-dism"),
            CreateExplicit("repair-sfc", "repair/sfc", "desktop/repair-sfc"),
            CreateExplicit("debloat-high-performance", "debloat-review/--apply-high-performance", "desktop/debloat-high-performance"),
            CreateExplicit("debloat-power-timers", "debloat-review/--apply-power-timers", "desktop/debloat-power-timers"),
            CreateExplicit("debloat-hibernation", "debloat-review/--disable-hibernation", "desktop/debloat-hibernation"),
            CreateExplicit("mace-package-removal", "desktop/mace-package-remove")
        }).ToArray());

    private static ExplicitRepairCapability CreateExplicit(string id, params string[] entryPoints) => new("command." + id, 1, id,
        "Only the guarded operation's documented verification scope; complete native scenario acceptance remains required.", entryPoints);

    private static readonly Lazy<IReadOnlyList<RepairCapability>> Current = new(() => Build());
    public static IReadOnlyList<RepairCapability> All => Current.Value;
    public static RepairCapability? Find(string id) => All.FirstOrDefault(item => item.Id == id);
    public static string LabelFor(string id) => Find(id)?.Label ?? RepairCapabilityInventory.Label(NativeCapabilityStatus.NotYetVerified);
    public static IReadOnlyList<RepairCapability> ForCommand(IReadOnlyList<string> args) => args.Count == 0 ? [] :
        All.Where(capability => capability.Implementations.Any(implementation => implementation.EntryPoints.Any(entry =>
            entry == args[0] || args.Skip(1).Any(argument => entry == args[0] + "/" + argument)))).ToArray();

    public static IReadOnlyList<RepairCapability> Build(string? evidenceDirectory = null) => RepairCapabilityInventory.Build(
        WindowsPacks.All(), BinaryFingerprint(), Explicit, evidenceDirectory ?? Path.Combine(AppContext.BaseDirectory, "NativeValidation"));

    public static IReadOnlyList<RepairCapability> FromRecords(IReadOnlyList<string> recordFiles) =>
        RepairCapabilityInventory.Build(WindowsPacks.All(), BinaryFingerprint(), Explicit, evidenceFiles: recordFiles);

    /// <summary>Hashes the exact shared engine images. Frontend guards, physical UI and signed packages require separate validation.</summary>
    public static string BinaryFingerprint()
    {
        var assemblies = new[] { typeof(RepairCapabilityInventory).Assembly, typeof(RecipePack).Assembly, typeof(WindowsRepairCapabilities).Assembly };
        var hashes = assemblies.Select(assembly =>
        {
            using var stream = SharedImplementationImages.Open(assembly);
            return assembly.GetName().Name + ":" + Convert.ToHexString(SHA256.HashData(stream));
        }).Order(StringComparer.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", hashes))));
    }
}
