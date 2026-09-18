using System.Net;
using System.Text;

namespace KillerBench.Engine;

public enum WorkflowReportScope { Diagnostics, DiagnosticsAndRepairs }

public static class WorkflowReportExport
{
    private sealed record Palette(string Key, string Label, string Background, string Surface, string Pane,
        string Accent, string Text, string Muted, string Border);

    private static readonly Palette[] Palettes =
    [
        new("dark", "Dark", "#1c1c1c", "#333333", "#3a3a3a", "#e8962c", "#e0e0e0", "#a0a0a0", "#2e2e2e"),
        new("light", "Light", "#dcdcdc", "#f0f0f0", "#c8c8c8", "#1b5e20", "#1a1a1a", "#555555", "#b0b0b0"),
        new("black", "Black", "#000000", "#0d0d0d", "#161616", "#00ff66", "#ffffff", "#cccccc", "#2a2a2a"),
        new("blood", "Blood", "#240c0d", "#2c1012", "#321416", "#e8485a", "#fffde8", "#f8c99e", "#401d1d"),
        new("greed", "Greed", "#002115", "#002e1c", "#003824", "#3fbf6f", "#fffde8", "#e0d49a", "#0f4a30"),
        new("cyanotic", "Cyanotic", "#001a28", "#00263a", "#002e48", "#3aa0d8", "#fffde8", "#e0d49a", "#183450"),
        new("ectoplasm", "Ectoplasm", "linear-gradient(90deg,#5e1764,#25232b)", "#314548", "#293c3f", "#ead900", "#f4f4f0", "#d7d7cf", "#096366"),
        new("decay", "Decay", "linear-gradient(90deg,#3b3f46,#50564b)", "#514e48", "#403f3b", "#8fa88d", "#f2f4f3", "#d8cfb5", "#5c554c"),
        new("malaise", "Malaise", "linear-gradient(90deg,#563343,#365b58)", "#3f4947", "#343b3d", "#ff6f91", "#f2f4f3", "#d8cfb5", "#394742"),
        new("sepulchre", "Sepulchre", "linear-gradient(90deg,#25282b,#2c5458)", "#454039", "#3a352f", "#4faaa8", "#f4f4f0", "#d7d7cf", "#4d3d2b"),
        new("delirium", "Delirium", "linear-gradient(90deg,#641d25,#2f2853)", "#3e3c50", "#343344", "#dd8500", "#f4f4f0", "#d7d7cf", "#5c527d"),
        new("mourning", "Mourning", "#3b3642", "#34373e", "#554c5d", "#ff6f91", "#f4f4f0", "#d7d7cf", "#756b79"),
        new("se98", "98SE", "#c0c0c0", "#c0c0c0", "#ffffff", "#000080", "#000000", "#404040", "#808080")
    ];

