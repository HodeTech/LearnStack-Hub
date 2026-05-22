using System.Text.Json;
using FluentAssertions;
using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using Xunit;

namespace LearnStack.Hub.Tests.Contract;

/// <summary>
/// EntitlementProjection_Shape_IsStable (ADR-0021 § Architecture tests). The
/// serialised <see cref="EntitlementProjectionDto"/> is the wire contract
/// LearnStack core consumes; this test snapshots its shape against the
/// checked-in <c>entitlement-v1.schema.json</c>. A breaking change (renamed /
/// removed / added property, or a cap-shape change) fails here and forces a
/// schema-version bump.
/// </summary>
public sealed class EntitlementProjectionShapeTests
{
    [Fact]
    public void EntitlementProjection_Shape_IsStable()
    {
        var dto = new EntitlementProjectionDto
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Tier = "growth",
            Features = new Dictionary<string, bool>
            {
                ["classroom.recording"] = true,
                ["identity.sso.saml"] = false,
            },
            Limits = new Dictionary<string, long>
            {
                ["limits.max_users"] = 500,
                ["limits.max_custom_content_types"] = -1,
            },
            Compliance = new ComplianceSectionDto
            {
                Caps = new Dictionary<string, ComplianceCapDto>
                {
                    ["audit.retention.days"] = new() { Allowed = true, Forced = true, Value = "365" },
                    ["gdpr.hard_delete.enabled"] = new() { Allowed = true, Forced = false },
                },
            },
            ExpiresAt = new DateTimeOffset(2027, 5, 18, 0, 0, 0, TimeSpan.Zero),
            GraceUntil = null,
            Generation = 42,
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));
        using var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath()));

        AssertConformsToSchema(json.RootElement, schema.RootElement, "$");
    }

    [Fact]
    public void EntitlementProjection_OmitsCapValue_WhenNull()
    {
        var cap = new ComplianceCapDto { Allowed = false, Forced = true };

        var json = JsonSerializer.Serialize(cap);

        json.Should().NotContain("value", "a null cap value is omitted (JsonIgnoreCondition.WhenWritingNull)");
        json.Should().Contain("allowed").And.Contain("forced");
    }

    private static string SchemaPath() =>
        Path.Combine(AppContext.BaseDirectory, "entitlement-v1.schema.json");

    /// <summary>
    /// Lightweight structural conformance: every <c>required</c> property is
    /// present, the object's property set matches the schema's declared
    /// <c>properties</c> (when <c>additionalProperties:false</c>), and each
    /// value's JSON kind matches the declared type. Recurses into nested objects
    /// and <c>additionalProperties</c> maps.
    /// </summary>
    private static void AssertConformsToSchema(JsonElement value, JsonElement schema, string path)
    {
        var type = schema.GetProperty("type").GetString();
        type.Should().Be("object", $"{path} schema node should describe an object");
        value.ValueKind.Should().Be(JsonValueKind.Object, $"{path} must serialise as an object");

        var actualKeys = value.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray().Select(e => e.GetString()!))
            {
                actualKeys.Should().Contain(name, $"{path}.{name} is required by the schema");
            }
        }

        if (schema.TryGetProperty("properties", out var properties))
        {
            var declaredKeys = properties.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

            if (schema.TryGetProperty("additionalProperties", out var addl)
                && addl.ValueKind == JsonValueKind.False)
            {
                actualKeys.Should().BeSubsetOf(declaredKeys,
                    $"{path} must not carry properties the schema does not declare (additionalProperties:false)");
            }

            foreach (var declared in properties.EnumerateObject())
            {
                if (value.TryGetProperty(declared.Name, out var child))
                {
                    AssertValueMatches(child, declared.Value, $"{path}.{declared.Name}");
                }
            }
        }
        else if (schema.TryGetProperty("additionalProperties", out var mapSchema)
                 && mapSchema.ValueKind == JsonValueKind.Object)
        {
            // A map (e.g. features / limits / caps): every entry conforms to the value schema.
            foreach (var entry in value.EnumerateObject())
            {
                AssertValueMatches(entry.Value, mapSchema, $"{path}.{entry.Name}");
            }
        }
    }

    private static void AssertValueMatches(JsonElement value, JsonElement schema, string path)
    {
        var typeNode = schema.GetProperty("type");
        var allowed = typeNode.ValueKind == JsonValueKind.Array
            ? typeNode.EnumerateArray().Select(e => e.GetString()!).ToArray()
            : [typeNode.GetString()!];

        if (allowed.Contains("object"))
        {
            AssertConformsToSchema(value, schema, path);
            return;
        }

        var matches = value.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False => allowed.Contains("boolean"),
            JsonValueKind.Number => allowed.Contains("integer") || allowed.Contains("number"),
            JsonValueKind.String => allowed.Contains("string"),
            JsonValueKind.Null => allowed.Contains("null"),
            _ => false,
        };

        matches.Should().BeTrue($"{path} (kind {value.ValueKind}) must match schema type [{string.Join(", ", allowed)}]");
    }
}
