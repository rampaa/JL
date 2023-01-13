using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    public static bool IsValidEpwingResultForDictType(IEpwingRecord epwingRecord, Dict dict)
    {
        string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

        foreach (string badCharacter in badCharacters)
        {
            if (epwingRecord.PrimarySpelling.Contains(badCharacter))
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
                return FilterDuplicateEntries(epwingRecord, dict);

            case DictType.Daijirin:
                if (epwingRecord.Definitions is not null)
                {
                    // english definitions
                    if (epwingRecord.Definitions.Any(static def => def.Contains("→英和") || def.Contains("\\u003")))
                    {
                        return false;
                    }

                    // english definitions
                    if (!epwingRecord.Definitions.Any(Storage.JapaneseRegex.IsMatch))
                    {
                        return false;
                    }
                }
                // todo: missing FilterDuplicateEntries call?
                break;

            case DictType.Daijisen:
                // kanji definitions
                if (epwingRecord.Definitions?.Any(static def => def.Contains("［音］")) ?? false)
                {
                    return false;
                }

                return FilterDuplicateEntries(epwingRecord, dict);

            default:
                return FilterDuplicateEntries(epwingRecord, dict);
        }

        return true;
    }

    private static bool FilterDuplicateEntries(IEpwingRecord epwingRecord, Dict dict)
    {
        if (dict.Contents.TryGetValue(
                Kana.KatakanaToHiragana(epwingRecord.PrimarySpelling),
                out List<IDictRecord>? previousResults))
        {
            int prevResultCount = previousResults.Count;
            for (int i = 0; i < prevResultCount; i++)
            {
                var previousResult = (IEpwingRecord)previousResults[i];

                if (epwingRecord.Definitions is not null)
                {
                    epwingRecord.Definitions = epwingRecord.Definitions.Select(static def => def.Replace("┏", "")).ToList();

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

        else if (epwingRecord.Definitions is not null)
        {
            epwingRecord.Definitions = epwingRecord.Definitions.Select(static def => def.Replace("┏", "")).ToList();
        }

        return true;
    }
}
