using System.Collections.Frozen;
using JL.Core.Deconjugation;

namespace JL.Core.Lookup;

internal readonly struct RuleBucket(VirtualRule[] allRules, FrozenDictionary<string, VirtualRule[]> rulesByTag) : IEquatable<RuleBucket>
{
    public VirtualRule[] AllRules { get; } = allRules;
    public FrozenDictionary<string, VirtualRule[]> RulesByTag { get; } = rulesByTag;

    public bool Equals(RuleBucket other)
    {
        if (AllRules.Length != other.AllRules.Length || RulesByTag.Count != other.RulesByTag.Count)
        {
            return false;
        }

        if (!AllRules.SequenceEqual(other.AllRules))
        {
            return false;
        }

        foreach (KeyValuePair<string, VirtualRule[]> entry in RulesByTag)
        {
            if (!other.RulesByTag.TryGetValue(entry.Key, out VirtualRule[]? otherRules) || !entry.Value.SequenceEqual(otherRules))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is RuleBucket other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (ref readonly VirtualRule rule in AllRules.AsSpan())
            {
                hash = (hash * 37) + rule.GetHashCode();
            }

            foreach ((string tag, VirtualRule[] rules) in RulesByTag)
            {
                hash = (hash * 37) + tag.GetHashCode(StringComparison.Ordinal);
                foreach (ref readonly VirtualRule rule in rules.AsSpan())
                {
                    hash = (hash * 37) + rule.GetHashCode();
                }
            }

            return hash;
        }
    }

    public static bool operator ==(RuleBucket left, RuleBucket right) => left.Equals(right);
    public static bool operator !=(RuleBucket left, RuleBucket right) => !(left == right);
}
