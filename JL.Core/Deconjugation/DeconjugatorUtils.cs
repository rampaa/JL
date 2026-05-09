using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        BuildIndex(rules);
    }

    public static void BuildIndex(Rule[] rules)
    {
        Dictionary<char, List<VirtualRule>> bucketBuilders = new(59);
        List<VirtualRule> universalBuilder = new(9);

        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            string[] conEnds = rule.ConEnds;
            string[] decEnds = rule.DecEnds;
            string[] conTags = rule.ConTags;
            string[] decTags = rule.DecTags;
            string detail = rule.Detail;
            RuleType type = rule.Type;

            bool multiCon = conTags.Length > 1;
            bool multiDec = decTags.Length > 1;

            for (int j = 0; j < conEnds.Length; j++)
            {
                string conEnd = conEnds[j];
                string conTag = multiCon ? conTags[j] : conTags[0];
                string decTag = multiDec ? decTags[j] : decTags[0];

                VirtualRule virtualRule = new(type, decEnds[j], conEnd, decTag, conTag, detail);
                if (conEnd.Length is 0)
                {
                    universalBuilder.Add(virtualRule);
                }
                else
                {
                    char lastChar = conEnd[^1];
                    if (!bucketBuilders.TryGetValue(lastChar, out List<VirtualRule>? list))
                    {
                        list = new List<VirtualRule>(8);
                        bucketBuilders[lastChar] = list;
                    }

                    list.Add(virtualRule);
                }
            }
        }

        // Project directly into the FrozenDictionary to avoid intermediate dictionary allocations
        Deconjugator.s_index = bucketBuilders.ToFrozenDictionary(static kvp => kvp.Key, static kvp => kvp.Value.ToArray());
        Deconjugator.s_universalRules = universalBuilder.ToArray();
    }
}
