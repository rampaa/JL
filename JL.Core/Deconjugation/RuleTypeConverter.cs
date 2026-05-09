using System.Text.Json;
using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation;

internal sealed class RuleTypeConverter : JsonConverter<RuleType>
{
    public override RuleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.ValueTextEquals("stdrule"u8)
            ? RuleType.Standard
            : reader.ValueTextEquals("onlyfinalrule"u8)
                ? RuleType.OnlyFinal
                : reader.ValueTextEquals("rewriterule"u8)
                    ? RuleType.Rewrite
                    : reader.ValueTextEquals("neverfinalrule"u8)
                        ? RuleType.NeverFinal
                        : throw new JsonException($"Unknown RuleType: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, RuleType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            RuleType.Standard => "stdrule",
            RuleType.Rewrite => "rewriterule",
            RuleType.OnlyFinal => "onlyfinalrule",
            RuleType.NeverFinal => "neverfinalrule",
            _ => throw new JsonException($"Unknown RuleType: {value}")
        });
    }
}
