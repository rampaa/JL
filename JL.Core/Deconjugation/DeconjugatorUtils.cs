using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal static class DeconjugatorUtils
{
    public static async Task DeserializeRules()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "deconjugation_rules.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            Deconjugator.Rules = (await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, Utils.s_jso).ConfigureAwait(false))!;
        }

        Rule[] rules = Deconjugator.Rules;
        for (int i = 0; i < rules.Length; i++)
        {
            ref readonly Rule rule = ref rules[i];
            rule.DecEnd.DeduplicateStringsInArray();
            rule.ConEnd.DeduplicateStringsInArray();
            rule.DecTag?.DeduplicateStringsInArray();
            rule.ConTag?.DeduplicateStringsInArray();
        }
    }
}
