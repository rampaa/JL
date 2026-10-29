using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal sealed class DeconjugationRules(RuleSuffixMatcher initialMatcher, FrozenDictionary<string, RuleSuffixMatcher> matchersByConTag)
{
    internal RuleSuffixMatcher InitialMatcher { get; } = initialMatcher;
    internal FrozenDictionary<string, RuleSuffixMatcher> MatchersByConTag { get; } = matchersByConTag;

    internal static DeconjugationRules Create(ReadOnlySpan<VirtualRule> rules)
    {
        ushort[] characterCodes = new ushort[char.MaxValue + 1];

        // Leave code 0 for characters absent from all rule endings.
        // Their next-node entries stay 0, which stops searching for longer endings.
        int characterCodeCount = 1;
        List<VirtualRule> initialRules = new(rules.Length);
        Dictionary<string, List<VirtualRule>> rulesByConTag = new(StringComparer.Ordinal);
        foreach (ref readonly VirtualRule rule in rules)
        {
            foreach (char character in rule.ConEnd)
            {
                if (characterCodes[character] is 0)
                {
                    characterCodes[character] = checked((ushort)characterCodeCount++);
                }
            }

            if (rule.Type is not RuleType.NeverFinal)
            {
                initialRules.Add(rule);
            }

            // An empty tag permits OnlyFinal rules even after an earlier deconjugation step.
            if (rule.ConTag.Length is 0 ? rule.Type is RuleType.NeverFinal : rule.Type is RuleType.OnlyFinal)
            {
                continue;
            }

            ref List<VirtualRule>? rulesForConTag = ref CollectionsMarshal.GetValueRefOrAddDefault(rulesByConTag, rule.ConTag, out bool exists);
            if (!exists)
            {
                rulesForConTag = new List<VirtualRule>(8);
            }

            Debug.Assert(rulesForConTag is not null);
            rulesForConTag.Add(rule);
        }

        RuleSuffixMatcher initialMatcher = RuleSuffixMatcher.Create(initialRules.AsReadOnlySpan(), characterCodes, characterCodeCount);
        FrozenDictionary<string, RuleSuffixMatcher> matchersByConTag = rulesByConTag.ToFrozenDictionary(static entry => entry.Key,
            entry => RuleSuffixMatcher.Create(entry.Value.AsReadOnlySpan(), characterCodes, characterCodeCount), StringComparer.Ordinal);
        return new DeconjugationRules(initialMatcher, matchersByConTag);
    }

    internal RuleSuffixMatcher? GetSuffixMatcher(in Form form)
    {
        return form.Process is null
            ? InitialMatcher
            : MatchersByConTag.GetValueOrDefault(form.LastTag);
    }
}
