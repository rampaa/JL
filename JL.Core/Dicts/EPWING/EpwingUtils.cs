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
    private static readonly HashSet<char> s_invalidCharacters = new()
    {
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
    };

    public static bool IsValidEpwingResultForDictType(IEpwingRecord epwingRecord, Dict dict)
    {
        foreach (char c in epwingRecord.PrimarySpelling)
        {
            if (s_invalidCharacters.Contains(c) || char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        switch (dict.Type)
        {
            case DictType.Kenkyuusha:
                if ((dict.Options?.Examples?.Value ?? ExamplesOptionValue.None) is ExamplesOptionValue.None)
                {
                    if (epwingRecord.Definitions?.Count > 2)
                    {
                        for (int i = 2; i < epwingRecord.Definitions.Count; i++)
                        {
                            if (!char.IsDigit(epwingRecord.Definitions[i][0]))
                            {
                                epwingRecord.Definitions.RemoveAt(i);
                                --i;
                            }
                        }
                    }
                }
                else if (dict.Options is { Examples.Value: ExamplesOptionValue.One })
                {
                    if (epwingRecord.Definitions?.Count > 2)
                    {
                        bool isMainExample = true;

                        for (int i = 2; i < epwingRecord.Definitions.Count; i++)
                        {
                            if (char.IsDigit(epwingRecord.Definitions[i][0]))
                            {
                                isMainExample = true;
                            }

                            else
                            {
                                if (!isMainExample)
                                {
                                    epwingRecord.Definitions.RemoveAt(i);
                                    --i;
                                }

                                isMainExample = false;
                            }
                        }
                    }
                }
                if (epwingRecord.Definitions is not null)
                {
                    epwingRecord.Definitions = epwingRecord.Definitions.Select(static def => def.Replace("┏", "")).ToList();
                }
                break;

            case DictType.Daijirin:
                if (epwingRecord.Definitions is not null)
                {
                    // English definitions
                    if (epwingRecord.Definitions.Any(static def => def.Contains("→英和") || def.Contains("\\u003")))
                    {
                        return false;
                    }

                    // English definitions
                    if (!epwingRecord.Definitions.Any(JapaneseUtils.JapaneseRegex.IsMatch))
                    {
                        return false;
                    }
                }
                break;

            case DictType.Daijisen:
                // English words
                if (!JapaneseUtils.JapaneseRegex.IsMatch(epwingRecord.PrimarySpelling))
                {
                    return false;
                }

                // Kanji definitions
                if (epwingRecord.Definitions?.Any(static def => def.Contains("［音］")) ?? false)
                {
                    return false;
                }
                break;

            case DictType.Koujien:
                // English words
                if (!JapaneseUtils.JapaneseRegex.IsMatch(epwingRecord.PrimarySpelling))
                {
                    return false;
                }
                break;
        }

        return FilterDuplicateEntries(epwingRecord, dict);
    }

    private static bool FilterDuplicateEntries(IEpwingRecord epwingRecord, Dict dict)
    {
        if (dict.Contents.TryGetValue(
                JapaneseUtils.KatakanaToHiragana(epwingRecord.PrimarySpelling),
                out List<IDictRecord>? previousResults))
        {
            for (int i = 0; i < previousResults.Count; i++)
            {
                var previousResult = (IEpwingRecord)previousResults[i];

                if (epwingRecord.Definitions is not null)
                {
                    if (previousResult.Definitions?.SequenceEqual(epwingRecord.Definitions ?? new List<string>()) ?? epwingRecord.Definitions is null)
                    {
                        // If an entry has reading info while others don't, keep the one with the reading info.
                        if (string.IsNullOrEmpty(previousResult.Reading) &&
                            !string.IsNullOrEmpty(epwingRecord.Reading))
                        {
                            previousResults.RemoveAt(i);
                            break;
                        }

                        return false;
                    }
                }
            }
        }

        else if (epwingRecord.Reading is not null && dict.Contents.TryGetValue(
                JapaneseUtils.KatakanaToHiragana(epwingRecord.Reading),
                out previousResults))
        {
            for (int i = 0; i < previousResults.Count; i++)
            {
                var previousResult = (IEpwingRecord)previousResults[i];

                if (epwingRecord.Definitions is not null)
                {
                    if (previousResult.Definitions?.SequenceEqual(epwingRecord.Definitions ?? new List<string>()) ?? epwingRecord.Definitions is null)
                    {
                        if (string.IsNullOrEmpty(previousResult.Reading))
                        {
                            previousResults.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        return true;
    }
}
