using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public static class FileHistoryStatus
{
    private const string Name = "File History protection state";
    private const string Boundary = " This is the current-account state reported by the deprecated Windows File History configuration interface. The protected-until timestamp, target identity, configuration paths, protected items and credentials are excluded. It does not verify destination contents or restore success. No backup, file, schedule, retention setting, target, policy or service changed.";
    private const string Script = """
        $ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';
        $source=@'
        using System;
        using System.Runtime.InteropServices;
        [ComImport,Guid("6A5FEA5B-BF8F-4EE5-B8C3-44D8A0D7331C"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IFhConfigMgr {
         [PreserveSig]int LoadConfiguration();
         [PreserveSig]int CreateDefaultConfiguration([MarshalAs(UnmanagedType.Bool)]bool overwrite);
         [PreserveSig]int SaveConfiguration();
         [PreserveSig]int AddRemoveExcludeRule([MarshalAs(UnmanagedType.Bool)]bool add,int category,[MarshalAs(UnmanagedType.BStr)]string item);
         [PreserveSig]int GetIncludeExcludeRules([MarshalAs(UnmanagedType.Bool)]bool include,int category,out IntPtr iterator);
         [PreserveSig]int GetLocalPolicy(int type,out ulong value);
         [PreserveSig]int SetLocalPolicy(int type,ulong value);
         [PreserveSig]int GetBackupStatus(out int status);
         [PreserveSig]int SetBackupStatus(int status);
         [PreserveSig]int GetDefaultTarget(out IntPtr target);
         [PreserveSig]int ValidateTarget([MarshalAs(UnmanagedType.BStr)]string url,out int result);
         [PreserveSig]int ProvisionAndSetNewTarget([MarshalAs(UnmanagedType.BStr)]string url,[MarshalAs(UnmanagedType.BStr)]string name);
         [PreserveSig]int ChangeDefaultTargetRecommendation([MarshalAs(UnmanagedType.Bool)]bool recommend);
         [PreserveSig]int QueryProtectionStatus(out uint state,[MarshalAs(UnmanagedType.BStr)]out string protectedUntil);
        }
        public static class KillerBenchFileHistoryProbe {
         public static string Run() {
          object value=null;
          try {
           Type type=Type.GetTypeFromCLSID(new Guid("ED43BB3C-09E9-498A-9DF6-2177244C6DB4"),true);
           value=Activator.CreateInstance(type);IFhConfigMgr manager=(IFhConfigMgr)value;
           int load=manager.LoadConfiguration();if(load<0)return Result(false,load,null,null);
           uint state;string ignored;int query=manager.QueryProtectionStatus(out state,out ignored);
           return query<0?Result(false,load,query,null):Result(true,load,query,state);
          } finally {if(value!=null&&Marshal.IsComObject(value))Marshal.FinalReleaseComObject(value);}
         }
         static string Number(int value){return ((uint)value).ToString(System.Globalization.CultureInfo.InvariantCulture);}
         static string Result(bool complete,int? load,int? query,uint? state){return "{\"apiAvailable\":true,\"complete\":"+(complete?"true":"false")+",\"loadResult\":"+(load.HasValue?Number(load.Value):"null")+",\"queryResult\":"+(query.HasValue?Number(query.Value):"null")+",\"state\":"+(state.HasValue?state.Value.ToString(System.Globalization.CultureInfo.InvariantCulture):"null")+"}";}
        }
        '@;
        try{$null=Add-Type -TypeDefinition $source -Language CSharp;[KillerBenchFileHistoryProbe]::Run()}
        catch{if($_.Exception.HResult-eq-2147221164){'{"apiAvailable":false,"complete":true,"loadResult":null,"queryResult":null,"state":null}'}else{'{"apiAvailable":true,"complete":false,"loadResult":null,"queryResult":null,"state":null}'}}
        """;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("File History protection-state review requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("File History status provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("File History status provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "File History status deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 5) || !Boolean(root, "apiAvailable", out bool available) ||
                !Boolean(root, "complete", out bool complete) || !NullableUInt32(root, "loadResult", out uint? loadResult) ||
                !NullableUInt32(root, "queryResult", out uint? queryResult) || !NullableUInt32(root, "state", out uint? state))
                return Unknown("Evidence was malformed.", evidence);
            if (!available)
            {
                if (!complete || loadResult is not null || queryResult is not null || state is not null)
                    return Unknown("Interface-availability evidence was contradictory.", evidence);
                return new(Name, CheckState.Information,
                    "The File History configuration interface is not registered on this Windows installation." + Boundary,
                    Evidence: evidence);
            }
            if (!complete)
            {
                if (state is not null || loadResult is 0 && queryResult is 0)
                    return Unknown("Incomplete provider evidence was contradictory.", evidence);
                string result = queryResult is not null ? Hex(queryResult.Value) : loadResult is not null ? Hex(loadResult.Value) : "unavailable";
                return Unknown("The File History interface did not return a complete state. Provider result: " + result + ".", evidence);
            }
            if (loadResult != 0 || queryResult != 0 || state is null)
                return Unknown("Successful provider evidence was contradictory.", evidence);
            bool running = (state.Value & 0x100u) != 0;
            uint baseState = state.Value & ~0x100u;
            if (running && baseState is 0x00 or 0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x810)
                return Unknown("The reported running flag contradicted the protection state.", evidence);
            string runningText = running ? " A backup cycle is currently reported as running." : " No running backup cycle was reported.";
            return baseState switch
            {
                0x00 => Info("The current account is not tracked by File History, or its service is not started. This does not distinguish those conditions."),
                0x01 => Info("File History protection is off for the current account."),
                0x02 => Info("File History protection is disabled by Group Policy. The managed setting is preserved."),
                0x03 => Attention("File History reports a fatal configuration error for the current account."),
                0x04 => Info("File History reports that configuration migration is in progress."),
                0x05 => Info("File History reports that configuration rehydration is in progress."),
                0x0D => Attention("File History reports that the selected target filesystem cannot provide required capabilities."),
                0x0E => Attention("File History reports that the current account cannot write to the selected target."),
                0x0F => Attention("File History reports that the selected target volume is marked dirty."),
                0x10 => Attention("File History reports that its target is full and retention is already at the most aggressive policy."),
                0x11 => Attention("File History reports that its target is full."),
                0x12 => Attention("File History reports that its local staging area is full."),
                0x13 => Attention("File History reports low target space while retention is already at the most aggressive policy."),
                0x14 => Attention("File History reports low space on its target."),
                0x15 => Attention("File History reports that its target has been unavailable long enough to degrade protection."),
                0x16 or 0xF0 => Attention("File History reports that protected-file changes are too far behind for normal background cycles."),
                0xFF => new(Name, CheckState.Clear, "File History reports no current protection error." + runningText + Boundary, Evidence: evidence),
                0x810 => Attention("File History reports that backup is no longer supported for an existing configuration."),
                _ => Unknown("The provider returned an unknown state " + Hex(state.Value) + ".", evidence)
            };

            WorkflowCheck Info(string detail) => new(Name, CheckState.Information, detail + runningText + Boundary, Evidence: evidence);
            WorkflowCheck Attention(string detail) => new(Name, CheckState.Attention, detail + runningText + " Review the current configuration and target evidence before selecting any change." + Boundary, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static string Hex(uint value) => "0x" + value.ToString("X8", CultureInfo.InvariantCulture);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool NullableUInt32(JsonElement owner, string name, out uint? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetUInt32(out uint number)) return false;
        value = number;
        return true;
    }
}
