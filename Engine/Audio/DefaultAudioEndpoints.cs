using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace KillerBench.Engine.Audio;

public enum AudioEndpointDirection { Output, Input }
public enum AudioEndpointRole { Console, Multimedia, Communications }
public enum AudioExclusiveMixFormatStatus { Accepted, UnsupportedFormat, NotAllowed }
public enum AudioEndpointFormFactor
{
    RemoteNetworkDevice, Speakers, LineLevel, Headphones, Microphone, Headset, Handset, Unknown,
    DigitalAudioDisplayDevice, SPDIF, DigitalAudioDevice, AnalogAudioDevice
}

public sealed record AudioEndpointFormat(ushort Channels, uint SamplesPerSecond, ushort BitsPerSample);

public sealed record AudioEndpointSelection(AudioEndpointDirection? Direction = null, AudioEndpointRole? Role = null)
{
    public void Validate()
    {
        if (Direction is { } direction && !Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(Direction));
        if (Role is { } role && !Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(Role));
    }
}

public sealed record DefaultAudioEndpoint(AudioEndpointDirection Direction, string Id, string? Name, uint State,
    AudioEndpointFormat? SharedFormat = null, string? FormatError = null, AudioEndpointRole Role = AudioEndpointRole.Multimedia,
    AudioEndpointFormFactor? FormFactor = null, bool? SystemEffectsEnabled = null, bool? EventDrivenModeSupported = null,
    string? PropertyError = null, AudioExclusiveMixFormatStatus? ExclusiveMixFormatStatus = null, string? ExclusiveFormatError = null,
    float? MasterVolumeScalar = null, bool? Muted = null, string? VolumeError = null);

public sealed record DefaultAudioEndpointInventory(IReadOnlyList<DefaultAudioEndpoint> Endpoints, bool Complete, string? Error = null);