    public static string BuildHtml(WorkflowResult result, WorkflowReportScope scope, string themeKey = "dark", string accentKey = "orange")
    {
        ArgumentNullException.ThrowIfNull(result);
        string theme = NormalizeTheme(themeKey);
        string accent = NormalizeAccent(accentKey);
        var summary = result.Summary;
        string nextAction = WorkflowReport.NextAction(result);
        var verdicts = result.Checks.Where(IsReadinessVerdict).ToArray();
        var detailedChecks = result.Checks.Where(check => !IsReadinessVerdict(check)).ToArray();
        var problemChecks = detailedChecks.Where(c => c.EffectiveState is CheckState.Repairable or CheckState.Attention or CheckState.Unknown)
            .OrderBy(check => ReviewPriority(check.EffectiveState)).ThenBy(check => check.Name, StringComparer.Ordinal).ToArray();
        var otherChecks = detailedChecks.Except(problemChecks).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang='en' class='theme-" + theme + "'><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/>");
        sb.AppendLine("<title>KillerBench " + (scope == WorkflowReportScope.Diagnostics ? "Diagnostic" : "Diagnostic and Repair") + " Report</title><style>");
        foreach (var p in Palettes)
            sb.Append("html.theme-").Append(p.Key).Append("{--bg:").Append(p.Background).Append(";--surface:").Append(p.Surface)
                .Append(";--pane:").Append(p.Pane).Append(";--accent:").Append(p.Accent).Append(";--text:").Append(p.Text)
                .Append(";--muted:").Append(p.Muted).Append(";--border:").Append(p.Border).AppendLine("}");
        sb.AppendLine("*{box-sizing:border-box}body{margin:0;padding:24px;background:var(--bg);color:var(--text);font:14px/1.5 'Segoe UI',sans-serif}.wrap{max-width:1120px;margin:auto}.top{display:flex;justify-content:space-between;gap:24px;align-items:flex-start}.brand{font:700 30px Consolas,monospace}.brand b{color:var(--accent)}h1{font:600 23px Consolas,monospace;margin:20px 0 4px}.sub,.muted{color:var(--muted)}.switchers{display:grid;gap:8px}.switchrow{display:flex;align-items:center;justify-content:flex-end;gap:9px}.switchlabel{color:var(--muted);font:10px Consolas,monospace;text-transform:uppercase}.switch{display:flex;flex-wrap:wrap;gap:7px;justify-content:flex-end}.switch button{width:19px;height:19px;border-radius:50%;border:2px solid var(--border);padding:0;cursor:pointer}.switch button.active{outline:2px solid var(--text);outline-offset:2px}.panel{margin-top:16px;padding:17px;border:1px solid var(--border);border-radius:9px;background-color:var(--pane);background-image:radial-gradient(rgba(255,255,255,.035) .65px,transparent .75px);background-size:3px 3px;box-shadow:0 10px 28px rgba(0,0,0,.35)}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(135px,1fr));gap:10px}.metric{padding:12px;background:var(--surface);border:1px solid var(--border);border-radius:7px}.metric strong{display:block;font:700 25px Consolas,monospace;color:var(--accent)}h2{font:600 18px Consolas,monospace;color:var(--accent);margin:0 0 12px}.item{background:var(--surface);border:1px solid var(--border);border-left:5px solid var(--accent);border-radius:7px;padding:14px;margin:10px 0}.item.verdict{border-left-width:9px;box-shadow:inset 0 0 24px color-mix(in srgb,var(--accent) 12%,transparent)}.item h3{margin:0 0 7px;font:600 16px Consolas,monospace}.label{font:700 10px Consolas,monospace;text-transform:uppercase;letter-spacing:.6px;color:var(--muted);margin-top:9px}.text{white-space:pre-wrap;overflow-wrap:anywhere}.badge{display:inline-block;padding:3px 8px;border-radius:99px;background:var(--accent);color:#111;font:700 11px Consolas,monospace}details{margin:8px 0}summary{cursor:pointer;color:var(--accent)}code{font:12px Consolas,monospace}.notice{border-left:5px solid var(--accent)}.footer{margin:18px 0;color:var(--muted);font-size:11px}.footer a{color:var(--accent)}@media(max-width:650px){body{padding:12px}.top{display:block}.switchrow{justify-content:flex-start;margin-top:10px}}@media print{body{padding:0;background:#fff;color:#111}.switchers{display:none}.panel,.item,.metric{box-shadow:none;break-inside:avoid}.panel{background:#fff;border-color:#aaa}.muted,.sub,.label{color:#555}}</style></head><body><main class='wrap'>");
        sb.AppendLine("<div class='top'><div><div class='brand'>Killer<b>Bench</b></div><h1>" + (scope == WorkflowReportScope.Diagnostics ? "Diagnostic report" : "Diagnostic and repair report") + "</h1><div class='sub'>Structured findings, actions, and verification</div></div><div class='switchers'><div class='switchrow'><span class='switchlabel'>Theme</span><div class='switch' id='themes' aria-label='Report theme'></div></div><div class='switchrow' id='accentrow'><span class='switchlabel'>Accent</span><div class='switch' id='accents' aria-label='Report accent'></div></div></div></div>");
        sb.Append("<section class='panel'><div class='grid'>");
        Metric(sb, "Status", result.Status); Metric(sb, "Problems", problemChecks.Length.ToString()); Metric(sb, "Verified repairs", summary.VerifiedRepairs.ToString()); Metric(sb, "Incomplete", summary.Incomplete.ToString());
        sb.Append("</div><p><b>Machine:</b> ").Append(E(result.Machine)).Append("<br/><b>Workflow:</b> ").Append(E(Humanize(result.Kind.ToString())))
            .Append("<br/><b>Started:</b> ").Append(E(result.StartedUtc.ToString("yyyy-MM-dd HH:mm:ss zzz"))).AppendLine("</p></section>");
        sb.Append("<section class='panel notice'><h2>Start here</h2><p>").Append(E(nextAction)).AppendLine("</p></section>");
        sb.AppendLine("<section class='panel notice'><h2>Scope and limitations</h2><p>This report covers only the checks selected for this workflow. A clear result does not prove overall computer health. Historical evidence does not by itself prove a current fault or authorize a repair.</p></section>");
        if (verdicts.Length > 0)
        {
            sb.AppendLine("<section class='panel'><h2>Area outcomes</h2>");
            foreach (var verdict in verdicts) VerdictCard(sb, verdict);
            sb.AppendLine("</section>");
        }
        sb.AppendLine("<section class='panel'><h2>Problems and review items</h2>");
        if (problemChecks.Length == 0) sb.AppendLine("<p>No problem was established in the completed checks. Review incomplete coverage before treating the computer as healthy.</p>");
        foreach (var check in problemChecks) CheckCard(sb, check);
        sb.AppendLine("</section>");
        if (scope == WorkflowReportScope.DiagnosticsAndRepairs)
        {
            sb.AppendLine("<section class='panel'><h2>Actions and results</h2>");
            if (result.Actions.Count == 0 && result.Repairs.Count == 0) sb.AppendLine("<p>No repair was executed in this workflow.</p>");
            foreach (var action in result.Actions) ActionCard(sb, action);
            foreach (var repair in result.Repairs) RepairCard(sb, repair);
            foreach (var recipe in result.Recipes.Where(r => r.Repair is null && r.EffectiveOutcome != RecipeOutcome.Clear)
                .OrderBy(recipe => ReviewPriority(recipe.EffectiveOutcome)).ThenBy(recipe => recipe.RecipeId, StringComparer.Ordinal))
                sb.Append("<article class='item'><h3>").Append(E(Humanize(recipe.RecipeId))).Append("</h3><span class='badge'>").Append(E(Humanize(recipe.EffectiveOutcome.ToString()))).Append("</span><div class='label'>Result</div><div class='text'>").Append(E(recipe.Detail)).AppendLine("</div></article>");
            sb.AppendLine("</section>");
        }
        sb.AppendLine("<section class='panel'><h2>Completed checks</h2>");
        if (otherChecks.Length == 0) sb.AppendLine("<p>No additional completed checks were recorded.</p>");
        foreach (var check in otherChecks) CheckCard(sb, check);
        sb.AppendLine("</section>");
        if (result.RequiresRestart) sb.AppendLine("<section class='panel notice'><h2>Restart required</h2><p>One or more completed actions reported that Windows must restart. Recheck the affected workflow after restart before treating the repair as verified.</p></section>");
        sb.AppendLine("<p class='footer'>Generated locally by <a href='https://killerbench.net/'>KillerBench</a>. No report data was uploaded.</p></main><script>");
        sb.Append("var T=").Append('[').Append(string.Join(',', Palettes.Select(p => "['" + p.Key + "','" + p.Label + "','" + DotColor(p.Background) + "']"))).AppendLine("];var A=[['red','#DD504B'],['orange','#E8962C'],['green','#1EA54C'],['teal','#1FB8A8'],['blue','#50AEE8'],['purple','#B982E3']];var host=document.getElementById('themes'),ahost=document.getElementById('accents'),custom={dark:1,light:1,black:1,se98:1};function setAccent(k){var a=A.filter(function(x){return x[0]===k})[0]||A[1];document.documentElement.style.setProperty('--accent',a[1]);try{localStorage.setItem('kbReportAccent',a[0])}catch(e){}Array.prototype.forEach.call(ahost.children,function(b){b.className=b.dataset.key===a[0]?'active':''})}function setTheme(k){document.documentElement.className='theme-'+k;document.getElementById('accentrow').style.display=custom[k]?'flex':'none';if(custom[k]){var a=null;try{a=localStorage.getItem('kbReportAccent')}catch(e){}setAccent(a||'").Append(accent).Append("')}else document.documentElement.style.removeProperty('--accent');try{localStorage.setItem('kbReportTheme',k)}catch(e){}Array.prototype.forEach.call(host.children,function(b){b.className=b.dataset.key===k?'active':''})}T.forEach(function(t){var b=document.createElement('button');b.title=t[1];b.dataset.key=t[0];b.style.background=t[2];b.onclick=function(){setTheme(t[0])};host.appendChild(b)});A.forEach(function(a){var b=document.createElement('button');b.title=a[0].charAt(0).toUpperCase()+a[0].slice(1);b.dataset.key=a[0];b.style.background=a[1];b.onclick=function(){setAccent(a[0])};ahost.appendChild(b)});var saved=null;try{saved=localStorage.getItem('kbReportTheme')}catch(e){};setTheme(T.some(function(t){return t[0]===saved})?saved:'").Append(theme).AppendLine("');</script></body></html>");
        return sb.ToString();
    }

