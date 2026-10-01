using System.Collections.ObjectModel;
using System.Globalization;

namespace KillerBench.Engine.Devices;

public sealed record DeviceProblemCodeEntry(uint Code, string Symbol, string Summary, string NextAction,
    bool RestartMayResolve = false);

public static class DeviceProblemCodeCatalog
{
    private static readonly IReadOnlyDictionary<uint, DeviceProblemCodeEntry> Entries =
        new ReadOnlyDictionary<uint, DeviceProblemCodeEntry>(new Dictionary<uint, DeviceProblemCodeEntry>
        {
            [0] = Entry(0, "CM_PROB_NONE", "Windows reports no device configuration error.", "Feature operation and connectivity still require separate testing."),
            [1] = Driver(1, "CM_PROB_NOT_CONFIGURED", "The device is not configured correctly."),
            [3] = Driver(3, "CM_PROB_OUT_OF_MEMORY", "The driver may be damaged or Windows may lack required memory or resources."),
            [9] = Firmware(9, "CM_PROB_INVALID_DATA", "The device firmware reported invalid resource information."),
            [10] = Driver(10, "CM_PROB_FAILED_START", "The device could not start."),
            [12] = Entry(12, "CM_PROB_NORMAL_CONFLICT", "The device cannot find enough free resources.", "Review resource conflicts and firmware configuration before changing drivers."),
            [14] = Entry(14, "CM_PROB_NEED_RESTART", "Windows requires a restart before the device can work correctly.", "Save work and restart when appropriate, then check the same device again.", true),
            [16] = Entry(16, "CM_PROB_PARTIAL_LOG_CONF", "Windows could not identify every resource used by the device.", "Review Device Manager resources and firmware configuration."),
            [18] = Driver(18, "CM_PROB_REINSTALL", "Windows requests driver reinstallation."),
            [19] = Driver(19, "CM_PROB_REGISTRY", "Device configuration data in the registry is incomplete or damaged."),
            [21] = Entry(21, "CM_PROB_WILL_BE_REMOVED", "Windows is removing the device.", "Wait for removal to finish, then rescan the exact device state."),
            [22] = Entry(22, "CM_PROB_DISABLED", "The device is disabled.", "Confirm whether the disabled state is intentional or managed before enabling it."),
            [24] = Entry(24, "CM_PROB_DEVICE_NOT_THERE", "The device is absent, not working correctly, or lacks required drivers.", "Check its physical or remote-session presence before reviewing the approved driver."),
            [28] = Driver(28, "CM_PROB_FAILED_INSTALL", "Device drivers are not installed."),
            [29] = Firmware(29, "CM_PROB_HARDWARE_DISABLED", "Device firmware reports the device disabled."),
            [31] = Driver(31, "CM_PROB_FAILED_ADD", "Windows could not load the drivers required for the device."),
            [32] = Driver(32, "CM_PROB_DISABLED_SERVICE", "The device driver service is disabled."),
            [33] = Entry(33, "CM_PROB_TRANSLATION_FAILED", "Windows could not translate the device resources.", "Review firmware, resource configuration and the approved vendor driver."),
            [34] = Entry(34, "CM_PROB_NO_SOFTCONFIG", "Windows cannot determine settings for the device.", "Review manual resource configuration and vendor documentation."),
            [35] = Firmware(35, "CM_PROB_BIOS_TABLE", "System firmware lacks information required to configure the device."),
            [36] = Firmware(36, "CM_PROB_IRQ_TRANSLATION_FAILED", "Windows could not translate the device interrupt request."),
            [37] = Driver(37, "CM_PROB_FAILED_DRIVER_ENTRY", "The driver returned a failure from its initialization entry point."),
            [38] = Driver(38, "CM_PROB_DRIVER_FAILED_PRIOR_UNLOAD", "Windows cannot load the driver because a previous instance remains loaded."),
            [39] = Driver(39, "CM_PROB_DRIVER_FAILED_LOAD", "The device driver could not be loaded."),
            [40] = Driver(40, "CM_PROB_DRIVER_SERVICE_KEY_INVALID", "Driver service configuration is invalid or missing."),
            [41] = Driver(41, "CM_PROB_LEGACY_SERVICE_NO_DEVICES", "Windows loaded the driver but cannot find its device."),
            [42] = Entry(42, "CM_PROB_DUPLICATE_DEVICE", "Windows detected a duplicate device instance.", "Review duplicate hardware instances and recent device changes before removing anything."),
            [43] = Entry(43, "CM_PROB_FAILED_POST_START", "The driver reported a device failure and Windows stopped the device.", "Run approved vendor diagnostics and review recent changes; this code alone does not prove hardware failure."),
            [44] = Entry(44, "CM_PROB_HALTED", "An application or service stopped the device.", "Review the owning application or service and rescan after it releases the device."),
            [45] = Entry(45, "CM_PROB_PHANTOM", "The device is not currently connected.", "Check the intended physical or remote-session connection."),
            [46] = Entry(46, "CM_PROB_SYSTEM_SHUTDOWN", "Windows is shutting down and the device is unavailable.", "Recheck after Windows starts normally."),
            [47] = Entry(47, "CM_PROB_HELD_FOR_EJECT", "The device is prepared for safe removal but has not been removed.", "Remove it safely or reconnect it, then rescan."),
            [48] = Driver(48, "CM_PROB_DRIVER_BLOCKED", "Windows blocked the driver because of known compatibility problems."),
            [49] = Entry(49, "CM_PROB_REGISTRY_TOO_LARGE", "The system registry limit prevented the device from starting.", "Review system-wide registry resource pressure before changing this device."),
            [50] = Driver(50, "CM_PROB_SETPROPERTIES_FAILED", "Windows could not apply the device properties."),
            [51] = Entry(51, "CM_PROB_WAITING_ON_DEPENDENCY", "The device is waiting for another device or dependency to start.", "Identify and resolve the exact dependency before changing this device."),
            [52] = Entry(52, "CM_PROB_UNSIGNED_DRIVER", "Windows could not verify the driver's digital signature.", "Obtain a correctly signed approved package; do not disable signature enforcement."),
            [53] = Entry(53, "CM_PROB_USED_BY_DEBUGGER", "The device is reserved for kernel debugging.", "Review the intentional debugging configuration before changing the device."),
            [54] = Entry(54, "CM_PROB_DEVICE_RESET", "The device failed and is being reset.", "Wait for reset completion, then rescan the exact device.", true),
            [55] = Entry(55, "CM_PROB_CONSOLE_LOCKED", "The device configuration is blocked while the console is locked.", "Unlock the console, then rescan the exact device."),
            [56] = Entry(56, "CM_PROB_NEED_CLASS_CONFIG", "Windows is still setting up the device class.", "Allow setup to finish, then rescan before considering broader recovery.", true),
            [57] = Entry(57, "CM_PROB_GUEST_ASSIGNMENT_FAILED", "Windows could not assign the device to the requested guest environment.", "Review virtualization assignment policy and host support."),
        });

