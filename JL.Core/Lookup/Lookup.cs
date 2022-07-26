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
using JL.Core.Frequency;
using JL.Core.PoS;
using JL.Core.Utilities;

namespace JL.Core.Lookup;

public static class Lookup
{
    private static DateTime s_lastLookupTime;

    // public static readonly LRUCache<string, List<LookupResult>?> LookupResultCache = new(
    //     Storage.CacheSize, Storage.CacheSize / 8);

    public static List<LookupResult>? LookupText(string text, bool useCache = true)
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
            Dict? kanjidic = Storage.Dicts.Values.FirstOrDefault(dict => dict.Type == DictType.Kanjidic);
            Dict? kanjigen = Storage.Dicts.Values.FirstOrDefault(dict => dict.Type == DictType.KanjigenYomichan);

            if (kanjidic?.Active ?? false)
            {
                lookupResults.AddRange(KanjidicResultBuilder(GetKanjiResults(text, kanjidic)));
            }

            if (kanjigen?.Active ?? false)
            {
                lookupResults.AddRange(EpwingYomichanResultBuilder(GetKanjiResults(text, kanjigen)));
            }

            return lookupResults.Any() ? SortLookupResults(lookupResults) : null;
        }

        Dictionary<string, IntermediaryResult> jMdictResults = new();
        Dictionary<string, IntermediaryResult> jMnedictResults = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanWordResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingNazekaWordResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanKanjiResultsList = new();
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
                        epwingYomichanKanjiResultsList.Add(GetKanjiResults(text, dict));
                        break;

                    case DictType.CustomWordDictionary:
                        customWordResults = GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dict);
                        break;

                    case DictType.CustomNameDictionary:
                        customNameResults = GetNameResults(text, textInHiraganaList, dict);
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
                    case DictType.NonspecificYomichan:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dict));
                        break;

                    case DictType.DaijirinNazeka:
                    case DictType.KenkyuushaNazeka:
                    case DictType.ShinmeikaiNazeka:
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

        if (epwingYomichanWordResultsList.Any())
        {
            for (int i = 0; i < epwingYomichanWordResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingYomichanResultBuilder(epwingYomichanWordResultsList[i]));
            }
        }

        if (jMnedictResults.Any())
            lookupResults.AddRange(JmnedictResultBuilder(jMnedictResults));

        if (kanjidicResults.Any())
            lookupResults.AddRange(KanjidicResultBuilder(kanjidicResults));

        if (epwingYomichanKanjiResultsList.Any())
        {
            for (int i = 0; i < epwingYomichanKanjiResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingYomichanResultBuilder(epwingYomichanKanjiResultsList[i]));
            }
        }

        if (customWordResults.Any())
            lookupResults.AddRange(CustomWordResultBuilder(customWordResults));

        if (customNameResults.Any())
            lookupResults.AddRange(CustomNameResultBuilder(customNameResults));

        if (epwingNazekaWordResultsList.Any())
        {
            for (int i = 0; i < epwingNazekaWordResultsList.Count; i++)
            {
                lookupResults.AddRange(EpwingNazekaResultBuilder(epwingNazekaWordResultsList[i]));
            }
        }

        if (lookupResults.Any())
            lookupResults = SortLookupResults(lookupResults);

        // if (useCache)
        //     LookupResultCache.AddReplace(text, lookupResults.ToList());

        return lookupResults;
    }

    private static List<LookupResult> SortLookupResults(
    List<LookupResult> lookupResults)
    {
        string longestFoundForm = lookupResults.Aggregate((r1, r2) => r1.FoundForm.Length > r2.FoundForm.Length ? r1 : r2).FoundForm;

        return lookupResults
            .OrderByDescending(dict => longestFoundForm == dict.FoundSpelling)
            .ThenByDescending(dict => dict.Readings?.Contains(longestFoundForm))
            .ThenByDescending(dict => dict.FoundForm.Length)
            .ThenByDescending(dict => longestFoundForm.Length >= dict.FoundSpelling.Length && longestFoundForm[..dict.FoundSpelling.Length] == dict.FoundSpelling)
            .ThenBy(dict => dict.Dict?.Priority ?? int.MaxValue)
            .ThenBy(dict => dict.Frequencies.Count > 0 ? dict.Frequencies.First().Freq : int.MaxValue)
            .ToList();
    }

    private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(Dict dict,
        Dictionary<string, IntermediaryResult> results,
        HashSet<Form> deconjugationList,
        string foundForm,
        string textInHiragana,
        int succAttempt)
    {
        Dictionary<string, List<IResult>> dictionary = dict.Contents;

        bool tryLongVowelConversion = true;

        if (dictionary.TryGetValue(textInHiragana, out List<IResult>? tempResult))
        {
            results.TryAdd(textInHiragana,
                new IntermediaryResult(new List<List<IResult>> { tempResult }, null, foundForm,
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
                            if (r.FoundForm == deconjugationResult.OriginalText)
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
                                    foundForm,
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
                    new IntermediaryResult(new List<List<IResult>> { result }, null, text[..^i], dict));
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
                new IntermediaryResult(new List<List<IResult>> { result }, null, kanji, dict));
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

                    LookupResult result = new()
                    {
                        FoundSpelling = jMDictResult.PrimarySpelling,
                        Readings = jMDictResult.Readings ?? new(),
                        FoundForm = wordResult.FoundForm,
                        EdictId = jMDictResult.Id,
                        AlternativeSpellings = jMDictResult.AlternativeSpellings ?? new(),
                        Process = ProcessProcess(wordResult.Processes?[i]),
                        Frequencies = GetFrequencies(jMDictResult, wordResult.Dict),
                        POrthographyInfoList = jMDictResult.POrthographyInfoList ?? new(),
                        ROrthographyInfoList = rOrthographyInfoList,
                        AOrthographyInfoList = aOrthographyInfoList,
                        Dict = wordResult.Dict,
                        FormattedDefinitions = BuildJmdictDefinition(jMDictResult, wordResult.Dict),
                    };

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

                    LookupResult result = new()
                    {
                        EdictId = jMnedictResult.Id,
                        FoundSpelling = jMnedictResult.PrimarySpelling,
                        AlternativeSpellings = jMnedictResult.AlternativeSpellings ?? new(),
                        Readings = jMnedictResult.Readings ?? new(),
                        FoundForm = nameResult.FoundForm,
                        Dict = nameResult.Dict,
                        FormattedDefinitions = jMnedictResult.Definitions != null
                            ? BuildJmnedictDefinition(jMnedictResult)
                            : null
                    };

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

        List<List<IResult>> iResult = kanjiResults.First().Value.Results;
        KanjiResult kanjiResult = (KanjiResult)iResult[0][0];

        List<string> allReadings = new();

        if (kanjiResult.OnReadings != null)
            allReadings.AddRange(kanjiResult.OnReadings);

        if (kanjiResult.KunReadings != null)
            allReadings.AddRange(kanjiResult.KunReadings);

        if (kanjiResult.Nanori != null)
            allReadings.AddRange(kanjiResult.Nanori);

        LookupResult result = new()
        {
            FoundSpelling = kanjiResults.First().Key,
            Readings = allReadings,
            OnReadings = kanjiResult.OnReadings ?? new(),
            KunReadings = kanjiResult.KunReadings ?? new(),
            Nanori = kanjiResult.Nanori ?? new(),
            StrokeCount = kanjiResult.StrokeCount,
            Grade = kanjiResult.Grade,
            Composition = kanjiResult.Composition,
            Frequencies = new() { new(kanjiResults.First().Value.Dict.Name, kanjiResult.Frequency) },
            FoundForm = kanjiResults.First().Value.FoundForm,
            Dict = kanjiResults.First().Value.Dict,
            FormattedDefinitions = kanjiResult.Meanings != null
            ? string.Join(", ", kanjiResult.Meanings)
            : null,
        };

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

                    LookupResult result = new()
                    {
                        FoundSpelling = epwingResult.PrimarySpelling,
                        FoundForm = wordResult.FoundForm,
                        Process = ProcessProcess(wordResult.Processes?[i]),
                        Frequencies = GetFrequencies(epwingResult, wordResult.Dict),
                        Dict = wordResult.Dict,
                        Readings = epwingResult.Reading != null
                            ? new List<string> { epwingResult.Reading }
                            : new(),
                        FormattedDefinitions = epwingResult.Definitions != null
                            ? BuildEpwingDefinition(epwingResult.Definitions, wordResult.Dict)
                            : null
                    };

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

                    LookupResult result = new()
                    {
                        FoundSpelling = epwingResult.PrimarySpelling,
                        AlternativeSpellings = epwingResult.AlternativeSpellings ?? new(),
                        FoundForm = wordResult.FoundForm,
                        Process = ProcessProcess(wordResult.Processes?[i]),
                        Frequencies = GetFrequencies(epwingResult, wordResult.Dict),
                        Dict = wordResult.Dict,
                        Readings = epwingResult.Reading != null
                            ? new List<string> { epwingResult.Reading }
                            : new(),
                        FormattedDefinitions = epwingResult.Definitions != null
                            ? BuildEpwingDefinition(epwingResult.Definitions, wordResult.Dict)
                            : null
                    };

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

                    List<LookupFrequencyResult> freqs = GetFrequencies(customWordDictResult, wordResult.Dict);
                    foreach (LookupFrequencyResult freqResult in freqs)
                    {
                        if (freqResult.Freq == int.MaxValue)
                            freqResult.Freq = -i;
                    }

                    LookupResult result = new()
                    {
                        Frequencies = freqs,
                        FoundSpelling = customWordDictResult.PrimarySpelling,
                        FoundForm = wordResult.FoundForm,
                        Process = ProcessProcess(wordResult.Processes?[i]),
                        Dict = wordResult.Dict,
                        Readings = customWordDictResult.Readings ?? new(),
                        AlternativeSpellings = customWordDictResult.AlternativeSpellings ?? new(),
                        FormattedDefinitions = BuildCustomWordDefinition(customWordDictResult, wordResult.Dict),
                    };

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
                    LookupResult result = new()
                    {
                        FoundSpelling = customNameDictResult.PrimarySpelling,
                        FoundForm = customNameResult.Value.FoundForm,
                        Frequencies = new() { new(customNameResult.Value.Dict.Name, -freq) },
                        Dict = customNameResult.Value.Dict,
                        Readings = new List<string> { customNameDictResult.Reading },
                        FormattedDefinitions = BuildCustomNameDefinition(customNameDictResult),
                    };

                    ++freq;
                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupFrequencyResult> GetFrequencies(IResult result, Dict dict)
    {
        List<LookupFrequencyResult> freqsList = new();

        foreach (Freq freq in Storage.FreqDicts.Values)
        {
            if (freq.Active)
            {
                switch (dict.Type)
                {
                    case DictType.CustomWordDictionary:
                        freqsList.Add(new(freq.Name, GetCustomWordFreq((CustomWordEntry)result, freq)));
                        break;

                    case DictType.JMdict:
                        freqsList.Add(new(freq.Name, GetJmdictFreq((JMdictResult)result, freq)));
                        break;

                    case DictType.DaijirinNazeka:
                    case DictType.KenkyuushaNazeka:
                    case DictType.ShinmeikaiNazeka:
                    case DictType.NonspecificNazeka:
                        freqsList.Add(new(freq.Name, GetEpwingNazekaFreq((EpwingNazekaResult)result, freq)));
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
                    case DictType.KanjigenYomichan:
                    case DictType.KireiCakeYomichan:
                    case DictType.NonspecificYomichan:
                        freqsList.Add(new(freq.Name, GetEpwingFreq((EpwingYomichanResult)result, freq)));
                        break;
                }
            }
        }

        return freqsList;
    }

    private static int GetJmdictFreq(JMdictResult jmdictResult, Freq freq)
    {
        int frequency = int.MaxValue;
        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(jmdictResult.PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if ((jmdictResult.Readings != null && jmdictResult.Readings.Contains(freqResult.Spelling))
                    || (jmdictResult.Readings == null && jmdictResult.PrimarySpelling == freqResult.Spelling))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency == int.MaxValue && jmdictResult.AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = jmdictResult.AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(jmdictResult.AlternativeSpellings[i]),
                            out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                            if (jmdictResult.Readings != null
                                && jmdictResult.Readings.Contains(alternativeSpellingFreqResult.Spelling))
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (jmdictResult.Readings != null)
        {
            int readingCount = jmdictResult.Readings.Count;
            for (int i = 0; i < readingCount; i++)
            {
                string reading = jmdictResult.Readings[i];

                if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                        out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];

                        if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                            || (jmdictResult.AlternativeSpellings != null
                                && jmdictResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                        {
                            if (frequency > readingFreqResult.Frequency)
                            {
                                frequency = readingFreqResult.Frequency;
                            }
                        }
                    }
                }
            }
        }

        return frequency;
    }

    private static int GetEpwingNazekaFreq(EpwingNazekaResult epwingNazekaResult, Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingNazekaResult.PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if ((epwingNazekaResult.Reading == freqResult.Spelling)
                    || (epwingNazekaResult.Reading == null &&
                        epwingNazekaResult.PrimarySpelling == freqResult.Spelling))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency == int.MaxValue && epwingNazekaResult.AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = epwingNazekaResult.AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(
                            Kana.KatakanaToHiraganaConverter(epwingNazekaResult.AlternativeSpellings[i]),
                            out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                            if (epwingNazekaResult.Reading == alternativeSpellingFreqResult.Spelling)
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (epwingNazekaResult.Reading != null)
        {
            string reading = epwingNazekaResult.Reading;

            if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                    out List<FrequencyRecord>? readingFreqResults))
            {
                int readingFreqResultsCount = readingFreqResults.Count;
                for (int j = 0; j < readingFreqResultsCount; j++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[j];

                    if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                        || (epwingNazekaResult.AlternativeSpellings != null
                            && epwingNazekaResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                    {
                        if (frequency > readingFreqResult.Frequency)
                        {
                            frequency = readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return frequency;
    }

    private static int GetEpwingFreq(EpwingYomichanResult epwingYomichanResult, Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingYomichanResult.PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if (epwingYomichanResult.Reading == freqResult.Spelling
                    || (string.IsNullOrEmpty(epwingYomichanResult.Reading)
                        && epwingYomichanResult.PrimarySpelling == freqResult.Spelling))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }
        }

        else if (!string.IsNullOrEmpty(epwingYomichanResult.Reading)
                 && freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingYomichanResult.Reading),
                     out List<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultsCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultsCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];

                if (epwingYomichanResult.Reading == readingFreqResult.Spelling && Kana.IsKatakana(epwingYomichanResult.Reading))
                {
                    if (frequency > readingFreqResult.Frequency)
                    {
                        frequency = readingFreqResult.Frequency;
                    }
                }
            }
        }

        return frequency;
    }

    private static int GetCustomWordFreq(CustomWordEntry customWordResult, Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(customWordResult.PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if (customWordResult.Readings != null && customWordResult.Readings.Contains(freqResult.Spelling)
                    || (customWordResult.Readings == null
                        && customWordResult.PrimarySpelling == freqResult.Spelling))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency == int.MaxValue && customWordResult.AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = customWordResult.AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(
                            Kana.KatakanaToHiraganaConverter(customWordResult.AlternativeSpellings[i]),
                            out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                            if (customWordResult.Readings != null
                                && customWordResult.Readings.Contains(alternativeSpellingFreqResult.Spelling)
                               )
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (customWordResult.Readings != null)
        {
            int readingCount = customWordResult.Readings.Count;
            for (int i = 0; i < readingCount; i++)
            {
                string reading = customWordResult.Readings[i];

                if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                        out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];

                        if ((reading == readingFreqResult.Spelling && Kana.IsKatakana(reading))
                            || (customWordResult.AlternativeSpellings != null
                                && customWordResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                        {
                            if (frequency > readingFreqResult.Frequency)
                            {
                                frequency = readingFreqResult.Frequency;
                            }
                        }
                    }
                }
            }
        }

        return frequency;
    }

    private static string BuildJmdictDefinition(JMdictResult jMDictResult, Dict dict)
    {
        bool newlines = dict.Options is { NewlineBetweenDefinitions.Value: true };

        string separator = newlines ? "\n" : "";

        int count = 1;

        StringBuilder defResult = new();

        int definitionCount = jMDictResult.Definitions.Count;

        for (int i = 0; i < definitionCount; i++)
        {
            if (newlines)
                defResult.Append($"({count}) ");

            if ((dict.Options?.WordClassInfo?.Value ?? true) && (jMDictResult.WordClasses?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.WordClasses[i]!));
                defResult.Append(") ");
            }

            if (!newlines)
                defResult.Append($"({count}) ");

            if ((dict.Options?.DialectInfo?.Value ?? true) && (jMDictResult.Dialects?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.Dialects[i]!));
                defResult.Append(") ");
            }

            if ((dict.Options?.ExtraDefinitionInfo?.Value ?? true)
                && (jMDictResult.DefinitionInfo?.Any() ?? false)
                && jMDictResult.DefinitionInfo[i] != null)
            {
                defResult.Append('(');
                defResult.Append(jMDictResult.DefinitionInfo[i]);
                defResult.Append(") ");
            }

            if ((dict.Options?.MiscInfo?.Value ?? true) && (jMDictResult.MiscList?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.MiscList[i]!));
                defResult.Append(") ");
            }

            if ((dict.Options?.WordTypeInfo?.Value ?? true) && (jMDictResult.FieldList?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.FieldList[i]!));
                defResult.Append(") ");
            }

            defResult.Append(string.Join("; ", jMDictResult.Definitions[i]) + " ");

            if ((dict.Options?.SpellingRestrictionInfo?.Value ?? true)
                && ((jMDictResult.RRestrictions?[i]?.Any() ?? false)
                    || (jMDictResult.KRestrictions?[i]?.Any() ?? false)))
            {
                defResult.Append("(only applies to ");

                if (jMDictResult.KRestrictions?[i]?.Any() ?? false)
                {
                    defResult.Append(string.Join("; ", jMDictResult.KRestrictions[i]!));
                }

                if (jMDictResult.RRestrictions?[i]?.Any() ?? false)
                {
                    if (jMDictResult.KRestrictions?[i]?.Any() ?? false)
                        defResult.Append("; ");

                    defResult.Append(string.Join("; ", jMDictResult.RRestrictions[i]!));
                }

                defResult.Append(") ");
            }

            if ((dict.Options?.LoanwordEtymology?.Value ?? true) && (jMDictResult.LoanwordEtymology?[i]?.Any() ?? false))
            {
                defResult.Append('(');

                List<LSource> lSources = jMDictResult.LoanwordEtymology[i]!;

                int lSourceCount = lSources.Count;
                for (int j = 0; j < lSourceCount; j++)
                {
                    if (lSources[j].IsWasei)
                        defResult.Append("Wasei ");

                    defResult.Append(lSources[j].Language);

                    if (lSources[j].OriginalWord != null)
                    {
                        defResult.Append(": ");
                        defResult.Append(lSources[j].OriginalWord);
                    }

                    if (j + 1 < lSourceCount)
                    {
                        defResult.Append(lSources[j].IsPart ? " + " : ", ");
                    }
                }

                defResult.Append(") ");
            }

            if ((dict.Options?.RelatedTerm?.Value ?? false) && (jMDictResult.RelatedTerms?[i]?.Any() ?? false))
            {
                defResult.Append("(related terms: ");
                defResult.Append(string.Join(", ", jMDictResult.RelatedTerms[i]!));
                defResult.Append(") ");
            }

            if ((dict.Options?.Antonym?.Value ?? false) && (jMDictResult.Antonyms?[i]?.Any() ?? false))
            {
                defResult.Append("(antonyms: ");
                defResult.Append(string.Join(", ", jMDictResult.Antonyms[i]!));
                defResult.Append(") ");
            }

            defResult.Append(separator);

            ++count;
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    private static string BuildJmnedictDefinition(JMnedictResult jMnedictResult)
    {
        int count = 1;
        StringBuilder defResult = new();

        if (jMnedictResult.NameTypes != null &&
            (jMnedictResult.NameTypes.Count > 1 || !jMnedictResult.NameTypes.Contains("unclass")))
        {
            for (int i = 0; i < jMnedictResult.NameTypes.Count; i++)
            {
                defResult.Append('(');
                defResult.Append(jMnedictResult.NameTypes[i]);
                defResult.Append(") ");
            }
        }

        for (int i = 0; i < jMnedictResult.Definitions?.Count; i++)
        {
            if (jMnedictResult.Definitions.Any())
            {
                if (jMnedictResult.Definitions.Count > 0)
                    defResult.Append($"({count}) ");

                defResult.Append($"{string.Join("; ", jMnedictResult.Definitions[i])} ");
                ++count;
            }
        }

        return defResult.ToString();
    }

    private static string? BuildEpwingDefinition(List<string>? epwingDefinitions, Dict dict)
    {
        if (epwingDefinitions == null)
            return null;

        StringBuilder defResult = new();

        string separator = dict.Options?.NewlineBetweenDefinitions?.Value ?? true
            ? "\n"
            : "; ";

        for (int i = 0; i < epwingDefinitions.Count; i++)
        {
            defResult.Append(epwingDefinitions[i] + separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    private static string BuildCustomWordDefinition(CustomWordEntry customWordResult, Dict dict)
    {
        string separator = dict.Options is { NewlineBetweenDefinitions.Value: true }
            ? "\n"
            : "";
        int count = 1;
        StringBuilder defResult = new();

        if (customWordResult.WordClasses.Any())
        {
            string tempWordClass;
            if (customWordResult.WordClasses.Contains("adj-i"))
                tempWordClass = "adjective";
            else if (customWordResult.WordClasses.Contains("v1"))
                tempWordClass = "verb";
            else if (customWordResult.WordClasses.Contains("noun"))
                tempWordClass = "noun";
            else
                tempWordClass = "other";

            defResult.Append($"({tempWordClass}) ");
        }

        for (int i = 0; i < customWordResult.Definitions.Count; i++)
        {
            if (customWordResult.Definitions.Any())
            {
                defResult.Append($"({count}) ");
                defResult.Append(string.Join("; ", customWordResult.Definitions[i]) + " ");
                defResult.Append(separator);

                ++count;
            }
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    private static string BuildCustomNameDefinition(CustomNameEntry customNameDictResult)
    {
        return $"({customNameDictResult.NameType.ToLower()}) {customNameDictResult.Reading}";
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