    public static string BuildText(WorkflowResult result, WorkflowReportScope scope)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        string nextAction = WorkflowReport.NextAction(result);
        sb.AppendLine("KILLERBENCH " + (scope == WorkflowReportScope.Diagnostics ? "DIAGNOSTIC REPORT" : "DIAGNOSTIC AND REPAIR REPORT"));
        sb.AppendLine("Status: " + result.Status).AppendLine("Machine: " + result.Machine).AppendLine("Workflow: " + Humanize(result.Kind.ToString()))
            .AppendLine("Started: " + result.StartedUtc.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        sb.AppendLine().AppendLine("START HERE").AppendLine(nextAction);
        sb.AppendLine().AppendLine("SCOPE AND LIMITATIONS").AppendLine("This report covers only the checks selected for this workflow. Historical evidence does not prove a current fault or authorize a repair.");
        var verdicts = result.Checks.Where(IsReadinessVerdict).ToArray();
        if (verdicts.Length > 0)
        {
            sb.AppendLine().AppendLine("AREA OUTCOMES");
            foreach (var verdict in verdicts) TextVerdict(sb, verdict);
        }
        sb.AppendLine().AppendLine("PROBLEMS AND REVIEW ITEMS");
        var detailedChecks = result.Checks.Where(check => !IsReadinessVerdict(check)).ToArray();
        var problems = detailedChecks.Where(c => c.EffectiveState is CheckState.Repairable or CheckState.Attention or CheckState.Unknown)
            .OrderBy(check => ReviewPriority(check.EffectiveState)).ThenBy(check => check.Name, StringComparer.Ordinal).ToArray();
        if (problems.Length == 0) sb.AppendLine("No problem was established in the completed checks.");
        foreach (var check in problems) TextCheck(sb, check);
        if (scope == WorkflowReportScope.DiagnosticsAndRepairs)
        {
            sb.AppendLine().AppendLine("ACTIONS AND RESULTS");
            if (result.Actions.Count == 0 && result.Repairs.Count == 0) sb.AppendLine("No repair was executed in this workflow.");
            foreach (var action in result.Actions)
                sb.AppendLine().AppendLine(action.Name).AppendLine("Result: " + action.Status)
                    .AppendLine(action.Detail).AppendLine("Next: " + action.NextAction);
            foreach (var repair in result.Repairs)
            {
                sb.AppendLine().AppendLine(Repairs.Plan(repair.Kind).Title).AppendLine("Result: " + repair.Status);
                foreach (var step in repair.Steps)
                    sb.AppendLine(StepLabel(step) + ": " + step.Step.Tool + " " + string.Join(' ', step.Step.Arguments))
                        .AppendLine("Outcome: " + Describe(step));
            }
        }
        sb.AppendLine().AppendLine("COMPLETED CHECKS");
        foreach (var check in detailedChecks.Except(problems)) TextCheck(sb, check);
        if (result.RequiresRestart) sb.AppendLine().AppendLine("RESTART REQUIRED").AppendLine("Restart Windows, then recheck the affected workflow.");
        sb.AppendLine().AppendLine("Generated locally by KillerBench. No report data was uploaded.");
        return sb.ToString();
    }

