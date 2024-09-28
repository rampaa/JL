using System.Collections.Concurrent;
using System.Diagnostics;
using JL.Core.Config;
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
    private static DateTime s_lastLookupTime;

    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms, string parameterOrQuery);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string dbName, string term);

    public static List<LookupResult>? LookupText(string text)
    {
        DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
        if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < CoreConfigManager.LookupRate)
        {
            return null;
        }

        s_lastLookupTime = preciseTimeNow;

        List<Freq> dbFreqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Active: true, Type: not FreqType.YomichanKanji, Options.UseDB.Value: true, Ready: true }).ToList();

        _ = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict);
        bool pitchDictIsActive = pitchDict?.Active ?? false;
        bool useDBForPitchDict = pitchDictIsActive && pitchDict!.Options.UseDB.Value && pitchDict.Ready;

        ConcurrentBag<LookupResult> lookupResults = [];
        string kanji = "";
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = text.EnumerateRunes().First().ToString();
            if (CoreConfigManager.KanjiMode)
            {
                _ = Parallel.ForEach(DictUtils.Dicts.Values.ToList(), dict =>
                {
                    bool useDB = dict.Options.UseDB.Value && dict.Ready;
                    if (!dict.Active)
                    {
                        return;
                    }

                    if (dict.Type is DictType.Kanjidic)
                    {
                        Dictionary<string, IntermediaryResult>? results = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, KanjidicDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (results?.Count > 0)
                        {
                            lookupResults.AddRange(BuildKanjidicResult(results, useDBForPitchDict, pitchDict));
                        }
                    }

                    else if (dict.Type is DictType.NonspecificKanjiWithWordSchemaYomichan)
                    {
                        Dictionary<string, IntermediaryResult>? results = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (results?.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingYomichanResult(results, dbFreqs, useDBForPitchDict, pitchDict));
                        }
                    }

                    else if (dict.Type is DictType.NonspecificKanjiYomichan)
                    {
                        Dictionary<string, IntermediaryResult>? results = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (results?.Count > 0)
                        {
                            lookupResults.AddRange(BuildYomichanKanjiResult(results, useDBForPitchDict, pitchDict));
                        }
                    }

                    else if (dict.Type is DictType.NonspecificKanjiNazeka)
                    {
                        Dictionary<string, IntermediaryResult>? results = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (results?.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingNazekaResult(results, dbFreqs, useDBForPitchDict, pitchDict));
                        }
                    }
                });

                return lookupResults.IsEmpty
                    ? null
                    : SortLookupResults(lookupResults);
            }
        }

        else if (CoreConfigManager.KanjiMode)
        {
            return null;
        }

        List<string> textList = [];
        List<string> textInHiraganaList = [];
        List<HashSet<Form>> deconjugationResultsList = [];

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
        }

        List<string>? deconjugatedTexts = null;
        string? parameter = null;
        string? verbParameter = null;
        string? yomichanWordQuery = null;
        string? yomichanVerbQuery = null;
        string? nazekaWordQuery = null;
        string? nazekaVerbQuery = null;

        List<Dict> dicts = DictUtils.Dicts.Values.ToList();

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
                    Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, dict, useDB, JmdictDBManager.GetRecordsFromDB, parameter, verbParameter, false, false);
                    if (jmdictResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, dbFreqs, useDBForPitchDict, pitchDict));
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
                        lookupResults.AddRange(BuildKanjidicResult(kanjidicResults, useDBForPitchDict, pitchDict));
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
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanKanjiWithWordSchemaResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, dict, false, null, parameter, verbParameter, false, false);
                    if (customWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, dbFreqs, useDBForPitchDict, pitchDict));
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
                        lookupResults.AddRange(BuildYomichanKanjiResult(epwingYomichanKanjiResults, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery);

                    if (epwingYomichanNameResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery, yomichanVerbQuery, true, false);
                    if (epwingYomichanWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }

                    break;

                case DictType.NonspecificKanjiNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingNazekaKanjiResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaKanjiResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery);
                    if (epwingNazekaNameResults?.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTexts, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery, nazekaVerbQuery, false, true);
                    if (epwingNazekaWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaWordResults, dbFreqs, useDBForPitchDict, pitchDict));
                    }
                    break;

                case DictType.PitchAccentYomichan:
                    break;

