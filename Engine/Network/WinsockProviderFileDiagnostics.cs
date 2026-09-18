using System.Text.Json;

namespace KillerBench.Engine;

public static class WinsockProviderFileDiagnostics
{
    private const int MaximumProviders = 128;
    private static readonly HashSet<string> Signatures = new([
        "Valid", "UnknownError", "NotSigned", "HashMismatch", "NotTrusted", "NotSupportedFileFormat", "Incompatible", "Missing", "Unavailable"
    ], StringComparer.Ordinal);
    private const string Name = "Winsock protocol provider files";
    private const string Boundary = " Full paths, provider names, signer identities and file contents are excluded. This checks only the protocol catalog visible to the current process architecture. It does not load a provider or prove that an application can connect. No file, provider, catalog entry, socket, network setting or service changed.";
    private const string Script = """
        $ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';
        Import-Module ($PSHOME+'\Modules\Microsoft.PowerShell.Security\Microsoft.PowerShell.Security.psd1') -ErrorAction Stop;
        $source=@'
        using System;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        using System.Text;
        public static class KillerBenchWinsockProviderFileProbe {
          const int SOCKET_ERROR=-1, WSAENOBUFS=10055, MaximumEntries=512, MaximumProviders=128;
          [StructLayout(LayoutKind.Sequential)] public struct ProtocolChain { public int ChainLen; [MarshalAs(UnmanagedType.ByValArray,SizeConst=7)] public uint[] ChainEntries; }
          [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct ProtocolInfo {
            public uint ServiceFlags1,ServiceFlags2,ServiceFlags3,ServiceFlags4,ProviderFlags;
            public Guid ProviderId; public uint CatalogEntryId; public ProtocolChain Chain;
            public int Version,AddressFamily,MaxSockAddr,MinSockAddr,SocketType,Protocol,ProtocolMaxOffset,NetworkByteOrder,SecurityScheme;
            public uint MessageSize,ProviderReserved;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string ProtocolName;
          }
          public sealed class Row { public string provider=""; public bool resolved; public string path=""; public int error; }
          public sealed class Result { public bool complete; public int error; public Row[] items=Array.Empty<Row>(); }
          [DllImport("Ws2_32.dll")] static extern int WSAStartup(ushort version,IntPtr data);
          [DllImport("Ws2_32.dll")] static extern int WSACleanup();
          [DllImport("Ws2_32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern int WSAEnumProtocolsW(IntPtr protocols,IntPtr buffer,ref int length);
          [DllImport("Ws2_32.dll",CharSet=CharSet.Unicode)] static extern int WSCGetProviderPath(ref Guid provider,StringBuilder path,ref int length,out int error);
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
                int size=Marshal.SizeOf(typeof(ProtocolInfo)); var ids=new HashSet<Guid>();
                for(int index=0;index<count&&index<MaximumEntries;index++) {
                  var item=(ProtocolInfo)Marshal.PtrToStructure(IntPtr.Add(memory,index*size),typeof(ProtocolInfo));
                  if(item.ProviderId!=Guid.Empty)ids.Add(item.ProviderId);
                }
                var rows=new List<Row>(Math.Min(ids.Count,MaximumProviders));
                foreach(Guid value in ids) {
                  if(rows.Count>=MaximumProviders)break;
                  Guid id=value; int chars=1024; var path=new StringBuilder(chars); int pathError;
                  int result=WSCGetProviderPath(ref id,path,ref chars,out pathError);
                  rows.Add(new Row{provider=id.ToString("D"),resolved=result!=SOCKET_ERROR,path=result==SOCKET_ERROR?"":path.ToString(),error=result==SOCKET_ERROR?pathError:0});
                }
                return new Result{complete=count<=MaximumEntries&&ids.Count<=MaximumProviders,error=0,items=rows.ToArray()};
              } finally { Marshal.FreeHGlobal(memory); }
            } finally { if(initialized==0)WSACleanup(); Marshal.FreeHGlobal(startup); }
          }
        }
        '@;
        $null=Add-Type -TypeDefinition $source -Language CSharp;
        $native=[KillerBenchWinsockProviderFileProbe]::Read();$complete=[bool]$native.complete;$items=@();
        foreach($row in @($native.items)){
          $file='';$exists=$false;$signature='Unavailable';$lookupError=[int]$row.error;
          if($row.resolved){
            try{$path=[Environment]::ExpandEnvironmentVariables([string]$row.path);$file=[IO.Path]::GetFileName($path);if([string]::IsNullOrWhiteSpace($file)){throw 'Provider path has no file name.'};$exists=Test-Path -LiteralPath $path -PathType Leaf;$signature=if($exists){[string](Get-AuthenticodeSignature -LiteralPath $path -ErrorAction Stop).Status}else{'Missing'}}
            catch{$complete=$false;$file='';$exists=$false;$signature='Unavailable';$lookupError=-1}
          }else{$complete=$false}
          $items+=[pscustomobject]@{provider=[string]$row.provider;resolved=[bool]$row.resolved;file=$file;exists=[bool]$exists;signature=$signature;error=$lookupError}
        }
        [pscustomobject]@{complete=($complete-and [int]$native.error-eq 0);items=$items}|ConvertTo-Json -Depth 4 -Compress
        """;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Winsock provider file inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Winsock provider file provider returned no task."))
                .WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Winsock provider file provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "Winsock provider file deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() is 0 or > MaximumProviders)
                return Unknown("The provider file inventory was empty, incomplete or malformed.", evidence);

