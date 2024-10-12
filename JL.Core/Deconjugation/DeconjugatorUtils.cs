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
            Deconjugator.Rules = (await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, Utils.s_jsoNotIgnoringNull).ConfigureAwait(false))!;
        }

        for (int i = 0; i < Deconjugator.Rules.Length; i++)
        {
            Rule rule = Deconjugator.Rules[i];

            rule.Type = rule.Type.GetPooledString();
            rule.Detail = rule.Detail.GetPooledString();
            rule.ContextRule = rule.ContextRule?.GetPooledString();
            rule.DecEnd.DeduplicateStringsInArray();
            rule.ConEnd.DeduplicateStringsInArray();
            rule.DecTag?.DeduplicateStringsInArray();
            rule.ConTag?.DeduplicateStringsInArray();
        }
    }
}
