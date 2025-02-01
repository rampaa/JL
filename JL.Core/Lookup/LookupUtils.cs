using System.Collections.Concurrent;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core.Lookup;

public static class LookupUtils
{
    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms, string parameterOrQuery);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string dbName, string term);

    public static LookupResult[]? LookupText(string text)
    {
        bool dbIsUsedForPitchDict = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict)
            && pitchDict is { Active: true, Options.UseDB.Value: true, Ready: true };

        ConcurrentBag<LookupResult> lookupResults = [];

        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        List<Freq>? kanjiFreqs = FreqUtils.KanjiFreqs;
        string kanji = "";
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = text.EnumerateRunes().First().ToString();

            kanjiFrequencyResults = kanjiFreqs is not null
                ? GetKanjiFrequencies(kanji, kanjiFreqs)
                : null;
        }

        List<Freq>? wordFreqs = FreqUtils.WordFreqs;
        List<Freq>? dbWordFreqs = null;
        if (wordFreqs is not null)
        {
            dbWordFreqs = wordFreqs
                .Where(static f => f is { Options.UseDB.Value: true, Ready: true })
                .ToList();

            if (dbWordFreqs.Count is 0)
            {
                dbWordFreqs = null;
            }
        }

        List<string> textList = new(text.Length);
        List<string> textInHiraganaList = new(text.Length);
        List<List<Form>> deconjugationResultsList = new(text.Length);
        List<List<string>?>? textWithoutLongVowelMarkList = null;

        bool doesNotStartWithLongVowelMark = text[0] is not 'ー';
        bool countLongVowelMark = doesNotStartWithLongVowelMark;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[text.Length - i - 1]))
            {
                continue;
            }

            string currentText = text[..^i];

            textList.Add(currentText);

            string textInHiragana = JapaneseUtils.KatakanaToHiragana(currentText);
            textInHiraganaList.Add(textInHiragana);

            deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));

            if (doesNotStartWithLongVowelMark)
            {
                int longVowelMarkCount = 0;
                if (countLongVowelMark)
                {
                    foreach (char c in textInHiragana)
                    {
                        if (c is 'ー')
                        {
                            ++longVowelMarkCount;
                        }
                    }
                }

                if (longVowelMarkCount > 0)
                {
                    textWithoutLongVowelMarkList ??= new List<List<string>?>(text.Length);
                    textWithoutLongVowelMarkList.Add(longVowelMarkCount < 5
                        ? JapaneseUtils.LongVowelMarkToKana(textInHiragana)
                        : null);
                }
                else
                {
                    textWithoutLongVowelMarkList?.Add(null);
                    countLongVowelMark = false;
                }
            }
        }

        string? parameter = null;
        string? verbParameter = null;
        string? yomichanWordQuery = null;
        string? yomichanVerbQuery = null;
        string? nazekaWordQuery = null;
        string? nazekaVerbQuery = null;
        List<string?>? nazekaTextWithoutLongVowelMarkQueries = null;
        List<string?>? yomichanTextWithoutLongVowelMarkQueries = null;
        List<string?>? jmdictTextWithoutLongVowelMarkParameters = null;

        bool dbIsUsedForAtLeastOneWordFreqDict = wordFreqs is not null;
        List<string>? deconjugatedTexts = DictUtils.DBIsUsedForAtLeastOneWordDict || dbIsUsedForPitchDict || dbIsUsedForAtLeastOneWordFreqDict
            ? deconjugationResultsList.SelectMany(static lf => lf.Select(static f => f.Text)).Distinct().ToList()
            : null;

        if (DictUtils.DBIsUsedForAtLeastOneDict)
        {
            parameter = DBUtils.GetParameter(textInHiraganaList.Count);

            if (deconjugatedTexts?.Count > 0)
            {
                verbParameter = DBUtils.GetParameter(deconjugatedTexts.Count);
            }

            if (textWithoutLongVowelMarkList is not null)
            {
                if (DictUtils.DBIsUsedForJmdict)
                {
                    jmdictTextWithoutLongVowelMarkParameters ??= new List<string?>(textWithoutLongVowelMarkList.Count);
                }
                if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                {
                    yomichanTextWithoutLongVowelMarkQueries ??= new List<string?>(textWithoutLongVowelMarkList.Count);
                }
                if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                {
                    nazekaTextWithoutLongVowelMarkQueries ??= new List<string?>(textWithoutLongVowelMarkList.Count);
                }

                for (int i = 0; i < textWithoutLongVowelMarkList.Count; i++)
                {
                    List<string>? textWithoutLongVowelMark = textWithoutLongVowelMarkList[i];
                    if (textWithoutLongVowelMark is not null)
                    {
                        if (DictUtils.DBIsUsedForJmdict)
                        {
                            jmdictTextWithoutLongVowelMarkParameters!.Add(DBUtils.GetParameter(textWithoutLongVowelMark.Count));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                        {
                            yomichanTextWithoutLongVowelMarkQueries!.Add(EpwingYomichanDBManager.GetQuery(textWithoutLongVowelMark));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                        {
                            nazekaTextWithoutLongVowelMarkQueries!.Add(EpwingNazekaDBManager.GetQuery(textWithoutLongVowelMark));
                        }
                    }
                    else
                    {
                        jmdictTextWithoutLongVowelMarkParameters?.Add(null);
                        yomichanTextWithoutLongVowelMarkQueries?.Add(null);
                        nazekaTextWithoutLongVowelMarkQueries?.Add(null);
                    }
                }
            }

            if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
            {
                yomichanWordQuery = EpwingYomichanDBManager.GetQuery(parameter);

                if (verbParameter is not null)
                {
                    yomichanVerbQuery = EpwingYomichanDBManager.GetQuery(verbParameter);
                }
            }

            if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
            {
                nazekaWordQuery = EpwingNazekaDBManager.GetQuery(parameter);

                if (verbParameter is not null)
                {
                    nazekaVerbQuery = EpwingNazekaDBManager.GetQuery(verbParameter);
                }
            }
        }

        bool dbIsUsedAtLeastOneYomichanOrNazekaWordDict = DictUtils.DBIsUsedAtLeastOneYomichanOrNazekaWordDict;
        HashSet<string>? allSearchKeys = dbIsUsedForPitchDict || dbIsUsedAtLeastOneYomichanOrNazekaWordDict
            ? textWithoutLongVowelMarkList is not null
                ? new HashSet<string>([.. textInHiraganaList, .. deconjugatedTexts!, .. textWithoutLongVowelMarkList.Where(static tl => tl is not null).SelectMany(static textList => textList!)])
                : new HashSet<string>([.. textInHiraganaList, .. deconjugatedTexts!])
            : null;

        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (allSearchKeys is not null)
        {
            Parallel.Invoke(
            () =>
            {
                frequencyDicts = dbWordFreqs is not null && dbIsUsedAtLeastOneYomichanOrNazekaWordDict
                    ? GetFrequencyDictsFromDB(dbWordFreqs, allSearchKeys)
                    : null;
            },
            () =>
            {
                pitchAccentDict = dbIsUsedForPitchDict
                    ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, allSearchKeys)
                    : pitchDict?.Contents;
            });
        }
        else
        {
            pitchAccentDict = pitchDict?.Contents;
        }

        List<Dict> dicts = DictUtils.Dicts.Values.Where(static dict => dict is { Active: true, Type: not DictType.PitchAccentYomichan }).ToList();
        _ = Parallel.ForEach(dicts, dict =>
        {
            bool useDB = dict.Options.UseDB.Value && dict.Ready;
            switch (dict.Type)
            {
                case DictType.JMdict:
                    Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, JmdictDBManager.GetRecordsFromDB, parameter, verbParameter, jmdictTextWithoutLongVowelMarkParameters);
                    if (jmdictResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.JMnedict:
                    Dictionary<string, IntermediaryResult>? jmnedictResults = GetNameResults(textList, textInHiraganaList, dict, useDB, JmnedictDBManager.GetRecordsFromDB, parameter);
                    if (jmnedictResults is not null)
                    {
                        lookupResults.AddRange(BuildJmnedictResult(jmnedictResults, pitchAccentDict));
                    }

                    break;

                case DictType.Kanjidic:
                    IntermediaryResult? kanjidicResult = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, KanjidicDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (kanjidicResult is not null)
                    {
                        lookupResults.Add(BuildKanjidicResult(kanji, kanjidicResult, kanjiFrequencyResults, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                    // Template-wise, it is a word dictionary that's why its results are put into Yomichan Word Results
                    // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                    IntermediaryResult? epwingYomichanKanjiWithWordSchemaResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingYomichanKanjiWithWordSchemaResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResultForKanjiWithWordSchema(epwingYomichanKanjiWithWordSchemaResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, false, null, parameter, verbParameter, null);
                    if (customWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    Dictionary<string, IntermediaryResult>? customNameResults = GetNameResults(textList, textInHiraganaList, dict, false, null, parameter);
                    if (customNameResults is not null)
                    {
                        lookupResults.AddRange(BuildCustomNameResult(customNameResults, pitchAccentDict));
                    }
                    break;

                case DictType.NonspecificKanjiYomichan:
                    IntermediaryResult? epwingYomichanKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, YomichanKanjiDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingYomichanKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildYomichanKanjiResult(kanji, epwingYomichanKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery);
                    if (epwingYomichanNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery, yomichanVerbQuery, yomichanTextWithoutLongVowelMarkQueries);
                    if (epwingYomichanWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, wordFreqs, frequencyDicts, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificKanjiNazeka:
                    IntermediaryResult? epwingNazekaKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingNazekaKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResultForKanji(epwingNazekaKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery);
                    if (epwingNazekaNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQueries);
                    if (epwingNazekaWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaWordResults, wordFreqs, frequencyDicts, pitchAccentDict));
                    }
                    break;

                case DictType.PitchAccentYomichan:
                    break;

                default:
                    Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(LookupText), dict.Type);
                    Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                    break;
            }
        });

        return lookupResults.IsEmpty
            ? null
            : SortLookupResults(lookupResults);
    }

    private static LookupResult[] SortLookupResults(IEnumerable<LookupResult> lookupResults)
    {
        return lookupResults
            .OrderByDescending(static lookupResult => lookupResult.MatchedText.Length)
            .ThenByDescending(static lookupResult => lookupResult.PrimarySpelling == lookupResult.MatchedText)
            .ThenByDescending(static lookupResult => lookupResult.Readings?.Contains(lookupResult.MatchedText) ?? false)
            .ThenByDescending(static lookupResult => lookupResult.DeconjugationProcess is null ? int.MaxValue : lookupResult.PrimarySpelling.Length)
            .ThenBy(static lookupResult => lookupResult.Dict.Priority)
            .ThenBy(static lookupResult =>
            {
                if (lookupResult.PrimarySpellingOrthographyInfoList is not null && lookupResult.PrimarySpelling == lookupResult.MatchedText)
                {
                    for (int i = 0; i < lookupResult.PrimarySpellingOrthographyInfoList.Length; i++)
                    {
                        if (lookupResult.PrimarySpellingOrthographyInfoList[i] is "oK" or "iK" or "rK")
                        {
                            return 1;
                        }
                    }
                }

                return 0;
            })
            .ThenBy(static lookupResult =>
            {
                int index = lookupResult.Readings is not null
                    ? Array.IndexOf(lookupResult.Readings, lookupResult.MatchedText)
                    : -1;

                if (index < 0)
                {
                    return 2;
                }

                if (lookupResult.MiscList is not null)
                {
                    for (int i = 0; i < lookupResult.MiscList.Length; i++)
                    {
                        if (lookupResult.MiscList[i]?.Contains("uk") ?? false)
                        {
                            return 0;
                        }
                    }
                }

                string[]? readingsOrthographyInfo = lookupResult.ReadingsOrthographyInfoList?[index];
                if (readingsOrthographyInfo is not null)
                {
                    for (int i = 0; i < readingsOrthographyInfo.Length; i++)
                    {
                        if (readingsOrthographyInfo[i] is "ok" or "ik" or "rk")
                        {
                            return 3;
                        }
                    }
                }

                return 1;
            })
            .ThenBy(static lookupResult =>
            {
                if (lookupResult.Frequencies?.Count > 0)
                {
                    LookupFrequencyResult freqResult = lookupResult.Frequencies[0];
                    return !freqResult.HigherValueMeansHigherFrequency
                        ? freqResult.Freq
                        : freqResult.Freq is int.MaxValue
                            ? int.MaxValue
                            : int.MaxValue - freqResult.Freq;
                }

                return int.MaxValue;
            })
            .ThenBy(static lookupResult =>
            {
                int index = lookupResult.Readings is not null
                    ? Array.IndexOf(lookupResult.Readings, lookupResult.MatchedText)
                    : -1;

                return index >= 0
                    ? index
                    : int.MaxValue;
            })
            // .ThenBy(static lookupResult => lookupResult.EntryId)
            .ToArray();
    }

    private static void GetWordResultsHelper(Dict dict,
        Dictionary<string, IntermediaryResult> results,
        List<Form>? deconjugationResults,
        string matchedText,
        string textInHiragana,
        IDictionary<string, IList<IDictRecord>>? dbWordDict,
        IDictionary<string, IList<IDictRecord>>? dbVerbDict)
    {
        IDictionary<string, IList<IDictRecord>> wordDict = dbWordDict ?? dict.Contents;
        IDictionary<string, IList<IDictRecord>> verbDict = dbVerbDict ?? dict.Contents;

        if (wordDict.TryGetValue(textInHiragana, out IList<IDictRecord>? tempResult))
        {
            _ = results.TryAdd(textInHiragana,
                new IntermediaryResult(matchedText, dict, [tempResult]));
        }

        if (deconjugationResults is not null)
        {
            foreach (Form deconjugationResult in deconjugationResults)
            {
                if (verbDict.TryGetValue(deconjugationResult.Text, out IList<IDictRecord>? dictResults))
                {
                    List<IDictRecord> resultsList = GetValidDeconjugatedResults(dict, deconjugationResult, dictResults);

                    if (resultsList.Count > 0)
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? r))
                        {
                            if (r.MatchedText == deconjugationResult.OriginalText)
                            {
                                int index = r.Results.FindIndex(rs => rs.SequenceEqual(resultsList));
                                if (index >= 0)
                                {
                                    r.Processes![index].Add(deconjugationResult.Process);
                                }
                                else
                                {
                                    r.Results.Add(resultsList);
                                    r.Processes!.Add([deconjugationResult.Process]);
                                }
                            }
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult(matchedText,
                                    dict,
                                    [resultsList],
                                    deconjugationResult.Text,
                                    [[deconjugationResult.Process]])
                            );
                        }
                    }
                }
            }
        }
    }

    private static Dictionary<string, IntermediaryResult> GetWordResults(List<string> textList, List<string> textInHiraganaList,
        List<List<Form>> deconjugationResultsList, List<string>? deconjugatedTexts, List<List<string>?>? textWithoutLongVowelMarkList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB,
        string? queryOrParameter, string? verbQueryOrParameter, List<string?>? longVowelQueryOrParameters)
    {
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;

        if (useDB)
        {
            Parallel.Invoke(
                () =>
                {
                    dbWordDict = getRecordsFromDB!(dict.Name, textInHiraganaList, queryOrParameter!);
                },
                () =>
                {
                    dbVerbDict = getRecordsFromDB!(dict.Name, deconjugatedTexts!, verbQueryOrParameter!);
                });
        }

        bool textWithoutLongVowelMarkListExist = textWithoutLongVowelMarkList is not null;
        Dictionary<string, IntermediaryResult> results = new(StringComparer.Ordinal);
        int textListCount = textList.Count;
        for (int i = 0; i < textListCount; i++)
        {
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], textList[i], textInHiraganaList[i], dbWordDict, dbVerbDict);

            List<string>? textWithoutLongVowelMark = textWithoutLongVowelMarkListExist ? textWithoutLongVowelMarkList![i] : null;
            if (textWithoutLongVowelMark is not null)
            {
                Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = useDB
                    ? getRecordsFromDB!(dict.Name, textWithoutLongVowelMark, longVowelQueryOrParameters![i]!)
                    : null;

                string text = textList[i];
                for (int j = 0; j < textWithoutLongVowelMark.Count; j++)
                {
                    GetWordResultsHelper(dict, results, null, text, textWithoutLongVowelMark[j], dbWordDictForLongVowelConversion, null);
                }
            }
        }

        return results;
    }

    private static List<IDictRecord> GetValidDeconjugatedResults(Dict dict, Form deconjugationResult, IList<IDictRecord> dictResults)
    {
        string lastTag = deconjugationResult.Tags[^1];

        int dictResultCount = dictResults.Count;
        List<IDictRecord> resultsList = new(dictResultCount);
        switch (dict.Type)
        {
            case DictType.JMdict:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    JmdictRecord dictResult = (JmdictRecord)dictResults[i];
                    if (dictResult.WordClasses.Any(wordClasses => wordClasses.Contains(lastTag)))
                    {
                        resultsList.Add(dictResult);
                    }
                }

                break;
            }

            case DictType.CustomWordDictionary:
            case DictType.ProfileCustomWordDictionary:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    CustomWordRecord dictResult = (CustomWordRecord)dictResults[i];
                    if (dictResult.WordClasses.Contains(lastTag))
                    {
                        resultsList.Add(dictResult);
                    }
                }

                break;
            }

            case DictType.NonspecificWordYomichan:
            case DictType.NonspecificYomichan:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    EpwingYomichanRecord dictResult = (EpwingYomichanRecord)dictResults[i];
                    if (dictResult.WordClasses is not null)
                    {
                        if (dictResult.WordClasses.Contains(lastTag))
                        {
                            resultsList.Add(dictResult);
                        }
                    }
                    else if (WordClassDictionaryContainsTag(dictResult, lastTag))
                    {
                        resultsList.Add(dictResult);
                    }
                }

                break;
            }

            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificNazeka:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    EpwingNazekaRecord dictResult = (EpwingNazekaRecord)dictResults[i];
                    if (WordClassDictionaryContainsTag(dictResult, lastTag))
                    {
                        resultsList.Add(dictResult);
                    }
                }

                break;
            }

            case DictType.PitchAccentYomichan:
            case DictType.JMnedict:
            case DictType.Kanjidic:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomNameDictionary:
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
                break;

            default:
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(GetValidDeconjugatedResults), dict.Type);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                break;
        }

        return resultsList;
    }

    private static Dictionary<string, IntermediaryResult>? GetNameResults(List<string> textList, List<string> textInHiraganaList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB, string? queryOrParameter)
    {
        IDictionary<string, IList<IDictRecord>>? nameDict = useDB
            ? getRecordsFromDB!(dict.Name, textInHiraganaList, queryOrParameter!)
            : dict.Contents;

        if (nameDict is null)
        {
            return null;
        }

        int textListCount = textList.Count;
        Dictionary<string, IntermediaryResult> nameResults = new(textListCount, StringComparer.Ordinal);
        for (int i = 0; i < textListCount; i++)
        {
            string textInHiragana = textInHiraganaList[i];
            if (nameDict.TryGetValue(textInHiragana, out IList<IDictRecord>? result))
            {
                nameResults.Add(textInHiragana,
                    new IntermediaryResult(textList[i], dict, [result]));
            }
        }

        return nameResults.Count > 0
            ? nameResults
            : null;
    }

    private static IntermediaryResult? GetKanjiResults(string kanji, Dict dict)
    {
        return dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result)
            ? new IntermediaryResult(kanji, dict, [result])
            : null;
    }

    private static IntermediaryResult? GetKanjiResultsFromDB(string kanji, Dict dict, GetKanjiRecordsFromDB getKanjiRecordsFromDB)
    {
        List<IDictRecord>? results = getKanjiRecordsFromDB(dict.Name, kanji);

        return results?.Count > 0
            ? new IntermediaryResult(kanji, dict, [results])
            : null;
    }

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(List<Freq> dbFreqs, HashSet<string> searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new(-1, dbFreqs.Count, StringComparer.Ordinal);
        _ = Parallel.For(0, dbFreqs.Count, i =>
        {
            Freq freq = dbFreqs[i];
            Dictionary<string, List<FrequencyRecord>>? freqRecords = FreqDBManager.GetRecordsFromDB(freq.Name, searchKeys);
            if (freqRecords is not null)
            {
                _ = frequencyDicts.TryAdd(freq.Name, freqRecords);
            }
        });

        return frequencyDicts;
    }

    private static List<LookupResult> BuildJmdictResult(
        Dictionary<string, IntermediaryResult> jmdictResults, List<Freq>? wordFreqs, List<Freq>? dbWordFreqs, bool dbIsUsedForPitchDict, Dict? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(jmdictResults)
            : null;

        bool pitchAccentDictExists = false;
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        Parallel.Invoke(
        () =>
        {
            if (dbWordFreqsExist)
            {
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs!, searchKeys!);
            }
        },
        () =>
        {
            pitchAccentDict = dbIsUsedForPitchDict
                ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys!)
                : pitchDict?.Contents;

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in jmdictResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes![i])
                    : null;

                IList<IDictRecord> dictRecords = wordResult.Results[i];
                for (int j = 0; j < dictRecords.Count; j++)
                {
                    JmdictRecord jmdictResult = (JmdictRecord)dictRecords[j];
                    LookupResult result = new
                    (
                        primarySpelling: jmdictResult.PrimarySpelling,
                        readings: jmdictResult.Readings,
                        matchedText: wordResult.MatchedText,
                        entryId: jmdictResult.Id,
                        alternativeSpellings: jmdictResult.AlternativeSpellings,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: deconjugationProcess,
                        frequencies: wordFreqsExist ? GetWordFrequencies(jmdictResult, wordFreqs!, frequencyDicts) : null,
                        primarySpellingOrthographyInfoList: jmdictResult.PrimarySpellingOrthographyInfo,
                        readingsOrthographyInfoList: jmdictResult.ReadingsOrthographyInfo,
                        alternativeSpellingsOrthographyInfoList: jmdictResult.AlternativeSpellingsOrthographyInfo,
                        miscList: jmdictResult.Misc,
                        dict: wordResult.Dict,
                        formattedDefinitions: jmdictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(jmdictResult.PrimarySpelling, jmdictResult.Readings, pitchAccentDict!) : null
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static HashSet<string> GetSearchKeysFromRecords(Dictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
                {
                    IDictRecordWithMultipleReadings record = (IDictRecordWithMultipleReadings)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling));
                    if (record.Readings is not null)
                    {
                        searchKeys.UnionWith(record.Readings.Select(JapaneseUtils.KatakanaToHiragana));
                    }
                }
            }
        }

        return searchKeys;
    }

    private static List<LookupResult> BuildJmnedictResult(
        Dictionary<string, IntermediaryResult> jmnedictResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        List<LookupResult> results = [];
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IntermediaryResult nameResult in jmnedictResults.Values)
        {
            int resultCount = nameResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = nameResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    JmnedictRecord jmnedictRecord = (JmnedictRecord)dictRecords[j];

                    LookupResult result = new
                    (
                        entryId: jmnedictRecord.Id,
                        primarySpelling: jmnedictRecord.PrimarySpelling,
                        alternativeSpellings: jmnedictRecord.AlternativeSpellings,
                        readings: jmnedictRecord.Readings,
                        matchedText: nameResult.MatchedText,
                        dict: nameResult.Dict,
                        formattedDefinitions: jmnedictRecord.BuildFormattedDefinition(nameResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(jmnedictRecord.PrimarySpelling, jmnedictRecord.Readings, pitchAccentDict!) : null
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static LookupResult BuildKanjidicResult(string kanji, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        KanjidicRecord kanjiRecord = (KanjidicRecord)intermediaryResult.Results[0][0];

        string[]? allReadings = Utils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        _ = DictUtils.KanjiCompositionDict.TryGetValue(kanji, out string? kanjiComposition);

        bool pitchAccentDictExists = pitchAccentDict is not null;
        LookupResult result = new
        (
            primarySpelling: kanji,
            readings: allReadings,
            onReadings: kanjiRecord.OnReadings,
            kunReadings: kanjiRecord.KunReadings,
            nanoriReadings: kanjiRecord.NanoriReadings,
            radicalNames: kanjiRecord.RadicalNames,
            strokeCount: kanjiRecord.StrokeCount,
            kanjiGrade: kanjiRecord.Grade,
            kanjiComposition: kanjiComposition,
            frequencies: GetKanjidicFrequencies(kanjiRecord.Frequency, kanjiFrequencyResults),
            matchedText: intermediaryResult.MatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null
        );

        return result;
    }

    private static List<LookupResult> BuildYomichanKanjiResult(
        string kanji, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        int resultCount = intermediaryResult.Results.Count;
        for (int i = 0; i < resultCount; i++)
        {
            IList<IDictRecord> dictRecords = intermediaryResult.Results[i];
            int dictRecordCount = dictRecords.Count;
            for (int j = 0; j < dictRecordCount; j++)
            {
                YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)dictRecords[j];

                string[]? allReadings = Utils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);

                _ = DictUtils.KanjiCompositionDict.TryGetValue(kanji, out string? kanjiComposition);

                LookupResult result = new
                (
                    primarySpelling: kanji,
                    readings: allReadings,
                    onReadings: yomichanKanjiDictResult.OnReadings,
                    kunReadings: yomichanKanjiDictResult.KunReadings,
                    kanjiComposition: kanjiComposition,
                    kanjiStats: yomichanKanjiDictResult.BuildFormattedStats(),
                    frequencies: kanjiFrequencyResults,
                    matchedText: intermediaryResult.MatchedText,
                    dict: intermediaryResult.Dict,
                    formattedDefinitions: yomichanKanjiDictResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null
                );
                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingYomichanResult(
        IDictionary<string, IntermediaryResult> epwingResults, List<Freq>? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool freqsExist = freqs is not null;
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in epwingResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes![i])
                    : null;

                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)dictRecords[j];

                    string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: deconjugationProcess,
                        frequencies: freqsExist ? GetWordFrequencies(epwingResult, freqs!, frequencyDicts) : null,
                        dict: wordResult.Dict,
                        readings: readings,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingYomichanResultForKanjiWithWordSchema(IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        int resultCount = intermediaryResult.Results.Count;
        for (int i = 0; i < resultCount; i++)
        {
            IList<IDictRecord> dictRecords = intermediaryResult.Results[i];
            int dictRecordCount = dictRecords.Count;
            for (int j = 0; j < dictRecordCount; j++)
            {
                EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)dictRecords[j];
                string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: epwingResult.PrimarySpelling,
                    matchedText: intermediaryResult.MatchedText,
                    frequencies: kanjiFrequencyResults,
                    dict: intermediaryResult.Dict,
                    readings: readings,
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingNazekaResult(
        IDictionary<string, IntermediaryResult> epwingNazekaResults, List<Freq>? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        bool freqsExist = freqs is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in epwingNazekaResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes![i])
                    : null;

                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)dictRecords[j];

                    string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        alternativeSpellings: epwingResult.AlternativeSpellings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: deconjugationProcess,
                        frequencies: freqsExist ? GetWordFrequencies(epwingResult, freqs!, frequencyDicts) : null,
                        dict: wordResult.Dict,
                        readings: readings,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }


    private static bool WordClassDictionaryContainsTag<T>(T record, string tag) where T : IDictRecordWithSingleReading
    {
        if (DictUtils.WordClassDictionary.TryGetValue(record.PrimarySpelling, out IList<JmdictWordClass>? jmdictWcResults))
        {
            for (int i = 0; i < jmdictWcResults.Count; i++)
            {
                JmdictWordClass result = jmdictWcResults[i];
                if (record.PrimarySpelling == result.Spelling
                    && ((record.Reading is not null && result.Readings is not null && result.Readings.Contains(record.Reading))
                        || (record.Reading is null && result.Readings is null))
                    && result.WordClasses.Contains(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<LookupResult> BuildEpwingNazekaResultForKanji(IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        int resultCount = intermediaryResult.Results.Count;
        for (int i = 0; i < resultCount; i++)
        {
            IList<IDictRecord> dictRecords = intermediaryResult.Results[i];
            int dictRecordCount = dictRecords.Count;
            for (int j = 0; j < dictRecordCount; j++)
            {
                EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)dictRecords[j];
                string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: epwingResult.PrimarySpelling,
                    alternativeSpellings: epwingResult.AlternativeSpellings,
                    matchedText: intermediaryResult.MatchedText,
                    frequencies: kanjiFrequencyResults,
                    dict: intermediaryResult.Dict,
                    readings: readings,
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults, List<Freq>? wordFreqs, List<Freq>? dbWordFreqs, bool dbIsUsedForPitchDict, Dict? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(customWordResults)
            : null;

        bool pitchAccentDictExists = false;
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        Parallel.Invoke(
        () =>
        {
            if (dbWordFreqsExist)
            {
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs!, searchKeys!);
            }
        },
        () =>
        {
            pitchAccentDict = dbIsUsedForPitchDict
                ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys!)
                : pitchDict?.Contents;

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in customWordResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes![i])
                    : null;

                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    CustomWordRecord customWordDictResult = (CustomWordRecord)dictRecords[j];

                    LookupResult result = new
                    (
                        primarySpelling: customWordDictResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: customWordDictResult.HasUserDefinedWordClass ? deconjugationProcess : null,
                        dict: wordResult.Dict,
                        readings: customWordDictResult.Readings,
                        alternativeSpellings: customWordDictResult.AlternativeSpellings,
                        formattedDefinitions: customWordDictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(customWordDictResult.PrimarySpelling, customWordDictResult.Readings, pitchAccentDict!) : null,
                        frequencies: wordFreqsExist ? GetWordFrequencies(customWordDictResult, wordFreqs!, frequencyDicts) : null
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildCustomNameResult(
        Dictionary<string, IntermediaryResult> customNameResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult customNameResult in customNameResults.Values)
        {
            int freq = 0;
            int resultCount = customNameResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = customNameResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    CustomNameRecord customNameDictResult = (CustomNameRecord)dictRecords[j];
                    string[]? readings = customNameDictResult.Reading is not null ? [customNameDictResult.Reading] : null;
                    LookupResult result = new
                    (
                        primarySpelling: customNameDictResult.PrimarySpelling,
                        matchedText: customNameResult.MatchedText,
                        frequencies: [new LookupFrequencyResult(customNameResult.Dict.Name, -freq, false)],
                        dict: customNameResult.Dict,
                        readings: readings,
                        formattedDefinitions: customNameDictResult.BuildFormattedDefinition(),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(customNameDictResult.PrimarySpelling, readings, pitchAccentDict!) : null
                    );

                    ++freq;
                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupFrequencyResult>? GetWordFrequencies<T>(T record, List<Freq> wordFreqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB) where T : IGetFrequency
    {
        bool freqDictsFromDBExist = freqDictsFromDB is not null;
        int freqCount = wordFreqs.Count;
        List<LookupFrequencyResult> freqsList = new(freqCount);
        for (int i = 0; i < freqCount; i++)
        {
            Freq freq = wordFreqs[i];
            bool useDB = freq.Options.UseDB.Value && freq.Ready;

            if (useDB)
            {
                if (freqDictsFromDBExist && freqDictsFromDB!.TryGetValue(freq.Name, out Dictionary<string, List<FrequencyRecord>>? freqDict))
                {
                    freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freqDict), freq.Options.HigherValueMeansHigherFrequency.Value));
                }
            }

            else
            {
                freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freq.Contents), freq.Options.HigherValueMeansHigherFrequency.Value));
            }
        }

        return freqsList.Count > 0
            ? freqsList
            : null;
    }

    private static List<LookupFrequencyResult>? GetKanjiFrequencies(string kanji, List<Freq> kanjiFreqs)
    {
        int kanjiFreqCount = kanjiFreqs.Count;
        List<LookupFrequencyResult> freqsList = new(kanjiFreqCount);
        for (int i = 0; i < kanjiFreqCount; i++)
        {
            Freq kanjiFreq = kanjiFreqs[i];
            bool useDB = kanjiFreq.Options.UseDB.Value && kanjiFreq.Ready;
            IList<FrequencyRecord>? freqResultList;

            if (useDB)
            {
                freqResultList = FreqDBManager.GetRecordsFromDB(kanjiFreq.Name, kanji);
            }
            else
            {
                _ = kanjiFreq.Contents.TryGetValue(kanji, out freqResultList);
            }

            if (freqResultList is not null)
            {
                int frequency = freqResultList[0].Frequency;
                if (frequency is not 0)
                {
                    freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency, false));
                }
            }
        }

        return freqsList.Count > 0
            ? freqsList
            : null;
    }

    private static List<LookupFrequencyResult>? GetKanjidicFrequencies(int frequency, List<LookupFrequencyResult>? frequencyResults)
    {
        bool kanjidicFreqExists = frequency is not 0;
        bool freqsExist = frequencyResults is not null;

        return !kanjidicFreqExists && !freqsExist
            ? null
            : !kanjidicFreqExists && freqsExist
                ? frequencyResults
                : kanjidicFreqExists && !freqsExist
                    ? [new LookupFrequencyResult("KANJIDIC2", frequency, false)]
                    : [new LookupFrequencyResult("KANJIDIC2", frequency, false), .. frequencyResults!];
    }

    private static byte[]? GetPitchPosition(string primarySpelling, string[]? readings, IDictionary<string, IList<IDictRecord>> pitchDictionary)
    {
        if (readings is null)
        {
            if (pitchDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<IDictRecord>? records))
            {
                for (int i = 0; i < records.Count; i++)
                {
                    PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)records[i];
                    if (pitchAccentRecord.Reading is null && pitchAccentRecord.Spelling == primarySpelling)
                    {
                        return [pitchAccentRecord.Position];
                    }
                }
            }

            return null;
        }
        else
        {
            byte[]? positions = null;
            if (pitchDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<IDictRecord>? records))
            {
                for (int i = 0; i < readings.Length; i++)
                {
                    byte position = byte.MaxValue;
                    string reading = readings[i];
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading);
                    for (int j = 0; j < records.Count; j++)
                    {
                        PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)records[j];
                        if (pitchAccentRecord.Reading is not null && readingInHiragana == JapaneseUtils.KatakanaToHiragana(pitchAccentRecord.Reading))
                        {
                            if (positions is null)
                            {
                                positions = new byte[readings.Length];
                                for (int k = 0; j < k; k++)
                                {
                                    positions[k] = byte.MaxValue;
                                }
                            }

                            position = pitchAccentRecord.Position;
                            break;
                        }
                    }

                    if (positions is not null)
                    {
                        positions[i] = position;
                    }
                }
            }
            else
            {
                for (int i = 0; i < readings.Length; i++)
                {
                    string reading = readings[i];
                    if (pitchDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out records))
                    {
                        byte position = byte.MaxValue;
                        for (int j = 0; j < records.Count; j++)
                        {
                            PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)records[j];
                            if (pitchAccentRecord.Spelling == primarySpelling
                                || (pitchAccentRecord.Reading is null && pitchAccentRecord.Spelling == reading && JapaneseUtils.IsKatakana(reading[0])))
                            {
                                if (positions is null)
                                {
                                    positions = new byte[readings.Length];
                                    for (int k = 0; i < k; k++)
                                    {
                                        positions[k] = byte.MaxValue;
                                    }
                                }

                                position = pitchAccentRecord.Position;
                                break;
                            }
                        }

                        if (positions is not null)
                        {
                            positions[i] = position;
                        }
                    }
                }
            }

            return positions;
        }
    }
}
