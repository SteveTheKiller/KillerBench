using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsTruncatedEvidenceChecks
{
    internal static void Run()
    {
        (string Recipe, Func<ToolResult, PrimitiveResult> Parse, string Prefix)[] cases =
        [
            ("backup.vss-writers", WindowsVssWritersProbe.Interpret, "Writer name: 'First writer'\r\n   State: [1] Stable\r\n"),
            ("backup.shadow-storage", WindowsShadowStorageProbe.Interpret, "Used Shadow Copy Storage space: 1 GB\r\nMaximum Shadow Copy Storage space: 10 GB\r\n"),
            ("performance.disk-busy", WindowsDiskBusyProbe.Interpret, "\"10/20/2026 10:00:00\",\"1.0\"\r\n"),
            ("identity.secure-channel", WindowsSecureChannelProbe.Interpret, "True\r\n"),
            ("identity.entra-token", WindowsEntraStatusProbe.Interpret, "AzureAdJoined : YES\r\nDomainJoined : NO\r\nAzureAdPrt : YES\r\n"),
            ("identity.group-policy-age", WindowsGroupPolicyAgeProbe.Interpret, "1.5\r\n"),
            ("hardware.memory-diagnostic", WindowsBcdBootSequenceProbe.Interpret, "identifier              {bootmgr}\r\n"),
            ("power.wake-timers", WindowsWakeTimersProbe.Interpret, "There are no active wake timers in the system.\r\n")
        ];
        var recipes = WindowsPacks.All().SelectMany(pack => pack.Recipes).ToDictionary(recipe => recipe.Id);
        foreach (var item in cases)
        {
            var complete = item.Parse(new ToolResult(0, item.Prefix, "", false));
            if (!complete.Complete) throw new Exception(item.Recipe + " fixture prefix was not valid complete evidence.");
            foreach (bool timedOut in new[] { false, true })
            {
                var truncated = item.Parse(new ToolResult(0, item.Prefix, "", true) { TimedOut = timedOut });
                if (truncated.Complete || !truncated.Truncated || truncated.TimedOut != timedOut || truncated.Value is not null)
                    throw new Exception(item.Recipe + " discarded truncation or interpreted a partial prefix as complete.");
                if (Expectation.Evaluate(recipes[item.Recipe].Detect.Expect, truncated).Verdict != ProbeVerdict.Unknown)
                    throw new Exception(item.Recipe + " did not preserve Unknown recipe coverage for truncated evidence.");
            }
        }
    }
}
