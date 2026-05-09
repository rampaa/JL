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
            Deconjugator.Rules = rules;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
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
