using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

[method: JsonConstructor]
internal readonly struct Rule(string type, string[] decEnds, string[] conEnds, string detail, string? contextRule = null, string[]? decTags = null, string[]? conTags = null) : IEquatable<Rule>
{
    [JsonPropertyName("type")] public string Type { get; } = type.GetPooledString();
    [JsonPropertyName("dec_end")] public string[] DecEnds { get; } = decEnds;
    [JsonPropertyName("con_end")] public string[] ConEnds { get; } = conEnds;
    [JsonPropertyName("detail")] public string Detail { get; } = detail.GetPooledString();
    [JsonPropertyName("contextrule")] public string? ContextRule { get; } = contextRule?.GetPooledString();
    [JsonPropertyName("dec_tag")] public string[]? DecTags { get; } = decTags;
    [JsonPropertyName("con_tag")] public string[]? ConTags { get; } = conTags;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Type.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Detail.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + ContextRule?.GetHashCode(StringComparison.Ordinal) ?? 37;

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

    public override bool Equals(object? obj)
    {
        return obj is Rule rule && Equals(rule);
    }

    public bool Equals(Rule other)
    {
        return Type == other.Type
               && Detail == other.Detail
               && ContextRule == other.ContextRule
               && DecEnds.AsSpan().SequenceEqual(other.DecEnds)
               && ConEnds.AsSpan().SequenceEqual(other.ConEnds)
               && (other.DecTags is not null ? DecTags?.AsSpan().SequenceEqual(other.DecTags) ?? false : DecTags is null)
               && (other.ConTags is not null ? ConTags?.AsSpan().SequenceEqual(other.ConTags) ?? false : ConTags is null);
    }

    public static bool operator ==(Rule left, Rule right) => left.Equals(right);
    public static bool operator !=(Rule left, Rule right) => !left.Equals(right);
}
