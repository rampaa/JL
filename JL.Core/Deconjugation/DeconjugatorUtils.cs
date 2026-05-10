using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using JL.Core.Lookup;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal static class DeconjugatorUtils
{
    private static readonly JsonSerializerOptions s_jso = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new RuleTypeConverter() }
    };

    public static async Task DeserializeRules()
    {
        Rule[]? rules;

        FileStream fileStream = new(Path.Join(AppInfo.ResourcesPath, "deconjugation_rules.json"), FileStreamOptionsPresets.s_asyncReadFso);
        await using (fileStream.ConfigureAwait(false))
        {
            rules = await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, s_jso).ConfigureAwait(false);
            Debug.Assert(rules is not null);
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            rule.DecEnds.DeduplicateStringsInArray();
            rule.ConEnds.DeduplicateStringsInArray();
            rule.DecTags.DeduplicateStringsInArray();
            rule.ConTags.DeduplicateStringsInArray();
        }

        BuildRuleDictionaries(rules);
    }

    private static void BuildRuleDictionaries(Rule[] rules)
    {
        Dictionary<char, List<VirtualRule>> rulesByLastConEndChar = new(59);
        List<VirtualRule> rulesWithEmptyConEnd = new(9);

        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            string detail = rule.Detail;
            RuleType type = rule.Type;
            string[] conEnds = rule.ConEnds;
            string[] decEnds = rule.DecEnds;
            string[] conTags = rule.ConTags;
            string[] decTags = rule.DecTags;

            string? singleConTag = conTags.Length is 1 ? conTags[0] : null;
            string? singleDecTag = decTags.Length is 1 ? decTags[0] : null;

            for (int j = 0; j < conEnds.Length; j++)
            {
                string conEnd = conEnds[j];
                string decEnd = decEnds[j];
                string conTag = singleConTag ?? conTags[j];
                string decTag = singleDecTag ?? decTags[j];

                VirtualRule virtualRule = new(type, decEnd, conEnd, decTag, conTag, detail);
                if (conEnd.Length is 0)
                {
                    rulesWithEmptyConEnd.Add(virtualRule);
                }
                else
                {
                    char lastChar = conEnd[^1];
                    ref List<VirtualRule>? list = ref CollectionsMarshal.GetValueRefOrAddDefault(rulesByLastConEndChar, lastChar, out bool exists);
                    if (!exists)
                    {
                        list = new List<VirtualRule>(16);
                    }

                    Debug.Assert(list is not null);
                    list.Add(virtualRule);
                }
            }
        }

        Deconjugator.RuleBucketsByLastDecEndChar = rulesByLastConEndChar.ToFrozenDictionary(static entry => entry.Key, static (entry) => CreateBucket(entry.Value.AsReadOnlySpan()));
        Deconjugator.RulesWithEmptyConEnd = CreateBucket(rulesWithEmptyConEnd.AsReadOnlySpan());
    }

    private static RuleBucket CreateBucket(ReadOnlySpan<VirtualRule> rules)
    {
        Dictionary<string, List<VirtualRule>> rulesByConTag = new(StringComparer.Ordinal);
        List<VirtualRule> allUniqueRules = new(rules.Length);

        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly VirtualRule rule = ref rules[i];

            ref List<VirtualRule>? rulesForConTag = ref CollectionsMarshal.GetValueRefOrAddDefault(rulesByConTag, rule.ConTag, out bool exists);
            if (!exists)
            {
                rulesForConTag = new List<VirtualRule>(8);
            }

            Debug.Assert(rulesForConTag is not null);
            rulesForConTag.Add(rule);

            //bool isDuplicate = false;
            //for (int j = 0; j < allUniqueRules.Count; j++)
            //{
            //    VirtualRule existing = allUniqueRules[j];
            //    if (rule.Type == existing.Type
            //        && rule.DecEnd == existing.DecEnd
            //        && rule.ConEnd == existing.ConEnd
            //        && rule.DecTag == existing.DecTag
            //        && rule.Detail == existing.Detail)
            //    {
            //        isDuplicate = true;
            //        break;
            //    }
            //}

            allUniqueRules.Add(rule);
        }

        return new RuleBucket(allUniqueRules.ToArray(), rulesByConTag.ToFrozenDictionary(static entry => entry.Key, static (entry) => entry.Value.ToArray(), StringComparer.Ordinal));
    }
}