            var providers = new HashSet<Guid>();
            int missing = 0, signatureReview = 0;
            foreach (var item in items.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(item, 6) || !GuidText(item, "provider", out Guid provider) || !providers.Add(provider) ||
                    !Boolean(item, "resolved", out bool resolved) || !Text(item, "file", 260, allowEmpty: true, out string file) ||
                    !Boolean(item, "exists", out bool exists) || !Text(item, "signature", 32, allowEmpty: false, out string signature) ||
                    !Signatures.Contains(signature) || !Int32(item, "error", out int error) ||
                    !Consistent(resolved, file, exists, signature, error))
                    return Unknown("One or more provider file records were duplicated, incomplete or malformed.", evidence);
                if (!exists) missing++;
                else if (signature != "Valid") signatureReview++;
            }
            if (!complete || items.EnumerateArray().Any(item => !item.GetProperty("resolved").GetBoolean() || item.GetProperty("signature").GetString() == "Unavailable"))
                return Unknown("One or more registered provider paths or files could not be inspected.", evidence);

            string detail = $"Reviewed {items.GetArrayLength()} unique protocol provider files from the native Winsock catalog.";
            if (missing == 0 && signatureReview == 0)
                detail += " Every registered provider file is present and reports a Valid Authenticode status.";
            else
                detail += $" Review: {missing} missing and {signatureReview} present with a non-Valid Authenticode status. Compare installed networking or security software before considering provider recovery.";
            return new(Name, missing == 0 && signatureReview == 0 ? CheckState.Information : CheckState.Attention,
                detail + Boundary, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Provider file evidence could not be interpreted.", evidence);
        }
    }

    private static bool Consistent(bool resolved, string file, bool exists, string signature, int error)
    {
        if (!resolved) return file.Length == 0 && !exists && signature == "Unavailable" && error != 0;
        if (error != 0 || file.Length == 0 || Path.GetFileName(file) != file || file.Any(character => character is '/' or '\\')) return false;
        if (!exists) return signature == "Missing";
        return signature is not ("Missing" or "Unavailable");
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
    private static bool GuidText(JsonElement owner, string name, out Guid value)
    {
        value = Guid.Empty;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            Guid.TryParseExact(item.GetString(), "D", out value) && value != Guid.Empty;
    }
    private static bool Text(JsonElement owner, string name, int maximum, bool allowEmpty, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length > maximum || (!allowEmpty && text.Length == 0) || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
