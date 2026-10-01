using System.Net;
using System.Text;

namespace KillerBench.Engine.Infrastructure;

public sealed record SessionActivityEntry(DateTimeOffset Time, string Message);
public enum SessionActivityFormat { Text, Csv, Html }

public static class SessionActivityExport
{
    public static string Format(IEnumerable<SessionActivityEntry> entries, SessionActivityFormat format)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var rows = entries.ToArray();
        const string title = "KillerBench session activity";
        const string coverage = "Retained activity only (up to 150 entries). Earlier entries may have been discarded.";
        return format switch
        {
            SessionActivityFormat.Text => title + "\r\n" + coverage + "\r\n\r\n" +
                string.Join("\r\n", rows.Select(e => $"{e.Time:O}  {e.Message}")) + "\r\n",
            SessionActivityFormat.Csv => "Time,Message\r\n" + string.Concat(rows.Select(e =>
                Csv(e.Time.ToString("O")) + "," + Csv(e.Message) + "\r\n")),
            SessionActivityFormat.Html => "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>" + title +
                "</title><style>body{font:15px system-ui;margin:2rem;background:#171717;color:#eee}table{border-collapse:collapse;width:100%}th,td{text-align:left;vertical-align:top;padding:.6rem;border-bottom:1px solid #555}td{white-space:pre-wrap;overflow-wrap:anywhere}th:first-child{width:18rem}</style></head><body><h1>" +
                title + "</h1><p>" + coverage + "</p><table><thead><tr><th scope=\"col\">Time</th><th scope=\"col\">Message</th></tr></thead><tbody>" +
                string.Concat(rows.Select(e => "<tr><td>" + WebUtility.HtmlEncode(e.Time.ToString("O")) + "</td><td>" +
                    WebUtility.HtmlEncode(e.Message) + "</td></tr>")) + "</tbody></table></body></html>",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static string Csv(string value)
    {
        string trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
