using System.Diagnostics;
using System.Text;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomDict;
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

    public static List<LookupResult>? LookupText(string text)
    {
        DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
        if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < Storage.Frontend.CoreConfig.LookupRate)
            return null;
        s_lastLookupTime = preciseTimeNow;

        if (Storage.Frontend.CoreConfig.KanjiMode)
        {
            return KanjiResultBuilder(GetKanjidicResults(text, DictType.Kanjidic));
        }

        Dictionary<string, IntermediaryResult> jMdictResults = new();
        Dictionary<string, IntermediaryResult> jMnedictResults = new();
        List<Dictionary<string, IntermediaryResult>> epwingYomichanWordResultsList = new();
        List<Dictionary<string, IntermediaryResult>> epwingNazekaWordResultsList = new();
        Dictionary<string, IntermediaryResult> kanjiResult = new();
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

        foreach ((DictType dictType, Dict dict) in Storage.Dicts.ToList())
        {
            if (dict.Active)
            {
                switch (dictType)
                {
                    case DictType.JMdict:
                        jMdictResults = GetWordResults(text, textInHiraganaList, deconjugationResultsList,
                            dictType);
                        break;
                    case DictType.JMnedict:
                        jMnedictResults = GetNameResults(text, textInHiraganaList, dictType);
                        break;
                    case DictType.Kanjidic:
                        if (dict.Options is not { RequireKanjiMode.Value: true })
                            kanjiResult = GetKanjidicResults(text, DictType.Kanjidic);
                        break;
                    case DictType.Kenkyuusha:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Daijirin:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Daijisen:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Koujien:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Meikyou:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Gakken:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Kotowaza:
                        epwingYomichanWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.CustomWordDictionary:
                        customWordResults = GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType);
                        break;
                    case DictType.CustomNameDictionary:
                        customNameResults = GetNameResults(text, textInHiraganaList, dictType);
                        break;
                    case DictType.DaijirinNazeka:
                        epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.KenkyuushaNazeka:
                        epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.ShinmeikaiNazeka:
                        epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                            deconjugationResultsList, dictType));
                        break;
                    case DictType.Kanjium:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                }
            }
        }

        List<LookupResult> lookupResults = new();

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

        if (kanjiResult.Any())
            lookupResults.AddRange(KanjiResultBuilder(kanjiResult));

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

        return lookupResults;
    }

    private static List<LookupResult> SortLookupResults(
        List<LookupResult> lookupResults)
    {
        List<LookupResult> sortedLookupResults = lookupResults
            .OrderByDescending(dict => dict.FoundForm.Length)
            .ThenBy(dict => Enum.TryParse(dict.DictType, out DictType dictType)
                ? Storage.Dicts[dictType].Priority
                : int.MaxValue)
            .ThenBy(dict => dict.Frequency).ToList();

        string longestFoundForm = sortedLookupResults.First().FoundForm;

        sortedLookupResults = sortedLookupResults
            .OrderByDescending(dict => longestFoundForm == dict.FoundSpelling)
            .ThenByDescending(dict => dict.Readings?.Contains(longestFoundForm))
            .ToList();

        return sortedLookupResults.ToList();
    }

    private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(DictType dictType,
        Dictionary<string, IntermediaryResult> results,
        HashSet<Form> deconjugationList,
        string foundForm,
        string textInHiragana,
        int succAttempt)
    {
        Dictionary<string, List<IResult>> dictionary = Storage.Dicts[dictType].Contents;

        bool tryLongVowelConversion = true;

        if (dictionary.TryGetValue(textInHiragana, out List<IResult>? tempResult))
        {
            results.TryAdd(textInHiragana,
                new IntermediaryResult(tempResult, null, foundForm,
                    dictType));
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

                    switch (dictType)
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

                                    else if (Storage.WcDict!.TryGetValue(deconjugationResult.Text,
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
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    var dictResult = (EpwingNazekaResult)dictResults[i];

                                    if (deconjugationResult.Tags.Count == 0)
                                    {
                                        resultsList.Add(dictResult);
                                    }

                                    else if (Storage.WcDict!.TryGetValue(deconjugationResult.Text,
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

                        case DictType.Kanjium:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                    }

                    if (resultsList.Any())
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? r))
                        {
                            if (r.FoundForm == deconjugationResult.OriginalText)
                                r.ProcessListList?.Add(deconjugationResult.Process);
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult(resultsList,
                                    new List<List<string>> { deconjugationResult.Process },
                                    foundForm,
                                    dictType)
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
        List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, DictType dictType)
    {
        Dictionary<string, IntermediaryResult> results = new();

        int succAttempt = 0;

        for (int i = 0; i < text.Length; i++)
        {
            (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dictType, results,
                deconjugationResultsList[i], text[..^i], textInHiraganaList[i], succAttempt);

            if (tryLongVowelConversion && textInHiraganaList[i].Contains('ー') &&
                textInHiraganaList[i][0] != 'ー')
            {
                List<string> textWithoutLongVowelMarkList = Kana.LongVowelMarkConverter(textInHiraganaList[i]);

                for (int j = 0; j < textWithoutLongVowelMarkList.Count; j++)
                {
                    succAttempt = GetWordResultsHelper(dictType, results, deconjugationResultsList[i],
                        text[..^i], textWithoutLongVowelMarkList[j], succAttempt).succAttempt;
                }
            }
        }

        return results;
    }

    private static Dictionary<string, IntermediaryResult> GetNameResults(string text,
        List<string> textInHiraganaList, DictType dictType)
    {
        Dictionary<string, IntermediaryResult> nameResults = new();

        for (int i = 0; i < text.Length; i++)
        {
            if (Storage.Dicts[dictType].Contents
                .TryGetValue(textInHiraganaList[i], out List<IResult>? result))
            {
                nameResults.TryAdd(textInHiraganaList[i],
                    new IntermediaryResult(result, null, text[..^i], dictType));
            }
        }

        return nameResults;
    }

    private static Dictionary<string, IntermediaryResult> GetKanjidicResults(string text, DictType dictType)
    {
        Dictionary<string, IntermediaryResult> kanjiResults = new();

        string? kanji = text.UnicodeIterator().FirstOrDefault();

        if (kanji != null && Storage.Dicts[DictType.Kanjidic].Contents.TryGetValue(kanji, out List<IResult>? result))
        {
            kanjiResults.Add(kanji,
                new IntermediaryResult(result, null, kanji, dictType));
        }

        return kanjiResults;
    }

    private static IEnumerable<LookupResult> JmdictResultBuilder(
        Dictionary<string, IntermediaryResult> jmdictResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in jmdictResults.Values.ToList())
        {
            int resultListCount = wordResult.ResultsList.Count;
            for (int i = 0; i < resultListCount; i++)
            {
                var jMDictResult = (JMdictResult)wordResult.ResultsList[i];

                LookupResult result = new();

                List<List<string>?> rLists = jMDictResult.ROrthographyInfoList ?? new();
                List<List<string>?> aLists = jMDictResult.AOrthographyInfoList ?? new();
                List<string> rOrthographyInfoList = new();
                List<string> aOrthographyInfoList = new();

                for (int j = 0; j < rLists.Count; j++)
                {
                    StringBuilder formattedROrthographyInfo = new();

                    for (int k = 0; k < rLists[j]?.Count; k++)
                    {
                        formattedROrthographyInfo.Append(rLists[j]![k]);
                        formattedROrthographyInfo.Append(", ");
                    }

                    rOrthographyInfoList.Add(formattedROrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                }

                for (int j = 0; j < aLists.Count; j++)
                {
                    StringBuilder formattedAOrthographyInfo = new();

                    for (int k = 0; k < aLists[j]?.Count; k++)
                    {
                        formattedAOrthographyInfo.Append(aLists[j]![k]);
                        formattedAOrthographyInfo.Append(", ");
                    }

                    aOrthographyInfoList.Add(formattedAOrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                }

                result.FoundSpelling = jMDictResult.PrimarySpelling;
                result.Readings = jMDictResult.Readings ?? new();
                result.FoundForm = wordResult.FoundForm;
                result.EdictID = jMDictResult.Id;
                result.AlternativeSpellings = jMDictResult.AlternativeSpellings ?? new();
                result.Process = ProcessProcess(wordResult);
                result.Frequency = GetJMDictFreq(jMDictResult);
                result.POrthographyInfoList = jMDictResult.POrthographyInfoList ?? new();
                result.ROrthographyInfoList = rOrthographyInfoList;
                result.AOrthographyInfoList = aOrthographyInfoList;
                result.DictType = wordResult.DictType.ToString();
                result.FormattedDefinitions = jMDictResult.Definitions != null
                    ? BuildJmdictDefinition(jMDictResult, wordResult.DictType)
                    : null;

                results.Add(result);
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
            int resultListCount = nameResult.ResultsList.Count;
            for (int i = 0; i < resultListCount; i++)
            {
                var jMnedictResult = (JMnedictResult)nameResult.ResultsList[i];

                LookupResult result = new()
                {
                    EdictID = jMnedictResult.Id,
                    FoundSpelling = jMnedictResult.PrimarySpelling,
                    AlternativeSpellings = jMnedictResult.AlternativeSpellings ?? new(),
                    Readings = jMnedictResult.Readings ?? new(),
                    FoundForm = nameResult.FoundForm,
                    DictType = nameResult.DictType.ToString(),
                    FormattedDefinitions = jMnedictResult.Definitions != null
                        ? BuildJmnedictDefinition(jMnedictResult)
                        : null
                };

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> KanjiResultBuilder(
        Dictionary<string, IntermediaryResult> kanjiResults)
    {
        List<LookupResult> results = new();
        LookupResult result = new();

        if (!kanjiResults.Any())
            return results;

        List<IResult> iResult = kanjiResults.First().Value.ResultsList;
        KanjiResult kanjiResult = (KanjiResult)iResult.First();

        result.FoundSpelling = kanjiResults.First().Key;
        result.OnReadings = kanjiResult.OnReadings ?? new();
        result.KunReadings = kanjiResult.KunReadings ?? new();
        result.Nanori = kanjiResult.Nanori ?? new();
        result.StrokeCount = kanjiResult.StrokeCount;
        result.Grade = kanjiResult.Grade;
        result.Composition = kanjiResult.Composition;
        result.Frequency = kanjiResult.Frequency;
        result.FoundForm = kanjiResults.First().Value.FoundForm;
        result.DictType = kanjiResults.First().Value.DictType.ToString();
        result.FormattedDefinitions = kanjiResult.Meanings != null
            ? string.Join(", ", kanjiResult.Meanings)
            : null;

        List<string> allReadings = new();

        if (kanjiResult.OnReadings != null)
            allReadings.AddRange(kanjiResult.OnReadings);

        if (kanjiResult.KunReadings != null)
            allReadings.AddRange(kanjiResult.KunReadings);

        if (kanjiResult.Nanori != null)
            allReadings.AddRange(kanjiResult.Nanori);

        result.Readings = allReadings;

        results.Add(result);
        return results;
    }

    private static IEnumerable<LookupResult> EpwingYomichanResultBuilder(
        Dictionary<string, IntermediaryResult> epwingResults)
    {
        List<LookupResult> results = new();

        foreach (IntermediaryResult wordResult in epwingResults.Values.ToList())
        {
            int resultListCount = wordResult.ResultsList.Count;
            for (int i = 0; i < resultListCount; i++)
            {
                var epwingResult = (EpwingYomichanResult)wordResult.ResultsList[i];

                LookupResult result = new()
                {
                    FoundSpelling = epwingResult.PrimarySpelling,
                    FoundForm = wordResult.FoundForm,
                    Process = ProcessProcess(wordResult),
                    Frequency = GetEpwingFreq(epwingResult),
                    DictType = wordResult.DictType.ToString(),
                    Readings = epwingResult.Reading != null
                        ? new List<string> { epwingResult.Reading }
                        : new(),
                    FormattedDefinitions = epwingResult.Definitions != null
                        ? BuildEpwingDefinition(epwingResult.Definitions, wordResult.DictType)
                        : null
                };

                results.Add(result);
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
            int resultListCount = wordResult.ResultsList.Count;
            for (int i = 0; i < resultListCount; i++)
            {
                var epwingResult = (EpwingNazekaResult)wordResult.ResultsList[i];

                LookupResult result = new()
                {
                    FoundSpelling = epwingResult.PrimarySpelling,
                    AlternativeSpellings = epwingResult.AlternativeSpellings ?? new(),
                    FoundForm = wordResult.FoundForm,
                    Process = ProcessProcess(wordResult),
                    Frequency = GetEpwingNazekaFreq(epwingResult),
                    DictType = wordResult.DictType.ToString(),
                    Readings = epwingResult.Reading != null
                        ? new List<string> { epwingResult.Reading }
                        : new(),
                    FormattedDefinitions = epwingResult.Definitions != null
                        ? BuildEpwingDefinition(epwingResult.Definitions, wordResult.DictType)
                        : null
                };

                results.Add(result);
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
            int wordResultCount = wordResult.ResultsList.Count;
            for (int i = 0; i < wordResultCount; i++)
            {
                var customWordDictResult = (CustomWordEntry)wordResult.ResultsList[i];
                LookupResult result = new();

                int frequency = GetCustomWordFreq(customWordDictResult);

                if (frequency == int.MaxValue)
                    frequency = wordResultCount - i;

                result.Frequency = frequency;
                result.FoundSpelling = customWordDictResult.PrimarySpelling;
                result.FoundForm = wordResult.FoundForm;
                result.Process = ProcessProcess(wordResult);
                result.DictType = wordResult.DictType.ToString();

                result.Readings = customWordDictResult.Readings ?? new();
                result.AlternativeSpellings = customWordDictResult.AlternativeSpellings ?? new();
                result.FormattedDefinitions = BuildCustomWordDefinition(customWordDictResult, wordResult.DictType);

                results.Add(result);
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
            int resultCount = customNameResult.Value.ResultsList.Count;
            for (int i = 0; i < resultCount; i++)
            {
                var customNameDictResult = (CustomNameEntry)customNameResult.Value.ResultsList[i];
                LookupResult result = new()
                {
                    FoundSpelling = customNameDictResult.PrimarySpelling,
                    FoundForm = customNameResult.Value.FoundForm,
                    Frequency = resultCount - i,
                    DictType = customNameResult.Value.DictType.ToString(),
                    Readings = new List<string> { customNameDictResult.Reading },
                    FormattedDefinitions = BuildCustomNameDefinition(customNameDictResult),
                };
                results.Add(result);
            }
        }

        return results;
    }

    private static int GetJMDictFreq(JMdictResult jMDictResult)
    {
        int frequency = int.MaxValue;

        Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName,
            out Dictionary<string, List<FrequencyEntry>>? freqDict);

        if (freqDict == null)
            return frequency;

        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(jMDictResult.PrimarySpelling),
                out List<FrequencyEntry>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyEntry freqResult = freqResults[i];

                if ((jMDictResult.Readings != null && jMDictResult.Readings.Contains(freqResult.Spelling))
                    || (jMDictResult.Readings == null && jMDictResult.PrimarySpelling == freqResult.Spelling))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency == int.MaxValue && jMDictResult.AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = jMDictResult.AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(jMDictResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                            if (jMDictResult.Readings != null
                                && jMDictResult.Readings.Contains(alternativeSpellingFreqResult.Spelling))
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

        else if (jMDictResult.Readings != null)
        {
            int readingCount = jMDictResult.Readings.Count;
            for (int i = 0; i < readingCount; i++)
            {
                string reading = jMDictResult.Readings[i];

                if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                        out List<FrequencyEntry>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyEntry readingFreqResult = readingFreqResults[j];

                        if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                            || (jMDictResult.AlternativeSpellings != null
                                && jMDictResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
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

    private static int GetEpwingNazekaFreq(EpwingNazekaResult epwingNazekaResult)
    {
        int frequency = int.MaxValue;

        Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName,
            out Dictionary<string, List<FrequencyEntry>>? freqDict);

        if (freqDict == null)
            return frequency;

        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingNazekaResult.PrimarySpelling),
                out List<FrequencyEntry>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyEntry freqResult = freqResults[i];

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
                    if (freqDict.TryGetValue(
                            Kana.KatakanaToHiraganaConverter(epwingNazekaResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

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

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                    out List<FrequencyEntry>? readingFreqResults))
            {
                int readingFreqResultsCount = readingFreqResults.Count;
                for (int j = 0; j < readingFreqResultsCount; j++)
                {
                    FrequencyEntry readingFreqResult = readingFreqResults[j];

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

    private static int GetEpwingFreq(EpwingYomichanResult epwingYomichanResult)
    {
        int frequency = int.MaxValue;

        Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName,
            out Dictionary<string, List<FrequencyEntry>>? freqDict);

        if (freqDict == null)
            return frequency;

        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingYomichanResult.PrimarySpelling),
                out List<FrequencyEntry>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyEntry freqResult = freqResults[i];

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
                 && freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingYomichanResult.Reading),
                     out List<FrequencyEntry>? readingFreqResults))
        {
            int readingFreqResultsCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultsCount; i++)
            {
                FrequencyEntry readingFreqResult = readingFreqResults[i];

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

    private static int GetCustomWordFreq(CustomWordEntry customWordResult)
    {
        int frequency = int.MaxValue;

        Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName,
            out Dictionary<string, List<FrequencyEntry>>? freqDict);

        if (freqDict == null)
            return frequency;

        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(customWordResult.PrimarySpelling),
                out List<FrequencyEntry>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyEntry freqResult = freqResults[i];

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
                    if (freqDict.TryGetValue(
                            Kana.KatakanaToHiraganaConverter(customWordResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

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

                if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                        out List<FrequencyEntry>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyEntry readingFreqResult = readingFreqResults[j];

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

    private static string BuildJmdictDefinition(JMdictResult jMDictResult, DictType dictType)
    {
        bool newlines = Storage.Dicts[dictType].Options is { NewlineBetweenDefinitions.Value: true };
        string separator = newlines
            ? "\n"
            : "";
        int count = 1;
        StringBuilder defResult = new();

        int definitionCount = jMDictResult.Definitions?.Count ?? 0;

        for (int i = 0; i < definitionCount; i++)
        {
            if (newlines)
                defResult.Append($"({count}) ");

            if (jMDictResult.WordClasses?[i]?.Any() ?? false)
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.WordClasses[i]!));
                defResult.Append(") ");
            }

            if (!newlines)
                defResult.Append($"({count}) ");

            if (jMDictResult.Dialects?[i]?.Any() ?? false)
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.Dialects[i]!));
                defResult.Append(") ");
            }

            if (jMDictResult.DefinitionInfo != null && jMDictResult.DefinitionInfo.Any() &&
                jMDictResult.DefinitionInfo[i] != null)
            {
                defResult.Append('(');
                defResult.Append(jMDictResult.DefinitionInfo[i]);
                defResult.Append(") ");
            }

            if (jMDictResult.MiscList?[i]?.Any() ?? false)
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.MiscList[i]!));
                defResult.Append(") ");
            }

            if (jMDictResult.TypeList?[i]?.Any() ?? false)
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", jMDictResult.TypeList[i]!));
                defResult.Append(") ");
            }

            defResult.Append(string.Join("; ", jMDictResult.Definitions![i]) + " ");

            if ((jMDictResult.RRestrictions?[i]?.Any() ?? false) ||
                (jMDictResult.KRestrictions?[i]?.Any() ?? false))
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

            defResult.Append(separator);

            ++count;
        }

        return defResult.ToString().Trim('\n');
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

    private static string? BuildEpwingDefinition(List<string>? epwingDefinitions, DictType dictType)
    {
        if (epwingDefinitions == null)
            return null;

        StringBuilder defResult = new();

        string separator = Storage.Dicts[dictType].Options is { NewlineBetweenDefinitions.Value: true }
            ? "\n"
            : "; ";

        for (int i = 0; i < epwingDefinitions.Count; i++)
        {
            defResult.Append(epwingDefinitions[i] + separator);
        }

        return defResult.ToString().Trim('\n');
    }

    private static string BuildCustomWordDefinition(CustomWordEntry customWordResult, DictType dictType)
    {
        string separator = Storage.Dicts[dictType].Options is { NewlineBetweenDefinitions.Value: true }
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

        return defResult.ToString().Trim('\n');
    }

    private static string BuildCustomNameDefinition(CustomNameEntry customNameDictResult)
    {
        return $"({customNameDictResult.NameType.ToLower()}) {customNameDictResult.Reading}";
    }

    private static string? ProcessProcess(IntermediaryResult intermediaryResult)
    {
        StringBuilder deconj = new();
        bool first = true;

        int processListListCount = intermediaryResult.ProcessListList?.Count ?? 0;
        for (int i = 0; i < processListListCount; i++)
        {
            List<string> form = intermediaryResult.ProcessListList![i];

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
