using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class WinsockCatalogDiagnostics
{
    private const int MaximumEntries = 512;
    private const string Name = "Winsock protocol catalog";
    private const string Boundary = " Protocol names, DLL paths, namespace providers and application traffic are excluded. This structural inventory does not prove that an application can resolve a name or connect to a destination. No catalog entry, provider, socket, network setting or service changed.";
    private const string Script = """
        $ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';
        $source=@'
        using System;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        public static class KillerBenchWinsockCatalogProbe {
          const int SOCKET_ERROR=-1, WSAENOBUFS=10055, MaximumEntries=512;
          [StructLayout(LayoutKind.Sequential)] public struct ProtocolChain { public int ChainLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=7)] public uint[] ChainEntries; }
          [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct ProtocolInfo {
            public uint ServiceFlags1,ServiceFlags2,ServiceFlags3,ServiceFlags4,ProviderFlags;
            public Guid ProviderId; public uint CatalogEntryId; public ProtocolChain Chain;
            public int Version,AddressFamily,MaxSockAddr,MinSockAddr,SocketType,Protocol,ProtocolMaxOffset,NetworkByteOrder,SecurityScheme;
            public uint MessageSize,ProviderReserved;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string ProtocolName;
          }
          public sealed class Row { public uint catalogId; public string provider=""; public int chainLength; public int addressFamily; public int socketType; public int protocol; }
          public sealed class Result { public bool complete; public int error; public Row[] items=Array.Empty<Row>(); }
          [DllImport("Ws2_32.dll")] static extern int WSAStartup(ushort version,IntPtr data);
          [DllImport("Ws2_32.dll")] static extern int WSACleanup();
          [DllImport("Ws2_32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern int WSAEnumProtocolsW(IntPtr protocols,IntPtr buffer,ref int length);
          [DllImport("Ws2_32.dll")] static extern int WSAGetLastError();
          public static Result Read() {
            IntPtr startup=Marshal.AllocHGlobal(512); int initialized=WSAStartup(0x0202,startup);
            try {
              if(initialized!=0)return new Result{complete=false,error=initialized};
              int length=0; int first=WSAEnumProtocolsW(IntPtr.Zero,IntPtr.Zero,ref length); int error=WSAGetLastError();
              if(first!=SOCKET_ERROR||error!=WSAENOBUFS||length<=0)return new Result{complete=false,error=error};
              IntPtr memory=Marshal.AllocHGlobal(length);
              try {
                int count=WSAEnumProtocolsW(IntPtr.Zero,memory,ref length);
                if(count==SOCKET_ERROR)return new Result{complete=false,error=WSAGetLastError()};
                int size=Marshal.SizeOf(typeof(ProtocolInfo)); var rows=new List<Row>(Math.Min(count,MaximumEntries));
                for(int index=0;index<count&&index<MaximumEntries;index++) {
                  var item=(ProtocolInfo)Marshal.PtrToStructure(IntPtr.Add(memory,index*size),typeof(ProtocolInfo));
                  rows.Add(new Row{catalogId=item.CatalogEntryId,provider=item.ProviderId.ToString("D"),chainLength=item.Chain.ChainLen,addressFamily=item.AddressFamily,socketType=item.SocketType,protocol=item.Protocol});
                }
                return new Result{complete=count<=MaximumEntries,error=0,items=rows.ToArray()};
              } finally { Marshal.FreeHGlobal(memory); }
            } finally { if(initialized==0)WSACleanup(); Marshal.FreeHGlobal(startup); }
          }
        }
        '@;
        $null=Add-Type -TypeDefinition $source -Language CSharp;
        [KillerBenchWinsockCatalogProbe]::Read()|ConvertTo-Json -Depth 4 -Compress
        """;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Winsock catalog inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Winsock catalog provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Winsock catalog provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "Winsock catalog deadline expired.", false) { TimedOut = true });
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
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !Int32(root, "error", out int error) ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                !complete || error != 0 || items.GetArrayLength() is 0 or > MaximumEntries)
                return Unknown("The native catalog inventory was incomplete, empty or malformed.", evidence);

            var catalogIds = new HashSet<uint>();
            var providers = new HashSet<Guid>();
            int baseEntries = 0, layeredEntries = 0, dummyEntries = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 6) || !UInt32(item, "catalogId", out uint catalogId) || catalogId == 0 || !catalogIds.Add(catalogId) ||
                    !GuidText(item, "provider", out Guid provider) || !Int32(item, "chainLength", out int chainLength) || chainLength is < 0 or > 7 ||
                    !Int32(item, "addressFamily", out int addressFamily) || addressFamily < 0 ||
                    !Int32(item, "socketType", out int socketType) || socketType < 0 || !Int32(item, "protocol", out _))
                    return Unknown("One or more native catalog records were duplicated, incomplete or malformed.", evidence);
                providers.Add(provider);
                if (chainLength == 0) dummyEntries++;
                else if (chainLength == 1) baseEntries++;
                else layeredEntries++;
            }

            return new(Name, CheckState.Information,
                $"Windows returned {items.GetArrayLength()} protocol catalog entries across {providers.Count} provider identifiers: {baseEntries} base, {layeredEntries} layered and {dummyEntries} dummy-chain entries. The native enumeration completed without a structural contradiction." + Boundary,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Catalog evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Int32(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value);
    }
    private static bool UInt32(JsonElement owner, string name, out uint value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out value);
    }
    private static bool GuidText(JsonElement owner, string name, out Guid value)
    {
        value = Guid.Empty;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            Guid.TryParseExact(item.GetString(), "D", out value) && value != Guid.Empty;
    }
}
