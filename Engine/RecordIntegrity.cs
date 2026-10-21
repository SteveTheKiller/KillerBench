using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace KillerBench.Engine;

/// <summary>
/// Rejects deserialized records whose non-nullable members or collection entries are null, or whose enums hold undefined values.
/// JSON deserialization does not enforce C# nullability, so saved files must pass this before domain code reads them.
/// </summary>
public static class RecordIntegrity
{
    private const int MaximumDepth = 32;

    public static void Require(object record, string name)
    {
        ArgumentNullException.ThrowIfNull(record);
        Walk(record, name, new NullabilityInfoContext(), 0);
    }

    private static void Walk(object value, string path, NullabilityInfoContext context, int depth)
    {
        if (depth > MaximumDepth) throw new InvalidDataException(path + " is nested too deeply.");
        var type = value.GetType();
        if (type.IsEnum)
        {
            if (!type.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(type, value))
                throw new InvalidDataException(path + " has an undefined value.");
            return;
        }
        if (type.Namespace?.StartsWith("KillerBench", StringComparison.Ordinal) != true || value is JsonElement) return;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Computed members have no setter and derive from the stored ones.
            if (property.SetMethod is null || property.GetIndexParameters().Length > 0) continue;
            var nullability = context.Create(property);
            string member = path + "." + property.Name;
            object? current = property.GetValue(value);
            if (current is null)
            {
                if (nullability.ReadState == NullabilityState.NotNull) throw new InvalidDataException(member + " is missing.");
                continue;
            }
            if (current is IEnumerable items and not string)
            {
                var element = nullability.ElementType ?? nullability.GenericTypeArguments.FirstOrDefault();
                int index = 0;
                foreach (object? item in items)
                {
                    string entry = member + "[" + index++ + "]";
                    if (item is null)
                    {
                        if (element?.ReadState == NullabilityState.NotNull) throw new InvalidDataException(entry + " is missing.");
                        continue;
                    }
                    Walk(item, entry, context, depth + 1);
                }
                continue;
            }
            Walk(current, member, context, depth + 1);
        }
    }
}
