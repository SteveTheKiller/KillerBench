using System.Security.Cryptography;
using System.Text;
using KillerBench.Engine.Core;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Windows.Packs;
using KillerBench.Engine.Windows.Primitives;

/// <summary>Drive letter assignment acts only on the exact eligible partition named in the evidence, and verifies that partition.</summary>
internal static class DriveLetterTargetChecks
{
    // Disk 2 has two eligible unlettered data partitions; partition 3 on disk 2 is hidden, and disk 3 partition 1 is a recovery partition.
    private const string Fixture =
        "$parts=@(" +
        "[pscustomobject]@{DiskNumber=2;PartitionNumber=1;DriveLetter=[char]0;Type='Basic';IsHidden=$false;IsSystem=$false;Size=200MB;UniqueId='disk2-part1';FS='NTFS'}," +
        "[pscustomobject]@{DiskNumber=2;PartitionNumber=2;DriveLetter=[char]0;Type='Basic';IsHidden=$false;IsSystem=$false;Size=200MB;UniqueId='disk2-part2';FS='exFAT'}," +
        "[pscustomobject]@{DiskNumber=2;PartitionNumber=3;DriveLetter=[char]0;Type='Basic';IsHidden=$true;IsSystem=$false;Size=200MB;UniqueId='disk2-part3';FS='NTFS'}," +
        "[pscustomobject]@{DiskNumber=3;PartitionNumber=1;DriveLetter=[char]0;Type='Recovery';IsHidden=$false;IsSystem=$false;Size=900MB;UniqueId='disk3-part1';FS='NTFS'});" +
        "function Get-Partition { param($DiskNumber,$PartitionNumber,$ErrorAction) if($null -eq $DiskNumber){$parts}else{@($parts|Where-Object{$_.DiskNumber -eq $DiskNumber -and $_.PartitionNumber -eq $PartitionNumber})} };" +
        "function Get-Volume { param([Parameter(ValueFromPipeline=$true)]$InputObject,$DriveLetter) process { if($DriveLetter){ if($DriveLetter -eq 'E'){[pscustomobject]@{FileSystem='NTFS'}} } elseif($InputObject -and $InputObject.FS){[pscustomobject]@{FileSystem=$InputObject.FS}} } };" +
        "function Set-Partition { param($DiskNumber,$PartitionNumber,$NewDriveLetter,$ErrorAction) [Console]::Error.WriteLine('MUTATION_REACHED'); " +
        "($parts|Where-Object{$_.DiskNumber -eq $DiskNumber -and $_.PartitionNumber -eq $PartitionNumber}).DriveLetter=[char]$NewDriveLetter };";

    internal static async Task RunAsync()
    {
        static Task<ToolResult> Run(string script, CancellationToken token, string[] arguments) =>
            WindowsNetAdapterPrimitives.RunAsync(Fixture + script, token, arguments);
        string first = Fingerprint("disk2-part1"), second = Fingerprint("disk2-part2");

        var inventory = await new WindowsUnletteredVolumesProbe(Run).ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(30), CancellationToken.None);
        Require(inventory.Complete && inventory.Value == "2" && inventory.Raw.Contains("2|1|0.2|NTFS|" + first, StringComparison.Ordinal) &&
            inventory.Raw.Contains("2|2|0.2|exFAT|" + second, StringComparison.Ordinal), "Unlettered evidence lists each eligible partition with its fingerprint: " + inventory.Raw);

        async Task<ActionResult> Assign(string disk, string partition, string fingerprint, string letter) =>
            await new WindowsAssignDriveLetterAction(Run).ExecuteAsync(new Dictionary<string, string>
            { ["disk"] = disk, ["partition"] = partition, ["fingerprint"] = fingerprint, ["letter"] = letter }, TimeSpan.FromSeconds(30), null, CancellationToken.None);

        foreach (var (disk, partition, fingerprint, why) in new[]
        {
            ("2", "3", Fingerprint("disk2-part3"), "a hidden partition"),
            ("3", "1", Fingerprint("disk3-part1"), "a recovery partition"),
            ("2", "1", second, "a renumbered partition whose fingerprint changed")
        })
        {
            var refused = await Assign(disk, partition, fingerprint, "F");
            Require(refused is { Dispatched: false, Succeeded: false } && !refused.Raw.Contains("MUTATION_REACHED", StringComparison.Ordinal),
                "Assignment must refuse " + why + " before Set-Partition: " + refused);
        }

        var assigned = await Assign("2", "1", first, "F");
        Require(assigned is { Dispatched: true, Succeeded: true }, "The exact eligible partition is assigned: " + assigned);

        var recipe = WindowsMaintenancePack.Build().Recipes.Single(candidate => candidate.Id == "storage.assign-letter");
        var options = new Dictionary<string, string> { ["disk"] = "2", ["partition"] = "1", ["fingerprint"] = first, ["letter"] = "F" };
        Require(RecipeOptions.Resolve(recipe, options, out var resolvedOptions).Count == 0, "The assign recipe takes disk, partition, fingerprint and letter");
        var resolved = RecipeOptions.Apply(recipe, resolvedOptions);
        var probe = new WindowsPartitionLetterProbe(Run);
        // The fixture state is rebuilt per script run, so read the probe through a run that first applies the assignment.
        static Task<ToolResult> AfterAssign(string script, CancellationToken token, string[] arguments) =>
            WindowsNetAdapterPrimitives.RunAsync(Fixture + "($parts|Where-Object{$_.DiskNumber -eq 2 -and $_.PartitionNumber -eq 1}).DriveLetter=[char]'F';" + script, token, arguments);
        var before = await probe.ObserveAsync(resolved.Detect.Parameters, TimeSpan.FromSeconds(30), CancellationToken.None);
        Require(Expectation.Evaluate(resolved.Detect.Expect, before).Verdict == ProbeVerdict.Repairable, "Detection judges the selected partition: " + before.Detail);
        var after = await new WindowsPartitionLetterProbe(AfterAssign).ObserveAsync(resolved.Verify!.Parameters, TimeSpan.FromSeconds(30), CancellationToken.None);
        Require(Expectation.Evaluate(resolved.Verify.Expect, after).Verdict == ProbeVerdict.Clear,
            "Verification confirms the selected partition has the requested letter while another volume stays unlettered: " + after.Value);
        var moved = await probe.ObserveAsync(new Dictionary<string, string> { ["disk"] = "2", ["partition"] = "1", ["fingerprint"] = second }, TimeSpan.FromSeconds(30), CancellationToken.None);
        Require(!moved.Complete, "A partition whose fingerprint changed is not judged");
        Console.WriteLine("PASS: drive letter assignment is bound to the exact eligible partition by fingerprint and verified on that partition.");
    }

    private static string Fingerprint(string uniqueId) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uniqueId)))[..12];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
