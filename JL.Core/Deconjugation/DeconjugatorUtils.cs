using System.Diagnostics;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal static class DeconjugatorUtils
{
    public static async Task DeserializeRules()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "deconjugation_rules.json"));

        Rule[]? rules;
        await using (fileStream.ConfigureAwait(false))
        {
            rules = await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, Utils.Jso).ConfigureAwait(false);
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