public static partial class DefaultAudioEndpoints
{
    private const uint DeviceStateActive = 0x00000001;
    private const uint ClsCtxAll = 23;
    private static readonly StrategyBasedComWrappers ComWrappers = new();
    private static readonly Guid MMDeviceEnumeratorClassId = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid MMDeviceEnumeratorInterfaceId = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private const uint ReadPropertyStore = 0;
    private const uint AllServerContexts = 23;
    private static readonly PropertyKey FriendlyName = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);
    private static readonly PropertyKey FormFactor = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 0);
    private static readonly PropertyKey DisableSystemEffects = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 5);
    private static readonly PropertyKey SupportsEventDrivenMode = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 7);
    private static readonly Guid AudioClientId = new("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
    private static readonly Guid AudioEndpointVolumeId = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
        => await CheckAsync(new(), token).ConfigureAwait(false);

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(AudioEndpointSelection selection, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(selection);
        selection.Validate();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Default audio endpoint inspection requires Windows.");
        token.ThrowIfCancellationRequested();
        DefaultAudioEndpointInventory inventory = await Task.Run(Read, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return Describe(inventory, selection);
    }

    public static IReadOnlyList<WorkflowCheck> Describe(DefaultAudioEndpointInventory inventory)
        => Describe(inventory, new());

    public static IReadOnlyList<WorkflowCheck> Describe(DefaultAudioEndpointInventory inventory, AudioEndpointSelection selection)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(selection);
        selection.Validate();
        if (!inventory.Complete)
            return [new("Default audio endpoints", CheckState.Unknown,
                "Windows default input and output endpoint inspection was incomplete. " + (inventory.Error ?? "No endpoint evidence was returned.") +
                " Selected devices, formats, levels and application routing were not changed.")];

        var checks = new List<WorkflowCheck>();
        var directions = selection.Direction is { } selectedDirection ? [selectedDirection] : Enum.GetValues<AudioEndpointDirection>();
        var rolesToInspect = selection.Role is { } selectedRole ? [selectedRole] : Enum.GetValues<AudioEndpointRole>();
        foreach (AudioEndpointDirection direction in directions)
        {
            var directional = inventory.Endpoints.Where(endpoint => endpoint.Direction == direction &&
                (selection.Role is null || endpoint.Role == selection.Role)).ToArray();
            var missingRoles = rolesToInspect.Where(role =>
            {
                var matches = directional.Where(endpoint => endpoint.Role == role).ToArray();
                return matches.Length != 1 || string.IsNullOrWhiteSpace(matches[0].Id);
            }).ToArray();
            if (missingRoles.Length > 0)
            {
                checks.Add(new("Default audio " + direction.ToString().ToLowerInvariant(), CheckState.Unknown,
                    "Windows did not return one complete default " + direction.ToString().ToLowerInvariant() +
                    " endpoint identity for: " + string.Join(", ", missingRoles) +
                    ". Review endpoint availability and user selection. Other retained role evidence remains valid. No audio setting was changed."));
            }
            var valid = directional.Where(endpoint => !string.IsNullOrWhiteSpace(endpoint.Id) &&
                directional.Count(other => other.Role == endpoint.Role) == 1).ToArray();
            foreach (var group in valid.GroupBy(endpoint => endpoint.Id, StringComparer.OrdinalIgnoreCase))
            {
                DefaultAudioEndpoint endpoint = group.First();
                bool validVolume = endpoint.MasterVolumeScalar is null ||
                    float.IsFinite(endpoint.MasterVolumeScalar.Value) && endpoint.MasterVolumeScalar is >= 0 and <= 1;
                bool consistent = group.All(value => value.State == endpoint.State && value.SharedFormat == endpoint.SharedFormat && value.FormFactor == endpoint.FormFactor &&
                    value.SystemEffectsEnabled == endpoint.SystemEffectsEnabled && value.EventDrivenModeSupported == endpoint.EventDrivenModeSupported &&
                    value.ExclusiveMixFormatStatus == endpoint.ExclusiveMixFormatStatus &&
                    value.MasterVolumeScalar == endpoint.MasterVolumeScalar && value.Muted == endpoint.Muted &&
                    string.Equals(value.VolumeError, endpoint.VolumeError, StringComparison.Ordinal) &&
                    string.Equals(value.ExclusiveFormatError, endpoint.ExclusiveFormatError, StringComparison.Ordinal) &&
                    string.Equals(value.PropertyError, endpoint.PropertyError, StringComparison.Ordinal) &&
                    string.Equals(value.Name, endpoint.Name, StringComparison.Ordinal));
                CheckState state = !consistent || !validVolume || !string.IsNullOrWhiteSpace(endpoint.PropertyError)
                    ? CheckState.Unknown
                    : endpoint.State == DeviceStateActive ? CheckState.Information : CheckState.Attention;
                string label = string.IsNullOrWhiteSpace(endpoint.Name) ? "Name unavailable" : endpoint.Name.Trim();
                string roles = string.Join(", ", group.Select(value => value.Role).OrderBy(role => role).Distinct());
                string form = endpoint.FormFactor is null ? "Form factor unavailable. " : "Form factor: " + FormFactorName(endpoint.FormFactor.Value) + ". ";
                string format = endpoint.SharedFormat is null
                    ? "Shared format unavailable" + (string.IsNullOrWhiteSpace(endpoint.FormatError) ? ". " : ": " + endpoint.FormatError.Trim() + ". ")
                    : $"Shared format: {endpoint.SharedFormat.Channels} {(endpoint.SharedFormat.Channels == 1 ? "channel" : "channels")}, {endpoint.SharedFormat.SamplesPerSecond} Hz, {endpoint.SharedFormat.BitsPerSample} bits. ";
                string effects = endpoint.SystemEffectsEnabled is bool effectsEnabled
                    ? "Shared-mode system effects: " + (effectsEnabled ? "enabled. " : "disabled. ")
                    : "Shared-mode system effects: not reported. ";
                string eventMode = endpoint.EventDrivenModeSupported is bool eventDriven
                    ? "Event-driven mode support: " + (eventDriven ? "reported. " : "not reported by the endpoint. ")
                    : "Event-driven mode support: unavailable. ";
                string exclusive = endpoint.ExclusiveMixFormatStatus switch
                {
                    AudioExclusiveMixFormatStatus.Accepted => "Exclusive-mode request for the current shared mix format: accepted by the endpoint. ",
                    AudioExclusiveMixFormatStatus.UnsupportedFormat => "Exclusive-mode request for the current shared mix format: format not accepted by the endpoint. ",
                    AudioExclusiveMixFormatStatus.NotAllowed => "Exclusive mode: not allowed by the current endpoint policy. ",
                    _ => "Exclusive-mode request for the current shared mix format: unavailable" +
                        (string.IsNullOrWhiteSpace(endpoint.ExclusiveFormatError) ? ". " : ": " + endpoint.ExclusiveFormatError.Trim() + ". ")
                };
                string volume = validVolume
                    ? "Endpoint master volume: " + (endpoint.MasterVolumeScalar is float scalar
                        ? Math.Round(scalar * 100, MidpointRounding.AwayFromZero).ToString("0") + "%"
                        : "unavailable") + "; mute: " + (endpoint.Muted is bool muted ? muted ? "on" : "off" : "unavailable") + ". " +
                        (string.IsNullOrWhiteSpace(endpoint.VolumeError) ? "" : "Endpoint volume evidence was incomplete: " + endpoint.VolumeError.Trim() + ". ")
                    : "Endpoint master volume evidence was invalid. ";
                string propertyIssue = string.IsNullOrWhiteSpace(endpoint.PropertyError) ? "" : "Endpoint property evidence was incomplete: " + endpoint.PropertyError.Trim() + ". ";
                checks.Add(new("Default audio " + direction.ToString().ToLowerInvariant() + (valid.Select(value => value.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1 ? " (" + roles + ")" : ""), state,
                    $"{label}. Roles: {roles}. Endpoint ID: {endpoint.Id}. Windows state: 0x{endpoint.State:X8}. " + form + format + effects + eventMode + exclusive + volume + propertyIssue +
                    (!consistent
                        ? "Windows returned conflicting observations for this endpoint identity. Recollect before choosing a repair."
                        : !validVolume
                            ? "Windows did not return a valid normalized endpoint volume. Recollect before choosing a repair."
                        : !string.IsNullOrWhiteSpace(endpoint.PropertyError)
                            ? "Windows did not return valid documented endpoint property values. Recollect before choosing a repair."
                        : state == CheckState.Information
                            ? "Windows reports the endpoint active. This capability result does not establish that an application can open the endpoint or that playback or recording works. Per-channel levels, live signal activity and application routing remain untested."
                            : "Windows does not report the selected default endpoint active. Review device presence and the selected endpoint before restarting services or changing drivers.") +
                    " No audio setting was changed."));
            }
        }
        return checks.AsReadOnly();
    }

    [SupportedOSPlatform("windows")]
    private static DefaultAudioEndpointInventory Read()
    {
        var endpoints = new List<DefaultAudioEndpoint>(6);
        try
        {
            Marshal.ThrowExceptionForHR(CoCreateInstance(in MMDeviceEnumeratorClassId, IntPtr.Zero, ClsCtxAll,
                in MMDeviceEnumeratorInterfaceId, out IntPtr enumeratorPointer));
            IMMDeviceEnumerator enumerator;
            try { enumerator = (IMMDeviceEnumerator)ComWrappers.GetOrCreateObjectForComInstance(enumeratorPointer, CreateObjectFlags.UniqueInstance); }
            finally { Marshal.Release(enumeratorPointer); }
            try
            {
                foreach (AudioEndpointRole role in Enum.GetValues<AudioEndpointRole>())
                {
                    endpoints.Add(ReadEndpoint(enumerator, DataFlow.Render, AudioEndpointDirection.Output, role));
                    endpoints.Add(ReadEndpoint(enumerator, DataFlow.Capture, AudioEndpointDirection.Input, role));
                }
            }
            finally { ReleaseCom(enumerator); }
            return new(endpoints.AsReadOnly(), true);
        }
        catch (Exception error) when (error is COMException or InvalidCastException or ArgumentException)
        {
            return new(endpoints.AsReadOnly(), false, error.Message);
        }
    }

    [SupportedOSPlatform("windows")]
    private static DefaultAudioEndpoint ReadEndpoint(IMMDeviceEnumerator enumerator, DataFlow flow, AudioEndpointDirection direction, AudioEndpointRole role)
    {
        Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(flow, (NativeRole)role, out IMMDevice device));
        try
        {
            Marshal.ThrowExceptionForHR(device.GetId(out string id));
            Marshal.ThrowExceptionForHR(device.GetState(out uint state));
            string? name = null;
            AudioEndpointFormFactor? formFactor = null;
            bool? systemEffectsEnabled = null;
            bool? eventDrivenModeSupported = null;
            string? propertyError = null;
            if (device.OpenPropertyStore(ReadPropertyStore, out IPropertyStore store) >= 0)
            {
                try
                {
                    if (store.GetValue(FriendlyName, out PropVariant value) >= 0)
                    {
                        try { name = value.StringValue; }
                        finally { _ = PropVariantClear(ref value); } // Clearing only releases the value's memory; a failure leaves nothing to recover.
                    }
                    if (store.GetValue(FormFactor, out PropVariant formValue) >= 0)
                    {
                        try
                        {
                            uint? raw = formValue.UInt32Value;
                            if (raw <= (uint)AudioEndpointFormFactor.AnalogAudioDevice)
                                formFactor = (AudioEndpointFormFactor)raw.Value;
                        }
                        finally { _ = PropVariantClear(ref formValue); }
                    }
                    if (store.GetValue(DisableSystemEffects, out PropVariant effectsValue) >= 0)
                    {
                        try
                        {
                            uint? raw = effectsValue.UInt32Value;
                            if (raw is 0 or 1) systemEffectsEnabled = raw == 0;
                            else if (raw is not null) propertyError = "Unsupported system-effects value " + raw.Value;
                        }
                        finally { _ = PropVariantClear(ref effectsValue); }
                    }
                    if (store.GetValue(SupportsEventDrivenMode, out PropVariant eventValue) >= 0)
                    {
                        try
                        {
                            uint? raw = eventValue.UInt32Value;
                            if (raw is 0 or 1) eventDrivenModeSupported = raw == 1;
                            else if (raw is not null) propertyError = AppendPropertyError(propertyError, "Unsupported event-driven-mode value " + raw.Value);
                        }
                        finally { _ = PropVariantClear(ref eventValue); }
                    }
                }
                finally { ReleaseCom(store); }
            }
            AudioEndpointFormat? format = null;
            AudioExclusiveMixFormatStatus? exclusiveMixFormatStatus = null;
            string? formatError = null;
            string? exclusiveFormatError = null;
            float? masterVolumeScalar = null;
            bool? muted = null;
            string? volumeError = null;
            try
            {
                var client = ReadAudioClientFormat(device);
                format = client.Format;
                exclusiveMixFormatStatus = client.ExclusiveMixFormatStatus;
                exclusiveFormatError = client.ExclusiveFormatError;
            }
            catch (Exception error) when (error is COMException or InvalidCastException or ArgumentException or InvalidDataException)
            { formatError = error.Message; }
            try
            {
                var volume = ReadEndpointVolume(device);
                masterVolumeScalar = volume.MasterVolumeScalar;
                muted = volume.Muted;
            }
            catch (Exception error) when (error is COMException or InvalidCastException or ArgumentException or InvalidDataException)
            { volumeError = error.Message; }
            return new(direction, id, name, state, format, formatError, role, formFactor, systemEffectsEnabled,
                eventDrivenModeSupported, propertyError, exclusiveMixFormatStatus, exclusiveFormatError,
                masterVolumeScalar, muted, volumeError);
        }
        finally { ReleaseCom(device); }
    }

    private static void ReleaseCom(object comObject)
    {
        if (comObject is ComObject wrapper) wrapper.FinalRelease();
    }

    private static string AppendPropertyError(string? current, string next) =>
        string.IsNullOrWhiteSpace(current) ? next : current + "; " + next;

    private static string FormFactorName(AudioEndpointFormFactor value) => value switch
    {
        AudioEndpointFormFactor.RemoteNetworkDevice => "remote network device",
        AudioEndpointFormFactor.LineLevel => "line level",
        AudioEndpointFormFactor.DigitalAudioDisplayDevice => "digital display audio",
        AudioEndpointFormFactor.SPDIF => "S/PDIF",
        AudioEndpointFormFactor.DigitalAudioDevice => "digital audio device",
        AudioEndpointFormFactor.AnalogAudioDevice => "analog audio device",
        _ => value.ToString().ToLowerInvariant()
    };

    [SupportedOSPlatform("windows")]
    private sealed record AudioClientFormatResult(AudioEndpointFormat Format, AudioExclusiveMixFormatStatus? ExclusiveMixFormatStatus,
        string? ExclusiveFormatError);

    [SupportedOSPlatform("windows")]
    private static AudioClientFormatResult ReadAudioClientFormat(IMMDevice device)
    {
        Guid interfaceId = AudioClientId;
        Marshal.ThrowExceptionForHR(device.Activate(ref interfaceId, AllServerContexts, IntPtr.Zero, out IntPtr instance));
        try
        {
            var client = (IAudioClient)ComWrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
            try
            {
                Marshal.ThrowExceptionForHR(client.GetMixFormat(out IntPtr formatPointer));
                try
                {
                    WaveFormat format = Marshal.PtrToStructure<WaveFormat>(formatPointer);
                    if (format.Channels == 0 || format.SamplesPerSecond == 0 || format.BitsPerSample == 0)
                        throw new InvalidDataException("Windows returned an incomplete shared audio format.");
                    AudioExclusiveMixFormatStatus? exclusiveStatus = null;
                    string? exclusiveError = null;
                    IntPtr closest = IntPtr.Zero;
                    int result = client.IsFormatSupported(1, formatPointer, out closest);
                    try
                    {
                        if (result == 0) exclusiveStatus = AudioExclusiveMixFormatStatus.Accepted;
                        else if (result == unchecked((int)0x88890008)) exclusiveStatus = AudioExclusiveMixFormatStatus.UnsupportedFormat;
                        else if (result == unchecked((int)0x8889000E)) exclusiveStatus = AudioExclusiveMixFormatStatus.NotAllowed;
                        else exclusiveError = $"Windows Audio Client returned 0x{result:X8}";
                    }
                    finally { if (closest != IntPtr.Zero) CoTaskMemFree(closest); }
                    return new(new(format.Channels, format.SamplesPerSecond, format.BitsPerSample),
                        exclusiveStatus, exclusiveError);
                }
                finally { CoTaskMemFree(formatPointer); }
            }
            finally { ReleaseCom(client); }
        }
        finally { Marshal.Release(instance); }
    }

    private sealed record AudioEndpointVolumeResult(float MasterVolumeScalar, bool Muted);

    [SupportedOSPlatform("windows")]
    private static AudioEndpointVolumeResult ReadEndpointVolume(IMMDevice device)
    {
        Guid interfaceId = AudioEndpointVolumeId;
        Marshal.ThrowExceptionForHR(device.Activate(ref interfaceId, AllServerContexts, IntPtr.Zero, out IntPtr instance));
        try
        {
            var volume = (IAudioEndpointVolume)ComWrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
            try
            {
                Marshal.ThrowExceptionForHR(volume.GetMasterVolumeLevelScalar(out float scalar));
                if (!float.IsFinite(scalar) || scalar is < 0 or > 1)
                    throw new InvalidDataException("Windows returned an invalid normalized endpoint volume.");
                Marshal.ThrowExceptionForHR(volume.GetMute(out int muted));
                if (muted is not 0 and not 1)
                    throw new InvalidDataException("Windows returned an invalid endpoint mute state.");
                return new(scalar, muted == 1);
            }
            finally { ReleaseCom(volume); }
        }
        finally { Marshal.Release(instance); }
    }

    internal enum DataFlow { Render, Capture, All }
    internal enum NativeRole { Console, Multimedia, Communications }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct PropVariant
    {
        [FieldOffset(0)] private readonly ushort valueType;
        [FieldOffset(8)] private readonly IntPtr pointerValue;
        [FieldOffset(8)] private readonly uint uint32Value;
        internal readonly string? StringValue => valueType == 31 ? Marshal.PtrToStringUni(pointerValue) : null;
        internal readonly uint? UInt32Value => valueType == 19 ? uint32Value : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct WaveFormat
    {
        public readonly ushort FormatTag;
        public readonly ushort Channels;
        public readonly uint SamplesPerSecond;
        public readonly uint AverageBytesPerSecond;
        public readonly ushort BlockAlign;
        public readonly ushort BitsPerSample;
        public readonly ushort ExtraSize;
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    internal partial interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(DataFlow dataFlow, uint stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(DataFlow dataFlow, NativeRole role, out IMMDevice endpoint);
        [PreserveSig] int GetDevice(string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    internal partial interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid interfaceId, uint context, IntPtr activationParameters, out IntPtr instance);
        [PreserveSig] int OpenPropertyStore(uint access, out IPropertyStore properties);
        [PreserveSig] int GetId(out string id);
        [PreserveSig] int GetState(out uint state);
    }

    [GeneratedComInterface, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    internal partial interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(in PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(in PropertyKey key, in PropVariant value);
        [PreserveSig] int Commit();
    }

    [GeneratedComInterface, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
    internal partial interface IAudioClient
    {
        [PreserveSig] int Initialize(uint shareMode, uint streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr sessionGuid);
        [PreserveSig] int GetBufferSize(out uint bufferFrames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint paddingFrames);
        [PreserveSig] int IsFormatSupported(uint shareMode, IntPtr format, out IntPtr closestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid interfaceId, out IntPtr service);
    }

    [GeneratedComInterface, Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    internal partial interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, IntPtr eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, IntPtr eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, IntPtr eventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, IntPtr eventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute(int muted, IntPtr eventContext);
        [PreserveSig] int GetMute(out int muted);
        [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
        [PreserveSig] int VolumeStepUp(IntPtr eventContext);
        [PreserveSig] int VolumeStepDown(IntPtr eventContext);
        [PreserveSig] int QueryHardwareSupport(out uint hardwareSupportMask);
        [PreserveSig] int GetVolumeRange(out float minimumDb, out float maximumDb, out float incrementDb);
    }

    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(ref PropVariant value);

    [LibraryImport("ole32.dll")]
    private static partial void CoTaskMemFree(IntPtr value);

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid classId, IntPtr outer, uint classContext, in Guid interfaceId, out IntPtr instance);
}