#pragma warning disable CS0618 // Type or member is obsolete
                case DictType.Kenkyuusha:
                case DictType.Daijisen:
                case DictType.Daijirin:
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
                case DictType.KanjigenYomichan:
                case DictType.DaijirinNazeka:
                case DictType.ShinmeikaiNazeka:
                case DictType.KenkyuushaNazeka:
#pragma warning restore CS0618 // Type or member is obsolete
                    throw new ArgumentOutOfRangeException(null, dict.Type, "Obsolete DictType");

                default:
                    throw new ArgumentOutOfRangeException(null, dict.Type, "Invalid DictType");
            }
        });

        return lookupResults.IsEmpty
            ? null
            : SortLookupResults(lookupResults);
    }

    private static List<LookupResult> SortLookupResults(IEnumerable<LookupResult> lookupResults)
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
                    return 3;
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
                            return 2;
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
            .ToList();
    }

    private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(Dict dict,
        Dictionary<string, IntermediaryResult> results,
        HashSet<Form>? deconjugationResults,
        string matchedText,
        string textInHiragana,
        int succAttempt,
        IDictionary<string, IList<IDictRecord>>? dbWordDict,
        IDictionary<string, IList<IDictRecord>>? dbVerbDict)
    {
        IDictionary<string, IList<IDictRecord>> wordDict = dbWordDict ?? dict.Contents;
        IDictionary<string, IList<IDictRecord>> verbDict = dbVerbDict ?? dict.Contents;

        bool tryLongVowelConversion = true;

        if (wordDict.TryGetValue(textInHiragana, out IList<IDictRecord>? tempResult))
        {
            _ = results.TryAdd(textInHiragana,
                new IntermediaryResult([tempResult], null, matchedText, matchedText,
                    dict));
            tryLongVowelConversion = false;
        }

        if (deconjugationResults is not null && succAttempt < 3)
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

                        ++succAttempt;
                        tryLongVowelConversion = false;
                    }
                }
            }
        }

        return (tryLongVowelConversion, succAttempt);
    }

    private static Dictionary<string, IntermediaryResult> GetWordResults(List<string> textList,
        List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, List<string>? deconjugatedTexts, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB, string? queryOrParameter, string? verbQueryOrParameter, bool isYomichan, bool isNazeka)
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
        int succAttempt = 0;
        int textListCount = textList.Count;
        for (int i = 0; i < textListCount; i++)
        {
            (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dict, results,
                deconjugationResultsList[i], textList[i], textInHiraganaList[i], succAttempt, dbWordDict, dbVerbDict);

            if (tryLongVowelConversion && textInHiraganaList[i][0] is not 'ー')
            {
                int count = 0;
                foreach (char c in textInHiraganaList[i])
                {
                    if (c is 'ー')
                    {
                        ++count;
                    }
                }

                if (count is > 0 and < 4)
                {
                    List<string> textWithoutLongVowelMarkList = JapaneseUtils.LongVowelMarkToKana(textInHiraganaList[i]);
                    Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = null;
                    if (useDB)
                    {
                        string longVowelQueryOrParameter;
                        if (isYomichan)
                        {
                            longVowelQueryOrParameter = EpwingYomichanDBManager.GetQuery(textWithoutLongVowelMarkList);
                        }
                        else if (isNazeka)
                        {
                            longVowelQueryOrParameter = EpwingNazekaDBManager.GetQuery(textWithoutLongVowelMarkList);
                        }
                        else
                        {
                            longVowelQueryOrParameter = DBUtils.GetParameter(textWithoutLongVowelMarkList.Count);
                        }

                        dbWordDictForLongVowelConversion = getRecordsFromDB!(dict.Name, textWithoutLongVowelMarkList, longVowelQueryOrParameter);
                    }

                    int textWithoutLongVowelMarkListCount = textWithoutLongVowelMarkList.Count;
                    for (int j = 0; j < textWithoutLongVowelMarkListCount; j++)
                    {
                        _ = GetWordResultsHelper(dict, results, null, textList[i], textWithoutLongVowelMarkList[j], succAttempt, useDB ? dbWordDictForLongVowelConversion : dbWordDict, null);
                    }
                }
            }
        }

        return results;
    }

    private static List<IDictRecord> GetValidDeconjugatedResults(Dict dict, Form deconjugationResult, IList<IDictRecord> dictResults)
    {
        string? lastTag = deconjugationResult.Tags.Count > 0
            ? deconjugationResult.Tags[^1]
            : null;

        List<IDictRecord> resultsList = [];
        int dictResultCount = dictResults.Count;
        switch (dict.Type)
        {
            case DictType.JMdict:
            {
                for (int i = 0; i < dictResultCount; i++)
                {
                    JmdictRecord dictResult = (JmdictRecord)dictResults[i];
                    if (lastTag is null || dictResult.WordClasses.Any(wordClasses => wordClasses.Contains(lastTag)))
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
                    if (lastTag is null || dictResult.WordClasses.Contains(lastTag))
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
                    if (lastTag is null || (dictResult.WordClasses?.Contains(lastTag) ?? false))
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
                    if (lastTag is null)
                    {
                        resultsList.Add(dictResult);
                    }

                    else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                 out IList<JmdictWordClass>? jmdictWcResults))
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

#pragma warning disable CS0618 // Type or member is obsolete
            case DictType.Daijisen:
            case DictType.Kenkyuusha:
            case DictType.Daijirin:
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
            case DictType.KanjigenYomichan:
            case DictType.DaijirinNazeka:
            case DictType.ShinmeikaiNazeka:
            case DictType.KenkyuushaNazeka:
#pragma warning restore CS0618 // Type or member is obsolete
                throw new ArgumentOutOfRangeException(null, dict.Type, "Obsolete DictType");

            default:
                throw new ArgumentOutOfRangeException(null, dict.Type, "Invalid DictType");
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

        Dictionary<string, IntermediaryResult> nameResults = new(StringComparer.Ordinal);
        int textListCount = textList.Count;
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

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(List<Freq> dbFreqs, List<string> searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new(StringComparer.Ordinal);
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

    private static List<string> GetSearchKeysFromJmdictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<string> GetSearchKeysFromCustomWordRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<string> GetSearchKeysFromJmnedictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<string> GetSearchKeysFromCustomNameRecord(Dictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<string> GetSearchKeyForEpwingYomichanRecord(IDictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<string> GetSearchKeysFromEpwingNazekaRecord(IDictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = [];
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

        return searchKeys.ToList();
    }

    private static List<LookupResult> BuildJmdictResult(
        Dictionary<string, IntermediaryResult> jmdictResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs.Count > 0)
                {
                    List<string> searchKeys = GetSearchKeysFromJmdictRecord(jmdictResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    List<string> searchKeys = GetSearchKeysFromJmdictRecord(jmdictResults, false);
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
                        edictId: jmdictResult.Id,
                        alternativeSpellings: jmdictResult.AlternativeSpellings,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(jmdictResult, frequencyDicts),
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
            List<string> searchKeys = GetSearchKeysFromJmnedictRecord(jmnedictResults, false);
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
                        edictId: jmnedictRecord.Id,
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

    private static List<LookupResult> BuildKanjidicResult(IDictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict)
    {
        KeyValuePair<string, IntermediaryResult> dictResult = kanjiResults.First();

        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = useDBForPitchDict
            ? YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, dictResult.Key)
            : null;

        List<IList<IDictRecord>> iResult = dictResult.Value.Results;
        KanjidicRecord kanjiRecord = (KanjidicRecord)iResult[0][0];

        string[]? allReadings = Utils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        IntermediaryResult intermediaryResult = kanjiResults.First().Value;

        _ = DictUtils.KanjiCompositionDict.TryGetValue(dictResult.Key, out string? kanjiComposition);

        LookupResult result = new
        (
            primarySpelling: dictResult.Key,
            readings: allReadings,
            onReadings: kanjiRecord.OnReadings,
            kunReadings: kanjiRecord.KunReadings,
            nanoriReadings: kanjiRecord.NanoriReadings,
            radicalNames: kanjiRecord.RadicalNames,
            strokeCount: kanjiRecord.StrokeCount,
            kanjiGrade: kanjiRecord.Grade,
            kanjiComposition: kanjiComposition,
            frequencies: GetKanjidicFrequencies(dictResult.Key, kanjiRecord.Frequency),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchAccentDict: pitchAccentDict
        );

        return [result];
    }

    private static ConcurrentBag<LookupResult> BuildYomichanKanjiResult(
        IDictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict)
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
                        frequencies: GetYomichanKanjiFrequencies(kanjiResult.Key),
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
        IDictionary<string, IntermediaryResult> epwingResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs.Count > 0)
                {
                    List<string> searchKeys = GetSearchKeyForEpwingYomichanRecord(epwingResults);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    List<string> searchKeys = GetSearchKeyForEpwingYomichanRecord(epwingResults);
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
                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult, frequencyDicts),
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
        IDictionary<string, IntermediaryResult> epwingNazekaResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs.Count > 0)
                {
                    List<string> searchKeys = GetSearchKeysFromEpwingNazekaRecord(epwingNazekaResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    List<string> searchKeys = GetSearchKeysFromEpwingNazekaRecord(epwingNazekaResults, false);
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
                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        alternativeSpellings: epwingResult.AlternativeSpellings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult, frequencyDicts),
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
        Dictionary<string, IntermediaryResult> customWordResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
    {
        IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        Parallel.Invoke(() =>
            {
                if (dbFreqs.Count > 0)
                {
                    List<string> searchKeys = GetSearchKeysFromCustomWordRecord(customWordResults, true);
                    frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
                }
            },
            () =>
            {
                if (useDBForPitchDict)
                {
                    List<string> searchKeys = GetSearchKeysFromCustomWordRecord(customWordResults, false);
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
                        frequencies: GetWordFrequencies(customWordDictResult, frequencyDicts)
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
            List<string> searchKeys = GetSearchKeysFromCustomNameRecord(customNameResults);
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

    private static List<LookupFrequencyResult>? GetWordFrequencies(IGetFrequency record, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB)
    {
        List<Freq> freqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Active: true, Type: not FreqType.YomichanKanji }).OrderBy(static f => f.Priority).ToList();
        if (freqs.Count is 0)
        {
            return null;
        }

        List<LookupFrequencyResult> freqsList = [];
        int freqCount = freqs.Count;
        for (int i = 0; i < freqCount; i++)
        {
            Freq freq = freqs[i];
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

    private static List<LookupFrequencyResult>? GetYomichanKanjiFrequencies(string kanji)
    {
        List<Freq> kanjiFreqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Type: FreqType.YomichanKanji, Active: true }).OrderBy(static f => f.Priority).ToList();
        int kanjiFreqCount = kanjiFreqs.Count;
        if (kanjiFreqCount is 0)
        {
            return null;
        }

        List<LookupFrequencyResult> freqsList = [];
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

    private static List<LookupFrequencyResult> GetKanjidicFrequencies(string kanji, int frequency)
    {
        List<LookupFrequencyResult> freqsList = [];

        if (frequency is not 0)
        {
            freqsList.Add(new LookupFrequencyResult("KANJIDIC2", frequency, false));
        }

        List<LookupFrequencyResult>? frequencies = GetYomichanKanjiFrequencies(kanji);
        if (frequencies?.Count > 0)
        {
            freqsList.AddRange(frequencies);
        }

        return freqsList;
    }
}
