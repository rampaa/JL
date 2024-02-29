using System.Collections.Concurrent;
using System.Diagnostics;
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

    private delegate Dictionary<string, IList<IDictRecord>> GetRecordsFromDB(string dbName, List<string> terms);
    private delegate List<IDictRecord> GetKanjiRecordsFromDB(string dbName, string term);

    public static List<LookupResult>? LookupText(string text)
    {
        DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
        if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < CoreConfig.LookupRate)
        {
            return null;
        }

        s_lastLookupTime = preciseTimeNow;

        List<Freq> dbFreqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Active: true, Type: not FreqType.YomichanKanji } && (f.Options?.UseDB?.Value ?? false) && f.Ready).ToList();

        _ = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict);
        bool pitchDictIsActive = pitchDict?.Active ?? false;
        bool useDBForPitchDict = pitchDictIsActive && (pitchDict!.Options?.UseDB?.Value ?? false) && pitchDict.Ready;

        ConcurrentBag<LookupResult> lookupResults = new();

        if (CoreConfig.KanjiMode)
        {
            _ = Parallel.ForEach(DictUtils.Dicts.Values.ToList(), dict =>
            {
                bool useDB = (dict.Options?.UseDB?.Value ?? false) && dict.Ready;
                if (dict.Active)
                {
                    if (dict.Type is DictType.Kanjidic)
                    {
                        Dictionary<string, IntermediaryResult> results = useDB
                            ? GetKanjiResultsFromDB(text, dict, KanjidicDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        if (results.Count > 0)
                        {
                            lookupResults.AddRange(BuildKanjidicResult(results, useDBForPitchDict, pitchDict));
                        }
                    }

                    else if (dict.Type is DictType.KanjigenYomichan or DictType.NonspecificKanjiWithWordSchemaYomichan)
                    {
                        Dictionary<string, IntermediaryResult> results = useDB
                            ? GetKanjiResultsFromDB(text, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        if (results.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingYomichanResult(results, dbFreqs, useDBForPitchDict, pitchDict));
                        }
                    }

                    else if (DictUtils.s_kanjiDictTypes.Contains(dict.Type))
                    {
                        if (DictUtils.YomichanDictTypes.Contains(dict.Type))
                        {
                            Dictionary<string, IntermediaryResult> results = useDB
                                ? GetKanjiResultsFromDB(text, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                                : GetKanjiResults(text, dict);

                            if (results.Count > 0)
                            {
                                lookupResults.AddRange(BuildYomichanKanjiResult(results, useDBForPitchDict, pitchDict));
                            }
                        }

                        else //if (DictUtils.NazekaDictTypes.Contains(dict.Type))
                        {
                            Dictionary<string, IntermediaryResult> results = useDB
                                ? GetKanjiResultsFromDB(text, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                                : GetKanjiResults(text, dict);

                            if (results.Count > 0)
                            {
                                lookupResults.AddRange(BuildEpwingNazekaResult(results, dbFreqs, useDBForPitchDict, pitchDict));
                            }
                        }
                    }
                }
            });

            return lookupResults.IsEmpty
                ? null
                : SortLookupResults(lookupResults);
        }

        List<string> textList = new();
        List<string> textInHiraganaList = new();
        List<HashSet<Form>> deconjugationResultsList = new();

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

        _ = Parallel.ForEach(DictUtils.Dicts.Values.ToList(), dict =>
        {
            if (dict.Active)
            {
                bool useDB = (dict.Options?.UseDB?.Value ?? false) && dict.Ready;

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, dict, useDB, JmdictDBManager.GetRecordsFromDB);
                        if (jmdictResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildJmdictResult(jmdictResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }
                        break;

                    case DictType.JMnedict:
                        Dictionary<string, IntermediaryResult> jmnedictResults = GetNameResults(textList, textInHiraganaList, dict, useDB, JmnedictDBManager.GetRecordsFromDB);
                        if (jmnedictResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildJmnedictResult(jmnedictResults, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.Kanjidic:
                        Dictionary<string, IntermediaryResult> kanjidicResults = useDB
                            ? GetKanjiResultsFromDB(text, dict, KanjidicDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        if (kanjidicResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildKanjidicResult(kanjidicResults, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.NonspecificKanjiWithWordSchemaYomichan:
                    case DictType.KanjigenYomichan:
                        // Template-wise, Kanjigen is a word dictionary that's why its results are put into Yomichan Word Results
                        // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                        Dictionary<string, IntermediaryResult> epwingYomichanKanjiWithWordSchemaResults = useDB
                            ? GetKanjiResultsFromDB(text, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanKanjiWithWordSchemaResults, dbFreqs, useDBForPitchDict, pitchDict));

                        break;

                    case DictType.CustomWordDictionary:
                    case DictType.ProfileCustomWordDictionary:
                        Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, dict, false, null);
                        if (customWordResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildCustomWordResult(customWordResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }
                        break;

                    case DictType.CustomNameDictionary:
                    case DictType.ProfileCustomNameDictionary:
                        Dictionary<string, IntermediaryResult> customNameResults = GetNameResults(textList, textInHiraganaList, dict, false, null);
                        if (customNameResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildCustomNameResult(customNameResults, useDBForPitchDict, pitchDict));
                        }
                        break;

                    case DictType.NonspecificKanjiYomichan:
                        Dictionary<string, IntermediaryResult> epwingYomichanKanjiResults = useDB
                            ? GetKanjiResultsFromDB(text, dict, YomichanKanjiDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        if (epwingYomichanKanjiResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildYomichanKanjiResult(epwingYomichanKanjiResults, useDBForPitchDict, pitchDict));
                        }
                        break;

                    case DictType.NonspecificNameYomichan:
                        Dictionary<string, IntermediaryResult> epwingYomichanNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB);

                        if (epwingYomichanNameResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }

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
                        Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB);
                        if (epwingYomichanWordResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.NonspecificKanjiNazeka:
                        Dictionary<string, IntermediaryResult> epwingNazekaKanjiResults = useDB
                            ? GetKanjiResultsFromDB(text, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                            : GetKanjiResults(text, dict);

                        if (epwingNazekaKanjiResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaKanjiResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.NonspecificNameNazeka:
                        Dictionary<string, IntermediaryResult> epwingNazekaNameResults = GetNameResults(textList, textInHiraganaList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB);
                        if (epwingNazekaNameResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.DaijirinNazeka:
                    case DictType.KenkyuushaNazeka:
                    case DictType.ShinmeikaiNazeka:
                    case DictType.NonspecificWordNazeka:
                    case DictType.NonspecificNazeka:
                        Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB);
                        if (epwingNazekaWordResults.Count > 0)
                        {
                            lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaWordResults, dbFreqs, useDBForPitchDict, pitchDict));
                        }

                        break;

                    case DictType.PitchAccentYomichan:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                }
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

                if (index is -1)
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

                return index is not -1
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
            Dictionary<string, IList<IDictRecord>>? dbWordDict,
            Dictionary<string, IList<IDictRecord>>? dbVerbDict)
    {
        Dictionary<string, IList<IDictRecord>> wordDict = dbWordDict ?? dict.Contents;
        Dictionary<string, IList<IDictRecord>> verbDict = dbVerbDict ?? dict.Contents;

        bool tryLongVowelConversion = true;

        if (wordDict.TryGetValue(textInHiragana, out IList<IDictRecord>? tempResult))
        {
            _ = results.TryAdd(textInHiragana,
                new IntermediaryResult(new List<IList<IDictRecord>> { tempResult }, null, matchedText, matchedText,
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
                                if (index is not -1)
                                {
                                    //if (!r.Processes?[index].Any(p => p.SequenceEqual(deconjugationResult.Process)) ?? false)
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
                                new IntermediaryResult(new List<IList<IDictRecord>> { resultsList },
                                    new List<List<List<string>>> { new() { deconjugationResult.Process } },
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
        List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB)
    {
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;

        if (useDB)
        {
            Parallel.Invoke(() => dbWordDict = getRecordsFromDB!(dict.Name, textInHiraganaList),
                () => dbVerbDict = getRecordsFromDB!(dict.Name, deconjugationResultsList
                    .SelectMany(static lf => lf.Select(static f => f.Text))
                    .Distinct().ToList()));
        }

        Dictionary<string, IntermediaryResult> results = new();
        int succAttempt = 0;
        for (int i = 0; i < textList.Count; i++)
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

                    if (useDB)
                    {
                        dbWordDict = getRecordsFromDB!(dict.Name, textWithoutLongVowelMarkList);
                    }

                    for (int j = 0; j < textWithoutLongVowelMarkList.Count; j++)
                    {
                        _ = GetWordResultsHelper(dict, results, null, textList[i], textWithoutLongVowelMarkList[j], succAttempt, dbWordDict, null);
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

        List<IDictRecord> resultsList = new();

        switch (dict.Type)
        {
            case DictType.JMdict:
                {
                    int dictResultsCount = dictResults.Count;
                    for (int i = 0; i < dictResultsCount; i++)
                    {
                        JmdictRecord dictResult = (JmdictRecord)dictResults[i];

                        if (lastTag is null || dictResult.WordClasses.SelectMany(static pos => pos).Contains(lastTag))
                        {
                            resultsList.Add(dictResult);
                        }
                    }
                }
                break;

            case DictType.CustomWordDictionary:
            case DictType.ProfileCustomWordDictionary:
                {
                    int dictResultsCount = dictResults.Count;
                    for (int i = 0; i < dictResultsCount; i++)
                    {
                        CustomWordRecord dictResult = (CustomWordRecord)dictResults[i];

                        if (lastTag is null || dictResult.WordClasses.Contains(lastTag))
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
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
                {
                    int dictResultsCount = dictResults.Count;
                    for (int i = 0; i < dictResultsCount; i++)
                    {
                        EpwingYomichanRecord dictResult = (EpwingYomichanRecord)dictResults[i];

                        if (lastTag is null || (dictResult.WordClasses?.Contains(lastTag) ?? false))
                        {
                            resultsList.Add(dictResult);
                        }

                        else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                     out IList<JmdictWordClass>? jmdictWcResults))
                        {
                            for (int j = 0; j < jmdictWcResults.Count; j++)
                            {
                                JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                                if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                    && (jmdictWordClassResult.Readings?.Contains(dictResult.Reading ?? string.Empty)
                                        ?? string.IsNullOrEmpty(dictResult.Reading)))
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
                        EpwingNazekaRecord dictResult = (EpwingNazekaRecord)dictResults[i];

                        if (deconjugationResult.Tags.Count is 0)
                        {
                            resultsList.Add(dictResult);
                        }

                        else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                     out IList<JmdictWordClass>? jmdictWcResults))
                        {
                            for (int j = 0; j < jmdictWcResults.Count; j++)
                            {
                                JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                                if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                    && (jmdictWordClassResult.Readings?.Contains(dictResult.Reading ?? "")
                                        ?? string.IsNullOrEmpty(dictResult.Reading)))
                                {
                                    if (lastTag is not null && jmdictWordClassResult.WordClasses.Contains(lastTag))
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
            case DictType.JMnedict:
            case DictType.Kanjidic:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomNameDictionary:
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid DictType");
        }

        return resultsList;
    }

    private static Dictionary<string, IntermediaryResult> GetNameResults(List<string> textList, List<string> textInHiraganaList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB)
    {
        Dictionary<string, IList<IDictRecord>> nameDict = useDB
            ? getRecordsFromDB!(dict.Name, textInHiraganaList)
            : dict.Contents;

        Dictionary<string, IntermediaryResult> nameResults = new();

        for (int i = 0; i < textList.Count; i++)
        {
            if (nameDict.TryGetValue(textInHiraganaList[i], out IList<IDictRecord>? result))
            {
                nameResults.Add(textInHiraganaList[i],
                    new IntermediaryResult(new List<IList<IDictRecord>> { result }, null, textList[i], textList[i], dict));
            }
        }

        return nameResults;
    }

    private static Dictionary<string, IntermediaryResult> GetKanjiResults(string text, Dict dict)
    {
        Dictionary<string, IntermediaryResult> kanjiResults = new();

        string kanji = text.EnumerateRunes().First().ToString();

        if (dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result))
        {
            kanjiResults.Add(kanji,
                new IntermediaryResult(new List<IList<IDictRecord>> { result }, null, kanji, kanji, dict));
        }

        return kanjiResults;
    }

    private static Dictionary<string, IntermediaryResult> GetKanjiResultsFromDB(string text, Dict dict, GetKanjiRecordsFromDB getKanjiRecordsFromDB)
    {
        Dictionary<string, IntermediaryResult> kanjiResults = new();

        string kanji = text.EnumerateRunes().First().ToString();

        List<IDictRecord> results = getKanjiRecordsFromDB(dict.Name, kanji);

        if (results.Count > 0)
        {
            kanjiResults.Add(kanji, new IntermediaryResult(new List<IList<IDictRecord>> { results }, null, kanji, kanji, dict));
        }

        return kanjiResults;
    }

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(List<Freq> dbFreqs, List<string> searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new();
        _ = Parallel.For(0, dbFreqs.Count, i =>
        {
            Freq freq = dbFreqs[i];
            _ = frequencyDicts.TryAdd(freq.Name, FreqDBManager.GetRecordsFromDB(freq.Name, searchKeys));
        });

        return frequencyDicts;
    }

    private static List<string> GetSearchKeysFromJmdictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new();
        foreach ((string key, IntermediaryResult intermediaryResult) in dictResults)
        {
            _ = searchKeys.Add(key);

            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
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

    private static List<string> GetSearchKeysFromJmnedictRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new();
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
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
        HashSet<string> searchKeys = new();
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
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

    private static List<string> GetSearchKeyForEpwingYomichanRecord(Dictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = new();
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
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

    private static List<string> GetSearchKeysFromEpwingNazekaRecord(Dictionary<string, IntermediaryResult> dictResults, bool includeAlternativeSpellings)
    {
        HashSet<string> searchKeys = new();
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            List<IList<IDictRecord>> dictRecordsList = intermediaryResult.Results;
            for (int i = 0; i < dictRecordsList.Count; i++)
            {
                IList<IDictRecord> dictRecords = dictRecordsList[i];
                for (int j = 0; j < dictRecords.Count; j++)
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

        List<LookupResult> results = new();
        foreach (IntermediaryResult wordResult in jmdictResults.Values.ToList())
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    JmdictRecord jmdictResult = (JmdictRecord)wordResult.Results[i][j];
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

        List<LookupResult> results = new();
        foreach (IntermediaryResult nameResult in jmnedictResults.Values.ToList())
        {
            int resultsListCount = nameResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = nameResult.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    JmnedictRecord jmnedictRecord = (JmnedictRecord)nameResult.Results[i][j];

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

    private static List<LookupResult> BuildKanjidicResult(Dictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict)
    {
        List<LookupResult> results = new();

        KeyValuePair<string, IntermediaryResult> dictResult = kanjiResults.First();

        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (useDBForPitchDict)
        {
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, dictResult.Key);
        }

        List<IList<IDictRecord>> iResult = dictResult.Value.Results;
        KanjidicRecord kanjiRecord = (KanjidicRecord)iResult[0][0];

        string[]? allReadings = Utils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        IntermediaryResult intermediaryResult = kanjiResults.First().Value;

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
            kanjiComposition: DictUtils.s_kanjiCompositionDict.GetValueOrDefault(dictResult.Key),
            frequencies: GetKanjidicFrequencies(dictResult.Key, kanjiRecord.Frequency),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchAccentDict: pitchAccentDict
        );

        results.Add(result);
        return results;
    }

    private static ConcurrentBag<LookupResult> BuildYomichanKanjiResult(
        Dictionary<string, IntermediaryResult> kanjiResults, bool useDBForPitchDict, Dict? pitchDict)
    {
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (useDBForPitchDict)
        {
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, kanjiResults.First().Key);
        }

        ConcurrentBag<LookupResult> results = new();
        _ = Parallel.ForEach(kanjiResults.ToList(), kanjiResult =>
        {
            int resultsListCount = kanjiResult.Value.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = kanjiResult.Value.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)kanjiResult.Value.Results[i][j];

                    string[]? allReadings = Utils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);

                    LookupResult result = new
                    (
                        primarySpelling: kanjiResult.Key,
                        readings: allReadings,
                        onReadings: yomichanKanjiDictResult.OnReadings,
                        kunReadings: yomichanKanjiDictResult.KunReadings,
                        kanjiComposition: DictUtils.s_kanjiCompositionDict.GetValueOrDefault(kanjiResult.Key),
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
    Dictionary<string, IntermediaryResult> epwingResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
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

        List<LookupResult> results = new();
        foreach (IntermediaryResult wordResult in epwingResults.Values.ToList())
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)wordResult.Results[i][j];

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        deconjugationProcess: LookupResultUtils.DeconjugationProcessesToText(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult, frequencyDicts),
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading is not null
                            ? new[] { epwingResult.Reading }
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
        Dictionary<string, IntermediaryResult> epwingNazekaResults, List<Freq> dbFreqs, bool useDBForPitchDict, Dict? pitchDict)
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

        ConcurrentBag<LookupResult> results = new();
        _ = Parallel.ForEach(epwingNazekaResults.Values.ToList(), wordResult =>
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)wordResult.Results[i][j];

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
                            ? new[] { epwingResult.Reading }
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
                List<string> searchKeys = GetSearchKeysFromEpwingNazekaRecord(customWordResults, true);
                frequencyDicts = GetFrequencyDictsFromDB(dbFreqs, searchKeys);
            }
        },
        () =>
        {
            if (useDBForPitchDict)
            {
                List<string> searchKeys = GetSearchKeysFromEpwingNazekaRecord(customWordResults, false);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict!.Name, searchKeys);
            }
        });

        ConcurrentBag<LookupResult> results = new();
        _ = Parallel.ForEach(customWordResults.Values.ToList(), wordResult =>
        {
            int wordResultsListCount = wordResult.Results.Count;
            for (int i = 0; i < wordResultsListCount; i++)
            {
                int wordResultCount = wordResult.Results[i].Count;

                for (int j = 0; j < wordResultCount; j++)
                {
                    CustomWordRecord customWordDictResult = (CustomWordRecord)wordResult.Results[i][j];

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

        ConcurrentBag<LookupResult> results = new();
        _ = Parallel.ForEach(customNameResults.ToList(), customNameResult =>
        {
            int resultsListCount = customNameResult.Value.Results.Count;
            int freq = 0;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = customNameResult.Value.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    CustomNameRecord customNameDictResult = (CustomNameRecord)customNameResult.Value.Results[i][j];
                    LookupResult result = new
                    (
                        primarySpelling: customNameDictResult.PrimarySpelling,
                        matchedText: customNameResult.Value.MatchedText,
                        deconjugatedMatchedText: customNameResult.Value.DeconjugatedMatchedText,
                        frequencies: new List<LookupFrequencyResult> { new(customNameResult.Value.Dict.Name, -freq, false) },
                        dict: customNameResult.Value.Dict,
                        readings: customNameDictResult.Reading is not null
                            ? new[] { customNameDictResult.Reading }
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

    private static List<LookupFrequencyResult> GetWordFrequencies(IGetFrequency record, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB)
    {
        List<LookupFrequencyResult> freqsList = new();
        List<Freq> freqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Active: true, Type: not FreqType.YomichanKanji }).OrderBy(static f => f.Priority).ToList();

        for (int i = 0; i < freqs.Count; i++)
        {
            Freq freq = freqs[i];
            bool useDB = (freq.Options?.UseDB?.Value ?? false) && freq.Ready;

            if (useDB)
            {
                if (freqDictsFromDB?.TryGetValue(freq.Name, out Dictionary<string, List<FrequencyRecord>>? freqDict) ?? false)
                {
                    freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequencyFromDB(freqDict), freq.Options?.HigherValueMeansHigherFrequency?.Value ?? false));
                }
            }

            else
            {
                freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freq), freq.Options?.HigherValueMeansHigherFrequency?.Value ?? false));
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult> GetYomichanKanjiFrequencies(string kanji)
    {
        List<LookupFrequencyResult> freqsList = new();
        List<Freq> kanjiFreqs = FreqUtils.FreqDicts.Values.Where(static f => f is { Type: FreqType.YomichanKanji, Active: true }).OrderBy(static f => f.Priority).ToList();
        for (int i = 0; i < kanjiFreqs.Count; i++)
        {
            Freq kanjiFreq = kanjiFreqs[i];
            bool useDB = (kanjiFreq.Options?.UseDB?.Value ?? false) && kanjiFreq.Ready;
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
        List<LookupFrequencyResult> freqsList = new();

        if (frequency is not 0)
        {
            freqsList.Add(new LookupFrequencyResult("KANJIDIC2", frequency, false));
        }

        freqsList.AddRange(GetYomichanKanjiFrequencies(kanji));
        return freqsList;
    }
}
