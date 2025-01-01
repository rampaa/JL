using System.Collections.Concurrent;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
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
        bool useDBForPitchDict = false;
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict))
        {
            useDBForPitchDict = pitchDict is { Active: true, Options.UseDB.Value: true, Ready: true };
        }

        ConcurrentBag<LookupResult> lookupResults = [];

        List<Freq>? kanjiFreqs = null;
        List<Freq>? dbKanjiFreqs = null;
        string kanji = "";
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanjiFreqs = FreqUtils.FreqDicts.Values
                .Where(static f => f is { Type: FreqType.YomichanKanji, Active: true })
                .OrderBy(static f => f.Priority)
                .ToList();

            dbKanjiFreqs = kanjiFreqs
                .Where(static f => f is { Options.UseDB.Value: true, Ready: true })
                .ToList();

            kanji = text.EnumerateRunes().First().ToString();
        }

        List<Freq> wordFreqs = FreqUtils.FreqDicts.Values
            .Where(static f => f is { Active: true, Type: not FreqType.YomichanKanji })
            .OrderBy(static f => f.Priority)
            .ToList();

        List<Freq> dbWordFreqs = wordFreqs
            .Where(static f => f is { Options.UseDB.Value: true, Ready: true })
            .ToList();

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

        List<string>? deconjugatedTexts = null;
        string? parameter = null;
        string? verbParameter = null;
        string? yomichanWordQuery = null;
        string? yomichanVerbQuery = null;
        string? nazekaWordQuery = null;
        string? nazekaVerbQuery = null;
        List<string?>? nazekaTextWithoutLongVowelMarkQueries = null;
        List<string?>? yomichanTextWithoutLongVowelMarkQueries = null;
        List<string?>? jmdictTextWithoutLongVowelMarkParameters = null;

        Dict[] dicts = DictUtils.Dicts.Values.ToArray();

        if (DictUtils.DBIsUsedForAtLeastOneDict)
        {
            parameter = DBUtils.GetParameter(textInHiraganaList.Count);

            deconjugatedTexts = deconjugationResultsList
                .SelectMany(static lf => lf.Select(static f => f.Text))
                .Distinct().ToList();

            if (deconjugatedTexts.Count > 0)
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

        _ = Parallel.ForEach(dicts, dict =>
        {
            if (!dict.Active)
            {
                return;
            }

            bool useDB = dict.Options.UseDB.Value && dict.Ready;
            switch (dict.Type)
            {
                case DictType.JMdict:
                    Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, JmdictDBManager.GetRecordsFromDB, parameter, verbParameter, jmdictTextWithoutLongVowelMarkParameters);
                    if (jmdictResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, wordFreqs, dbWordFreqs, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.JMnedict:
                    Dictionary<string, IntermediaryResult>? jmnedictResults = GetNameResults(textList, textInHiraganaList, dict, useDB, JmnedictDBManager.GetRecordsFromDB, parameter);
                    if (jmnedictResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmnedictResult(jmnedictResults, useDBForPitchDict, pitchDict));
                    }

                    break;

                case DictType.Kanjidic:
                    Dictionary<string, IntermediaryResult>? kanjidicResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, KanjidicDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (kanjidicResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildKanjidicResult(kanjidicResults, useDBForPitchDict, pitchDict, kanjiFreqs!));
                    }

                    break;

                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                    // Template-wise, it is a word dictionary that's why its results are put into Yomichan Word Results
                    // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                    Dictionary<string, IntermediaryResult>? epwingYomichanKanjiWithWordSchemaResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingYomichanKanjiWithWordSchemaResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanKanjiWithWordSchemaResults, kanjiFreqs, dbKanjiFreqs, useDBForPitchDict, pitchDict, dict.Type));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, false, null, parameter, verbParameter, null);
                    if (customWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, wordFreqs, dbWordFreqs, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    Dictionary<string, IntermediaryResult>? customNameResults = GetNameResults(textList, textInHiraganaList, dict, false, null, parameter);
                    if (customNameResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomNameResult(customNameResults, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.NonspecificKanjiYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, YomichanKanjiDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingYomichanKanjiResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildYomichanKanjiResult(epwingYomichanKanjiResults, useDBForPitchDict, pitchDict, kanjiFreqs!));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery);

                    if (epwingYomichanNameResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, null, null, useDBForPitchDict, pitchDict, dict.Type));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery, yomichanVerbQuery, yomichanTextWithoutLongVowelMarkQueries);
                    if (epwingYomichanWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, wordFreqs, dbWordFreqs, useDBForPitchDict, pitchDict, dict.Type));
                    }

                    break;

                case DictType.NonspecificKanjiNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingNazekaKanjiResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaKanjiResults, kanjiFreqs, dbKanjiFreqs, useDBForPitchDict, pitchDict, dict.Type));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery);
                    if (epwingNazekaNameResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, null, null, useDBForPitchDict, pitchDict, dict.Type));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarkList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQueries);
                    if (epwingNazekaWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaWordResults, wordFreqs, dbWordFreqs, useDBForPitchDict, pitchDict, dict.Type));
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
                new IntermediaryResult([tempResult], null, matchedText, matchedText,
                    dict));
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
                                    //if (!r.Processes?[index].Any(p => p.SequenceEqual(deconjugationResult.Process)) ?? false)
                                    r.Processes?[index].Add(deconjugationResult.Process);
                                }
                                else
                                {
                                    r.Results.Add(resultsList);
                                    r.Processes?.Add([deconjugationResult.Process]);
                                }
                            }
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult([resultsList],
                                    [[deconjugationResult.Process]],
                                    matchedText,
                                    deconjugationResult.Text,
                                    dict)
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

        Dictionary<string, IntermediaryResult> results = new(StringComparer.Ordinal);
        int textListCount = textList.Count;
        for (int i = 0; i < textListCount; i++)
        {
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], textList[i], textInHiraganaList[i], dbWordDict, dbVerbDict);

            List<string>? textWithoutLongVowelMark = textWithoutLongVowelMarkList?[i];
            if (textWithoutLongVowelMark is not null)
            {
                Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = useDB
                    ? getRecordsFromDB!(dict.Name, textWithoutLongVowelMark, longVowelQueryOrParameters![i]!)
                    : null;

                for (int j = 0; j < textWithoutLongVowelMark.Count; j++)
                {
                    GetWordResultsHelper(dict, results, null, textList[i], textWithoutLongVowelMark[j], dbWordDictForLongVowelConversion, null);
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
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    EpwingYomichanRecord dictResult = (EpwingYomichanRecord)dictResults[i];
                    if (dictResult.WordClasses?.Contains(lastTag) ?? false)
                    {
                        resultsList.Add(dictResult);
                    }

                    else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                 out IList<JmdictWordClass>? jmdictWcResults))
                    {
                        int jmdictWcResultCount = jmdictWcResults.Count;
                        for (int j = 0; j < jmdictWcResultCount; j++)
                        {
                            JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                            if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                && ((dictResult.Reading is not null && jmdictWordClassResult.Readings is not null && jmdictWordClassResult.Readings.Contains(dictResult.Reading))
                                    || (dictResult.Reading is null && jmdictWordClassResult.Readings is null)))
                            {
                                if (jmdictWordClassResult.WordClasses.Contains(lastTag))
                                {
                                    resultsList.Add(dictResult);
                                    break;
                                }
                            }
                        }
                    }
                }

                break;
            }

            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
            case DictType.NonspecificNazeka:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    EpwingNazekaRecord dictResult = (EpwingNazekaRecord)dictResults[i];
                    if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text, out IList<JmdictWordClass>? jmdictWcResults))
                    {
                        int jmdictWCResultCount = jmdictWcResults.Count;
                        for (int j = 0; j < jmdictWCResultCount; j++)
                        {
                            JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                            if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                && ((dictResult.Reading is not null && jmdictWordClassResult.Readings is not null && jmdictWordClassResult.Readings.Contains(dictResult.Reading))
                                    || (dictResult.Reading is null && jmdictWordClassResult.Readings is null)))
                            {
                                if (jmdictWordClassResult.WordClasses.Contains(lastTag))
                                {
                                    resultsList.Add(dictResult);
                                    break;
                                }
                            }
                        }
                    }
                }

                break;
            }

            case DictType.PitchAccentYomichan:
            case DictType.JMnedict:
            case DictType.Kanjidic:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomNameDictionary:
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
            if (nameDict.TryGetValue(textInHiraganaList[i], out IList<IDictRecord>? result))
            {
                nameResults.Add(textInHiraganaList[i],
                    new IntermediaryResult([result], null, textList[i], textList[i], dict));
            }
        }

        return nameResults;
    }

    private static Dictionary<string, IntermediaryResult>? GetKanjiResults(string kanji, Dict dict)
    {
        return dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result)
            ? new Dictionary<string, IntermediaryResult>(1, StringComparer.Ordinal)
            {
                {
                    kanji,
                    new IntermediaryResult([result], null, kanji, kanji, dict)
                }
            }
            : null;
    }

    private static Dictionary<string, IntermediaryResult>? GetKanjiResultsFromDB(string kanji, Dict dict, GetKanjiRecordsFromDB getKanjiRecordsFromDB)
    {
        List<IDictRecord>? results = getKanjiRecordsFromDB(dict.Name, kanji);

        return results?.Count > 0
            ? new Dictionary<string, IntermediaryResult>(1, StringComparer.Ordinal)
            {
                {
                    kanji, new IntermediaryResult([results], null, kanji, kanji, dict)
                }
            }
            : null;
    }

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(List<Freq> dbFreqs, string[] searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new(-1, dbFreqs.Count, StringComparer.Ordinal);
        _ = Parallel.For(0, dbFreqs.Count, i =>
        {
            Freq freq = dbFreqs[i];
            Dictionary<string, List<FrequencyRecord>>? freqRecords = FreqDBManager.GetRecordsFromDB(freq.Name, searchKeys);
            if (freqRecords?.Count > 0)
            {
                _ = frequencyDicts.TryAdd(freq.Name, freqRecords);
            }
        });

        return frequencyDicts;
    }

    private static string[] GetSearchKeysFromJmdictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach ((string key, IntermediaryResult intermediaryResult) in dictResults)
        {
            _ = searchKeys.Add(key);

            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    JmdictRecord jmdictRecord = (JmdictRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(jmdictRecord.PrimarySpelling));
                    if (jmdictRecord.Readings is not null)
                    {
                        foreach (string reading in jmdictRecord.Readings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(reading);
                        }
                    }
                    if (includeAlternativeSpellings && jmdictRecord.AlternativeSpellings is not null)
                    {
                        foreach (string alternativeSpelling in jmdictRecord.AlternativeSpellings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(alternativeSpelling);
                        }
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static string[] GetSearchKeysFromCustomWordRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach ((string key, IntermediaryResult intermediaryResult) in dictResults)
        {
            _ = searchKeys.Add(key);

            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    CustomWordRecord customWordRecord = (CustomWordRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(customWordRecord.PrimarySpelling));
                    if (customWordRecord.Readings is not null)
                    {
                        foreach (string reading in customWordRecord.Readings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(reading);
                        }
                    }
                    if (includeAlternativeSpellings && customWordRecord.AlternativeSpellings is not null)
                    {
                        foreach (string alternativeSpelling in customWordRecord.AlternativeSpellings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(alternativeSpelling);
                        }
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static string[] GetSearchKeysFromJmnedictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    JmnedictRecord jmnedictRecord = (JmnedictRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(jmnedictRecord.PrimarySpelling));
                    if (jmnedictRecord.Readings is not null)
                    {
                        foreach (string reading in jmnedictRecord.Readings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(reading);
                        }
                    }
                    if (includeAlternativeSpellings && jmnedictRecord.AlternativeSpellings is not null)
                    {
                        foreach (string alternativeSpelling in jmnedictRecord.AlternativeSpellings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(alternativeSpelling);
                        }
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static string[] GetSearchKeysFromCustomNameRecord(Dictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    CustomNameRecord customNameRecord = (CustomNameRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(customNameRecord.PrimarySpelling));
                    if (customNameRecord.Reading is not null)
                    {
                        _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(customNameRecord.Reading));
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static string[] GetSearchKeyForEpwingYomichanRecord(IDictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingYomichanRecord epwingYomichanRecord = (EpwingYomichanRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(epwingYomichanRecord.PrimarySpelling));
                    if (epwingYomichanRecord.Reading is not null)
                    {
                        _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(epwingYomichanRecord.Reading));
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static string[] GetSearchKeysFromEpwingNazekaRecord(IDictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            int dictRecordsListCount = dictRecordsList.Count;
            for (int i = 0; i < dictRecordsListCount; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingNazekaRecord epwingNazekaRecord = (EpwingNazekaRecord)dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(epwingNazekaRecord.PrimarySpelling));
                    if (epwingNazekaRecord.Reading is not null)
                    {
                        _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(epwingNazekaRecord.Reading));
                    }
                    if (includeAlternativeSpellings && epwingNazekaRecord.AlternativeSpellings is not null)
                    {
                        foreach (string alternativeSpelling in epwingNazekaRecord.AlternativeSpellings.Select(JapaneseUtils.KatakanaToHiragana))
                        {
                            _ = searchKeys.Add(alternativeSpelling);
                        }
                    }
                }
            }
        }

        return searchKeys.ToArray();
    }

    private static List<LookupResult> BuildJmdictResult(
        Dictionary<string, IntermediaryResult> jmdictResults, List<Freq> wordFreqs, List<Freq> dbWordFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbWordFreqs.Count > 0)
                {
                    string[] searchKeys = GetSearchKeysFromJmdictRecord(jmdictResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    string[] searchKeys = GetSearchKeysFromJmdictRecord(jmdictResults, false);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
                }
            });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in jmdictResults.Values)
        {
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    JmdictRecord jmdictResult = (JmdictRecord)dictRecords[j];
                    LookupResult result = new
                    (
                        primarySpelling: jmdictResult.PrimarySpelling,
                        readings: jmdictResult.Readings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        entryId: jmdictResult.Id,
                        alternativeSpellings: jmdictResult.AlternativeSpellings,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(jmdictResult, wordFreqs, frequencyDicts),
                        primarySpellingOrthographyInfoList: jmdictResult.PrimarySpellingOrthographyInfo,
                        readingsOrthographyInfoList: jmdictResult.ReadingsOrthographyInfo,
                        alternativeSpellingsOrthographyInfoList: jmdictResult.AlternativeSpellingsOrthographyInfo,
                        miscList: jmdictResult.Misc,
                        dict: wordResult.Dict,
                        formattedDefinitions: jmdictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchAccentDict: pitchAccentDict
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildJmnedictResult(
        Dictionary<string, IntermediaryResult> jmnedictResults, bool useDBForPitchDict, Dict? pitchDict)
    {
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (useDBForPitchDict)
        {
            string[] searchKeys = GetSearchKeysFromJmnedictRecord(jmnedictResults, false);
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
        }

        List<LookupResult> results = [];
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
                        deconjugatedMatchedText: nameResult.DeconjugatedMatchedText,
                        dict: nameResult.Dict,
                        formattedDefinitions: jmnedictRecord.BuildFormattedDefinition(nameResult.Dict.Options),
                        pitchAccentDict: pitchAccentDict
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildKanjidicResult(IDictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict, List<Freq> kanjiFreqs)
    {
        (string kanji, IntermediaryResult intermediaryResult) = kanjiResults.First();

        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = useDBForPitchDict
            ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, kanji)
            : null;

        KanjidicRecord kanjiRecord = (KanjidicRecord)intermediaryResult.Results[0][0];

        string[]? allReadings = Utils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        _ = DictUtils.KanjiCompositionDict.TryGetValue(kanji, out string? kanjiComposition);

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
            frequencies: GetKanjidicFrequencies(kanji, kanjiRecord.Frequency, kanjiFreqs),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchAccentDict: pitchAccentDict
        );

        return [result];
    }

    private static ConcurrentBag<LookupResult> BuildYomichanKanjiResult(
        IDictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict, List<Freq> kanjiFreqs)
    {
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = useDBForPitchDict
            ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, kanjiResults.First().Key)
            : null;

        ConcurrentBag<LookupResult> results = [];
        _ = Parallel.ForEach(kanjiResults, kanjiResult =>
        {
            int resultCount = kanjiResult.Value.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = kanjiResult.Value.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)dictRecords[j];

                    string[]? allReadings = Utils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);

                    _ = DictUtils.KanjiCompositionDict.TryGetValue(kanjiResult.Key, out string? kanjiComposition);

                    LookupResult result = new
                    (
                        primarySpelling: kanjiResult.Key,
                        readings: allReadings,
                        onReadings: yomichanKanjiDictResult.OnReadings,
                        kunReadings: yomichanKanjiDictResult.KunReadings,
                        kanjiComposition: kanjiComposition,
                        kanjiStats: yomichanKanjiDictResult.BuildFormattedStats(),
                        frequencies: GetKanjiFrequencies(kanjiResult.Key, kanjiFreqs),
                        matchedText: kanjiResult.Value.MatchedText,
                        deconjugatedMatchedText: kanjiResult.Value.DeconjugatedMatchedText,
                        dict: kanjiResult.Value.Dict,
                        formattedDefinitions: yomichanKanjiDictResult.BuildFormattedDefinition(kanjiResult.Value.Dict.Options),
                        pitchAccentDict: pitchAccentDict
                    );
                    results.Add(result);
                }
            }
        });

        return results;
    }

    private static List<LookupResult> BuildEpwingYomichanResult(
        IDictionary<string, IntermediaryResult> epwingResults, List<Freq>? freqs, List<Freq>? dbFreqs, bool useDBForPitchDict, Dict? pitchDict, DictType dictType)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs?.Count > 0)
                {
                    string[] searchKeys = GetSearchKeyForEpwingYomichanRecord(epwingResults);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    string[] searchKeys = GetSearchKeyForEpwingYomichanRecord(epwingResults);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
                }
            });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in epwingResults.Values)
        {
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)dictRecords[j];

                    List<LookupFrequencyResult>? frequencies = null;
                    if (freqs is not null)
                    {
                        if (dictType is DictType.NonspecificWordYomichan or DictType.NonspecificYomichan)
                        {
                            frequencies = GetWordFrequencies(epwingResult, freqs, frequencyDicts);
                        }
                        else if (dictType is DictType.NonspecificKanjiWithWordSchemaYomichan)
                        {
                            frequencies = GetKanjiFrequencies(epwingResult.PrimarySpelling, freqs);
                        }
                    }

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: frequencies,
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading is not null
                            ? [epwingResult.Reading]
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchAccentDict: pitchAccentDict
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static ConcurrentBag<LookupResult> BuildEpwingNazekaResult(
        IDictionary<string, IntermediaryResult> epwingNazekaResults, List<Freq>? freqs, List<Freq>? dbFreqs, bool useDBForPitchDict, Dict? pitchDict, DictType dictType)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs?.Count > 0)
                {
                    string[] searchKeys = GetSearchKeysFromEpwingNazekaRecord(epwingNazekaResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    string[] searchKeys = GetSearchKeysFromEpwingNazekaRecord(epwingNazekaResults, false);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
                }
            });

        ConcurrentBag<LookupResult> results = [];
        _ = Parallel.ForEach(epwingNazekaResults.Values, wordResult =>
        {
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
                IList<IDictRecord> dictRecords = wordResult.Results[i];
                int dictRecordCount = dictRecords.Count;
                for (int j = 0; j < dictRecordCount; j++)
                {
                    EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)dictRecords[j];

                    List<LookupFrequencyResult>? frequencies = null;
                    if (freqs is not null)
                    {
                        if (dictType is DictType.NonspecificWordNazeka or DictType.NonspecificNazeka)
                        {
                            frequencies = GetWordFrequencies(epwingResult, freqs, frequencyDicts);
                        }
                        else if (dictType is DictType.NonspecificKanjiNazeka)
                        {
                            frequencies = GetKanjiFrequencies(epwingResult.PrimarySpelling, freqs);
                        }
                    }

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        alternativeSpellings: epwingResult.AlternativeSpellings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: frequencies,
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading is not null
                            ? [epwingResult.Reading]
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchAccentDict: pitchAccentDict
                    );

                    results.Add(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentBag<LookupResult> BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults, List<Freq> wordFreqs, List<Freq> dbWordFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbWordFreqs.Count > 0)
                {
                    string[] searchKeys = GetSearchKeysFromCustomWordRecord(customWordResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    string[] searchKeys = GetSearchKeysFromCustomWordRecord(customWordResults, false);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
                }
            });

        ConcurrentBag<LookupResult> results = [];
        _ = Parallel.ForEach(customWordResults.Values, wordResult =>
        {
            int resultCount = wordResult.Results.Count;
            for (int i = 0; i < resultCount; i++)
            {
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
                        deconjugationProcess: customWordDictResult.HasUserDefinedWordClass
                            ? LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i])
                            : null,
                        dict: wordResult.Dict,
                        readings: customWordDictResult.Readings,
                        alternativeSpellings: customWordDictResult.AlternativeSpellings,
                        formattedDefinitions: customWordDictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchAccentDict: pitchAccentDict,
                        frequencies: GetWordFrequencies(customWordDictResult, wordFreqs, frequencyDicts)
                    );

                    results.Add(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentBag<LookupResult> BuildCustomNameResult(
        Dictionary<string, IntermediaryResult> customNameResults, bool useDBForPitchDict, Dict? pitchDict)
    {
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (useDBForPitchDict)
        {
            string[] searchKeys = GetSearchKeysFromCustomNameRecord(customNameResults);
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
        }

        ConcurrentBag<LookupResult> results = [];
        _ = Parallel.ForEach(customNameResults.Values, customNameResult =>
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
                    LookupResult result = new
                    (
                        primarySpelling: customNameDictResult.PrimarySpelling,
                        matchedText: customNameResult.MatchedText,
                        deconjugatedMatchedText: customNameResult.DeconjugatedMatchedText,
                        frequencies: [new LookupFrequencyResult(customNameResult.Dict.Name, -freq, false)],
                        dict: customNameResult.Dict,
                        readings: customNameDictResult.Reading is not null
                            ? [customNameDictResult.Reading]
                            : null,
                        formattedDefinitions: customNameDictResult.BuildFormattedDefinition(),
                        pitchAccentDict: pitchAccentDict
                    );

                    ++freq;
                    results.Add(result);
                }
            }
        });

        return results;
    }

    private static List<LookupFrequencyResult>? GetWordFrequencies<T>(T record, List<Freq> wordFreqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB) where T : IGetFrequency
    {
        int freqCount = wordFreqs.Count;
        if (wordFreqs.Count is 0)
        {
            return null;
        }

        List<LookupFrequencyResult> freqsList = new(freqCount);
        for (int i = 0; i < freqCount; i++)
        {
            Freq freq = wordFreqs[i];
            bool useDB = freq.Options.UseDB.Value && freq.Ready;

            if (useDB)
            {
                if (freqDictsFromDB?.TryGetValue(freq.Name, out Dictionary<string, List<FrequencyRecord>>? freqDict) ?? false)
                {
                    freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequencyFromDB(freqDict), freq.Options.HigherValueMeansHigherFrequency.Value));
                }
            }

            else
            {
                freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freq), freq.Options.HigherValueMeansHigherFrequency.Value));
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult>? GetKanjiFrequencies(string kanji, List<Freq> kanjiFreqs)
    {
        int kanjiFreqCount = kanjiFreqs.Count;
        if (kanjiFreqCount is 0)
        {
            return null;
        }

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
                int frequency = freqResultList.FirstOrDefault().Frequency;
                if (frequency is not 0)
                {
                    freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency, false));
                }
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult>? GetKanjidicFrequencies(string kanji, int frequency, List<Freq> kanjiFreqs)
    {
        bool kanjidicFreqExists = frequency is not 0;

        List<LookupFrequencyResult>? frequencies = GetKanjiFrequencies(kanji, kanjiFreqs);
        bool freqsExist = frequencies?.Count > 0;

        return !kanjidicFreqExists && !freqsExist
            ? null
            : !kanjidicFreqExists && freqsExist
                ? frequencies
                : kanjidicFreqExists && !freqsExist
                    ? ([new LookupFrequencyResult("KANJIDIC2", frequency, false)])
                    : ([new LookupFrequencyResult("KANJIDIC2", frequency, false), .. frequencies]);
    }
}
