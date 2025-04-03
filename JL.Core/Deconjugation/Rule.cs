using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

[method: JsonConstructor]
internal readonly struct Rule(string type, string[] decEnd, string[] conEnd, string detail, string? contextRule = null, string[]? decTag = null, string[]? conTag = null) : IEquatable<Rule>
{
    [JsonPropertyName("type")] public string Type { get; } = type.GetPooledString();
    [JsonPropertyName("dec_end")] public string[] DecEnd { get; } = decEnd;
    [JsonPropertyName("con_end")] public string[] ConEnd { get; } = conEnd;
    [JsonPropertyName("detail")] public string Detail { get; } = detail.GetPooledString();
    [JsonPropertyName("contextrule")] public string? ContextRule { get; } = contextRule?.GetPooledString();
    [JsonPropertyName("dec_tag")] public string[]? DecTag { get; } = decTag;
    [JsonPropertyName("con_tag")] public string[]? ConTag { get; } = conTag;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Type.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Detail.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + ContextRule?.GetHashCode(StringComparison.Ordinal) ?? 37;

            foreach (string decEnd in DecEnd)
            {
                hash = (hash * 37) + decEnd.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string conEnd in ConEnd)
            {
                hash = (hash * 37) + conEnd.GetHashCode(StringComparison.Ordinal);
            }

            if (DecTag is not null)
            {
                foreach (string decTag in DecTag)
                {
                    hash = (hash * 37) + decTag.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (ConTag is not null)
            {
                foreach (string conTag in ConTag)
                {
                    hash = (hash * 37) + conTag.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Rule rule && Equals(rule);
    }

    public bool Equals(Rule other)
    {
        return Type == other.Type
               && Detail == other.Detail
               && ContextRule == other.ContextRule
               && DecEnd.AsSpan().SequenceEqual(other.DecEnd)
               && ConEnd.AsSpan().SequenceEqual(other.ConEnd)
               && (other.DecTag is not null ? DecTag?.AsSpan().SequenceEqual(other.DecTag) ?? false : DecTag is null)
               && (other.ConTag is not null ? ConTag?.AsSpan().SequenceEqual(other.ConTag) ?? false : ConTag is null);
    }

    public static bool operator ==(Rule left, Rule right) => left.Equals(right);
    public static bool operator !=(Rule left, Rule right) => !left.Equals(right);
}
