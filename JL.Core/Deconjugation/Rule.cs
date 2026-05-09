using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

[method: JsonConstructor]
internal readonly struct Rule(RuleType type, string[] decEnds, string[] conEnds, string detail, string[]? decTags = null, string[]? conTags = null) : IEquatable<Rule>
{
    [JsonPropertyName("type")] public RuleType Type { get; } = type;
    [JsonPropertyName("dec_end")] public string[] DecEnds { get; } = decEnds;
    [JsonPropertyName("con_end")] public string[] ConEnds { get; } = conEnds;
    [JsonPropertyName("detail")] public string Detail { get; } = detail.GetPooledString();
    [JsonPropertyName("dec_tag")] public string[]? DecTags { get; } = decTags;
    [JsonPropertyName("con_tag")] public string[]? ConTags { get; } = conTags;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Type.GetHashCode();
            hash = (hash * 37) + Detail.GetHashCode(StringComparison.Ordinal);

            string[] decEnds = DecEnds;
            foreach (string decEnd in decEnds)
            {
                hash = (hash * 37) + decEnd.GetHashCode(StringComparison.Ordinal);
            }

            string[] conEnds = ConEnds;
            foreach (string conEnd in conEnds)
            {
                hash = (hash * 37) + conEnd.GetHashCode(StringComparison.Ordinal);
            }

            string[]? decTags = DecTags;
            if (decTags is not null)
            {
                foreach (string decTag in decTags)
                {
                    hash = (hash * 37) + decTag.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[]? conTags = ConTags;
            if (conTags is not null)
            {
                foreach (string conTag in conTags)
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

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Rule rule && Equals(rule);
    }

    public bool Equals(Rule other)
    {
        return Type == other.Type
               && Detail == other.Detail
               && DecEnds.SequenceEqual(other.DecEnds)
               && ConEnds.SequenceEqual(other.ConEnds)
               && (other.DecTags is not null ? DecTags?.SequenceEqual(other.DecTags) ?? false : DecTags is null)
               && (other.ConTags is not null ? ConTags?.SequenceEqual(other.ConTags) ?? false : ConTags is null);
    }

    public static bool operator ==(Rule left, Rule right) => left.Equals(right);
    public static bool operator !=(Rule left, Rule right) => !left.Equals(right);
}
