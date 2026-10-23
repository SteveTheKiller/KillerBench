using System.Net;
using System.Text;

namespace KillerBench.Engine.Validation;

public static class NativeValidationReport
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

    public static string BuildHtml(NativeValidationCoverage coverage, string themeKey = "dark", string accentKey = "orange")
    {
        ArgumentNullException.ThrowIfNull(coverage);
        string theme = WorkflowReportExport.NormalizeTheme(themeKey);
        string accent = WorkflowReportExport.NormalizeAccent(accentKey);
        var records = coverage.LatestRecords.ToDictionary(record => record.ScenarioId, StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang='en' class='theme-" + theme + "'><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/><title>KillerBench Native Validation Coverage</title><style>");
        foreach (var palette in Palettes)
            sb.Append("html.theme-").Append(palette.Key).Append("{--bg:").Append(palette.Background).Append(";--surface:").Append(palette.Surface)
                .Append(";--pane:").Append(palette.Pane).Append(";--accent:").Append(palette.Accent).Append(";--text:").Append(palette.Text)
                .Append(";--muted:").Append(palette.Muted).Append(";--border:").Append(palette.Border).AppendLine("}");
        sb.AppendLine("*{box-sizing:border-box}body{margin:0;padding:24px;background:var(--bg);color:var(--text);font:14px/1.45 'Segoe UI',sans-serif}.wrap{max-width:1280px;margin:auto}.top{display:flex;justify-content:space-between;gap:20px}.brand,h1,h2,.metric strong,.status{font-family:Consolas,monospace}.brand{font-size:30px;font-weight:700}.brand b,h2{color:var(--accent)}h1{font-size:23px;margin:16px 0 3px}.muted{color:var(--muted)}.panel{margin-top:16px;padding:16px;border:1px solid var(--border);border-radius:9px;background-color:var(--pane);background-image:radial-gradient(rgba(255,255,255,.035) .65px,transparent .75px);background-size:3px 3px;box-shadow:0 10px 28px rgba(0,0,0,.35)}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(130px,1fr));gap:9px}.metric{padding:11px;background:var(--surface);border:1px solid var(--border);border-radius:7px}.metric strong{display:block;font-size:24px;color:var(--accent)}.tools{display:flex;gap:8px;flex-wrap:wrap}.tools input,.tools select{min-height:36px;padding:6px 9px;background:var(--surface);color:var(--text);border:1px solid var(--border);border-radius:5px}.tools input{flex:1;min-width:220px}table{width:100%;border-collapse:collapse;background:var(--surface)}th,td{text-align:left;padding:8px;border-bottom:1px solid var(--border);vertical-align:top}th{position:sticky;top:0;background:var(--pane);color:var(--accent);font:12px Consolas,monospace}.status{font-weight:700}.Passed{color:#62d884}.Failed{color:#ff6b6b}.Blocked{color:#ffc15a}.Inconclusive,.Missing{color:var(--muted)}.switchers{display:grid;gap:8px}.switch{display:flex;flex-wrap:wrap;gap:7px;justify-content:flex-end}.switch button{width:19px;height:19px;border-radius:50%;border:2px solid var(--border);padding:0;cursor:pointer}.switch button.active{outline:2px solid var(--text);outline-offset:2px}.switchlabel{color:var(--muted);font:10px Consolas,monospace;text-transform:uppercase}.switchrow{display:flex;gap:8px;align-items:center;justify-content:flex-end}.footer{margin:18px 0;color:var(--muted);font-size:11px}@media(max-width:700px){body{padding:10px}.top{display:block}.switchrow{justify-content:flex-start;margin-top:8px}th:nth-child(3),td:nth-child(3){display:none}}@media print{body{padding:0;background:#fff;color:#111}.switchers,.tools{display:none}.panel{box-shadow:none;background:#fff;border-color:#aaa}th{position:static;color:#111}}</style></head><body><main class='wrap'>");
        sb.AppendLine("<header class='top'><div><div class='brand'>Killer<b>Bench</b></div><h1>Native validation coverage</h1><div class='muted'>Current installed recipe versions only</div></div><div class='switchers'><div class='switchrow'><span class='switchlabel'>Theme</span><div class='switch' id='themes'></div></div><div class='switchrow' id='accentrow'><span class='switchlabel'>Accent</span><div class='switch' id='accents'></div></div></div></header>");
        sb.Append("<section class='panel grid'>");
        Metric(sb, "Recipes", coverage.InstalledRecipes); Metric(sb, "Scenarios", coverage.InstalledScenarios);
        Metric(sb, "Passed", coverage.PassedScenarios); Metric(sb, "Failed", coverage.FailedScenarios);
        Metric(sb, "Blocked", coverage.BlockedScenarios); Metric(sb, "Inconclusive", coverage.InconclusiveScenarios);
        Metric(sb, "Missing", coverage.MissingScenarioIds.Count); sb.AppendLine("</section>");
        sb.AppendLine("<section class='panel'><h2>Coverage boundary</h2><p>This report records only submitted evidence that matches the currently installed recipe and scenario versions. Missing evidence is not a pass. A recorded pass proves only that scenario and its stated artifact set.</p></section>");
        sb.AppendLine("<section class='panel'><div class='tools'><input id='search' type='search' placeholder='Filter recipes or scenarios'/><select id='status'><option value=''>All statuses</option><option>Passed</option><option>Failed</option><option>Blocked</option><option>Inconclusive</option><option>Missing</option></select></div><table><thead><tr><th>Recipe</th><th>Scenario</th><th>Expected outcome</th><th>Status</th><th>Recorded</th></tr></thead><tbody id='rows'>");
        foreach (var plan in NativeValidationCatalog.All)
            foreach (var scenario in plan.Scenarios)
            {
                records.TryGetValue(scenario.Id, out var record);
                string status = record?.Outcome.ToString() ?? "Missing";
                sb.Append("<tr data-status='").Append(status).Append("'><td>").Append(E(plan.Title)).Append("<div class='muted'>").Append(E(plan.RecipeId)).Append(" v").Append(plan.RecipeVersion)
                    .Append("</div></td><td>").Append(E(scenario.Title)).Append("<div class='muted'>").Append(E(scenario.Id)).Append("</div></td><td>").Append(E(scenario.ExpectedOutcome))
                    .Append("</td><td class='status ").Append(status).Append("'>").Append(status).Append("</td><td>").Append(record is null ? string.Empty : E(record.RecordedUtc.ToString("yyyy-MM-dd HH:mm:ss zzz"))).AppendLine("</td></tr>");
            }
        sb.AppendLine("</tbody></table></section><p class='footer'>Generated locally by KillerBench. No validation data was uploaded.</p></main><script>");
        sb.Append("var T=").Append('[').Append(string.Join(',', Palettes.Select(p => "['" + p.Key + "','" + p.Label + "','" + DotColor(p.Background) + "']"))).AppendLine("];var A=[['red','#DD504B'],['orange','#E8962C'],['green','#1EA54C'],['teal','#1FB8A8'],['blue','#50AEE8'],['purple','#B982E3']];var th=document.getElementById('themes'),ah=document.getElementById('accents'),custom={dark:1,light:1,black:1,se98:1};function accent(k){var a=A.find(function(x){return x[0]===k})||A[1];document.documentElement.style.setProperty('--accent',a[1]);Array.from(ah.children).forEach(function(b){b.className=b.dataset.key===a[0]?'active':''})}function theme(k){document.documentElement.className='theme-'+k;document.getElementById('accentrow').style.display=custom[k]?'flex':'none';if(custom[k])accent('" + accent + "');Array.from(th.children).forEach(function(b){b.className=b.dataset.key===k?'active':''})}T.forEach(function(t){var b=document.createElement('button');b.title=t[1];b.dataset.key=t[0];b.style.background=t[2];b.onclick=function(){theme(t[0])};th.appendChild(b)});A.forEach(function(a){var b=document.createElement('button');b.title=a[0];b.dataset.key=a[0];b.style.background=a[1];b.onclick=function(){accent(a[0])};ah.appendChild(b)});theme('" + theme + "');var q=document.getElementById('search'),s=document.getElementById('status');function filter(){var text=q.value.toLowerCase(),status=s.value;Array.from(document.querySelectorAll('#rows tr')).forEach(function(r){r.hidden=(status&&r.dataset.status!==status)||(text&&!r.textContent.toLowerCase().includes(text))})}q.oninput=filter;s.onchange=filter;</script></body></html>");
        return sb.ToString();
    }

    public static string BuildText(NativeValidationCoverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        var records = coverage.LatestRecords.ToDictionary(record => record.ScenarioId, StringComparer.Ordinal);
        var sb = new StringBuilder("KILLERBENCH NATIVE VALIDATION COVERAGE\n\n");
        sb.AppendLine("Installed recipes: " + coverage.InstalledRecipes).AppendLine("Installed scenarios: " + coverage.InstalledScenarios)
            .AppendLine("Passed: " + coverage.PassedScenarios).AppendLine("Failed: " + coverage.FailedScenarios)
            .AppendLine("Blocked: " + coverage.BlockedScenarios).AppendLine("Inconclusive: " + coverage.InconclusiveScenarios)
            .AppendLine("Missing: " + coverage.MissingScenarioIds.Count)
            .AppendLine().AppendLine("Missing evidence is not a pass. A recorded pass proves only that scenario and its stated artifact set.");
        foreach (var plan in NativeValidationCatalog.All)
            foreach (var scenario in plan.Scenarios)
            {
                records.TryGetValue(scenario.Id, out var record);
                sb.AppendLine().Append(plan.RecipeId).Append(" / ").Append(scenario.Id).Append(" [").Append(record?.Outcome.ToString() ?? "Missing").AppendLine("]")
                    .AppendLine(scenario.ExpectedOutcome);
                if (record is not null) sb.AppendLine("Recorded: " + record.RecordedUtc.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            }
        sb.AppendLine().AppendLine("Generated locally by KillerBench. No validation data was uploaded.");
        return sb.ToString();
    }

    public static string BuildText(IReadOnlyList<RepairCapability> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var text = new StringBuilder("KILLERBENCH NATIVE CAPABILITY COVERAGE\n\n");
        text.AppendLine("Capabilities: " + capabilities.Count).AppendLine("Implementations: " + capabilities.Sum(item => item.Implementations.Count))
            .AppendLine("Lab verified capabilities: " + capabilities.Count(item => item.Status == NativeCapabilityStatus.LabVerified))
            .AppendLine("Legacy, declarative and explicit entry points share this inventory. Missing, stale, failed or incomplete evidence is not a pass.");
        foreach (var capability in capabilities)
        {
            text.AppendLine().AppendLine(capability.Id + ": " + capability.Label);
            foreach (var implementation in capability.Implementations)
                text.AppendLine(implementation.Key + " v" + implementation.Version + ": " + implementation.Label)
                    .AppendLine("Definition SHA-256: " + implementation.DefinitionSha256)
                    .AppendLine("Implementation SHA-256: " + implementation.ImplementationSha256)
                    .AppendLine("Scope: " + implementation.VerificationScope)
                    .AppendLine("Required scenarios: " + string.Join(", ", implementation.Scenarios.Select(item => item.Id)))
                    .AppendLine("Retained records: " + string.Join(", ", implementation.EvidenceReferences));
        }
        return text.ToString();
    }

    public static string BuildHtml(IReadOnlyList<RepairCapability> capabilities, string themeKey = "dark", string accentKey = "orange")
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        string theme = WorkflowReportExport.NormalizeTheme(themeKey);
        string accent = WorkflowReportExport.NormalizeAccent(accentKey);
        var palette = Palettes.Single(item => item.Key == theme);
        string accentColor = accent switch { "red" => "#DD504B", "green" => "#1EA54C", "teal" => "#1FB8A8", "blue" => "#50AEE8", "purple" => "#B982E3", _ => "#E8962C" };
        var text = new StringBuilder("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>KillerBench native capability coverage</title><style>");
        text.Append("body{margin:24px;background:").Append(palette.Background).Append(";color:").Append(palette.Text)
            .Append(";font:14px/1.5 'Segoe UI',sans-serif}main{max-width:1280px;margin:auto}h1,h2{font-family:Consolas,monospace}section{padding:16px;margin:16px 0;border:1px solid ")
            .Append(palette.Border).Append(";background:").Append(palette.Pane).Append(";border-radius:9px}pre{white-space:pre-wrap;overflow-wrap:anywhere}p{overflow-wrap:anywhere}")
            .Append("h1,h2{color:").Append(theme is "dark" or "light" or "black" or "se98" ? accentColor : palette.Accent).Append("}")
            .Append("</style></head><body data-theme='").Append(E(theme)).Append("' data-accent='").Append(E(accent))
            .Append("'><main><h1>KillerBench native capability coverage</h1><p>Legacy, declarative and explicit repair entry points share this inventory. Missing, stale, failed or incomplete evidence is not a pass. A lab label proves only the listed scenarios for the exact implementation hashes.</p>");
        foreach (var capability in capabilities)
        {
            text.Append("<section><h2>").Append(E(capability.Title)).Append("</h2><p>").Append(E(capability.Id)).Append(": ").Append(E(capability.Label)).Append("</p>");
            foreach (var implementation in capability.Implementations)
                text.Append("<h3>").Append(E(implementation.Key)).Append(" v").Append(implementation.Version).Append("</h3><p>").Append(E(implementation.Label))
                    .Append("</p><p>Scope: ").Append(E(implementation.VerificationScope)).Append("</p><p>Definition SHA-256: ").Append(E(implementation.DefinitionSha256))
                    .Append("<br>Implementation SHA-256: ").Append(E(implementation.ImplementationSha256)).Append("</p><details><summary>Required scenarios and retained records</summary><pre>")
                    .Append(E(string.Join("\n", implementation.Scenarios.Select(item => item.Id)))).Append("</pre><p>")
                    .Append(E(string.Join(", ", implementation.EvidenceReferences))).Append("</p></details>");
            text.Append("</section>");
        }
        return text.Append("</main></body></html>").ToString();
    }

    private static void Metric(StringBuilder sb, string label, int value) => sb.Append("<div class='metric'><strong>").Append(value).Append("</strong>").Append(E(label)).Append("</div>");
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string DotColor(string background) => background.StartsWith("linear", StringComparison.Ordinal) ? background.Split('#')[1][..6].Insert(0, "#") : background;
}
