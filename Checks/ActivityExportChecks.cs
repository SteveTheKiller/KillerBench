using KillerBench.Engine;

internal static class ActivityExportChecks
{
    internal static void Run()
    {
        var time = new DateTimeOffset(2026, 9, 12, 12, 30, 0, TimeSpan.FromHours(-7));
        SessionActivityEntry[] rows = [new(time, "Comma, quote \" and\nsecond line"), new(time, "<script>alert('x')</script> & café"), new(time, "  =1+1")];
        string text = SessionActivityExport.Format(rows, SessionActivityFormat.Text);
        Require(text.Contains("-07:00") && text.Contains(rows[0].Message) && text.Contains("up to 150"), "Text retains time zone, messages and coverage");
        string csv = SessionActivityExport.Format(rows, SessionActivityFormat.Csv);
        Require(csv.StartsWith("Time,Message\r\n") && csv.Contains("\"Comma, quote \"\" and\nsecond line\"") && csv.Contains("\"'  =1+1\""),
            "CSV quotes embedded delimiters and multiline messages and neutralizes formula prefixes");
        string html = SessionActivityExport.Format(rows, SessionActivityFormat.Html);
        Require(!html.Contains("<script>") && html.Contains("&lt;script&gt;") && html.Contains("&amp;") && html.Contains("charset=\"utf-8\""),
            "HTML renders untrusted messages as text without active markup");
        foreach (var format in Enum.GetValues<SessionActivityFormat>())
            Require(SessionActivityExport.Format([], format).Length > 0, "Empty session has a valid export");
        Console.WriteLine("PASS: activity TXT/CSV/HTML formatting, Unicode, multiline escaping, HTML encoding and formula handling.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
