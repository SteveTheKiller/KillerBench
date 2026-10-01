using System.Globalization;
using System.Text.RegularExpressions;

namespace KillerBench.Engine.Core.Recipes;

/// <summary>Evaluates a probe result against an expectation. The first matching clause wins; incomplete evidence is always Unknown.</summary>
public static class Expectation
{
    public sealed record Evaluation(ProbeVerdict Verdict, string Detail, ExpectClause? MatchedClause);

    public static Evaluation Evaluate(ExpectSpec expect, PrimitiveResult result)
    {
        ArgumentNullException.ThrowIfNull(expect);
        ArgumentNullException.ThrowIfNull(result);
        if (!result.Complete || result.TimedOut || result.Truncated || !string.IsNullOrEmpty(result.Error))
            return new(ProbeVerdict.Unknown, "Evidence is incomplete" + (string.IsNullOrEmpty(result.Error) ? "." : ": " + result.Error), null);
        foreach (var clause in expect.Clauses)
        {
            if (clause is null) continue;
            if (Matches(clause, result.Value))
                return new(clause.Verdict, clause.Detail ?? result.Detail ?? Describe(clause, result.Value), clause);
        }
        return new(ProbeVerdict.Unknown, "Observed value '" + (result.Value ?? "<absent>") + "' matched no expectation clause.", null);
    }

    public static bool Matches(ExpectClause clause, string? value)
    {
        ArgumentNullException.ThrowIfNull(clause);
        switch (clause.Operator)
        {
            case "any": return true;
            case "absent": return value is null;
            case "present": return value is not null;
            case "equals": return value is not null && string.Equals(value, clause.Value, StringComparison.Ordinal);
            case "not-equals": return value is not null && !string.Equals(value, clause.Value, StringComparison.Ordinal);
            case "contains": return value is not null && value.Contains(clause.Value, StringComparison.Ordinal);
            case "matches":
                if (value is null) return false;
                try { return Regex.IsMatch(value, clause.Value, RegexOptions.None, TimeSpan.FromMilliseconds(200)); }
                catch (RegexMatchTimeoutException) { return false; }
            case "less-than":
                return TryNumber(value, out double observedLess) && TryNumber(clause.Value, out double limitLess) && observedLess < limitLess;
            case "greater-than":
                return TryNumber(value, out double observedGreater) && TryNumber(clause.Value, out double limitGreater) && observedGreater > limitGreater;
            default: return false;
        }
    }

    private static bool TryNumber(string? text, out double number) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    private static string Describe(ExpectClause clause, string? value) => clause.Operator switch
    {
        "any" => "Observed value '" + (value ?? "<absent>") + "'.",
        "absent" => "The observed item is absent.",
        "present" => "The observed item is present.",
        _ => "Observed value '" + (value ?? "<absent>") + "' " + clause.Operator + " '" + clause.Value + "'."
    };
}
