using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearnStack.Hub.SharedKernel.Persistence;

/// <summary>
/// EF Core value-converter + comparer factories for the JSONB dictionary
/// columns Hub stores (<c>Plan.features</c> / <c>.limits</c>,
/// <c>Entitlement.features</c> / <c>.limits</c> / <c>.compliance_caps</c>). The
/// converter serialises a <c>Dictionary&lt;string, TValue&gt;</c> to a JSON
/// string mapped to a <c>jsonb</c> column; the comparer gives EF correct
/// change-tracking semantics for the mutable dictionary.
/// </summary>
public static class JsonbConversions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Value converter for a string-keyed dictionary &lt;-&gt; JSON text (jsonb column).</summary>
    public static ValueConverter<Dictionary<string, TValue>, string> DictionaryConverter<TValue>() =>
        new(
            model => JsonSerializer.Serialize(model, Options),
            provider => JsonSerializer.Deserialize<Dictionary<string, TValue>>(provider, Options)
                ?? new Dictionary<string, TValue>(StringComparer.Ordinal));

    /// <summary>Change-tracking comparer for a string-keyed dictionary (snapshot + deep equality).</summary>
    public static ValueComparer<Dictionary<string, TValue>> DictionaryComparer<TValue>() =>
        new(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => new Dictionary<string, TValue>(value, StringComparer.Ordinal));

    private static string Serialize<TValue>(Dictionary<string, TValue>? value) =>
        value is null ? string.Empty : JsonSerializer.Serialize(value, Options);
}
