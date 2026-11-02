using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KillerBench.Engine.Profiles;
using Microsoft.Win32;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class ProfileCancellationNativeAcceptanceChecks
{
    private const string GuestParent = @"C:\KillerLab\bench-resume-20261101";
    private const string FileName = "synthetic-512MiB.bin";
    private const string IdentityPropertyAccess = "Properties_.Item(name,0).Value";
    private sealed class BlockedException(string message, object? evidence = null) : Exception(message)
    { internal object? Evidence { get; } = evidence; }
    private sealed record NativeProcess(int? Pid, int? Parent, string? Executable, string? CommandLine, string? Created, string[] IncompleteFields)
    { public bool Complete => IncompleteFields.Length == 0; }

    // The caller must use NativeAcceptancePackage.RunAsync to pin the package and runtime first.
    internal static async Task RunAsync()
    {
        string directory = RequireGuest();
        NativeProcess[] baseline = await Task.Run(QueryRobocopy).WaitAsync(TimeSpan.FromSeconds(10));
        if (baseline.Length != 0)
            throw new BlockedException("Baseline contains Robocopy. " + IncompleteSummary(baseline) + " No fixture writes or cancellation were performed.");
        string root = Path.Combine(directory, "profile-cancellation-" + Guid.NewGuid().ToString("N"));
        CreateProtectedDirectory(root);
        string outcome = "Failed", detail = "The cancellation case did not finish.";
        object? evidence = null;
        var diagnostics = new ProcessDiagnostics();
        try
        {
            evidence = await ObserveCancellationAsync(root, diagnostics).ConfigureAwait(false);
            outcome = "Passed";
            detail = "Cancellation was requested while the exact owned Robocopy was live; it had exited when CopyAsync returned.";
        }
        catch (BlockedException error) { outcome = "Blocked"; detail = error.Message; evidence = error.Evidence; }
        catch (Exception error)
        {
            detail = error.GetType().Name + ": " + error.Message;
            string diagnostic = error.ToString();
            evidence = new
            {
                hResult = error.HResult,
                exception = diagnostic[..Math.Min(diagnostic.Length, 32768)],
                exceptionTruncated = diagnostic.Length > 32768
            };
        }
        string report = Path.Combine(root, "cancellation-result.json");
        await using (var stream = new FileStream(report, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, new
            {
                schemaVersion = 1, selected = 1, outcome, detail, evidence, processDiagnostics = diagnostics.Snapshot(),
                baselineRobocopyCount = baseline.Length, syntheticDataOnly = true, identityPropertyAccess = IdentityPropertyAccess,
                cleanup = "Preserve all fixtures and evidence for coordinator snapshot restoration. No recursive cleanup or ACL relaxation."
            }, new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);
        Console.WriteLine(outcome.ToUpperInvariant() + ": profile cancellation: " + detail);
        Console.WriteLine("PROFILE_CANCELLATION_RESULT: " + report);
        if (outcome != "Passed") throw new InvalidOperationException("Native cancellation acceptance did not pass: " + outcome + ": " + detail);
    }

    private static async Task<object> ObserveCancellationAsync(string root, ProcessDiagnostics diagnostics)
    {
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "backup");
        CreateProtectedDirectory(source);
        string file = Path.Combine(source, FileName);
        byte[] block = new byte[1024 * 1024];
        for (int i = 0; i < block.Length; i++) block[i] = (byte)((i * 31 + (i >> 8) + 17) & 255);
        string writtenHash;
        using (var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            await using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                block.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
            for (int i = 0; i < 512; i++)
            {
                await stream.WriteAsync(block).ConfigureAwait(false);
                digest.AppendData(block);
            }
            stream.Flush(flushToDisk: true);
            writtenHash = Convert.ToHexString(digest.GetHashAndReset());
        }
        string before = await HashAsync(file).ConfigureAwait(false);
        Require(before == writtenHash && new FileInfo(file).Length == 512L * 1024 * 1024,
            "The independently read source differs from the 512 MiB of newly written deterministic bytes.");
        Require(!Directory.Exists(destination) && !File.Exists(destination), "The fresh backup destination exists.");
        var target = new ProfileSnapshot("S-1-5-21-1-2-3-1001", source, false, false, DateTimeOffset.UtcNow.AddDays(-120), false);
        var host = new WindowsProfileBackupHost();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var watcherStop = new CancellationTokenSource();
        var watcherReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var copySettled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NativeProcess? observed = null;
        Process? heldProcess = null;
        int incompleteObservations = 0;
        var incompleteFields = new HashSet<string>(StringComparer.Ordinal);
        void ObserveIncomplete(IEnumerable<NativeProcess> rows)
        {
            lock (incompleteFields)
            {
                foreach (var row in rows.Where(row => !row.Complete))
                {
                    incompleteObservations++;
                    foreach (string field in row.IncompleteFields) incompleteFields.Add(field);
                }
            }
        }
        bool liveAtCancellation = false, canceled = false, exitedAtReturn = false;
        Task watcher = Task.Run(async () =>
        {
            diagnostics.CaptureControl("before-copy");
            watcherReady.TrySetResult();
            try
            {
            var watch = Stopwatch.StartNew();
            while (!watcherStop.IsCancellationRequested && watch.Elapsed < TimeSpan.FromSeconds(45))
            {
                NativeProcess[] rows = QueryRobocopy(diagnostics);
                ObserveIncomplete(rows);
                if (watcherStop.IsCancellationRequested) return;
                foreach (var row in rows.Where(row => IsOwned(row, source, destination)))
                {
                    var process = Process.GetProcessById(row.Pid!.Value);
                    try
                    {
                        _ = process.Handle;
                        // Bind the held PID to the same live management identity before canceling.
                        var repeated = QueryRobocopy(diagnostics);
                        ObserveIncomplete(repeated);
                        if (process.HasExited || !repeated.Any(current => SameCompleteIdentity(current, row))) continue;
                        if (watcherStop.IsCancellationRequested) return;
                        heldProcess = process; process = null!;
                        observed = row;
                        liveAtCancellation = !heldProcess.HasExited;
                        if (!liveAtCancellation) return;
                        cancellation.Cancel();
                        return;
                    }
                    finally { process?.Dispose(); }
                }
                await Task.Delay(5, watcherStop.Token).ConfigureAwait(false);
            }
            if (!watcherStop.IsCancellationRequested) cancellation.Cancel();
            }
            finally
            {
                await copySettled.Task.ConfigureAwait(false);
                diagnostics.CaptureControl("after-copy");
            }
        });
        await watcherReady.Task.ConfigureAwait(false);
        Exception? copyError = null;
        bool copyReturned = false;
        try
        {
            diagnostics.EmitPhase(DiagnosticPhase.BeforeCopyInvocation);
            Task copy = host.CopyAsync(target, destination, null, cancellation.Token);
            diagnostics.SetCopyTask(copy);
            diagnostics.EmitPhase(DiagnosticPhase.CopyTaskReturned);
            try { await AwaitProtectedCopyAsync(copy, cancellation.Cancel, TimeSpan.FromSeconds(90)).ConfigureAwait(false); }
            catch (OperationCanceledException) { canceled = true; }
            catch (Exception error) { copyError = error; }
            copyReturned = copyError is not TimeoutException;
            // Observe termination before any subsequent hashing, artifact reading or delay.
            exitedAtReturn = heldProcess is not null && heldProcess.HasExited;
            diagnostics.EmitPhase(DiagnosticPhase.CopyAwaitSettled);
        }
        finally
        {
            copySettled.TrySetResult();
            watcherStop.Cancel();
            cancellation.Cancel();
            diagnostics.EmitPhase(DiagnosticPhase.WatcherWaitEnter);
            try { await watcher.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); diagnostics.EmitPhase(DiagnosticPhase.WatcherWaitReturn); }
            catch (OperationCanceledException) { diagnostics.EmitPhase(DiagnosticPhase.WatcherWaitCanceled); }
            catch (TimeoutException)
            {
                diagnostics.EmitPhase(DiagnosticPhase.WatcherWaitTimeout);
                lock (incompleteFields)
                {
                    throw new BlockedException("The read-only management watcher did not finish within its bound. Containment is unverified; restore the snapshot after coordinator reconciliation. " +
                        "Incomplete observations=" + incompleteObservations + "; fields=" + string.Join(",", incompleteFields.Order()),
                        new { incompleteObservations, incompleteFields = incompleteFields.Order().ToArray() });
                }
            }
        }
        using var observedLease = heldProcess;
        if (copyError is not null) throw new InvalidOperationException("CopyAsync failed before cancellation acceptance could be established. Containment must be reviewed.", copyError);
        if (observed is null || !liveAtCancellation)
            throw new BlockedException("No exact owned live Robocopy was observed before copying completed or the deadline. This timing-sensitive observation is not a pass. " +
                "Incomplete observations=" + incompleteObservations + "; fields=" + string.Join(",", incompleteFields.Order()),
                new { incompleteObservations, incompleteFields = incompleteFields.Order().ToArray() });
        Require(copyReturned && canceled && exitedAtReturn, "Cancellation did not throw OperationCanceledException after the observed process exited.");
        var remaining = await Task.Run(() => QueryRobocopy(diagnostics)).WaitAsync(TimeSpan.FromSeconds(10));
        ObserveIncomplete(remaining);
        if (remaining.Any(row => !row.Complete))
            throw new BlockedException("Postflight cannot establish absence of owned Robocopy. " + IncompleteSummary(remaining),
                new { incompleteObservations, incompleteFields = incompleteFields.Order().ToArray() });
        Require(!remaining.Any(row => IsOwned(row, source, destination)), "An exact owned Robocopy remains after CopyAsync returned.");
        string after = await HashAsync(file).ConfigureAwait(false);
        Require(before == after, "Cancellation changed the synthetic source bytes.");
        var proof = await host.VerifyAsync(target, destination, CancellationToken.None).ConfigureAwait(false);
        Require(!proof.Verified && proof.Receipt is null, "Canceled copying exposed completed proof.");
        string receipt = Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName);
        Require(!File.Exists(receipt), "A cancellation during native copying unexpectedly published a completed receipt.");
        Require(Directory.Exists(destination), "The observed native copy did not retain its reserved destination for containment review.");
        string dacl = ReadContainedDacl(destination);
        return new
        {
            bytes = 512L * 1024 * 1024, sourceSha256Before = before, sourceSha256After = after,
            observed, liveAtCancellation, exitedAtCopyReturn = exitedAtReturn,
            operationCanceled = canceled, remainingOwnedRobocopyCount = 0,
            incompleteObservations, incompleteFields = incompleteFields.Order().ToArray(),
            completedProof = proof.Verified, receiptAbsent = true, partialRootDacl = dacl,
            observedProcessStartUtc = observedLease!.StartTime.ToUniversalTime()
        };
    }

    internal static async Task AwaitProtectedCopyAsync(Task copy, Action requestCancellation, TimeSpan deadline)
    {
        try { await copy.WaitAsync(deadline).ConfigureAwait(false); }
        catch (TimeoutException timeout)
        {
            // A deadline is a failed observation, never permission to release protection around an active copy.
            try { requestCancellation(); }
            catch { /* Still wait for the owned copy if a cancellation callback fails. */ }
            try { await copy.ConfigureAwait(false); }
            catch { /* Completion errors cannot turn the original deadline failure into success. */ }
            // An outer owned guest process-tree watchdog is the final bound if native termination stalls.
            throw new TimeoutException("The protected copy exceeded its deadline. Cancellation was requested and the underlying copy has now completed.", timeout);
        }
    }

    private static bool IsOwned(NativeProcess row, string source, string destination)
    {
        if (!row.Complete || row.Parent != Environment.ProcessId || !row.Executable!.Equals(Path.Combine(Environment.SystemDirectory, "robocopy.exe"), StringComparison.OrdinalIgnoreCase)) return false;
        string[] args = Arguments(row.CommandLine!);
        return args.Length >= 4 && args[0].Equals(row.Executable, StringComparison.OrdinalIgnoreCase) &&
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[1])).Equals(source, StringComparison.OrdinalIgnoreCase) &&
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[2])).Equals(destination, StringComparison.OrdinalIgnoreCase) &&
            args[3] == FileName;
    }

    private static bool SameCompleteIdentity(NativeProcess left, NativeProcess right) =>
        left.Complete && right.Complete && left.Pid == right.Pid && left.Parent == right.Parent &&
        left.Executable == right.Executable && left.CommandLine == right.CommandLine && left.Created == right.Created;

    private static string IncompleteSummary(IEnumerable<NativeProcess> rows)
    {
        var incomplete = rows.Where(row => !row.Complete).ToArray();
        return "Incomplete rows=" + incomplete.Length + "; fields=" +
            string.Join(",", incomplete.SelectMany(row => row.IncompleteFields).Distinct().Order()) + ".";
    }

    private static NativeProcess ReadNativeProcess(Func<string, object?> read) => ReadNativeProcessObserved(read, null);

    private static NativeProcess ReadNativeProcessObserved(Func<string, object?> read, Action<FieldDiagnostic>? observe)
    {
        var incomplete = new List<string>();
        object? Field(string name)
        {
            try
            {
                object? value = read(name);
                observe?.Invoke(DescribeField(name, value, null));
                return value;
            }
            catch (Exception error) { observe?.Invoke(DescribeField(name, null, error)); incomplete.Add(name); return null; }
        }
        int? Number(string name)
        {
            object? value = Field(name);
            int? result = value switch { int number when number > 0 => number, uint number when number is > 0 and <= int.MaxValue => (int)number, _ => null };
            if (result is null && !incomplete.Contains(name)) incomplete.Add(name);
            return result;
        }
        string? Text(string name, int maximum)
        {
            object? value = Field(name);
            if (value is string text && text.Length > 0 && text.Length <= maximum) return text;
            if (!incomplete.Contains(name)) incomplete.Add(name);
            return null;
        }
        int? pid = Number("ProcessId"), parent = Number("ParentProcessId");
        string? executable = Text("ExecutablePath", 32767), command = Text("CommandLine", 32767), created = Text("CreationDate", 64);
        return new(pid, parent, executable, command, created, incomplete.ToArray());
    }

    private static NativeProcess[] QueryRobocopy() => QueryRobocopy(null);

    private static NativeProcess[] QueryRobocopy(ProcessDiagnostics? diagnostics)
    {
        object? locator = null, services = null, result = null;
        var rows = new List<NativeProcess>();
        bool supplementalQuery = false;
        try
        {
            locator = Activator.CreateInstance(Type.GetTypeFromProgID("WbemScripting.SWbemLocator", throwOnError: true)!);
            services = ((dynamic)locator!).ConnectServer(".", @"root\cimv2");
            result = ((dynamic)services).ExecQuery("SELECT ProcessId, ParentProcessId, ExecutablePath, CommandLine, CreationDate FROM Win32_Process WHERE Name='robocopy.exe'", "WQL", 0);
            foreach (object item in (System.Collections.IEnumerable)result)
            {
                bool supplementalRow = false;
                try
                {
                    if (rows.Count >= 64) throw new BlockedException("Robocopy management inventory exceeds its 64-process bound.");
                    var typed = new List<FieldDiagnostic>();
                    var identity = ReadNativeProcessObserved(name => ReadStandardProperty(item, name), typed.Add);
                    rows.Add(identity);
                    bool sample = !identity.Complete && diagnostics is not null && diagnostics.TryReserveSample();
                    if (sample) diagnostics!.EmitPhase(DiagnosticPhase.IdentityFieldsCollected, typed.ToArray());
                    if (diagnostics is not null)
                    {
                        diagnostics.RecordOutcomes(typed.ToArray());
                        if (sample)
                        {
                            supplementalRow = supplementalQuery = true;
                            diagnostics.EmitPhase(DiagnosticPhase.ComparisonGettersEnter);
                            var comparisonDirect = DiagnosticFields(name => ReadDirectProperty(item, name));
                            diagnostics.EmitPhase(DiagnosticPhase.ComparisonGettersReturn);
                            diagnostics.CompleteSample(new
                            {
                                elapsedMilliseconds = diagnostics.ElapsedMilliseconds,
                                copyTaskIsCompleted = diagnostics.CopyTaskIsCompleted,
                                strictComplete = identity.Complete,
                                authoritative = new { source = IdentityPropertyAccess, fields = typed.ToArray() },
                                comparison = new { source = "direct dynamic property access", ownership = false, fields = comparisonDirect },
                                correlation = Correlation(item),
                                security = ReadSecurity(services)
                            });
                            diagnostics.CaptureControl("first-incomplete");
                        }
                    }
                }
                finally
                {
                    if (supplementalRow) diagnostics!.EmitPhase(DiagnosticPhase.RowReleaseEnter);
                    Release(item);
                    if (supplementalRow) diagnostics!.EmitPhase(DiagnosticPhase.RowReleaseReturn);
                }
            }
            return rows.ToArray();
        }
        finally
        {
            if (supplementalQuery) diagnostics!.EmitPhase(DiagnosticPhase.QueryReleaseEnter);
            Release(result); Release(services); Release(locator);
            if (supplementalQuery) diagnostics!.EmitPhase(DiagnosticPhase.QueryReleaseReturn);
        }
        static void Release(object? value) { if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
    }

    private sealed record FieldDiagnostic(string Field, string Outcome, string? ClrType, string? ExceptionType, string? HResult, string? Message, int? MessageLength);
    private static readonly string[] DiagnosticFieldNames = ["ProcessId", "ParentProcessId", "ExecutablePath", "CommandLine", "CreationDate"];

    private static FieldDiagnostic DescribeField(string name, object? value, Exception? error)
    {
        static string Bound(string text) => text[..Math.Min(text.Length, 128)];
        if (error is not null)
            return new(name, "getter-exception", null, Bound(error.GetType().FullName ?? error.GetType().Name),
                "0x" + error.HResult.ToString("X8"),
                error.Message is "Unspecified error" or "Unspecified error." ? error.Message : "Provider message omitted for privacy.",
                Math.Min(error.Message.Length, 32768));
        if (value is null) return new(name, "null", null, null, null, null, null);
        if (value is DBNull) return new(name, "DBNull", typeof(DBNull).FullName, null, null, null, null);
        string type = Bound(value.GetType().FullName ?? value.GetType().Name);
        bool number = name is "ProcessId" or "ParentProcessId" or "ImpersonationLevel" or "AuthenticationLevel";
        bool expectedType = number ? value is int or uint : value is string;
        if (!expectedType) return new(name, "wrong-CLR-type", type, null, null, null, null);
        bool valid = number
            ? name is "ImpersonationLevel" or "AuthenticationLevel"
                ? value is int level && level is >= 0 and <= 10 || value is uint unsignedLevel && unsignedLevel <= 10
                : value is int id && id > 0 || value is uint unsigned && unsigned is > 0 and <= int.MaxValue
            : value is string text && text.Length > 0 && text.Length <= (name == "CreationDate" ? 64 : 32767);
        return new(name, valid ? "valid" : "invalid-value", type, null, null, null, null);
    }

    private static FieldDiagnostic[] DiagnosticFields(Func<string, object?> read) => DiagnosticFieldNames.Select(name =>
    {
        try { return DescribeField(name, read(name), null); }
        catch (Exception error) { return DescribeField(name, null, error); }
    }).ToArray();

    private static object? ReadDirectProperty(dynamic row, string name) => name switch
    {
        "ProcessId" => (object?)row.ProcessId, "ParentProcessId" => (object?)row.ParentProcessId,
        "ExecutablePath" => (object?)row.ExecutablePath, "CommandLine" => (object?)row.CommandLine,
        "CreationDate" => (object?)row.CreationDate, _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static object? ReadStandardProperty(dynamic row, string name)
    {
        object? properties = null, property = null;
        try { properties = row.Properties_; property = ((dynamic)properties).Item(name, 0); return ((dynamic)property).Value; }
        finally { ReleaseDiagnostic(property); ReleaseDiagnostic(properties); }
    }

    private static void ReleaseDiagnostic(object? value)
    {
        try { if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
        catch { /* Diagnostic-only COM release must not alter strict identity acceptance. */ }
    }

    private static object Correlation(dynamic row)
    {
        object? path = null;
        try
        {
            path = row.Path_;
            object? relative = ((dynamic)path).RelPath;
            string? key = relative is string text && text.Length <= 128 &&
                System.Text.RegularExpressions.Regex.IsMatch(text, "\\AWin32_Process\\.Handle=\"[0-9]{1,10}\"\\z") ? text : null;
            return new { rowClass = "Win32_Process", relativePath = key, correlationOnly = true };
        }
        catch (Exception error) { return DescribeField("row-correlation", null, error); }
        finally { ReleaseDiagnostic(path); }
    }

    private static object[] ReadSecurity(dynamic services)
    {
        object? security = null;
        try
        {
            security = services.Security_;
            return new[] { "ImpersonationLevel", "AuthenticationLevel" }.Select(name =>
            {
                try
                {
                    object? value = name == "ImpersonationLevel" ? (object?)((dynamic)security).ImpersonationLevel : (object?)((dynamic)security).AuthenticationLevel;
                    int? level = value switch { int n when n is >= 0 and <= 10 => n, uint n when n <= 10 => (int)n, _ => null };
                    return (object)new { typed = DescribeField(name, value, null), level };
                }
                catch (Exception error) { return DescribeField(name, null, error); }
            }).ToArray();
        }
        catch (Exception error) { return [DescribeField("COM-security", null, error)]; }
        finally { ReleaseDiagnostic(security); }
    }

    private static object OwnProcessControl(ProcessDiagnostics diagnostics)
    {
        object? locator = null, services = null, result = null;
        try
        {
            locator = Activator.CreateInstance(Type.GetTypeFromProgID("WbemScripting.SWbemLocator", true)!);
            services = ((dynamic)locator!).ConnectServer(".", @"root\cimv2");
            result = ((dynamic)services).ExecQuery("SELECT ProcessId, ParentProcessId, ExecutablePath, CommandLine, CreationDate FROM Win32_Process WHERE ProcessId=" +
                Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture), "WQL", 0);
            object? observation = null;
            int count = 0;
            foreach (object item in (System.Collections.IEnumerable)result)
            {
                try
                {
                    if (++count > 1) throw new InvalidOperationException("Own-process diagnostic row bound exceeded.");
                    var direct = DiagnosticFields(name => ReadDirectProperty(item, name));
                    diagnostics.EmitPhase(DiagnosticPhase.ControlDirectFieldsCollected, direct);
                    diagnostics.EmitPhase(DiagnosticPhase.ControlStandardGettersEnter);
                    var standard = DiagnosticFields(name => ReadStandardProperty(item, name));
                    diagnostics.EmitPhase(DiagnosticPhase.ControlStandardGettersReturn);
                    observation = new { direct, standard, security = ReadSecurity(services) };
                }
                finally
                {
                    diagnostics.EmitPhase(DiagnosticPhase.ControlReleaseEnter);
                    ReleaseDiagnostic(item);
                    diagnostics.EmitPhase(DiagnosticPhase.ControlReleaseReturn);
                }
            }
            return new { count, observation };
        }
        finally { ReleaseDiagnostic(result); ReleaseDiagnostic(services); ReleaseDiagnostic(locator); }
    }

    private sealed class ProcessDiagnostics
    {
        private readonly object gate = new();
        private readonly PhaseEvidence phaseEvidence = new(Console.Out);
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly Dictionary<string, int> counters = new(StringComparer.Ordinal);
        private readonly List<object> samples = [];
        private readonly Dictionary<string, object> controls = new(StringComparer.Ordinal);
        private Task? copyTask;
        private int reserved;
        private bool countersTruncated, samplesTruncated;
        internal long ElapsedMilliseconds => elapsed.ElapsedMilliseconds;
        internal bool? CopyTaskIsCompleted => Volatile.Read(ref copyTask)?.IsCompleted;
        internal void SetCopyTask(Task task) => Volatile.Write(ref copyTask, task);
        internal void EmitPhase(DiagnosticPhase phase, FieldDiagnostic[]? fields = null) => phaseEvidence.Emit(phase, ElapsedMilliseconds, CopyTaskIsCompleted, fields);
        internal void RecordOutcomes(FieldDiagnostic[] fields)
        {
            lock (gate)
                foreach (var field in fields)
                {
                    string key = field.Field + ":" + field.Outcome + ":" + field.ClrType + ":" + field.ExceptionType + ":" + field.HResult;
                    if (counters.TryGetValue(key, out int count))
                    { if (count < int.MaxValue) counters[key] = count + 1; else countersTruncated = true; }
                    else if (counters.Count < 64) counters[key] = 1;
                    else countersTruncated = true;
                }
        }
        internal bool TryReserveSample()
        {
            lock (gate)
            {
                if (reserved >= 3) { samplesTruncated = true; return false; }
                reserved++; return true;
            }
        }
        internal void CompleteSample(object sample) { lock (gate) { if (samples.Count < reserved) samples.Add(sample); } }
        internal void CaptureControl(string stage)
        {
            lock (gate)
            {
                if (controls.ContainsKey(stage) || controls.Count >= 3) return;
                controls[stage] = new { pending = true };
            }
            object observation;
            var phases = stage switch
            {
                "before-copy" => (DiagnosticPhase.BeforeCopyControlEnter, DiagnosticPhase.BeforeCopyControlReturn),
                "after-copy" => (DiagnosticPhase.AfterCopyControlEnter, DiagnosticPhase.AfterCopyControlReturn),
                _ => (DiagnosticPhase.NestedOwnControlEnter, DiagnosticPhase.NestedOwnControlReturn)
            };
            EmitPhase(phases.Item1);
            try { observation = OwnProcessControl(this); }
            catch (Exception error) { observation = DescribeField("own-process-control", null, error); }
            finally { EmitPhase(phases.Item2); }
            lock (gate) controls[stage] = new { elapsedMilliseconds = ElapsedMilliseconds, copyTaskIsCompleted = CopyTaskIsCompleted, observation };
        }
        internal object Snapshot()
        {
            lock (gate) return new { elapsedMilliseconds = ElapsedMilliseconds, copyTaskIsCompleted = CopyTaskIsCompleted,
                samples = samples.ToArray(), sampleLimit = 3, samplesTruncated,
                counters = counters.ToDictionary(item => item.Key, item => item.Value), counterKeyLimit = 64, countersTruncated,
                ownProcessControls = controls.ToDictionary(item => item.Key, item => item.Value), phaseEvidence = phaseEvidence.Snapshot() };
        }
    }

    private enum DiagnosticPhase
    {
        BeforeCopyControlEnter, BeforeCopyControlReturn, BeforeCopyInvocation, CopyTaskReturned, CopyAwaitSettled,
        IdentityFieldsCollected, ComparisonGettersEnter, ComparisonGettersReturn, NestedOwnControlEnter, NestedOwnControlReturn,
        RowReleaseEnter, RowReleaseReturn, QueryReleaseEnter, QueryReleaseReturn, WatcherWaitEnter, WatcherWaitReturn,
        WatcherWaitCanceled, WatcherWaitTimeout, AfterCopyControlEnter, AfterCopyControlReturn,
        ControlDirectFieldsCollected, ControlStandardGettersEnter, ControlStandardGettersReturn, ControlReleaseEnter, ControlReleaseReturn,
        PhaseEvidenceTruncated
    }

    private sealed class PhaseEvidence(TextWriter writer)
    {
        private readonly object gate = new();
        private int writes, bytes, lostWrites;
        private bool truncated;
        internal void Emit(DiagnosticPhase phase, long elapsedMilliseconds, bool? copyTaskIsCompleted, FieldDiagnostic[]? fields)
        {
            lock (gate)
            {
                if (truncated) return;
                try
                {
                    string line = JsonSerializer.Serialize(new
                    {
                        phase = phase.ToString(), elapsedMilliseconds, copyTaskIsCompleted, lostWrites,
                        typed = fields?.Take(5).Select(field => new { field.Field, field.Outcome, field.ClrType, field.ExceptionType, field.HResult }).ToArray()
                    });
                    int length = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                    if (writes >= 63 || bytes + length > 16384 - 256)
                    {
                        truncated = true;
                        line = JsonSerializer.Serialize(new { phase = DiagnosticPhase.PhaseEvidenceTruncated.ToString(), elapsedMilliseconds, copyTaskIsCompleted, lostWrites });
                        length = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                    }
                    if (writes >= 64 || bytes + length > 16384) { truncated = true; return; }
                    writes++; bytes += length;
                    writer.WriteLine(line);
                    writer.Flush();
                }
                catch { lostWrites++; }
            }
        }
        internal object Snapshot() { lock (gate) return new { writes, utf8Bytes = bytes, lostWrites, truncated, maximumWrites = 64, maximumUtf8Bytes = 16384 }; }
    }

    private static string[] Arguments(string command)
    {
        IntPtr list = CommandLineToArgv(command, out int count);
        if (list == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            if (count is < 1 or > 128) throw new BlockedException("The management command argument count exceeds its bound.");
            return Enumerable.Range(0, count).Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(list, i * IntPtr.Size)) ?? "").ToArray();
        }
        finally { LocalFree(list); }
    }

    private static string ReadContainedDacl(string destination)
    {
        var security = new DirectoryInfo(destination).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        Require(security.AreAccessRulesProtected && rules.Length == 3 &&
            security.GetOwner(typeof(SecurityIdentifier))?.Value == "S-1-5-32-544", "The partial root owner or protected DACL differs.");
        foreach (string sid in new[] { "S-1-5-18", "S-1-5-32-544", "S-1-3-4" })
        {
            var matches = rules.Where(rule => rule.IdentityReference.Value == sid).ToArray();
            FileSystemRights expected = sid == "S-1-3-4" ? FileSystemRights.ReadPermissions : FileSystemRights.FullControl;
            Require(matches.Length == 1 && !matches[0].IsInherited && matches[0].AccessControlType == AccessControlType.Allow &&
                matches[0].FileSystemRights == expected && matches[0].PropagationFlags == PropagationFlags.None &&
                matches[0].InheritanceFlags == (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit),
                "The partial root DACL did not retain exact BA/SY full control and Owner Rights read control.");
        }
        return security.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream).ConfigureAwait(false));
    }

    private static string RequireGuest()
    {
        if (!OperatingSystem.IsWindows() || Environment.MachineName != "KILLERLAB-W11" ||
            Environment.GetEnvironmentVariable("KILLERBENCH_DISPOSABLE") != "yes-this-machine-is-disposable" ||
            Environment.ProcessPath != @"C:\Program Files\dotnet\dotnet.exe")
            throw new InvalidOperationException("Native cancellation requires the exact admitted guest, runtime and disposable marker before writes.");
        using var bios = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
        if (!Equals(bios?.GetValue("SystemManufacturer"), "Microsoft Corporation") || !Equals(bios?.GetValue("SystemProductName"), "Virtual Machine"))
            throw new InvalidOperationException("Native cancellation requires Microsoft Virtual Machine BIOS identity.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) throw new InvalidOperationException("Native cancellation requires an elevated administrator.");
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        string name = Path.GetFileName(full);
        if (!string.Equals(Path.GetDirectoryName(full), GuestParent, StringComparison.OrdinalIgnoreCase) ||
            !name.StartsWith("green", StringComparison.Ordinal) || name.Length <= 5 || !name[5..].All(char.IsAsciiDigit))
            throw new InvalidOperationException("Native cancellation requires the exact admitted green-number package directory.");
        for (string? path = full; path is not null; path = Path.GetDirectoryName(path))
            if (!Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("The admitted package and ancestors must be existing nonredirected directories.");
        if (!new DriveInfo(Path.GetPathRoot(full)!).DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Native cancellation requires NTFS.");
        return full;
    }

    private static void CreateProtectedDirectory(string path)
    {
        if (Directory.Exists(path) || File.Exists(path)) throw new IOException("The unique owned fixture path already exists.");
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor("O:BAG:BAD:P(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)", 1, out var descriptor, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var attributes = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), Descriptor = descriptor };
            if (!CreateDirectory(path, ref attributes)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { LocalFree(descriptor); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
    [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateDirectory(string path, ref SecurityAttributes attributes);
    [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string text, uint revision, out IntPtr descriptor, out uint length);
    [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CommandLineToArgv(string command, out int count);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
}