    public static string NormalizeTheme(string? value)
    {
        string key = string.IsNullOrWhiteSpace(value) ? "dark" : value.Trim().ToLowerInvariant();
        if (key == "retro98") key = "se98";
        return Palettes.Any(p => p.Key == key) ? key : throw new ArgumentException("Unknown report theme.", nameof(value));
    }

    public static string NormalizeAccent(string? value)
    {
        string key = string.IsNullOrWhiteSpace(value) ? "orange" : value.Trim().ToLowerInvariant();
        return key is "red" or "orange" or "green" or "teal" or "blue" or "purple"
            ? key : throw new ArgumentException("Unknown report accent.", nameof(value));
    }

    private static void Metric(StringBuilder sb, string label, string value) => sb.Append("<div class='metric'><strong>").Append(E(value)).Append("</strong>").Append(E(label)).Append("</div>");
    private static void VerdictCard(StringBuilder sb, WorkflowCheck check) => sb.Append("<article class='item verdict'><h3>").Append(E(check.Name)).Append("</h3><span class='badge'>").Append(E(Humanize(check.EffectiveState.ToString()))).Append("</span><div class='label'>Outcome</div><div class='text'>").Append(E(check.Detail)).AppendLine("</div></article>");
    private static void CheckCard(StringBuilder sb, WorkflowCheck check) => sb.Append("<article class='item'><h3>").Append(E(check.Name)).Append("</h3><span class='badge'>").Append(E(Humanize(check.EffectiveState.ToString()))).Append("</span><div class='label'>Problem or observation</div><div class='text'>").Append(E(check.Detail)).Append("</div><div class='label'>Suggested action</div><div class='text'>").Append(E(check.Repair is null ? "Review the evidence and stated limits. No automatic repair was selected." : Repairs.Plan(check.Repair.Value).Effects)).AppendLine("</div></article>");
    private static void RepairCard(StringBuilder sb, RepairResult repair)
    {
        int dispatched = repair.Steps.Count(step => step.CommandDispatched);
        sb.Append("<article class='item'><h3>").Append(E(Repairs.Plan(repair.Kind).Title)).Append("</h3><span class='badge'>").Append(E(repair.Status))
            .Append("</span><div class='label'>Steps and dispatch status</div><div class='text'>")
            .Append(E($"{dispatched} of {repair.Steps.Count} command plans were dispatched.")).Append("</div>");
        foreach (var step in repair.Steps)
            sb.Append("<details open><summary>").Append(E(StepLabel(step))).Append("</summary><div class='text'><code>")
                .Append(E(step.Step.Tool + " " + string.Join(' ', step.Step.Arguments))).Append("</code><br/>")
                .Append(E(Describe(step))).Append("</div></details>");
        sb.Append("<div class='label'>Result</div><div class='text'>").Append(E(Repairs.IsVerified(repair) ? "Verified by the recorded follow-up step." : repair.RequiresRestart ? "Restart required. Verification must be repeated after restart." : "Not verified. Review the recorded outcome before taking another action.")).AppendLine("</div></article>");
    }
    private static void ActionCard(StringBuilder sb, WorkflowAction action) => sb.Append("<article class='item'><h3>")
        .Append(E(action.Name)).Append("</h3><span class='badge'>").Append(E(action.Status))
        .Append("</span><div class='label'>Outcome</div><div class='text'>").Append(E(action.Detail))
        .Append("</div><div class='label'>Next</div><div class='text'>").Append(E(action.NextAction)).AppendLine("</div></article>");
    private static void TextCheck(StringBuilder sb, WorkflowCheck check) => sb.AppendLine().AppendLine(check.Name + " [" + Humanize(check.EffectiveState.ToString()) + "]").AppendLine(check.Detail).AppendLine("Suggested action: " + (check.Repair is null ? "Review the evidence and stated limits." : Repairs.Plan(check.Repair.Value).Effects));
    private static void TextVerdict(StringBuilder sb, WorkflowCheck check) => sb.AppendLine().AppendLine(check.Name + " [" + Humanize(check.EffectiveState.ToString()) + "]").AppendLine(check.Detail);
    private static bool IsReadinessVerdict(WorkflowCheck check) => check.Repair is null && check.Name.EndsWith(" readiness verdict", StringComparison.Ordinal);
    private static int ReviewPriority(CheckState state) => state switch
    {
        CheckState.Repairable => 0,
        CheckState.Attention => 1,
        CheckState.Unknown => 2,
        _ => 3
    };
    private static int ReviewPriority(RecipeOutcome outcome) => outcome switch
    {
        RecipeOutcome.RepairAvailable => 0,
        RecipeOutcome.RestartRequired => 1,
        RecipeOutcome.Repaired => 2,
        RecipeOutcome.Unknown => 3,
        RecipeOutcome.Blocked => 4,
        RecipeOutcome.Canceled => 5,
        _ => 6
    };
    private static string StepLabel(RepairStepResult step) => !step.CommandDispatched ? "Command plan (not dispatched)" : step.Step.Verification ? "Verification" : "Action";
    private static string Describe(RepairStepResult step) => !step.CommandDispatched
        ? "Not dispatched. Final eligibility changed or the operation was skipped before Windows received this command."
        : Describe(step.Result);
    private static string Describe(ToolResult result) => result.TimedOut ? "Timed out. The last captured output is historical and current state is unknown." : result.Truncated ? "Command finished with exit code " + result.ExitCode + ". Captured output is incomplete." : result.ExitCode == 0 && string.IsNullOrWhiteSpace(result.Error) ? "Completed with exit code 0." : "Exit code " + result.ExitCode + (string.IsNullOrWhiteSpace(result.Error) ? "." : ": " + result.Error.Trim());
    private static string Humanize(string value) { var sb = new StringBuilder(); for (int i = 0; i < value.Length; i++) { if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) sb.Append(' '); sb.Append(value[i] is '.' or '-' ? ' ' : value[i]); } return sb.ToString(); }
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string DotColor(string background) => background.StartsWith("linear", StringComparison.Ordinal) ? background.Split('#')[1][..6].Insert(0, "#") : background;
}
