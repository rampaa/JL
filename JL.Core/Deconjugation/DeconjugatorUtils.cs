using System.Diagnostics;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal static class DeconjugatorUtils
{
    public static async Task DeserializeRules()
    {
        Rule[]? rules;

        FileStream fileStream = new(Path.Join(AppInfo.ResourcesPath, "deconjugation_rules.json"), FileStreamOptionsPresets.s_asyncReadFso);
        await using (fileStream.ConfigureAwait(false))
        {
            rules = await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            Debug.Assert(rules is not null);
            Deconjugator.Rules = rules;
        }

        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            rule.DecEnds.DeduplicateStringsInArray();
            rule.ConEnds.DeduplicateStringsInArray();
            rule.DecTags?.DeduplicateStringsInArray();
            rule.ConTags?.DeduplicateStringsInArray();
        }
    }
}
