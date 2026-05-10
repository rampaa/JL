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
            for (int i = 0; i < AllRules.Length; i++)
            {
                hash = (hash * 37) + AllRules[i].GetHashCode();
            }

            foreach (KeyValuePair<string, VirtualRule[]> entry in RulesByTag)
            {
                hash = (hash * 37) + entry.Key.GetHashCode(StringComparison.Ordinal);
                VirtualRule[] rules = entry.Value;
                for (int i = 0; i < rules.Length; i++)
                {
                    hash = (hash * 37) + rules[i].GetHashCode();
                }
            }

            return hash;
        }
    }

    public static bool operator ==(RuleBucket left, RuleBucket right) => left.Equals(right);
    public static bool operator !=(RuleBucket left, RuleBucket right) => !(left == right);
}
