using KillerBench.Engine;

internal static class DefaultAudioEndpointChecks
{
    internal static void Run()
    {
        var active = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "output-id", "Fixture speakers", 1, new(2, 48000, 32), Role: AudioEndpointRole.Console, FormFactor: AudioEndpointFormFactor.Speakers, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.Accepted, MasterVolumeScalar: 0.42f, Muted: false),
            new(AudioEndpointDirection.Output, "output-id", "Fixture speakers", 1, new(2, 48000, 32), Role: AudioEndpointRole.Multimedia, FormFactor: AudioEndpointFormFactor.Speakers, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.Accepted, MasterVolumeScalar: 0.42f, Muted: false),
            new(AudioEndpointDirection.Output, "output-id", "Fixture speakers", 1, new(2, 48000, 32), Role: AudioEndpointRole.Communications, FormFactor: AudioEndpointFormFactor.Speakers, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.Accepted, MasterVolumeScalar: 0.42f, Muted: false),
            new(AudioEndpointDirection.Input, "input-id", "Fixture microphone", 1, new(1, 44100, 16), Role: AudioEndpointRole.Console, FormFactor: AudioEndpointFormFactor.Microphone, SystemEffectsEnabled: false, EventDrivenModeSupported: false, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.UnsupportedFormat, MasterVolumeScalar: 0f, Muted: true),
            new(AudioEndpointDirection.Input, "input-id", "Fixture microphone", 1, new(1, 44100, 16), Role: AudioEndpointRole.Multimedia, FormFactor: AudioEndpointFormFactor.Microphone, SystemEffectsEnabled: false, EventDrivenModeSupported: false, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.UnsupportedFormat, MasterVolumeScalar: 0f, Muted: true),
            new(AudioEndpointDirection.Input, "input-id", "Fixture microphone", 1, new(1, 44100, 16), Role: AudioEndpointRole.Communications, FormFactor: AudioEndpointFormFactor.Microphone, SystemEffectsEnabled: false, EventDrivenModeSupported: false, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.UnsupportedFormat, MasterVolumeScalar: 0f, Muted: true)
        ], true));
        if (active.Count != 2 || active.Any(check => check.State != CheckState.Information || check.Repair is not null) ||
            !active[0].Detail.Contains("output-id", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("2 channels, 48000 Hz, 32 bits", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Form factor: speakers", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Shared-mode system effects: enabled", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Event-driven mode support: reported", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Exclusive-mode request for the current shared mix format: accepted", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Endpoint master volume: 42%; mute: off", StringComparison.Ordinal) ||
            !active[0].Detail.Contains("Console, Multimedia, Communications", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("Fixture microphone", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("Shared-mode system effects: disabled", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("Event-driven mode support: not reported", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("Exclusive-mode request for the current shared mix format: format not accepted", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("Endpoint master volume: 0%; mute: on", StringComparison.Ordinal) ||
            !active[1].Detail.Contains("does not establish that an application can open the endpoint", StringComparison.Ordinal))
            throw new Exception("Default audio endpoint identity, shared format, state or repair isolation was lost.");

        var exclusiveNotAllowed = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "output-id", "Fixture speakers", 1, new(2, 48000, 32),
                Role: AudioEndpointRole.Multimedia, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.NotAllowed)
        ], true), new(AudioEndpointDirection.Output, AudioEndpointRole.Multimedia));
        if (exclusiveNotAllowed.Count != 1 || exclusiveNotAllowed[0].State != CheckState.Information ||
            !exclusiveNotAllowed[0].Detail.Contains("Exclusive mode: not allowed by the current endpoint policy", StringComparison.Ordinal) ||
            exclusiveNotAllowed[0].Repair is not null)
            throw new Exception("Exclusive-mode policy refusal was not preserved as read-only evidence.");

        var unavailable = DefaultAudioEndpoints.Describe(new([], false, "Fixture provider failure"));
        if (unavailable.Count != 1 || unavailable[0].State != CheckState.Unknown ||
            !unavailable[0].Detail.Contains("Fixture provider failure", StringComparison.Ordinal))
            throw new Exception("Incomplete default audio endpoint evidence claimed coverage.");

        var partial = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "output-id", null, 8, FormatError: "Fixture format failure")
        ], true));
        if (partial.Count != 3 || partial.Count(check => check.State == CheckState.Unknown) != 2 ||
            !partial.Any(check => check.State == CheckState.Attention && check.Detail.Contains("0x00000008", StringComparison.Ordinal) &&
                check.Detail.Contains("Fixture format failure", StringComparison.Ordinal)) || partial.Any(check => check.Repair is not null))
            throw new Exception("Inactive or missing default audio endpoints lost honest classification.");

        var ambiguous = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "first", "First", 1, Role: AudioEndpointRole.Multimedia),
            new(AudioEndpointDirection.Output, "second", "Second", 1, Role: AudioEndpointRole.Multimedia),
            new(AudioEndpointDirection.Input, "input", "Input", 1, Role: AudioEndpointRole.Multimedia)
        ], true));
        if (!ambiguous.Any(check => check.State == CheckState.Unknown && check.Detail.Contains("Multimedia", StringComparison.Ordinal)))
            throw new Exception("Ambiguous default audio endpoint identity was accepted.");

        var splitRoles = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "speakers", "Speakers", 1, Role: AudioEndpointRole.Console),
            new(AudioEndpointDirection.Output, "speakers", "Speakers", 1, Role: AudioEndpointRole.Multimedia),
            new(AudioEndpointDirection.Output, "headset", "Headset", 1, Role: AudioEndpointRole.Communications),
            new(AudioEndpointDirection.Input, "microphone", "Microphone", 1, Role: AudioEndpointRole.Console),
            new(AudioEndpointDirection.Input, "microphone", "Microphone", 1, Role: AudioEndpointRole.Multimedia),
            new(AudioEndpointDirection.Input, "headset-mic", "Headset microphone", 1, Role: AudioEndpointRole.Communications)
        ], true));
        if (splitRoles.Count != 4 || !splitRoles.Any(check => check.Name.Contains("Communications", StringComparison.Ordinal)) ||
            splitRoles.Any(check => check.State != CheckState.Information))
            throw new Exception("Distinct communications endpoints were not identified by role.");

        var conflictingForm = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Console, FormFactor: AudioEndpointFormFactor.Speakers),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Multimedia, FormFactor: AudioEndpointFormFactor.Headphones),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Communications, FormFactor: AudioEndpointFormFactor.Speakers)
        ], true));
        if (!conflictingForm.Any(check => check.State == CheckState.Unknown && check.Detail.Contains("conflicting", StringComparison.Ordinal)))
            throw new Exception("Conflicting endpoint form-factor evidence was accepted.");

        var conflictingProperties = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Console, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.Accepted),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Multimedia, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.UnsupportedFormat),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Communications, SystemEffectsEnabled: true, EventDrivenModeSupported: true, ExclusiveMixFormatStatus: AudioExclusiveMixFormatStatus.Accepted)
        ], true));
        if (!conflictingProperties.Any(check => check.State == CheckState.Unknown && check.Detail.Contains("conflicting", StringComparison.Ordinal)))
            throw new Exception("Conflicting endpoint processing-property evidence was accepted.");

        var invalidVolume = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Multimedia, MasterVolumeScalar: 1.5f, Muted: false)
        ], true), new(AudioEndpointDirection.Output, AudioEndpointRole.Multimedia));
        if (invalidVolume.Count != 1 || invalidVolume[0].State != CheckState.Unknown ||
            !invalidVolume[0].Detail.Contains("master volume evidence was invalid", StringComparison.Ordinal) || invalidVolume[0].Repair is not null)
            throw new Exception("Invalid normalized endpoint volume evidence was accepted.");

        var invalidProperties = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Console, PropertyError: "Unsupported system-effects value 7"),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Multimedia, PropertyError: "Unsupported system-effects value 7"),
            new(AudioEndpointDirection.Output, "same", "Same", 1, Role: AudioEndpointRole.Communications, PropertyError: "Unsupported system-effects value 7")
        ], true));
        if (!invalidProperties.Any(check => check.State == CheckState.Unknown &&
            check.Detail.Contains("Unsupported system-effects value 7", StringComparison.Ordinal) && check.Repair is null))
            throw new Exception("Invalid endpoint processing-property evidence was accepted.");

        var selected = DefaultAudioEndpoints.Describe(new([
            new(AudioEndpointDirection.Output, "console-output", "Console output", 1, Role: AudioEndpointRole.Console),
            new(AudioEndpointDirection.Output, "communications-output", "Communications output", 1, Role: AudioEndpointRole.Communications),
            new(AudioEndpointDirection.Input, "console-input", "Console input", 1, Role: AudioEndpointRole.Console)
        ], true), new(AudioEndpointDirection.Output, AudioEndpointRole.Console));
        if (selected.Count != 1 || !selected[0].Detail.Contains("console-output", StringComparison.Ordinal) ||
            selected[0].Detail.Contains("communications-output", StringComparison.Ordinal) || selected[0].Name.Contains("input", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Exact direction and role selection did not limit endpoint evidence.");

        try { new AudioEndpointSelection((AudioEndpointDirection)99).Validate(); throw new Exception("Invalid endpoint direction was accepted."); }
        catch (ArgumentOutOfRangeException) { }
        try { new AudioEndpointSelection(Role: (AudioEndpointRole)99).Validate(); throw new Exception("Invalid endpoint role was accepted."); }
        catch (ArgumentOutOfRangeException) { }

        Console.WriteLine("PASS: default audio selection, roles, form factors, shared and exclusive-format capability, volume, mute, documented processing properties, states, incomplete evidence and repair isolation.");
    }
}
