using System.Collections.Frozen;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    //'×', '‐', '。', '、', '⻖', '/', '-', '・', '+', ':', '！', '！', '●',
    //'？', '～', '〃', '､', '!', '−', '＆', '?', '&', '－', '／', '√', '$',
    //'＄', '°', '＋', ',', '®', '＼', '─', '─', '．', '■', '’', '⻌', '◎',
    //'Ⓒ', 'Ⓡ', '’', '＠', '〒', '@', '〜', '，', '㏄', '\'', '％', '#',
    //'△', '~', '%', '℃', '：', '※', '㊙', '©', '—', '‘', '△', '*', '≒',
    //'←', '↑', '↓', '☆', '.', '･'
    private static readonly FrozenSet<char> s_invalidCharacters = FrozenSet.ToFrozenSet(
    [
        '�', '〓', '㋝', '㋜',
        '（', '）', '(', ')',
        '【', '】', '「', '」',
        '［', '］', '[', ']',
        '{', '}', '〈', '〉',
        '＜', '＞', '〔', '〕',
        '《', '》', '<', '>',
        '○', '∘', '＝', '=',
        '…', '‥', ';', '；',
        '→', '━'
    ]);

    public static bool IsValidEpwingResultForDictType(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        foreach (char c in primarySpelling)
        {
            if (s_invalidCharacters.Contains(c) || char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        if (dict.Type is DictType.Kenkyuusha)
        {
            if (dict.Options.Examples!.Value is ExamplesOptionValue.None)
            {
                if (definitions.Length > 2)
                {
                    for (int i = 2; i < definitions.Length; i++)
                    {
                        if (!char.IsDigit(definitions[i][0]))
                        {
                            definitions = definitions.RemoveAt(i)!;
                            --i;
                        }
                    }
                }
            }
            else if (dict.Options.Examples.Value is ExamplesOptionValue.One)
            {
                if (definitions.Length > 2)
                {
                    bool isMainExample = true;

                    for (int i = 2; i < definitions.Length; i++)
                    {
                        if (char.IsDigit(definitions[i][0]))
                        {
                            isMainExample = true;
                        }

                        else
                        {
                            if (!isMainExample)
                            {
                                definitions = definitions.RemoveAt(i)!;
                                --i;
                            }

                            isMainExample = false;
                        }
                    }
                }
            }
        }
        else if (dict.Type is DictType.Daijisen)
        {
            // Kanji definitions
            if (definitions.Any(static def => def.Contains("［音］", StringComparison.Ordinal)))
            {
                return false;
            }
        }

        return FilterDuplicateEntries(primarySpelling, reading, definitions, dict);
    }

    private static bool FilterDuplicateEntries(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        if (dict.Contents.TryGetValue(
                JapaneseUtils.KatakanaToHiragana(primarySpelling),
                out IList<IDictRecord>? previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.SequenceEqual(definitions))
                {
                    // If an entry has reading info while others don't, keep the one with the reading info.
                    if (string.IsNullOrEmpty(previousResult.Reading) &&
                        !string.IsNullOrEmpty(reading))
                    {
                        previousResults.RemoveAt(i);
                        break;
                    }

                    if (reading == previousResult.Reading)
                    {
                        return false;
                    }
                }
            }
        }

        else if (reading is not null && dict.Contents.TryGetValue(
                     JapaneseUtils.KatakanaToHiragana(reading),
                     out previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.SequenceEqual(definitions))
                {
                    if (string.IsNullOrEmpty(previousResult.Reading))
                    {
                        previousResults.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        return true;
    }
}
