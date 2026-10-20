using System.Runtime.InteropServices;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal sealed class RuleSuffixMatcher(ushort[] characterCodes, int characterCodeCount, int[] nextNodeIndices, VirtualRule[][] matchingRulesByNodeIndex, VirtualRule[] emptyEndingRules)
{
    internal ushort[] CharacterCodes { get; } = characterCodes;
    internal int CharacterCodeCount { get; } = characterCodeCount;
    internal int[] NextNodeIndices { get; } = nextNodeIndices;
    internal VirtualRule[][] MatchingRulesByNodeIndex { get; } = matchingRulesByNodeIndex;
    internal VirtualRule[] EmptyEndingRules { get; } = emptyEndingRules;

    internal static RuleSuffixMatcher Create(ReadOnlySpan<VirtualRule> rules, ushort[] characterCodes, int characterCodeCount)
    {
        List<RuleSuffixNodeBuilder> nodes = [new()];
        List<VirtualRule> emptyEndingRules = [];
        for (int ruleIndex = 0; ruleIndex < rules.Length; ++ruleIndex)
        {
            ref readonly VirtualRule rule = ref rules[ruleIndex];
            if (rule.ConEnd.Length is 0)
            {
                emptyEndingRules.Add(rule);
                continue;
            }

            int nodeIndex = 0;
            for (int position = rule.ConEnd.Length - 1; position >= 0; --position)
            {
                char character = rule.ConEnd[position];
                ref int nextNodeIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(nodes[nodeIndex].NextNodeIndicesByCharacter, character, out bool exists);
                if (!exists)
                {
                    nextNodeIndex = nodes.Count;
                    nodes.Add(new RuleSuffixNodeBuilder());
                }

                nodeIndex = nextNodeIndex;
            }

            nodes[nodeIndex].RuleIndices.Add(ruleIndex);
        }

        int[] nextNodeIndices = new int[nodes.Count * characterCodeCount];
        VirtualRule[][] matchingRulesByNodeIndex = new VirtualRule[nodes.Count][];
        BuildMatchingRules(0, [], [], nodes, rules, matchingRulesByNodeIndex);
        for (int index = 0; index < nodes.Count; ++index)
        {
            int transitionStart = index * characterCodeCount;
            foreach ((char character, int nextNodeIndex) in nodes[index].NextNodeIndicesByCharacter)
            {
                nextNodeIndices[transitionStart + characterCodes[character]] = nextNodeIndex;
            }
        }

        return new RuleSuffixMatcher(characterCodes, characterCodeCount, nextNodeIndices, matchingRulesByNodeIndex, emptyEndingRules.ToArray());
    }

    private static void BuildMatchingRules(int nodeIndex, ReadOnlySpan<int> inheritedRuleIndices, VirtualRule[] inheritedRules, List<RuleSuffixNodeBuilder> nodes,
        ReadOnlySpan<VirtualRule> rules, VirtualRule[][] matchingRulesByNodeIndex)
    {
        RuleSuffixNodeBuilder node = nodes[nodeIndex];
        ReadOnlySpan<int> ruleIndices = inheritedRuleIndices;
        VirtualRule[] matches = inheritedRules;
        if (node.RuleIndices.Count > 0)
        {
            if (inheritedRuleIndices.IsEmpty)
            {
                ruleIndices = node.RuleIndices.AsReadOnlySpan();
            }
            else
            {
                int[] combinedRuleIndices = new int[inheritedRuleIndices.Length + node.RuleIndices.Count];
                inheritedRuleIndices.CopyTo(combinedRuleIndices);
                node.RuleIndices.CopyTo(combinedRuleIndices, inheritedRuleIndices.Length);
                // Rule indices follow the JSON array order, independently of dictionary traversal order.
                Array.Sort(combinedRuleIndices);
                ruleIndices = combinedRuleIndices;
            }

            matches = new VirtualRule[ruleIndices.Length];
            for (int i = 0; i < ruleIndices.Length; ++i)
            {
                matches[i] = rules[ruleIndices[i]];
            }
        }

        matchingRulesByNodeIndex[nodeIndex] = matches;
        foreach (int nextNodeIndex in node.NextNodeIndicesByCharacter.Values)
        {
            BuildMatchingRules(nextNodeIndex, ruleIndices, matches, nodes, rules, matchingRulesByNodeIndex);
        }
    }

    internal ReadOnlySpan<VirtualRule> GetMatchingRules(ReadOnlySpan<char> text)
    {
        // Node 0 is the root and is never a transition target.
        int nodeIndex = 0;
        for (int position = text.Length - 1; position >= 0; --position)
        {
            int nextNodeIndex = NextNodeIndices[(nodeIndex * CharacterCodeCount) + CharacterCodes[text[position]]];
            if (nextNodeIndex is 0)
            {
                break;
            }

            nodeIndex = nextNodeIndex;
        }

        return MatchingRulesByNodeIndex[nodeIndex];
    }
}
