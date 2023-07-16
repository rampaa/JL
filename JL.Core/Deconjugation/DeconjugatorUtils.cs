using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;
public static class DeconjugatorUtils
{
    public static async Task DeserializeRules()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "deconjugation_rules.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            Deconjugator.Rules = (await JsonSerializer.DeserializeAsync<Rule[]>(fileStream, Utils.s_defaultJso).ConfigureAwait(false))!;
        }
    }
}
