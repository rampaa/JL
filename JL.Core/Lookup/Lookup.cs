using System.Diagnostics;
using System.Text;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Dicts.EPWING.EpwingYomichan;
using JL.Core.Dicts.YomichanKanji;
using JL.Core.Frequency;
using JL.Core.PoS;
using JL.Core.Utilities;

namespace JL.Core.Lookup;

public static class Lookup
{
    private static DateTime s_lastLookupTime;

    // public static readonly LRUCache<string, List<LookupResult>?> LookupResultCache = new(
    //     Storage.CacheSize, Storage.CacheSize / 8);

    public static List<LookupResult>? LookupText(string text) //, bool useCache = true
    {
        DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
        if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < Storage.Frontend.CoreConfig.LookupRate)
            return null;
        s_lastLookupTime = preciseTimeNow;

        // if (useCache && LookupResultCache.TryGet(text, out List<LookupResult>? data))
        //     return data;

        List<LookupResult> lookupResults = new();

        if (Storage.Frontend.CoreConfig.KanjiMode)
        {
            foreach (Dict dict in Storage.Dicts.Values)
            {
                if (dict.Active)
                {
                    if (dict.Type == DictType.Kanjidic)
                    {
                        lookupResults.AddRange(KanjidicResultBuilder(GetKanjiResults(text, dict)));
                    }

                    else if (Storage.KanjiDictTypes.Contains(dict.Type))
                    {
                        if (Storage.NazekaDictTypes.Contains(dict.Type))
                        {
                            lookupResults.AddRange(EpwingNazekaResultBuilder(GetKanjiResults(text, dict)));
                        }

                        else // if (Storage.YomichanDictTypes.Contains(dict.Type))
                        {
                            lookupResults.AddRange(YomichanKanjiResultBuilder(GetKanjiResults(text, dict)));
                        }
                    }
                }
            }

            return lookupResults.Any() ? SortLookupResults(lookupResults) : null;
        }

        Dictionary<string, IntermediaryResult> jMdictResults = new();
        Dictionary<string, IntermediaryResult> jMnedictResults = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanWordResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanKanjiResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanNameResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingNazekaWordResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingNazekaKanjiResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingNazekaNameResultsList = new();
        Dictionary<string, IntermediaryResult> kanjidicResults = new();
        Dictionary<string, IntermediaryResult> customWordResults = new();
        Dictionary<string, IntermediaryResult> customNameResults = new();

        List<string> textInHiraganaList = new();
        List<HashSet<Form>> deconjugationResultsList = new();

        for (int i = 0; i < text.Length; i++)
        {
            string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);
            textInHiraganaList.Add(textInHiragana);

            deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));
        }

        foreach (Dict dict in Storage.Dicts.Values.ToList())
        {
            if (dict.Active)
            {
                switch (dict.Type)
                {
                    case DictType.JMdict:
                        jMdictResults = GetWordResults(text, textInHiraganaList, deconjugationResultsList,
                            dict);
                        break;

                    case DictType.JMnedict:
                        jMnedictResults = GetNameResults(text, textInHiraganaList, dict);
                        break;

                    case DictType.Kanjidic:
                        kanjidicResults = GetKanjiResults(text, dict);
                        break;

                    case DictType.KanjigenYomichan:
                        // Template-wise, Kanjigen is a word dictionary that's why its results are put into Yomichan Word Results
                        // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                        epwingYomichanWordResultsList.Add(GetKanjiResults(text, dict));
                        break;

                    case DictType.CustomWordDictionary:
                        customWordResults = GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dict);
                        break;

                    case DictType.CustomNameDictionary:
                        customNameResults = GetNameResults(text, textInHiraganaList, dict);
                        break;

                    case DictType.NonspecificKanjiYomichan:
                        epwingYomichanKanjiResultsList.Add(GetKanjiResults(text, dict));
                        break;

                    case DictType.NonspecificNameYomichan:
                        epwingYomichanNameResultsList.Add(GetNameResults(text, textInHiraganaList, dict));
                        break;

                    case DictType.Kenkyuusha:
                    case DictType.Daijirin:
                    case DictType.Daijisen:
                    case DictType.Koujien:
                    case DictType.Meikyou:
                    case DictType.Gakken:
                    case DictType.Kotowaza:
                    case DictType.IwanamiYomichan:
                    case DictType.JitsuyouYomichan:
                    case DictType.ShinmeikaiYomichan:
                    case DictType.NikkokuYomichan:
                    case DictType.ShinjirinYomichan:
                    case DictType.OubunshaYomichan:
                    case DictType.ZokugoYomichan:
                    case DictType.WeblioKogoYomichan:
                    case DictType.GakkenYojijukugoYomichan:
                    case DictType.ShinmeikaiYojijukugoYomichan:
                    case DictType.KireiCakeYomichan:
                    case DictType.NonspecificWordYomichan:
                    case DictType.NonspecificYomichan:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dict));
                        break;

                    case DictType.NonspecificKanjiNazeka:
                        epwingNazekaKanjiResultsList.Add(GetNameResults(text, textInHiraganaList, dict));
                        break;

                    case DictType.NonspecificNameNazeka:
                        epwingNazekaNameResultsList.Add(GetNameResults(text, textInHiraganaList, dict));
                        break;

                    case DictType.DaijirinNazeka:
                    case DictType.KenkyuushaNazeka:
                    case DictType.ShinmeikaiNazeka:
                    case DictType.NonspecificWordNazeka:
                    case DictType.NonspecificNazeka:
                        epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dict));
                        break;

                    case DictType.PitchAccentYomichan:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                }
            }
        }

        if (jMdictResults.Any())
            lookupResults.AddRange(JmdictResultBuilder(jMdictResults));

        if (jMnedictResults.Any())
            lookupResults.AddRange(JmnedictResultBuilder(jMnedictResults));

        if (kanjidicResults.Any())
            lookupResults.AddRange(KanjidicResultBuilder(kanjidicResults));

        if (customWordResults.Any())
            lookupResults.AddRange(CustomWordResultBuilder(customWordResults));

        if (customNameResults.Any())
            lookupResults.AddRange(CustomNameResultBuilder(customNameResults));

        if (epwingYomichanWordResultsList.Any())
        {
            for (int i = 0; i < epwingYomichanWordResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingYomichanResultBuilder(epwingYomichanWordResultsList[i]));
            }
        }

        if (epwingYomichanKanjiResultsList.Any())
        {
            for (int i = 0; i < epwingYomichanKanjiResultsList.Count; i++)
            {
                lookupResults.AddRange(YomichanKanjiResultBuilder(epwingYomichanKanjiResultsList[i]));
            }
        }

        if (epwingYomichanNameResultsList.Any())
        {
            for (int i = 0; i < epwingYomichanNameResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingYomichanResultBuilder(epwingYomichanNameResultsList[i]));
            }
        }

        if (epwingNazekaWordResultsList.Any())
        {
            for (int i = 0; i < epwingNazekaWordResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingNazekaResultBuilder(epwingNazekaWordResultsList[i]));
            }
        }

        if (epwingNazekaNameResultsList.Any())
        {
            for (int i = 0; i < epwingNazekaNameResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingNazekaResultBuilder(epwingNazekaNameResultsList[i]));
            }
        }

        if (epwingNazekaKanjiResultsList.Any())
        {
            for (int i = 0; i < epwingNazekaKanjiResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingNazekaResultBuilder(epwingNazekaKanjiResultsList[i]));
            }
        }

        if (lookupResults.Any())
            lookupResults = SortLookupResults(lookupResults);

        // if (useCache)
        //     LookupResultCache.AddReplace(text, lookupResults.ToList());

        return lookupResults;
    }

    private static List<LookupResult> SortLookupResults(List<LookupResult> lookupResults)
    {
        string longestMatchedText = lookupResults.Aggregate((r1, r2) => r1.MatchedText.Length > r2.MatchedText.Length ? r1 : r2).MatchedText;

        return lookupResults
            .OrderByDescending(dict => longestMatchedText == dict.PrimarySpelling)
            .ThenByDescending(dict => dict.Readings?.Contains(longestMatchedText))
            .ThenByDescending(dict => dict.MatchedText.Length)
            .ThenByDescending(dict => longestMatchedText.Length >= dict.PrimarySpelling.Length && longestMatchedText[..dict.PrimarySpelling.Length] == dict.PrimarySpelling)
            .ThenBy(dict => dict.Dict.Priority)
            .ThenBy(dict => dict.Frequencies?.Count > 0 ? dict.Frequencies.First().Freq : int.MaxValue)
            .ToList();
    }

    private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(Dict dict,
        Dictionary<string, IntermediaryResult> results,
        HashSet<Form> deconjugationList,
        string matchedText,
        string textInHiragana,
        int succAttempt)
    {
        Dictionary<string, List<IResult>> dictionary = dict.Contents;

        bool tryLongVowelConversion = true;

        if (dictionary.TryGetValue(textInHiragana, out List<IResult>? tempResult))
        {
            results.TryAdd(textInHiragana,
                new IntermediaryResult(new List<List<IResult>> { tempResult }, null, matchedText, matchedText,
                    dict));
            tryLongVowelConversion = false;
        }

        if (succAttempt < 3)
        {
            foreach (Form deconjugationResult in deconjugationList)
            {
                string lastTag = "";
                if (deconjugationResult.Tags.Count > 0)
                    lastTag = deconjugationResult.Tags.Last();

                if (dictionary.TryGetValue(deconjugationResult.Text, out List<IResult>? dictResults))
                {
                    List<IResult> resultsList = new();

                    switch (dict.Type)
                    {
                        case DictType.JMdict:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    var dictResult = (JMdictResult)dictResults[i];

                                    if (deconjugationResult.Tags.Count == 0 || (dictResult.WordClasses
                                            ?.Where(pos => pos != null)
                                            .SelectMany(pos => pos!).Contains(lastTag) ?? false))
                                    {
                                        resultsList.Add(dictResult);
                                    }
                                }
                            }
                            break;

                        case DictType.CustomWordDictionary:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    var dictResult = (CustomWordEntry)dictResults[i];

                                    if (deconjugationResult.Tags.Count == 0 ||
                                        dictResult.WordClasses.Contains(lastTag))
                                    {
                                        resultsList.Add(dictResult);
                                    }
                                }
                            }
                            break;

                        case DictType.Daijirin:
                        case DictType.Daijisen:
                        case DictType.Gakken:
                        case DictType.Kenkyuusha:
                        case DictType.Kotowaza:
                        case DictType.Koujien:
                        case DictType.Meikyou:
                        case DictType.IwanamiYomichan:
                        case DictType.JitsuyouYomichan:
                        case DictType.ShinmeikaiYomichan:
                        case DictType.NikkokuYomichan:
                        case DictType.ShinjirinYomichan:
                        case DictType.OubunshaYomichan:
                        case DictType.ZokugoYomichan:
                        case DictType.WeblioKogoYomichan:
                        case DictType.GakkenYojijukugoYomichan:
                        case DictType.ShinmeikaiYojijukugoYomichan:
                        case DictType.KanjigenYomichan:
                        case DictType.KireiCakeYomichan:
                        case DictType.NonspecificWordYomichan:
                        case DictType.NonspecificKanjiYomichan:
                        case DictType.NonspecificNameYomichan:
                        case DictType.NonspecificYomichan:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    var dictResult = (EpwingYomichanResult)dictResults[i];

                                    if (deconjugationResult.Tags.Count == 0 ||
                                        (dictResult.WordClasses?.Contains(lastTag) ?? false))
                                    {
                                        resultsList.Add(dictResult);
                                    }

                                    else if (Storage.WcDict.TryGetValue(deconjugationResult.Text,
                                                 out List<JmdictWc>? jmdictWcResults))
                                    {
                                        for (int j = 0; j < jmdictWcResults.Count; j++)
                                        {
                                            JmdictWc jmdictWcResult = jmdictWcResults[j];

                                            if (dictResult.PrimarySpelling == jmdictWcResult.Spelling
                                                && (jmdictWcResult.Readings?.Contains(dictResult.Reading ?? string.Empty)
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                            {
                                                if (jmdictWcResult.WordClasses.Contains(lastTag))
                                                {
                                                    resultsList.Add(dictResult);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case DictType.DaijirinNazeka:
                        case DictType.KenkyuushaNazeka:
                        case DictType.ShinmeikaiNazeka:
                        case DictType.NonspecificWordNazeka:
                        case DictType.NonspecificKanjiNazeka:
                        case DictType.NonspecificNameNazeka:
                        case DictType.NonspecificNazeka:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    var dictResult = (EpwingNazekaResult)dictResults[i];

                                    if (deconjugationResult.Tags.Count == 0)
                                    {
                                        resultsList.Add(dictResult);
                                    }

                                    else if (Storage.WcDict.TryGetValue(deconjugationResult.Text,
                                                 out List<JmdictWc>? jmdictWcResults))
                                    {
                                        for (int j = 0; j < jmdictWcResults.Count; j++)
                                        {
                                            JmdictWc jmdictWcResult = jmdictWcResults[j];

                                            if (dictResult.PrimarySpelling == jmdictWcResult.Spelling
                                                && (jmdictWcResult.Readings?.Contains(dictResult.Reading ?? "")
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                            {
                                                if (jmdictWcResult.WordClasses.Contains(lastTag))
                                                {
                                                    resultsList.Add(dictResult);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case DictType.PitchAccentYomichan:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                    }

                    if (resultsList.Any())
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? r))
                        {
                            if (r.MatchedText == deconjugationResult.OriginalText)
                            {
                                int index = r.Results.FindIndex(rs => rs.SequenceEqual(resultsList));
                                if (index != -1)
                                {
                                    r.Processes?[index].Add(deconjugationResult.Process);
                                }

                                else
                                {
                                    r.Results.Add(resultsList);
                                    r.Processes?.Add(new List<List<string>> { deconjugationResult.Process });
                                }
                            }
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult(new List<List<IResult>> { resultsList },
                                    new List<List<List<string>>> { new List<List<string>> { deconjugationResult.Process } },
                                    matchedText,
                                    deconjugationResult.Text,
                                    dict)
                            );
                        }

                        ++succAttempt;
                        tryLongVowelConversion = false;
                    }
                }
            }
        }

        return (tryLongVowelConversion, succAttempt);
    }

    private static Dictionary<string, IntermediaryResult> GetWordResults(string text,
        List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, Dict dict)
    {
        Dictionary<string, IntermediaryResult> results = new();

        int succAttempt = 0;

        for (int i = 0; i < text.Length; i++)
        {
            (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dict, results,
                deconjugationResultsList[i], text[..^i], textInHiraganaList[i], succAttempt);

            if (tryLongVowelConversion && textInHiraganaList[i].Contains('ー') &&
                textInHiraganaList[i][0] != 'ー')
            {
                List<string> textWithoutLongVowelMarkList = Kana.LongVowelMarkConverter(textInHiraganaList[i]);

                for (int j = 0; j < textWithoutLongVowelMarkList.Count; j++)
                {
                    succAttempt = GetWordResultsHelper(dict, results, deconjugationResultsList[i],
                        text[..^i], textWithoutLongVowelMarkList[j], succAttempt).succAttempt;
                }
            }
        }

        return results;
    }

    private static Dictionary<string, IntermediaryResult> GetNameResults(string text,
        List<string> textInHiraganaList, Dict dict)
    {
        Dictionary<string, IntermediaryResult> nameResults = new();

        for (int i = 0; i < text.Length; i++)
        {
            if (dict.Contents
                .TryGetValue(textInHiraganaList[i], out List<IResult>? result))
            {
                nameResults.TryAdd(textInHiraganaList[i],
                    new IntermediaryResult(new List<List<IResult>> { result }, null, text[..^i], text[..^i], dict));
            }
        }

        return nameResults;
    }

    private static Dictionary<string, IntermediaryResult> GetKanjiResults(string text, Dict dict)
    {
        Dictionary<string, IntermediaryResult> kanjiResults = new();

        string? kanji = text.UnicodeIterator().FirstOrDefault();

        if (kanji != null && dict.Contents.TryGetValue(kanji, out List<IResult>? result))
        {
            kanjiResults.Add(kanji,
                new IntermediaryResult(new List<List<IResult>> { result }, null, kanji, kanji, dict));
        }

        return kanjiResults;
    }

    private static IEnumerable<LookupResult> JmdictResultBuilder(
        Dictionary<string, IntermediaryResult> jmdictResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in jmdictResults.Values.ToList())
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    var jMDictResult = (JMdictResult)wordResult.Results[i][j];

                    List<List<string>?> rLists = jMDictResult.ROrthographyInfoList ?? new();
                    List<List<string>?> aLists = jMDictResult.AOrthographyInfoList ?? new();
                    List<string> rOrthographyInfoList = new();
                    List<string> aOrthographyInfoList = new();

                    for (int k = 0; k < rLists.Count; k++)
                    {
                        StringBuilder formattedROrthographyInfo = new();

                        for (int l = 0; l < rLists[k]?.Count; l++)
                        {
                            formattedROrthographyInfo.Append(rLists[k]![l]);
                            formattedROrthographyInfo.Append(", ");
                        }

                        rOrthographyInfoList.Add(formattedROrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    for (int k = 0; k < aLists.Count; k++)
                    {
                        StringBuilder formattedAOrthographyInfo = new();

                        for (int l = 0; l < aLists[k]?.Count; l++)
                        {
                            formattedAOrthographyInfo.Append(aLists[k]![l]);
                            formattedAOrthographyInfo.Append(", ");
                        }

                        aOrthographyInfoList.Add(formattedAOrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    LookupResult result = new
                    (
                        primarySpelling: jMDictResult.PrimarySpelling,
                        readings: jMDictResult.Readings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        edictId: jMDictResult.Id,
                        alternativeSpellings: jMDictResult.AlternativeSpellings,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(jMDictResult),
                        pOrthographyInfoList: jMDictResult.POrthographyInfoList,
                        rOrthographyInfoList: rOrthographyInfoList,
                        aOrthographyInfoList: aOrthographyInfoList,
                        dict: wordResult.Dict,
                        formattedDefinitions: jMDictResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static IEnumerable<LookupResult> JmnedictResultBuilder(
        Dictionary<string, IntermediaryResult> jmnedictResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult nameResult in jmnedictResults.Values.ToList())
        {
            int resultsListCount = nameResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = nameResult.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    var jMnedictResult = (JMnedictResult)nameResult.Results[i][j];

                    LookupResult result = new
                    (
                        edictId: jMnedictResult.Id,
                        primarySpelling: jMnedictResult.PrimarySpelling,
                        alternativeSpellings: jMnedictResult.AlternativeSpellings,
                        readings: jMnedictResult.Readings,
                        matchedText: nameResult.MatchedText,
                        deconjugatedMatchedText: nameResult.DeconjugatedMatchedText,
                        dict: nameResult.Dict,
                        formattedDefinitions: jMnedictResult.BuildFormattedDefinition()
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> KanjidicResultBuilder(
        Dictionary<string, IntermediaryResult> kanjiResults)
    {
        List<LookupResult> results = new();

        if (!kanjiResults.Any())
            return results;

        KeyValuePair<string, IntermediaryResult> dictResult = kanjiResults.First();

        List<List<IResult>> iResult = dictResult.Value.Results;
        KanjidicResult kanjiResult = (KanjidicResult)iResult[0][0];

        List<string> allReadings = new();

        if (kanjiResult.OnReadings != null)
            allReadings.AddRange(kanjiResult.OnReadings);

        if (kanjiResult.KunReadings != null)
            allReadings.AddRange(kanjiResult.KunReadings);

        if (kanjiResult.Nanori != null)
            allReadings.AddRange(kanjiResult.Nanori);

        IntermediaryResult intermediaryResult = kanjiResults.First().Value;

        LookupResult result = new
        (
            primarySpelling: dictResult.Key,
            readings: allReadings,
            onReadings: kanjiResult.OnReadings,
            kunReadings: kanjiResult.KunReadings,
            nanori: kanjiResult.Nanori,
            strokeCount: kanjiResult.StrokeCount,
            kanjiGrade: kanjiResult.Grade,
            kanjiComposition: Storage.KanjiCompositionDict.GetValueOrDefault(dictResult.Key),
            frequencies: GetKanjidicFrequencies(dictResult.Key, kanjiResult.Frequency),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiResult.BuildFormattedDefinition()
        );

        results.Add(result);
        return results;
    }

    private static List<LookupResult> YomichanKanjiResultBuilder(
    Dictionary<string, IntermediaryResult> kanjiResults)
    {
        List<LookupResult> results = new();

        if (!kanjiResults.Any())
            return results;

        string kanji = kanjiResults.First().Key;

        IntermediaryResult intermediaryResult = kanjiResults.First().Value;
        var kanjiResult = (YomichanKanjiResult)intermediaryResult.Results[0][0];

        List<string> allReadings = new();

        if (kanjiResult.OnReadings != null)
            allReadings.AddRange(kanjiResult.OnReadings);

        if (kanjiResult.KunReadings != null)
            allReadings.AddRange(kanjiResult.KunReadings);

        LookupResult result = new
        (
            primarySpelling: kanji,
            readings: allReadings,
            onReadings: kanjiResult.OnReadings,
            kunReadings: kanjiResult.KunReadings,
            kanjiComposition: Storage.KanjiCompositionDict.GetValueOrDefault(kanji),
            kanjiStats: kanjiResult.BuildFormattedStats(),
            frequencies: GetYomichanKanjiFrequencies(kanji),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiResult.BuildFormattedDefinition(intermediaryResult.Dict.Options)
        );

        results.Add(result);
        return results;
    }

    private static IEnumerable<LookupResult> EpwingYomichanResultBuilder(
        Dictionary<string, IntermediaryResult> epwingResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in epwingResults.Values.ToList())
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    var epwingResult = (EpwingYomichanResult)wordResult.Results[i][j];

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult),
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading != null
                            ? new List<string> { epwingResult.Reading }
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> EpwingNazekaResultBuilder(
        Dictionary<string, IntermediaryResult> epwingNazekaResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in epwingNazekaResults.Values.ToList())
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {

                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    var epwingResult = (EpwingNazekaResult)wordResult.Results[i][j];

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        alternativeSpellings: epwingResult.AlternativeSpellings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult),
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading != null
                            ? new List<string> { epwingResult.Reading }
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> CustomWordResultBuilder(
        Dictionary<string, IntermediaryResult> customWordResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in customWordResults.Values.ToList())
        {
            int wordResultsListCount = wordResult.Results.Count;
            for (int i = 0; i < wordResultsListCount; i++)
            {
                int wordResultCount = wordResult.Results[i].Count;

                for (int j = 0; j < wordResultCount; j++)
                {
                    var customWordDictResult = (CustomWordEntry)wordResult.Results[i][j];

                    List<LookupFrequencyResult> freqs = GetWordFrequencies(customWordDictResult);
                    foreach (LookupFrequencyResult freqResult in freqs)
                    {
                        if (freqResult.Freq == int.MaxValue)
                            freqResult.Freq = -i;
                    }

                    LookupResult result = new
                    (
                        frequencies: freqs,
                        primarySpelling: customWordDictResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        dict: wordResult.Dict,
                        readings: customWordDictResult.Readings,
                        alternativeSpellings: customWordDictResult.AlternativeSpellings,
                        formattedDefinitions: customWordDictResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> CustomNameResultBuilder(
        Dictionary<string, IntermediaryResult> customNameResults)
    {
        List<LookupResult> results = new();

        foreach (KeyValuePair<string, IntermediaryResult> customNameResult in customNameResults.ToList())
        {
            int resultsListCount = customNameResult.Value.Results.Count;
            int freq = 0;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = customNameResult.Value.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    var customNameDictResult = (CustomNameEntry)customNameResult.Value.Results[i][j];
                    LookupResult result = new
                    (
                        primarySpelling: customNameDictResult.PrimarySpelling,
                        matchedText: customNameResult.Value.MatchedText,
                        deconjugatedMatchedText: customNameResult.Value.DeconjugatedMatchedText,
                        frequencies: new() { new(customNameResult.Value.Dict.Name, -freq) },
                        dict: customNameResult.Value.Dict,
                        readings: new List<string> { customNameDictResult.Reading },
                        formattedDefinitions: customNameDictResult.BuildFormattedDefinition()
                    );

                    ++freq;
                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupFrequencyResult> GetWordFrequencies(IHasGetFrequency result)
    {
        List<LookupFrequencyResult> freqsList = new();

        foreach (Freq freq in Storage.FreqDicts.Values)
        {
            if (freq.Active && freq.Type != FreqType.YomichanKanji)
            {
                freqsList.Add(new(freq.Name, result.GetFrequency(freq)));
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult> GetYomichanKanjiFrequencies(string kanji)
    {
        List<LookupFrequencyResult> freqsList = new();

        Freq? kanjiFreq = Storage.FreqDicts.Values.FirstOrDefault(f => f.Type == FreqType.YomichanKanji);

        if (kanjiFreq?.Active ?? false)
        {
            if (kanjiFreq.Contents.TryGetValue(kanji, out List<FrequencyRecord>? freqResultList))
            {
                int frequency = freqResultList.FirstOrDefault()?.Frequency ?? int.MaxValue;

                if (frequency != int.MaxValue)
                {
                    freqsList.Add(new(kanjiFreq.Name, frequency));
                }
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult> GetKanjidicFrequencies(string kanji, int frequency)
    {
        List<LookupFrequencyResult> freqsList = new();

        if (frequency != 0)
        {
            freqsList.Add(new("Kanjidic Freq", frequency));
        }

        freqsList.AddRange(GetYomichanKanjiFrequencies(kanji));
        return freqsList;
    }

    private static string? ProcessProcess(List<List<string>>? processList)
    {
        StringBuilder deconj = new();
        bool first = true;

        int processListListCount = processList?.Count ?? 0;
        for (int i = 0; i < processListListCount; i++)
        {
            List<string> form = processList![i];

            StringBuilder formText = new();
            int added = 0;

            for (int j = form.Count - 1; j >= 0; j--)
            {
                string info = form[j];

                if (info == "")
                    continue;

                if (info.StartsWith('(') && info.EndsWith(')') && j != 0)
                    continue;

                if (added > 0)
                    formText.Append('→');

                ++added;
                formText.Append(info);
            }

            if (formText.Length != 0)
            {
                if (first)
                    deconj.Append('～');
                else
                    deconj.Append("; ");

                deconj.Append(formText);
            }

            first = false;
        }

        return deconj.Length == 0 ? null : deconj.ToString();
    }
}
