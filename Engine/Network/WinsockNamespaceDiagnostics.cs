using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class WinsockNamespaceDiagnostics
{
    private const int MaximumEntries = 128;
    private const string Name = "Winsock namespace providers";
    private const string Boundary = " Provider names, DLL paths, query contents and application traffic are excluded. Registration and active-state evidence does not prove that a provider can resolve a specific name. No provider, catalog entry, socket, cache, resolver, network setting or service changed.";
    private const string Script = """
        $ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';
        $source=@'
        using System;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        public static class KillerBenchWinsockNamespaceProbe {
          const int SOCKET_ERROR=-1, WSAEFAULT=10014, MaximumEntries=128;
          [StructLayout(LayoutKind.Sequential)] public struct NamespaceInfo { public Guid ProviderId; public uint Namespace; public int Active; public uint Version; public IntPtr Identifier; }
          public sealed class Row { public string provider=""; public uint nameSpace; public bool active; public uint version; }
          public sealed class Result { public bool complete; public int error; public Row[] items=Array.Empty<Row>(); }
          [DllImport("Ws2_32.dll")] static extern int WSAStartup(ushort version,IntPtr data);
          [DllImport("Ws2_32.dll")] static extern int WSACleanup();
          [DllImport("Ws2_32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern int WSAEnumNameSpaceProvidersW(ref int length,IntPtr buffer);
          [DllImport("Ws2_32.dll")] static extern int WSAGetLastError();
          public static Result Read() {
            IntPtr startup=Marshal.AllocHGlobal(512); int initialized=WSAStartup(0x0202,startup);
            try {
              if(initialized!=0)return new Result{complete=false,error=initialized};
              int length=0; int first=WSAEnumNameSpaceProvidersW(ref length,IntPtr.Zero); int error=WSAGetLastError();
              if(first!=SOCKET_ERROR||error!=WSAEFAULT||length<=0)return new Result{complete=false,error=error};
              IntPtr memory=Marshal.AllocHGlobal(length);
              try {
                int count=WSAEnumNameSpaceProvidersW(ref length,memory);
                if(count==SOCKET_ERROR)return new Result{complete=false,error=WSAGetLastError()};
                int size=Marshal.SizeOf(typeof(NamespaceInfo)); var rows=new List<Row>(Math.Min(count,MaximumEntries));
                for(int index=0;index<count&&index<MaximumEntries;index++) {
                  var item=(NamespaceInfo)Marshal.PtrToStructure(IntPtr.Add(memory,index*size),typeof(NamespaceInfo));
                  rows.Add(new Row{provider=item.ProviderId.ToString("D"),nameSpace=item.Namespace,active=item.Active!=0,version=item.Version});
                }
                return new Result{complete=count<=MaximumEntries,error=0,items=rows.ToArray()};
              } finally { Marshal.FreeHGlobal(memory); }
            } finally { if(initialized==0)WSACleanup(); Marshal.FreeHGlobal(startup); }
          }
        }
        '@;
        $null=Add-Type -TypeDefinition $source -Language CSharp;
        [KillerBenchWinsockNamespaceProbe]::Read()|ConvertTo-Json -Depth 4 -Compress
        """;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Winsock namespace-provider inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Winsock namespace provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Winsock namespace provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "Winsock namespace-provider deadline expired.", false) { TimedOut = true });
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
                return Unknown("The native namespace-provider inventory was incomplete, empty or malformed.", evidence);

            var identities = new HashSet<Guid>();
            var namespaces = new HashSet<uint>();
            int active = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 4) || !GuidText(item, "provider", out Guid provider) || !identities.Add(provider) ||
                    !UInt32(item, "nameSpace", out uint nameSpace) || !Boolean(item, "active", out bool isActive) ||
                    !UInt32(item, "version", out _))
                    return Unknown("One or more native namespace-provider records were duplicated, incomplete or malformed.", evidence);
                namespaces.Add(nameSpace);
                if (isActive) active++;
            }

            return new(Name, CheckState.Information,
                $"Windows returned {items.GetArrayLength()} unique namespace-provider registrations across {namespaces.Count} namespace types: {active} active and {items.GetArrayLength() - active} inactive. The native enumeration completed without a structural contradiction." + Boundary,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Namespace-provider evidence could not be interpreted.", evidence);
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