    public static IReadOnlyCollection<DeviceProblemCodeEntry> All { get; } = Array.AsReadOnly(Entries.Values.ToArray());

    public static DeviceProblemCodeEntry Find(uint code) => Entries.TryGetValue(code, out var entry) ? entry :
        Entry(code, "CM_PROB_UNKNOWN", "Windows reports an unrecognized device configuration problem.",
            "Retain the numeric code and review it against documentation for this Windows build before selecting a change.");

    public static bool TryParse(string? value, out uint code)
    {
        code = 0;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Any(char.IsControl)) return false;
        bool hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        string digits = hex ? value[2..] : value;
        return digits.Length is > 0 and <= 10 && uint.TryParse(digits,
            hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out code);
    }

    private static DeviceProblemCodeEntry Entry(uint code, string symbol, string summary, string nextAction,
        bool restartMayResolve = false) => new(code, symbol, summary, nextAction, restartMayResolve);

    private static DeviceProblemCodeEntry Driver(uint code, string symbol, string summary) =>
        Entry(code, symbol, summary, "Review the exact hardware identity and obtain the approved vendor driver package.");

    private static DeviceProblemCodeEntry Firmware(uint code, string symbol, string summary) =>
        Entry(code, symbol, summary, "Review system firmware and the approved device-vendor guidance before changing Windows configuration.");
}
