using System.Collections.Concurrent;
using System.Diagnostics;
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
    private delegate Dictionary<string, IList<T>>? GetRecordsFromDB<T>(string dbName, ReadOnlySpan<string> terms, string parameterOrQuery) where T : IDictRecord;
    private delegate List<T>? GetKanjiRecordsFromDB<T>(string dbName, string term) where T : IDictRecord;

    public static LookupResult[]? LookupText(string text)
    {
        bool dbIsUsedForPitchDict = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out DictBase? pitchDictBase)
            && pitchDictBase is { Active: true, Options.UseDB.Value: true, Ready: true };

        Dict<PitchAccentRecord>? pitchDict = (Dict<PitchAccentRecord>?)pitchDictBase;

        ConcurrentBag<LookupResult> lookupResults = [];

        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        Freq[]? kanjiFreqs = FreqUtils.KanjiFreqs;
        string kanji = "";
        string[]? kanjiComposition = null;
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = TextUtils.GetFirstCharacter(text);
            _ = KanjiCompositionUtils.KanjiCompositionDict.TryGetValue(kanji, out kanjiComposition);

            kanjiFrequencyResults = kanjiFreqs is not null
                ? GetKanjiFrequencies(kanji, kanjiFreqs)
                : null;
        }

        Freq[]? wordFreqs = FreqUtils.WordFreqs;
        Freq[]? dbWordFreqs = null;
        if (wordFreqs is not null)
        {
            int validFreqCount = 0;
            foreach (Freq freq in wordFreqs)
            {
                if (freq.Options.UseDB.Value && freq.Ready)
                {
                    ++validFreqCount;
                }
            }

            if (validFreqCount > 0)
            {
                dbWordFreqs = new Freq[validFreqCount];
                int currentIndex = 0;
                foreach (Freq freq in wordFreqs)
                {
                    if (freq.Options.UseDB.Value && freq.Ready)
                    {
                        dbWordFreqs[currentIndex] = freq;
                        ++currentIndex;
                    }
                }
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
            foreach (ref readonly List<Form> deconjugationResults in deconjugationResultsList.AsReadOnlySpan())
            {
                foreach (ref readonly Form form in deconjugationResults.AsReadOnlySpan())
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
                ReadOnlySpan<List<string>?> textWithoutLongVowelMarkListSpan = textWithoutLongVowelMarksList.AsReadOnlySpan();
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
                            Debug.Assert(jmdictTextWithoutLongVowelMarkParameters is not null);
                            jmdictTextWithoutLongVowelMarkParameters.Add(DBUtils.GetParameter(textWithoutLongVowelMarkCount));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                        {
                            Debug.Assert(yomichanTextWithoutLongVowelMarkQueries is not null);
                            yomichanTextWithoutLongVowelMarkQueries.Add(EpwingYomichanDBManager.GetQuery(textWithoutLongVowelMarkCount));
                        }
                        if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                        {
                            Debug.Assert(nazekaTextWithoutLongVowelMarkQueries is not null);
                            nazekaTextWithoutLongVowelMarkQueries.Add(EpwingNazekaDBManager.GetQuery(textWithoutLongVowelMarkCount));
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
            Debug.Assert(deconjugatedTexts is not null);
            allSearchKeys = new HashSet<string>(textInHiraganaList.Count + deconjugatedTexts.Length + textWithoutLongVowelMarksCount, StringComparer.Ordinal);
            allSearchKeys.UnionWith(textInHiraganaList);
            allSearchKeys.UnionWith(deconjugatedTexts);

            if (textWithoutLongVowelMarksList is not null)
            {
                foreach (ref readonly List<string>? textWithoutLongVowelMarks in textWithoutLongVowelMarksList.AsReadOnlySpan())
                {
                    if (textWithoutLongVowelMarks is not null)
                    {
                        allSearchKeys.UnionWith(textWithoutLongVowelMarks);
                    }
                }
            }
        }

        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict = null;
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
                if (dbIsUsedForPitchDict)
                {
                    Debug.Assert(pitchDict is not null);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.Name, allSearchKeys);
                }
                else
                {
                    pitchAccentDict = pitchDict?.Contents;
                }
            });
        }
        else
        {
            pitchAccentDict = pitchDict?.Contents;
        }

        LookupCategory lookupType = CoreConfigManager.Instance.LookupCategory;
        List<DictBase> dicts = new(DictUtils.Dicts.Count);

        if (lookupType is LookupCategory.All)
        {
            foreach (DictBase dict in DictUtils.Dicts.Values)
            {
                if (dict is { Active: true, Type: not DictType.PitchAccentYomichan })
                {
                    dicts.Add(dict);
                }
            }
        }
        else if (lookupType is LookupCategory.Kanji)
        {
            if (DictUtils.AtLeastOneKanjiDictIsActive)
            {
                foreach (DictBase dict in DictUtils.Dicts.Values)
                {
                    if (dict.Active && DictUtils.KanjiDictTypes.Contains(dict.Type))
                    {
                        dicts.Add(dict);
                    }
                }
            }
        }
        else if (lookupType is LookupCategory.Name)
        {
            foreach (DictBase dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_nameDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }
        else if (lookupType is LookupCategory.Word)
        {
            foreach (DictBase dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_wordDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }
        else // if (lookupType is LookupCategory.Other)
        {
            foreach (DictBase dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_otherDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }

        _ = Parallel.ForEach(dicts, dict =>
        {
            bool useDB = dict.Options.UseDB.Value && dict.Ready;
            switch (dict.Type)
            {
                case DictType.JMdict:
                    Dictionary<string, IntermediaryResult<JmdictRecord>> jmdictResults = GetWordResults(textList.AsReadOnlySpan(), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, (Dict<JmdictRecord>)dict, useDB, JmdictDBManager.GetRecordsFromDB, parameter, verbParameter, jmdictTextWithoutLongVowelMarkParameters);
                    if (jmdictResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.JMnedict:
                    Dictionary<string, IntermediaryResult<JmnedictRecord>>? jmnedictResults = GetNameResults(textList.AsReadOnlySpan(), textInHiraganaList.AsReadOnlySpan(), (Dict<JmnedictRecord>)dict, useDB, JmnedictDBManager.GetRecordsFromDB, parameter);
                    if (jmnedictResults is not null)
                    {
                        lookupResults.AddRange(BuildJmnedictResult(jmnedictResults, pitchAccentDict));
                    }

                    break;

                case DictType.Kanjidic:
                    IntermediaryResult<KanjidicRecord>? kanjidicResult = useDB
                        ? GetKanjiResultsFromDB(kanji, (Dict<KanjidicRecord>)dict, KanjidicDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, (Dict<KanjidicRecord>)dict);

                    if (kanjidicResult is not null)
                    {
                        lookupResults.Add(BuildKanjidicResult(kanji, kanjiComposition, kanjidicResult, kanjiFrequencyResults, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                    // Template-wise, it is a word dictionary that's why its results are put into Yomichan Word Results
                    // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                    IntermediaryResult<EpwingYomichanRecord>? epwingYomichanKanjiWithWordSchemaResults = useDB
                        ? GetKanjiResultsFromDB(kanji, (Dict<EpwingYomichanRecord>)dict, EpwingYomichanDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, (Dict<EpwingYomichanRecord>)dict);

                    if (epwingYomichanKanjiWithWordSchemaResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResultForKanjiWithWordSchema(kanjiComposition, epwingYomichanKanjiWithWordSchemaResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult<CustomWordRecord>> customWordResults = GetWordResults(textList.AsReadOnlySpan(), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, (Dict<CustomWordRecord>)dict, false, null, parameter, verbParameter, null);
                    if (customWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, wordFreqs, dbWordFreqs, dbIsUsedForPitchDict, pitchDict));
                    }
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    Dictionary<string, IntermediaryResult<CustomNameRecord>>? customNameResults = GetNameResults(textList.AsReadOnlySpan(), textInHiraganaList.AsReadOnlySpan(), (Dict<CustomNameRecord>)dict, false, null, parameter);
                    if (customNameResults is not null)
                    {
                        lookupResults.AddRange(BuildCustomNameResult(customNameResults, pitchAccentDict));
                    }
                    break;

                case DictType.NonspecificKanjiYomichan:
                    IntermediaryResult<YomichanKanjiRecord>? epwingYomichanKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, (Dict<YomichanKanjiRecord>)dict, YomichanKanjiDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, (Dict<YomichanKanjiRecord>)dict);

                    if (epwingYomichanKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildYomichanKanjiResult(kanji, kanjiComposition, epwingYomichanKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult<EpwingYomichanRecord>>? epwingYomichanNameResults = GetNameResults(textList.AsReadOnlySpan(), textInHiraganaList.AsReadOnlySpan(), (Dict<EpwingYomichanRecord>)dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery);
                    if (epwingYomichanNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult<EpwingYomichanRecord>> epwingYomichanWordResults = GetWordResults(textList.AsReadOnlySpan(), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, (Dict<EpwingYomichanRecord>)dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, yomichanWordQuery, yomichanVerbQuery, yomichanTextWithoutLongVowelMarkQueries);
                    if (epwingYomichanWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, wordFreqs, frequencyDicts, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificKanjiNazeka:
                    IntermediaryResult<EpwingNazekaRecord>? epwingNazekaKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, (Dict<EpwingNazekaRecord>)dict, EpwingNazekaDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, (Dict<EpwingNazekaRecord>)dict);

                    if (epwingNazekaKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResultForKanji(kanjiComposition, epwingNazekaKanjiResults, kanjiFrequencyResults, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult<EpwingNazekaRecord>>? epwingNazekaNameResults = GetNameResults(textList.AsReadOnlySpan(), textInHiraganaList.AsReadOnlySpan(), (Dict<EpwingNazekaRecord>)dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery);
                    if (epwingNazekaNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, null, null, pitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult<EpwingNazekaRecord>> epwingNazekaWordResults = GetWordResults(textList.AsReadOnlySpan(), textInHiraganaList, deconjugationResultsList, deconjugatedTexts, textWithoutLongVowelMarksList, (Dict<EpwingNazekaRecord>)dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQueries);
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

        if (lookupResults.IsEmpty)
        {
            return null;
        }

        LookupResult[] sortedLookupResults = lookupResults.ToArray();
        Array.Sort(sortedLookupResults);
        return sortedLookupResults;
    }

    private static void GetWordResultsHelper<T>(Dict<T> dict,
        Dictionary<string, IntermediaryResult<T>> results,
        List<Form>? deconjugationResults,
        string matchedText,
        string textInHiragana,
        IDictionary<string, IList<T>>? dbWordDict,
        IDictionary<string, IList<T>>? dbVerbDict) where T : IDictRecord
    {
        IDictionary<string, IList<T>> wordDict = dbWordDict ?? dict.Contents;
        IDictionary<string, IList<T>> verbDict = dbVerbDict ?? dict.Contents;

        if (wordDict.TryGetValue(textInHiragana, out IList<T>? tempResult))
        {
            _ = results.TryAdd(textInHiragana, new IntermediaryResult<T>(matchedText, dict, [tempResult]));
        }

        if (deconjugationResults is not null)
        {
            foreach (ref readonly Form deconjugationResult in deconjugationResults.AsReadOnlySpan())
            {
                if (verbDict.TryGetValue(deconjugationResult.Text, out IList<T>? dictResults))
                {
                    List<T> resultsList = GetValidDeconjugatedResults(dict, deconjugationResult, dictResults);
                    if (resultsList.Count > 0)
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult<T>? result))
                        {
                            if (result.MatchedText == deconjugationResult.OriginalText)
                            {
                                int index = result.Results.FindIndex(rs => rs.SequenceEqual(resultsList));
                                if (index >= 0)
                                {
                                    Debug.Assert(result.Processes is not null);
                                    List<List<string>> processes = result.Processes[index];
                                    ReadOnlySpan<string> processSpan = deconjugationResult.Process.AsReadOnlySpan();

                                    bool addProcess = true;
                                    foreach (ref readonly List<string> process in processes.AsReadOnlySpan())
                                    {
                                        if (process.AsReadOnlySpan().SequenceEqual(processSpan))
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

                                    Debug.Assert(result.Processes is not null);
                                    result.Processes.Add([deconjugationResult.Process]);
                                }
                            }
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult<T>(matchedText,
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

    private static Dictionary<string, IntermediaryResult<T>> GetWordResults<T>(ReadOnlySpan<string> textList, List<string> textInHiraganaList,
        List<List<Form>> deconjugationResultsList, string[]? deconjugatedTexts, List<List<string>?>? textWithoutLongVowelMarkList, Dict<T> dict, bool useDB, GetRecordsFromDB<T>? getRecordsFromDB,
        string? queryOrParameter, string? verbQueryOrParameter, List<string?>? longVowelQueryOrParameters) where T : IDictRecord
    {
        Dictionary<string, IList<T>>? dbWordDict = null;
        Dictionary<string, IList<T>>? dbVerbDict = null;

        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            Debug.Assert(queryOrParameter is not null);
            Debug.Assert(deconjugatedTexts is not null);
            Debug.Assert(verbQueryOrParameter is not null);

            Parallel.Invoke(
                () =>
                {
                    dbWordDict = getRecordsFromDB(dict.Name, textInHiraganaList.AsReadOnlySpan(), queryOrParameter);
                },
                () =>
                {
                    dbVerbDict = getRecordsFromDB(dict.Name, deconjugatedTexts.AsReadOnlySpan(), verbQueryOrParameter);
                });
        }

        bool textWithoutLongVowelMarkListExist = textWithoutLongVowelMarkList is not null;
        Dictionary<string, IntermediaryResult<T>> results = new(StringComparer.Ordinal);
        for (int i = 0; i < textList.Length; i++)
        {
            ref readonly string text = ref textList[i];
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], text, textInHiraganaList[i], dbWordDict, dbVerbDict);

            List<string>? textsWithoutLongVowelMark = null;
            if (textWithoutLongVowelMarkListExist)
            {
                Debug.Assert(textWithoutLongVowelMarkList is not null);
                textsWithoutLongVowelMark = textWithoutLongVowelMarkList[i];
            }

            if (textsWithoutLongVowelMark is not null)
            {
                ReadOnlySpan<string> textsWithoutLongVowelMarkSpan = textsWithoutLongVowelMark.AsReadOnlySpan();
                Dictionary<string, IList<T>>? dbWordDictForLongVowelConversion = null;
                if (useDB)
                {
                    Debug.Assert(getRecordsFromDB is not null);
                    Debug.Assert(longVowelQueryOrParameters is not null);

                    string? longVowelQueryOrParameter = longVowelQueryOrParameters[i];
                    Debug.Assert(longVowelQueryOrParameter is not null);

                    dbWordDictForLongVowelConversion = getRecordsFromDB(dict.Name, textsWithoutLongVowelMarkSpan, longVowelQueryOrParameter);
                }

                foreach (ref readonly string textWithoutLongVowelMark in textsWithoutLongVowelMarkSpan)
                {
                    GetWordResultsHelper(dict, results, null, text, textWithoutLongVowelMark, dbWordDictForLongVowelConversion, null);
                }
            }
        }

        return results;
    }

    private static List<T> GetValidDeconjugatedResults<T>(Dict<T> dict, Form deconjugationResult, IList<T> dictResults) where T : IDictRecord
    {
        string lastTag = deconjugationResult.Tags[^1];
        List<T> resultsList = new(dictResults.Count);
        switch (resultsList)
        {
            case List<JmdictRecord> jmdictResults:
            {
                int dictResultsCount = jmdictResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    JmdictRecord dictResult = jmdictResults[i];
                    if (dictResult.WordClassesSharedByAllSenses is not null
                        && dictResult.WordClassesSharedByAllSenses.AsReadOnlySpan().Contains(lastTag))
                    {
                        resultsList.Add(resultsList[i]);
                    }

                    else if (dictResult.WordClasses is not null)
                    {
                        foreach (string[]? wordClasses in dictResult.WordClasses)
                        {
                            if (wordClasses is not null && wordClasses.AsReadOnlySpan().Contains(lastTag))
                            {
                                resultsList.Add(resultsList[i]);
                                break;
                            }
                        }
                    }
                }
                break;
            }

            case List<CustomWordRecord> customWordResults:
            {
                int dictResultsCount = customWordResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    CustomWordRecord dictResult = customWordResults[i];
                    if (dictResult.WordClasses.AsReadOnlySpan().Contains(lastTag))
                    {
                        resultsList.Add(dictResults[i]);
                    }
                }
                break;
            }

            case List<EpwingYomichanRecord> epwingYomichanResults:
            {
                int dictResultsCount = epwingYomichanResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    EpwingYomichanRecord dictResult = epwingYomichanResults[i];
                    if (dictResult.WordClasses is not null)
                    {
                        // It seems like instead of storing precise tags like v5r
                        // Yomichan dictionaries simply store the general v5 tag
                        foreach (string wordClass in dictResult.WordClasses)
                        {
                            if (lastTag.AsSpan().StartsWith(wordClass, StringComparison.Ordinal))
                            {
                                resultsList.Add(dictResults[i]);
                                break;
                            }
                        }
                    }
                    else if (WordClassDictionaryContainsTag(dictResult.PrimarySpelling, dictResult.Reading, lastTag))
                    {
                        resultsList.Add(dictResults[i]);
                    }
                }
                break;
            }

            case List<EpwingNazekaRecord> epwingNazekaResults:
            {
                int dictResultsCount = epwingNazekaResults.Count;
                for (int i = 0; i < dictResultsCount; i++)
                {
                    EpwingNazekaRecord dictResult = epwingNazekaResults[i];
                    if (WordClassDictionaryContainsTag(dictResult.PrimarySpelling, dictResult.Reading, lastTag))
                    {
                        resultsList.Add(dictResults[i]);
                    }
                }

                break;
            }

            default:
            {
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(GetValidDeconjugatedResults), dict.Type);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                break;
            }
        }

        return resultsList;
    }

    private static Dictionary<string, IntermediaryResult<T>>? GetNameResults<T>(ReadOnlySpan<string> textList, ReadOnlySpan<string> textInHiraganaList, Dict<T> dict, bool useDB, GetRecordsFromDB<T>? getRecordsFromDB, string? queryOrParameter) where T : IDictRecord
    {
        IDictionary<string, IList<T>>? nameDict;
        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            Debug.Assert(queryOrParameter is not null);
            nameDict = getRecordsFromDB(dict.Name, textInHiraganaList, queryOrParameter);
        }
        else
        {
            nameDict = dict.Contents;
        }

        if (nameDict is null)
        {
            return null;
        }

        Dictionary<string, IntermediaryResult<T>> nameResults = new(textList.Length, StringComparer.Ordinal);
        for (int i = 0; i < textList.Length; i++)
        {
            string textInHiragana = textInHiraganaList[i];
            if (nameDict.TryGetValue(textInHiragana, out IList<T>? result))
            {
                nameResults.Add(textInHiragana,
                    new IntermediaryResult<T>(textList[i], dict, [result]));
            }
        }

        return nameResults.Count > 0
            ? nameResults
            : null;
    }

    private static IntermediaryResult<T>? GetKanjiResults<T>(string kanji, Dict<T> dict) where T : IDictRecord
    {
        return dict.Contents.TryGetValue(kanji, out IList<T>? result)
            ? new IntermediaryResult<T>(kanji, dict, [result])
            : null;
    }

    private static IntermediaryResult<T>? GetKanjiResultsFromDB<T>(string kanji, Dict<T> dict, GetKanjiRecordsFromDB<T> getKanjiRecordsFromDB) where T : IDictRecord
    {
        List<T>? results = getKanjiRecordsFromDB(dict.Name, kanji);

        return results is not null && results.Count > 0
            ? new IntermediaryResult<T>(kanji, dict, [results])
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
        Dictionary<string, IntermediaryResult<JmdictRecord>> jmdictResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, bool dbIsUsedForPitchDict, Dict<PitchAccentRecord>? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(jmdictResults)
            : null;

        bool pitchAccentDictExists = false;
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict = null;
        Parallel.Invoke(
        () =>
        {
            if (dbWordFreqsExist)
            {
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, searchKeys);
            }
        },
        () =>
        {
            if (dbIsUsedForPitchDict)
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.Name, searchKeys);
            }
            else
            {
                pitchAccentDict = pitchDict?.Contents;
            }

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult<JmdictRecord> wordResult in jmdictResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<JmdictRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
                    : null;

                ref readonly IList<JmdictRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    JmdictRecord jmdictResult = dictRecords[j];
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

    private static HashSet<string> GetSearchKeysFromRecords<T>(Dictionary<string, IntermediaryResult<T>> dictResults) where T : IDictRecordWithMultipleReadings
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult<T> intermediaryResult in dictResults.Values)
        {
            foreach (ref readonly IList<T> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    T record = dictRecords[j];
                    _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling));
                    if (record.Readings is not null)
                    {
                        foreach (string reading in record.Readings)
                        {
                            _ = searchKeys.Add(JapaneseUtils.KatakanaToHiragana(reading));
                        }
                    }
                }
            }
        }

        return searchKeys;
    }

    private static List<LookupResult> BuildJmnedictResult(
        Dictionary<string, IntermediaryResult<JmnedictRecord>> jmnedictResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        List<LookupResult> results = [];
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IntermediaryResult<JmnedictRecord> nameResult in jmnedictResults.Values)
        {
            foreach (ref readonly IList<JmnedictRecord> dictRecords in nameResult.Results.AsReadOnlySpan())
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    JmnedictRecord jmnedictRecord = dictRecords[j];

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

    private static LookupResult BuildKanjidicResult(string kanji, string[]? kanjiComposition, IntermediaryResult<KanjidicRecord> intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        KanjidicRecord kanjiRecord = intermediaryResult.Results[0][0];

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
        string kanji, string[]? kanjiComposition, IntermediaryResult<YomichanKanjiRecord> intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<YomichanKanjiRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
            {
                YomichanKanjiRecord yomichanKanjiDictResult = dictRecords[j];

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
        IDictionary<string, IntermediaryResult<EpwingYomichanRecord>> epwingResults, Freq[]? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool freqsExist = freqs is not null;
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult<EpwingYomichanRecord> wordResult in epwingResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<EpwingYomichanRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
                    : null;

                ref readonly IList<EpwingYomichanRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    EpwingYomichanRecord epwingResult = dictRecords[j];

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

    private static List<LookupResult> BuildEpwingYomichanResultForKanjiWithWordSchema(string[]? kanjiComposition, IntermediaryResult<EpwingYomichanRecord> intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<EpwingYomichanRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
            {
                EpwingYomichanRecord epwingResult = dictRecords[j];
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
        IDictionary<string, IntermediaryResult<EpwingNazekaRecord>> epwingNazekaResults, Freq[]? freqs, IDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        bool freqsExist = freqs is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult<EpwingNazekaRecord> wordResult in epwingNazekaResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<EpwingNazekaRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
                    : null;

                ref readonly IList<EpwingNazekaRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    EpwingNazekaRecord epwingResult = dictRecords[j];

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
                    && ((hasReading && result.Readings is not null && result.Readings.AsReadOnlySpan().Contains(reading!))
                        || (!hasReading && result.Readings is null))
                    && result.WordClasses.AsReadOnlySpan().Contains(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<LookupResult> BuildEpwingNazekaResultForKanji(string[]? kanjiComposition, IntermediaryResult<EpwingNazekaRecord> intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<EpwingNazekaRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
            {
                EpwingNazekaRecord epwingResult = dictRecords[j];
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
        Dictionary<string, IntermediaryResult<CustomWordRecord>> customWordResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, bool dbIsUsedForPitchDict, Dict<PitchAccentRecord>? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(customWordResults)
            : null;

        bool pitchAccentDictExists = false;
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict = null;
        Parallel.Invoke(
        () =>
        {
            if (dbWordFreqsExist)
            {
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, searchKeys);
            }
        },
        () =>
        {
            if (dbIsUsedForPitchDict)
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.Name, searchKeys);
            }
            else
            {
                pitchAccentDict = pitchDict?.Contents;
            }

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult<CustomWordRecord> wordResult in customWordResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<CustomWordRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
                    : null;

                ref readonly IList<CustomWordRecord> dictRecords = ref resultsSpan[i];
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    CustomWordRecord customWordDictResult = dictRecords[j];

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
        Dictionary<string, IntermediaryResult<CustomNameRecord>> customNameResults, IDictionary<string, IList<PitchAccentRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];
        foreach (IntermediaryResult<CustomNameRecord> customNameResult in customNameResults.Values)
        {
            int freq = 0;
            foreach (ref readonly IList<CustomNameRecord> dictRecords in customNameResult.Results.AsReadOnlySpan())
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    CustomNameRecord customNameDictResult = dictRecords[j];
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
                if (freqDictsFromDBExist)
                {
                    Debug.Assert(freqDictsFromDB is not null);
                    if (freqDictsFromDB.TryGetValue(freq.Name, out Dictionary<string, List<FrequencyRecord>>? freqDict))
                    {
                        freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freqDict), freq.Options.HigherValueMeansHigherFrequency.Value));
                    }
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

    private static byte[]? GetPitchPosition(string primarySpelling, string[]? readings, IDictionary<string, IList<PitchAccentRecord>> pitchDictionary)
    {
        if (readings is null)
        {
            if (pitchDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<PitchAccentRecord>? records))
            {
                int recordsCount = records.Count;
                for (int i = 0; i < recordsCount; i++)
                {
                    PitchAccentRecord pitchAccentRecord = records[i];
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
            if (pitchDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<PitchAccentRecord>? records))
            {
                for (int i = 0; i < readings.Length; i++)
                {
                    byte position = byte.MaxValue;
                    string reading = readings[i];
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading);
                    int recordsCount = records.Count;
                    for (int j = 0; j < recordsCount; j++)
                    {
                        PitchAccentRecord pitchAccentRecord = records[j];
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
                            PitchAccentRecord pitchAccentRecord = records[j];
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
