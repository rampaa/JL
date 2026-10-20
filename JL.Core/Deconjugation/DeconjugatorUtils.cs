using System.Collections.Frozen;
using System.Diagnostics;
using System.Security.Cryptography;
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

    public static readonly FrozenSet<string> ValidWordClasses =
    [
        "adj-i", "adj-ix", "cop", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
    ];

    public static async Task DeserializeRules()
    {
        Deconjugator.Rules = await LoadRules(Path.Join(AppInfo.ResourcesPath, "deconjugation_rules.json"),
            Path.Join(AppInfo.ResourcesPath, "deconjugation_rules.bin")).ConfigureAwait(false);
    }

    internal static async Task<DeconjugationRules> LoadRules(string jsonPath, string binaryPath)
    {
        byte[] json = await File.ReadAllBytesAsync(jsonPath).ConfigureAwait(false);
        byte[] sourceHash = SHA256.HashData(json);
        DeconjugationRules? cachedRules = await DeconjugationRulesBinarySerializer.Deserialize(binaryPath, sourceHash).ConfigureAwait(false);
        if (cachedRules is not null)
        {
            return cachedRules;
        }

        DeconjugationRules rules = BuildRules(json);
        DeconjugationRulesBinarySerializer.Serialize(binaryPath, rules, sourceHash);
        return rules;
    }

    internal static DeconjugationRules BuildRules(ReadOnlySpan<byte> json)
    {
        Rule[]? rules = JsonSerializer.Deserialize<Rule[]>(json, s_jso);
        Debug.Assert(rules is not null);

        int virtualRuleCount = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            rule.DecEnds.DeduplicateStringsInArray();
            rule.ConEnds.DeduplicateStringsInArray();
            rule.DecTags.DeduplicateStringsInArray();
            rule.ConTags.DeduplicateStringsInArray();
            virtualRuleCount += rule.ConEnds.Length;
        }

        return BuildRules(rules, virtualRuleCount);
    }

    private static DeconjugationRules BuildRules(Rule[] rules, int virtualRuleCount)
    {
        List<VirtualRule> virtualRules = new(virtualRuleCount);

        foreach (ref readonly Rule rule in rules.AsSpan())
        {
            string detail = rule.Detail;
            RuleType type = rule.Type;
            string[] conEnds = rule.ConEnds;
            string[] decEnds = rule.DecEnds;
            string[] conTags = rule.ConTags;
            string[] decTags = rule.DecTags;

            Debug.Assert(decEnds.Length == conEnds.Length);
            Debug.Assert(conTags.Length is 1 || conTags.Length == conEnds.Length);
            Debug.Assert(decTags.Length is 1 || decTags.Length == conEnds.Length);

            string? singleConTag = conTags.Length is 1 ? conTags[0] : null;
            string? singleDecTag = decTags.Length is 1 ? decTags[0] : null;

            for (int i = 0; i < conEnds.Length; i++)
            {
                string conEnd = conEnds[i];
                string decEnd = decEnds[i];
                string conTag = singleConTag ?? conTags[i];
                string decTag = singleDecTag ?? decTags[i];

                VirtualRule virtualRule = new(type, decEnd, conEnd, decTag, conTag, detail);

                //bool isDuplicate = false;
                //for (int j = 0; j < virtualRules.Count; j++)
                //{
                //    VirtualRule existing = virtualRules[j];
                //    if (virtualRule.Type == existing.Type
                //        && virtualRule.DecEnd == existing.DecEnd
                //        && virtualRule.ConEnd == existing.ConEnd
                //        && virtualRule.DecTag == existing.DecTag
                //        && virtualRule.Detail == existing.Detail)
                //    {
                //        isDuplicate = true;
                //        break;
                //    }
                //}

                virtualRules.Add(virtualRule);
            }
        }

        return DeconjugationRules.Create(virtualRules.AsReadOnlySpan());
    }
}
