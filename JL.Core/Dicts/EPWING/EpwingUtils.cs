using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EPWING;

public static class EpwingUtils
{
    public static bool IsValidEpwingResultForDictType(IEpwingResult epwingResult, Dict dict)
    {
        string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

        foreach (string badCharacter in badCharacters)
        {
            if (epwingResult.PrimarySpelling.Contains(badCharacter))
                return false;
        }

        switch (dict.Type)
        {
            case DictType.Kenkyuusha:
                if ((dict.Options?.Examples?.Value ?? ExamplesOptionValue.None) == ExamplesOptionValue.None)
                {
                    if (epwingResult.Definitions?.Count > 2)
                    {
                        for (int i = 2; i < epwingResult.Definitions.Count; i++)
                        {
                            if (!char.IsDigit(epwingResult.Definitions[i][0]))
                            {
                                epwingResult.Definitions.RemoveAt(i);
                                --i;
                            }
                        }
                    }
                }
                else if (dict.Options is { Examples.Value: ExamplesOptionValue.One })
                {
                    if (epwingResult.Definitions?.Count > 2)
                    {
                        bool isMainExample = true;

                        for (int i = 2; i < epwingResult.Definitions.Count; i++)
                        {
                            if (char.IsDigit(epwingResult.Definitions[i][0]))
                            {
                                isMainExample = true;
                            }

                            else
                            {
                                if (!isMainExample)
                                {
                                    epwingResult.Definitions.RemoveAt(i);
                                    --i;
                                }

                                isMainExample = false;
                            }
                        }
                    }
                }
                return FilterDuplicateEntries(epwingResult, dict);

            case DictType.Daijirin:
            case DictType.DaijirinNazeka:
                if (epwingResult.Definitions != null)
                {
                    // english definitions
                    if (epwingResult.Definitions.Any(def => def.Contains("→英和") || def.Contains("\\u003")))
                        return false;

                    // english definitions
                    if (!epwingResult.Definitions.Any(def => Storage.JapaneseRegex.IsMatch(def)))
                        return false;
                }
                break;

            case DictType.Daijisen:
                // kanji definitions
                if (epwingResult.Definitions?.Any(def => def.Contains("［音］")) ?? false)
                    return false;

                return FilterDuplicateEntries(epwingResult, dict);

            case DictType.Gakken:
            case DictType.GakkenYojijukugoYomichan:
            case DictType.IwanamiYomichan:
            case DictType.JitsuyouYomichan:
            case DictType.KanjigenYomichan:
            case DictType.KenkyuushaNazeka:
            case DictType.KireiCakeYomichan:
            case DictType.Kotowaza:
            case DictType.Koujien:
            case DictType.Meikyou:
            case DictType.NikkokuYomichan:
            case DictType.OubunshaYomichan:
            case DictType.ShinjirinYomichan:
            case DictType.ShinmeikaiYomichan:
            case DictType.ShinmeikaiNazeka:
            case DictType.ShinmeikaiYojijukugoYomichan:
            case DictType.WeblioKogoYomichan:
            case DictType.ZokugoYomichan:
            case DictType.NonspecificWordYomichan:
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
            case DictType.NonspecificNazeka:
                return FilterDuplicateEntries(epwingResult, dict);

            default:
                throw new ArgumentOutOfRangeException(nameof(dict), dict.Name, null);
        }

        return true;
    }

    private static bool FilterDuplicateEntries(IEpwingResult epwingResult, Dict dict)
    {
        if (dict.Contents.TryGetValue(
                Kana.KatakanaToHiraganaConverter(epwingResult.PrimarySpelling),
                out List<IResult>? previousResults))
        {
            int prevResultCount = previousResults.Count;
            for (int i = 0; i < prevResultCount; i++)
            {
                var kenkyuushaResult = (IEpwingResult)previousResults[i];

                if (epwingResult.Definitions != null)
                {
                    epwingResult.Definitions = epwingResult.Definitions.Select(def => def.Replace("┏", "")).ToList();

                    if (kenkyuushaResult.Definitions?.SequenceEqual(epwingResult.Definitions) ?? false)
                    {
                        // If an entry has reading info while others don't, keep the one with the reading info.
                        if (string.IsNullOrEmpty(kenkyuushaResult.Reading) &&
                            !string.IsNullOrEmpty(epwingResult.Reading))
                        {
                            previousResults.RemoveAt(i);
                            break;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        else if (epwingResult.Definitions != null)
        {
            epwingResult.Definitions = epwingResult.Definitions.Select(def => def.Replace("┏", "")).ToList();
        }

        return true;
    }
}
