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
    private static readonly HashSet<char> s_invalidCharacters = new(38)
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
                    if (epwingRecord.Definitions?.Length > 2)
                    {
                        for (int i = 2; i < epwingRecord.Definitions.Length; i++)
                        {
                            if (!char.IsDigit(epwingRecord.Definitions[i][0]))
                            {
                                epwingRecord.Definitions = epwingRecord.Definitions.RemoveAt(i);
                                --i;
                            }
                        }
                    }
                }
                else if (dict.Options is { Examples.Value: ExamplesOptionValue.One })
                {
                    if (epwingRecord.Definitions?.Length > 2)
                    {
                        bool isMainExample = true;

                        for (int i = 2; i < epwingRecord.Definitions.Length; i++)
                        {
                            if (char.IsDigit(epwingRecord.Definitions[i][0]))
                            {
                                isMainExample = true;
                            }

                            else
                            {
                                if (!isMainExample)
                                {
                                    epwingRecord.Definitions = epwingRecord.Definitions.RemoveAt(i);
                                    --i;
                                }

                                isMainExample = false;
                            }
                        }
                    }
                }

                if (epwingRecord.Definitions is not null)
                {
                    epwingRecord.Definitions = epwingRecord.Definitions.Select(static def => def.Replace("┏", "", StringComparison.Ordinal)).ToArray();
                }

                break;

            case DictType.KenkyuushaNazeka:
                if (epwingRecord.Definitions is not null)
                {
                    epwingRecord.Definitions = epwingRecord.Definitions.Select(static def => def.Replace("┏", "", StringComparison.Ordinal)).ToArray();
                }

                break;

            case DictType.Daijirin:
                if (epwingRecord.Definitions is not null)
                {
                    // English definitions
                    if (epwingRecord.Definitions.Any(static def => def.Contains("→英和", StringComparison.Ordinal) || def.Contains("\\u003", StringComparison.Ordinal)))
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
                if (epwingRecord.Definitions?.Any(static def => def.Contains("［音］", StringComparison.Ordinal)) ?? false)
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
                out IList<IDictRecord>? previousResults))
        {
            for (int i = 0; i < previousResults.Count; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (epwingRecord.Definitions is not null)
                {
                    if (previousResult.Definitions?.SequenceEqual(epwingRecord.Definitions ?? Enumerable.Empty<string>()) ?? epwingRecord.Definitions is null)
                    {
                        // If an entry has reading info while others don't, keep the one with the reading info.
                        if (string.IsNullOrEmpty(previousResult.Reading) &&
                            !string.IsNullOrEmpty(epwingRecord.Reading))
                        {
                            previousResults.RemoveAt(i);
                            break;
                        }

                        if (epwingRecord.Reading == previousResult.Reading)
                        {
                            return false;
                        }
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
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (epwingRecord.Definitions is not null)
                {
                    if (previousResult.Definitions?.SequenceEqual(epwingRecord.Definitions ?? Enumerable.Empty<string>()) ?? epwingRecord.Definitions is null)
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
