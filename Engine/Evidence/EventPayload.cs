using System.Xml.Linq;

namespace KillerBench.Engine.Evidence;

internal static class EventPayload
{
    internal static string Preview(string validatedEvent)
    {
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
        var root = XElement.Parse(validatedEvent);
        var fields = (root.Element(ns + "EventData")?.Elements(ns + "Data") ?? [])
            .Select(field => ((string?)field.Attribute("Name") ?? "value", field.Value))
            .Concat((root.Element(ns + "UserData")?.Descendants().Where(field => !field.HasElements) ?? [])
                .Select(field => (field.Name.LocalName, field.Value))).Take(7).ToArray();
        if (fields.Length == 0) return "";
        static string Shorten(string text, int limit) => new([.. text.Take(limit).Select(c => char.IsControl(c) ? ' ' : c)]);
        return "\nReported fields: " + string.Join(" | ", fields.Take(6).Select(field =>
            Shorten(field.Item1, 64) + "=" + Shorten(field.Value, 200) +
            (field.Item1.Length > 64 || field.Value.Length > 200 ? " [shortened]" : ""))) +
            (fields.Length > 6 ? " | Additional fields retained in raw XML." : "");
    }
}
