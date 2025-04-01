using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JL.Core.Config;
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
    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string parameterOrQuery);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string dbName, string term);

    public static LookupResult[]? LookupText(string text)
    {
        bool dbIsUsedForPitchDict = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict)
            && pitchDict is { Active: true, Options.UseDB.Value: true, Ready: true };

        ConcurrentBag<LookupResult> lookupResults = [];

        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        Freq[]? kanjiFreqs = FreqUtils.KanjiFreqs;
        string kanji = "";
        string[]? kanjiComposition = null;
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = text.EnumerateRunes().First().ToString();
            _ = KanjiCompositionUtils.KanjiCompositionDict.TryGetValue(kanji, out kanjiComposition);

            kanjiFrequencyResults = kanjiFreqs is not null
                ? GetKanjiFrequencies(kanji, kanjiFreqs)
                : null;
        }

        Freq[]? wordFreqs = FreqUtils.WordFreqs;
        Freq[]? dbWordFreqs = null;
        if (wordFreqs is not null)
        {
            dbWordFreqs = wordFreqs
                .Where(static f => f is { Options.UseDB.Value: true, Ready: true })
                .ToArray();

            if (dbWordFreqs.Length is 0)
            {
                dbWordFreqs = null;
            }
        }

        int textLength = text.Length;
        List<string> textList = new(textLength);
        List<string> textInHiraganaList = new(textLength);
        List<List<Form>> deconjugationResultsList = new(textLength);
        List<List<string>?>? textWithoutLongVowelMarksList = null;
        int estimatedDeconjugatedTextCapacity = 0;
        int textWithoutLongVowelMarksCount = 0;

        bool doesNotStartWithLongVowelMark = text[0] is not 'ー';
        bool countLongVowelMark = doesNotStartWithLongVowelMark;
        for (int i = 0; i < textLength; i++)
        {
            if (char.IsHighSurrogate(text[textLength - i - 1]))
            {
                continue;
            }

            string currentText = text[..^i];

            textList.Add(currentText);

            string textInHiragana = JapaneseUtils.KatakanaToHiragana(currentText);
            textInHiraganaList.Add(textInHiragana);

            List<Form> deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
            estimatedDeconjugatedTextCapacity += deconjugationResults.Count;
            deconjugationResultsList.Add(deconjugationResults);

            if (doesNotStartWithLongVowelMark)
            {
                int longVowelMarkCount = 0;
                if (countLongVowelMark)
                {
                    foreach (char hiragana in textInHiragana)
                    {
                        if (hiragana is 'ー')
                        {
                            ++longVowelMarkCount;
                        }
                    }
                }

                if (longVowelMarkCount > 0)
                {
                    textWithoutLongVowelMarksList ??= new List<List<string>?>(textLength);
                    if (longVowelMarkCount < 5)
                    {
                        List<string> textWithoutLongVowelMarks = JapaneseUtils.LongVowelMarkToKana(textInHiragana);
                        textWithoutLongVowelMarksCount += textWithoutLongVowelMarks.Count;
                        textWithoutLongVowelMarksList.Add(textWithoutLongVowelMarks);
                    }
                    else
                    {
                        textWithoutLongVowelMarksList.Add(null);
                    }
                }
                else
                {
                    textWithoutLongVowelMarksList?.Add(null);
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
        string[]? deconjugatedTexts = null;
        if (DictUtils.DBIsUsedForAtLeastOneWordDict || dbIsUsedForPitchDict || dbIsUsedForAtLeastOneWordFreqDict)
        {
            HashSet<string> deconjugatedTextsHashSet = new(Math.Min(estimatedDeconjugatedTextCapacity, 256), StringComparer.Ordinal);
            foreach (ref readonly List<Form> deconjugationResults in CollectionsMarshal.AsSpan(deconjugationResultsList))
            {
                foreach (ref readonly Form form in CollectionsMarshal.AsSpan(deconjugationResults))
                {
                    _ = deconjugatedTextsHashSet.Add(form.Text);
                }
            }

            deconjugatedTexts = deconjugatedTextsHashSet.ToArray();
        }

        if (DictUtils.DBIsUsedForAtLeastOneDict)
        {
            parameter = DBUtils.GetParameter(textInHiraganaList.Count);

            if (deconjugatedTexts is not null && deconjugatedTexts.Length > 0)
            {
                verbParameter = DBUtils.GetParameter(deconjugatedTexts.Length);
            }

            if (textWithoutLongVowelMarksList is not null)
            {
                ReadOnlySpan<List<string>?> textWithoutLongVowelMarkListSpan = CollectionsMarshal.AsSpan(textWithoutLongVowelMarksList);
                int textWithoutLongVowelMarkListSpanLength = textWithoutLongVowelMarkListSpan.Length;

                if (DictUtils.DBIsUsedForJmdict)
                {
                    jmdictTextWithoutLongVowelMarkParameters = new List<string?>(textWithoutLongVowelMarkListSpanLength);
                }
                if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                {
                    yomichanTextWithoutLongVowelMarkQueries = new List<string?>(textWithoutLongVowelMarkListSpanLength);
                }
                if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                {
                    nazekaTextWithoutLongVowelMarkQueries = new List<string?>(textWithoutLongVowelMarkListSpanLength);
                }

                foreach (ref readonly List<string>? textWithoutLongVowelMark in textWithoutLongVowelMarkListSpan)
                {
                    if (textWithoutLongVowelMark is not null)
                    {
                        int textWithoutLongVowelMarkCount = textWithoutLongVowelMark.Count;
                        if (DictUtils.DBIsUsedForJmdict)
                        {
                            jmdictTextWithoutLongVowelMarkParameters!.Add(DBUtils.GetParameter(textWithoutLongVowelMarkCount));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                        {
                            yomichanTextWithoutLongVowelMarkQueries!.Add(EpwingYomichanDBManager.GetQuery(textWithoutLongVowelMarkCount));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                        {
                            nazekaTextWithoutLongVowelMarkQueries!.Add(EpwingNazekaDBManager.GetQuery(textWithoutLongVowelMarkCount));
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
        HashSet<string>? allSearchKeys = null;
        if (dbIsUsedForPitchDict || dbIsUsedAtLeastOneYomichanOrNazekaWordDict)
        {
            allSearchKeys = new HashSet<string>(textInHiraganaList.Count + deconjugatedTexts!.Length + textWithoutLongVowelMarksCount, StringComparer.Ordinal);
            allSearchKeys.UnionWith(textInHiraganaList);
            allSearchKeys.UnionWith(deconjugatedTexts);

            if (textWithoutLongVowelMarksList is not null)
            {
                foreach (ref readonly List<string>? textWithoutLongVowelMarks in CollectionsMarshal.AsSpan(textWithoutLongVowelMarksList))
                {
                    if (textWithoutLongVowelMarks is not null)
                    {
                        allSearchKeys.UnionWith(textWithoutLongVowelMarks);
                    }
                }
            }
        }

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

        LookupCategory lookupType = CoreConfigManager.Instance.LookupCategory;
        List<Dict> dicts;
        if (lookupType is LookupCategory.All)
        {
            dicts = DictUtils.Dicts.Values.Where(static dict => dict is { Active: true, Type: not DictType.PitchAccentYomichan }).ToList();
        }
        else if (lookupType is LookupCategory.Kanji)
        {
            dicts = DictUtils.AtLeastOneKanjiDictIsActive ? DictUtils.Dicts.Values.Where(static dict => dict.Active && DictUtils.KanjiDictTypes.Contains(dict.Type)).ToList() : [];
        }
        else if (lookupType is LookupCategory.Name)
        {
            dicts = DictUtils.Dicts.Values.Where(static dict => dict.Active && DictUtils.s_nameDictTypes.Contains(dict.Type)).ToList();
        }
        else if (lookupType is LookupCategory.Word)
        {
            dicts = DictUtils.Dicts.Values.Where(static dict => dict.Active && DictUtils.s_wordDictTypes.Contains(dict.Type)).ToList();
        }
        else // if (lookupType is LookupType.Other)
        {
            dicts = DictUtils.Dicts.Values.Where(static dict => dict.Active && DictUtils.s_otherDictTypes.Contains(dict.Type)).ToList();
        }

        _ = Parallel.ForEach(dicts, dict =>
        {
            bool useDB = dict.Options.UseDB.Value && dict.Ready;
            switch (dict.Type)
            {
                case DictType.JMdict:
                    Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, dict, useDB, JmdictDBManager.GetRecordsFromDB, parameter, verbParameter, jmdictTextWithoutLongVowelMarkParameters);
                    if (jmdictResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.JMnedict:
                    Dictionary<string, IntermediaryResult>? jmnedictResults = GetNameResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, dict, useDB, JmnedictDBManager.GetRecordsFromDB, parameter);
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
                        lookupResults.Add(BuildKanjidicResult(kanji, kanjiComposition, kanjidicResult, kanjiFrequencyResults, pitchAccentDict));
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
                        lookupResults.AddRange(BuildEpwingYomichanResultForKanjiWithWordSchema(kanjiComposition, epwingYomichanKanjiWithWordSchemaResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, dict, false, null, parameter, verbParameter, null);
                    if (customWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    Dictionary<string, IntermediaryResult>? customNameResults = GetNameResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, dict, false, null, parameter);
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
                        lookupResults.AddRange(BuildYomichanKanjiResult(kanji, kanjiComposition, epwingYomichanKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanNameResults = GetNameResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery);
                    if (epwingYomichanNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery, yomichanVerbQuery, yomichanTextWithoutLongVowelMarkQueries);
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
                        lookupResults.AddRange(BuildEpwingNazekaResultForKanji(kanjiComposition, epwingNazekaKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaNameResults = GetNameResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery);
                    if (epwingNazekaNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(CollectionsMarshal.AsSpan(textList), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQueries);
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
            .ThenByDescending(static lookupResult => lookupResult.Readings is not null && lookupResult.Readings.Contains(lookupResult.MatchedText))
            .ThenByDescending(static lookupResult => lookupResult.DeconjugationProcess is null ? int.MaxValue : lookupResult.PrimarySpelling.Length)
            .ThenBy(static lookupResult => lookupResult.Dict.Priority)
            .ThenBy(static lookupResult =>
            {
                JmdictLookupResult? jmdictResult = lookupResult.JmdictLookupResult;
                if (jmdictResult?.PrimarySpellingOrthographyInfoList is not null && lookupResult.PrimarySpelling == lookupResult.MatchedText)
                {
                    foreach (string primarySpellingOrthographyInfo in jmdictResult.PrimarySpellingOrthographyInfoList)
                    {
                        if (primarySpellingOrthographyInfo is "oK" or "iK" or "rK")
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

                JmdictLookupResult? jmdictLookupResult = lookupResult.JmdictLookupResult;
                if (jmdictLookupResult is not null)
                {
                    if (jmdictLookupResult.MiscSharedByAllSenses is not null && jmdictLookupResult.MiscSharedByAllSenses.Contains("uk"))
                    {
                        return 0;
                    }

                    if (jmdictLookupResult.MiscList is not null)
                    {
                        foreach (string[]? misc in jmdictLookupResult.MiscList)
                        {
                            if (misc is not null && misc.Contains("uk"))
                            {
                                return 0;
                            }
                        }
                    }

                    string[]? readingsOrthographyInfo = jmdictLookupResult.ReadingsOrthographyInfoList?[index];
                    if (readingsOrthographyInfo is not null)
                    {
                        foreach (string readingsOrthographyInfoItem in readingsOrthographyInfo)
                        {
                            if (readingsOrthographyInfoItem is "ok" or "ik" or "rk")
                            {
                                return 3;
                            }
                        }
                    }
                }

                return 1;
            })
            .ThenBy(static lookupResult =>
            {
                if (lookupResult.Frequencies is not null && lookupResult.Frequencies.Count > 0)
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
            foreach (ref readonly Form deconjugationResult in CollectionsMarshal.AsSpan(deconjugationResults))
            {
                if (verbDict.TryGetValue(deconjugationResult.Text, out IList<IDictRecord>? dictResults))
                {
                    List<IDictRecord> resultsList = GetValidDeconjugatedResults(dict, deconjugationResult, dictResults);
                    if (resultsList.Count > 0)
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? result))
                        {
                            if (result.MatchedText == deconjugationResult.OriginalText)
                            {
                                int index = result.Results.FindIndex(rs => rs.SequenceEqual(resultsList));
                                if (index >= 0)
                                {
                                    List<List<string>> processes = result.Processes![index];
                                    ReadOnlySpan<string> processSpan = CollectionsMarshal.AsSpan(deconjugationResult.Process);

                                    bool addProcess = true;
                                    foreach (ref readonly List<string> process in CollectionsMarshal.AsSpan(processes))
                                    {
                                        if (CollectionsMarshal.AsSpan(process).SequenceEqual(processSpan))
                                        {
                                            addProcess = false;
                                            break;
                                        }
                                    }

                                    if (addProcess)
                                    {
                                        processes.Add(deconjugationResult.Process);
                                    }
                                }
                                else
                                {
                                    result.Results.Add(resultsList);
                                    result.Processes!.Add([deconjugationResult.Process]);
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

    private static Dictionary<string, IntermediaryResult> GetWordResults(ReadOnlySpan<string> textList, List<string> textInHiraganaList,
        List<List<Form>> deconjugationResultsList, string[]? deconjugatedTexts, List<List<string>?>? textWithoutLongVowelMarkList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB,
        string? queryOrParameter, string? verbQueryOrParameter, List<string?>? longVowelQueryOrParameters)
    {
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;

        if (useDB)
        {
            Parallel.Invoke(
                () =>
                {
                    dbWordDict = getRecordsFromDB!(dict.Name, CollectionsMarshal.AsSpan(textInHiraganaList), queryOrParameter!);
                },
                () =>
                {
                    dbVerbDict = getRecordsFromDB!(dict.Name, deconjugatedTexts!.AsSpan(), verbQueryOrParameter!);
                });
        }

        bool textWithoutLongVowelMarkListExist = textWithoutLongVowelMarkList is not null;
        Dictionary<string, IntermediaryResult> results = new(StringComparer.Ordinal);
        for (int i = 0; i < textList.Length; i++)
        {
            ref readonly string text = ref textList[i];
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], text, textInHiraganaList[i], dbWordDict, dbVerbDict);

            List<string>? textsWithoutLongVowelMark = textWithoutLongVowelMarkListExist ? textWithoutLongVowelMarkList![i] : null;
            if (textsWithoutLongVowelMark is not null)
            {
                ReadOnlySpan<string> textsWithoutLongVowelMarkSpan = CollectionsMarshal.AsSpan(textsWithoutLongVowelMark);

                Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = useDB
                    ? getRecordsFromDB!(dict.Name, textsWithoutLongVowelMarkSpan, longVowelQueryOrParameters![i]!)
                    : null;

                foreach (ref readonly string textWithoutLongVowelMark in textsWithoutLongVowelMarkSpan)
                {
                    GetWordResultsHelper(dict, results, null, text, textWithoutLongVowelMark, dbWordDictForLongVowelConversion, null);
                }
            }
        }

        return results;
    }

    private static List<IDictRecord> GetValidDeconjugatedResults(Dict dict, Form deconjugationResult, IList<IDictRecord> dictResults)
    {
        string lastTag = deconjugationResult.Tags[^1];
        List<IDictRecord> resultsList = new(dictResults.Count);
        switch (dict.Type)
        {
            case DictType.JMdict:
            {
                int dictResultsCount = dictResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    JmdictRecord dictResult = (JmdictRecord)dictResults[i];
                    if (dictResult.WordClassesSharedByAllSenses is not null
                        && dictResult.WordClassesSharedByAllSenses.Contains(lastTag))
                    {
                        resultsList.Add(dictResult);
                    }

                    else if (dictResult.WordClasses is not null)
                    {
                        foreach (string[]? wordClasses in dictResult.WordClasses)
                        {
                            if (wordClasses is not null && wordClasses.Contains(lastTag))
                            {
                                resultsList.Add(dictResult);
                                break;
                            }
                        }
                    }
                }

                break;
            }

            case DictType.CustomWordDictionary:
            case DictType.ProfileCustomWordDictionary:
            {
                int dictResultsCount = dictResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
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
                int dictResultsCount = dictResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    EpwingYomichanRecord dictResult = (EpwingYomichanRecord)dictResults[i];
                    if (dictResult.WordClasses is not null)
                    {
                        // It seems like instead of storing precise tags like v5r
                        // Yomichan dictionaries simply store the general v5 tag
                        foreach (string wordClass in dictResult.WordClasses)
                        {
                            if (lastTag.StartsWith(wordClass, StringComparison.Ordinal))
                            {
                                resultsList.Add(dictResult);
                                break;
                            }
                        }
                    }
                    else if (WordClassDictionaryContainsTag(dictResult.PrimarySpelling, dictResult.Reading, lastTag))
                    {
                        resultsList.Add(dictResult);
                    }
                }

                break;
            }

            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificNazeka:
            {
                int dictResultsCount = dictResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    EpwingNazekaRecord dictResult = (EpwingNazekaRecord)dictResults[i];
                    if (WordClassDictionaryContainsTag(dictResult.PrimarySpelling, dictResult.Reading, lastTag))
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

    private static Dictionary<string, IntermediaryResult>? GetNameResults(ReadOnlySpan<string> textList, List<string> textInHiraganaList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB, string? queryOrParameter)
    {
        IDictionary<string, IList<IDictRecord>>? nameDict = useDB
            ? getRecordsFromDB!(dict.Name, CollectionsMarshal.AsSpan(textInHiraganaList), queryOrParameter!)
            : dict.Contents;

        if (nameDict is null)
        {
            return null;
        }

        Dictionary<string, IntermediaryResult> nameResults = new(textList.Length, StringComparer.Ordinal);
        for (int i = 0; i < textList.Length; i++)
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

        return results is not null && results.Count > 0
            ? new IntermediaryResult(kanji, dict, [results])
            : null;
    }

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, HashSet<string> searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new(-1, dbFreqs.Length, StringComparer.Ordinal);
        _ = Parallel.ForEach(dbFreqs, freq =>
        {
            Dictionary<string, List<FrequencyRecord>>? freqRecords = FreqDBManager.GetRecordsFromDB(freq.Name, searchKeys);
            if (freqRecords is not null)
            {
                _ = frequencyDicts.TryAdd(freq.Name, freqRecords);
            }
        });

        return frequencyDicts;
    }

    private static List<LookupResult> BuildJmdictResult(
        Dictionary<string, IntermediaryResult> jmdictResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, bool dbIsUsedForPitchDict, Dict? pitchDict)
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
            ReadOnlySpan<List<List<string>>> processesSpan = CollectionsMarshal.AsSpan(wordResult.Processes);
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = CollectionsMarshal.AsSpan(wordResult.Results);
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i])
                    : null;

                ref readonly IList<IDictRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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
                        jmdictLookupResult: new JmdictLookupResult(jmdictResult.PrimarySpellingOrthographyInfo, jmdictResult.ReadingsOrthographyInfo, jmdictResult.AlternativeSpellingsOrthographyInfo, jmdictResult.MiscSharedByAllSenses, jmdictResult.Misc, jmdictResult.WordClasses),
                        dict: wordResult.Dict,
                        formattedDefinitions: jmdictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(jmdictResult.PrimarySpelling, jmdictResult.Readings, pitchAccentDict!) : null,
                        wordClasses: jmdictResult.WordClassesSharedByAllSenses
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
            foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(intermediaryResult.Results))
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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
            foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(nameResult.Results))
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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

    private static LookupResult BuildKanjidicResult(string kanji, string[]? kanjiComposition, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        KanjidicRecord kanjiRecord = (KanjidicRecord)intermediaryResult.Results[0][0];

        string[]? allReadings = Utils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        bool pitchAccentDictExists = pitchAccentDict is not null;
        LookupResult result = new
        (
            primarySpelling: kanji,
            readings: allReadings,
            kanjiLookupResult: new KanjiLookupResult(kanjiComposition, kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings, kanjiRecord.RadicalNames, kanjiRecord.StrokeCount, kanjiRecord.Grade),
            frequencies: GetKanjidicFrequencies(kanjiRecord.Frequency, kanjiFrequencyResults),
            matchedText: intermediaryResult.MatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null
        );

        return result;
    }

    private static List<LookupResult> BuildYomichanKanjiResult(
        string kanji, string[]? kanjiComposition, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(intermediaryResult.Results))
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
            {
                YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)dictRecords[j];

                string[]? allReadings = Utils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);
                LookupResult result = new
                (
                    primarySpelling: kanji,
                    readings: allReadings,
                    kanjiLookupResult: new KanjiLookupResult(kanjiComposition, yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings, kanjiStats: yomichanKanjiDictResult.BuildFormattedStats()),
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
        IDictionary<string, IntermediaryResult> epwingResults, Freq[]? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool freqsExist = freqs is not null;
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in epwingResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = CollectionsMarshal.AsSpan(wordResult.Processes);
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = CollectionsMarshal.AsSpan(wordResult.Results);
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i])
                    : null;

                ref readonly IList<IDictRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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
                        pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                        wordClasses: epwingResult.WordClasses
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingYomichanResultForKanjiWithWordSchema(string[]? kanjiComposition, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(intermediaryResult.Results))
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
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
                    kanjiLookupResult: new KanjiLookupResult(kanjiComposition),
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingNazekaResult(
        IDictionary<string, IntermediaryResult> epwingNazekaResults, Freq[]? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        bool freqsExist = freqs is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in epwingNazekaResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = CollectionsMarshal.AsSpan(wordResult.Processes);
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = CollectionsMarshal.AsSpan(wordResult.Results);
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i])
                    : null;

                ref readonly IList<IDictRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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


    private static bool WordClassDictionaryContainsTag(string primarySpelling, string? reading, string tag)
    {
        if (DictUtils.WordClassDictionary.TryGetValue(primarySpelling, out IList<JmdictWordClass>? jmdictWcResults))
        {
            bool hasReading = reading is not null;
            int jmdictWcResultsCount = jmdictWcResults.Count;
            for (int i = 0; i < jmdictWcResultsCount; i++)
            {
                JmdictWordClass result = jmdictWcResults[i];
                if (primarySpelling == result.Spelling
                    && ((hasReading && result.Readings is not null && result.Readings.Contains(reading!))
                        || (!hasReading && result.Readings is null))
                    && result.WordClasses.Contains(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<LookupResult> BuildEpwingNazekaResultForKanji(string[]? kanjiComposition, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(intermediaryResult.Results))
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
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
                    kanjiLookupResult: new KanjiLookupResult(kanjiComposition),
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, bool dbIsUsedForPitchDict, Dict? pitchDict)
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
            ReadOnlySpan<List<List<string>>> processesSpan = CollectionsMarshal.AsSpan(wordResult.Processes);
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = CollectionsMarshal.AsSpan(wordResult.Results);
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i])
                    : null;

                ref readonly IList<IDictRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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
                        frequencies: wordFreqsExist ? GetWordFrequencies(customWordDictResult, wordFreqs!, frequencyDicts) : null,
                        wordClasses: customWordDictResult.WordClasses
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
            foreach (ref readonly IList<IDictRecord> dictRecords in CollectionsMarshal.AsSpan(customNameResult.Results))
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
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

    private static List<LookupFrequencyResult>? GetWordFrequencies<T>(T record, Freq[] wordFreqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB) where T : IGetFrequency
    {
        bool freqDictsFromDBExist = freqDictsFromDB is not null;
        List<LookupFrequencyResult> freqsList = new(wordFreqs.Length);
        foreach (Freq freq in wordFreqs)
        {
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

    private static List<LookupFrequencyResult>? GetKanjiFrequencies(string kanji, Freq[] kanjiFreqs)
    {
        List<LookupFrequencyResult> freqsList = new(kanjiFreqs.Length);
        foreach (Freq kanjiFreq in kanjiFreqs)
        {
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
                int recordsCount = records.Count;
                for (int i = 0; i < recordsCount; i++)
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
                    int recordsCount = records.Count;
                    for (int j = 0; j < recordsCount; j++)
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
                        int recordsCount = records.Count;
                        for (int j = 0; j < recordsCount; j++)
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
