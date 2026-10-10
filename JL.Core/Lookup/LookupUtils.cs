using System.Buffers;
using System.Diagnostics;
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
using JL.Core.Dicts.KanjiComposition;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Japanese;
using JL.Core.Utilities;
using JL.Core.Utilities.Array;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Lookup;

public static class LookupUtils
{
    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, int maxSearchKeyLengthForDict);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string readOnlyConnectionString, string term);
    private delegate Dictionary<string, IList<IDictRecord>>? GetKanjiRecordsWithVariationSelectorFromDB(string readOnlyConnectionString, string kanjiWithVariationSelector, string kanji);

    public static LookupResult[]? LookupText(string text)
    {
        LookupCategory lookupCategory = CoreConfigManager.Instance.LookupCategory;
        Dict[] dicts = DictUtils.GetDictForLookupCategoryType(lookupCategory);
        if (dicts.Length is 0)
        {
            return null;
        }

        bool includeWordCategory = lookupCategory is LookupCategory.All or LookupCategory.Word;
        bool includeOtherCategory = lookupCategory is LookupCategory.All or LookupCategory.Other;
        bool lookUpWordDicts = (includeWordCategory || includeOtherCategory) && DictUtils.LookupHasWordDicts;
        bool lookUpJmdict = includeWordCategory && DictUtils.LookupHasJmdict;
        bool lookUpCustomWordDict = includeWordCategory && DictUtils.LookupHasCustomWordDict;
        bool lookUpProfileCustomWordDict = includeWordCategory && DictUtils.LookupHasProfileCustomWordDict;
        bool queryWordDictsFromDB = (includeWordCategory && DictUtils.LookupWordDictsUseDB)
            || (includeOtherCategory && DictUtils.LookupOtherDictsUseDB);
        bool lookUpEpwingWordDicts = (includeWordCategory && DictUtils.LookupHasEpwingWordDicts)
            || (includeOtherCategory && DictUtils.LookupHasOtherDicts);

        string? kanji = null;
        string? kanjiWithVariationSelector = null;
        string[]? kanjiCompositions = null;
        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        bool kanjiExists = false;
        if ((lookupCategory is LookupCategory.Kanji or LookupCategory.All) && DictUtils.LookupHasKanjiDicts)
        {
            kanji = JapaneseUtils.GetFirstCharacterIfKanji(text, out kanjiWithVariationSelector);
            if (kanji is not null)
            {
                kanjiExists = true;
                kanjiCompositions = KanjiCompositionDBManager.GetRecordsFromDB(kanji);

                Freq[]? kanjiFreqs = FreqUtils.KanjiFreqs;
                kanjiFrequencyResults = kanjiFreqs is not null
                    ? GetKanjiFrequencies(kanji, kanjiWithVariationSelector, kanjiFreqs)
                    : null;
            }
        }

        Freq[]? wordFreqs = lookUpWordDicts ? FreqUtils.WordFreqs : null;
        Freq[]? dbWordFreqs = lookUpWordDicts ? FreqUtils.DBWordFreqs : null;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForJmdict = dbWordFreqs is not null && lookUpJmdict
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForCustomWordDict = dbWordFreqs is not null && lookUpCustomWordDict
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForProfileCustomWordDict = dbWordFreqs is not null && lookUpProfileCustomWordDict
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        PopulateFreqSqliteConnections(sqliteFreqConnectionsForJmdict.Items, sqliteFreqConnectionsForCustomWordDict.Items, sqliteFreqConnectionsForProfileCustomWordDict.Items, dbWordFreqs);
        RentedArrayBuffer<SqliteConnection?>? freqConnectionsForJmdict = sqliteFreqConnectionsForJmdict.Items;
        RentedArrayBuffer<SqliteConnection?>? freqConnectionsForCustomWordDict = sqliteFreqConnectionsForCustomWordDict.Items;
        RentedArrayBuffer<SqliteConnection?>? freqConnectionsForProfileCustomWordDict = sqliteFreqConnectionsForProfileCustomWordDict.Items;

        Dict? pitchDict = DictUtils.PitchDict;
        bool dbIsUsedForPitchDict = DictUtils.DBIsUsedForPitchDict
            // ReSharper disable once NullableWarningSuppressionIsUsed
            && pitchDict!.Ready;

        bool querySharedPitch = dbIsUsedForPitchDict
            && ((lookupCategory is LookupCategory.All && DictUtils.LookupNeedsSharedPitch)
                || lookUpEpwingWordDicts
                || lookupCategory is LookupCategory.Kanji or LookupCategory.Name);
        Freq[]? sharedDBWordFreqs = lookUpEpwingWordDicts ? dbWordFreqs : null;
        bool collectDeconjugatedTexts = queryWordDictsFromDB
            || (lookUpEpwingWordDicts && querySharedPitch) || sharedDBWordFreqs is not null;
        TextInfo textInfo = GetTextInfo(text, lookUpWordDicts, collectDeconjugatedTexts, querySharedPitch, sharedDBWordFreqs, pitchDict);
        List<string>? allTextWithoutLongVowelMark = null;
        if (queryWordDictsFromDB && textInfo.TextWithoutLongVowelMarksList is not null)
        {
            allTextWithoutLongVowelMark = new List<string>(textInfo.TextWithoutLongVowelMarksCount);
            foreach (ref readonly List<string>? textWithoutLongVowelMark in textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan())
            {
                if (textWithoutLongVowelMark is not null)
                {
                    allTextWithoutLongVowelMark.AddRange(textWithoutLongVowelMark.AsReadOnlySpan());
                }
            }
        }

        string? readOnlyConnectionStringForPitchDict;
        if (dbIsUsedForPitchDict)
        {
            Debug.Assert(pitchDict is not null);
            readOnlyConnectionStringForPitchDict = pitchDict.ReadOnlyConnectionString;
        }
        else
        {
            readOnlyConnectionStringForPitchDict = null;
        }

        using SqliteConnection? sqliteConnectionForJmdictPitch = readOnlyConnectionStringForPitchDict is not null && lookUpJmdict
            ? DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForPitchDict)
            : null;

        using SqliteConnection? sqliteConnectionForCustomWordPitch = readOnlyConnectionStringForPitchDict is not null && lookUpCustomWordDict
            ? DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForPitchDict)
            : null;

        using SqliteConnection? sqliteConnectionForProfileCustomWordPitch = readOnlyConnectionStringForPitchDict is not null && lookUpProfileCustomWordDict
            ? DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForPitchDict)
            : null;

        List<LookupResult>?[] resultSlots = ArrayPool<List<LookupResult>?>.Shared.Rent(dicts.Length);
        _ = Parallel.For(0, dicts.Length, i =>
        {
            Dict dict = dicts[i];
            bool useDB = dict is { Options.UseDB.Value: true, Ready: true, Active: true };
            switch (dict.Type)
            {
                case DictType.JMdict:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo, allTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, JmdictDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        resultSlots[i] = rentedLookupResults;
                        // ReSharper disable once AccessToDisposedClosure
                        BuildJmdictResult(results, rentedLookupResults, wordFreqs, dbWordFreqs, freqConnectionsForJmdict, dbIsUsedForPitchDict, sqliteConnectionForJmdictPitch, pitchDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.JMnedict:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo, dict, useDB, results, JmnedictDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildJmnedictResult(results, rentedLookupResults, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.Kanjidic:
                {
                    if (kanjiExists)
                    {
                        Debug.Assert(kanji is not null);
                        GetKanjiResults(kanji, dict, useDB, KanjidicDBManager.GetRecordsFromDB, out IntermediaryResult? kanjidicResult);

                        if (kanjidicResult is not null)
                        {
                            resultSlots[i] = [BuildKanjidicResult(kanji, kanjiCompositions, kanjidicResult, kanjiFrequencyResults, textInfo.PitchAccentDict)];
                        }
                    }

                    break;
                }

                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                {
                    if (kanjiExists)
                    {
                        Debug.Assert(kanji is not null);

                        // Template-wise, it is a word dictionary that's why its results are put into Yomichan Word Results
                        // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                        IntermediaryResult? epwingYomichanKanjiWithWordSchemaResults;
                        IntermediaryResult? epwingYomichanKanjiWithWordSchemaVariationResults;
                        if (kanjiWithVariationSelector is not null)
                        {
                            GetKanjiResults(kanji, kanjiWithVariationSelector, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, out epwingYomichanKanjiWithWordSchemaResults, out epwingYomichanKanjiWithWordSchemaVariationResults);
                        }
                        else
                        {
                            GetKanjiResults(kanji, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, out epwingYomichanKanjiWithWordSchemaResults);
                            epwingYomichanKanjiWithWordSchemaVariationResults = null;
                        }

                        bool hasKanjiResults = epwingYomichanKanjiWithWordSchemaResults is not null;
                        bool hasKanjiVariationResults = epwingYomichanKanjiWithWordSchemaVariationResults is not null;
                        if (hasKanjiResults || hasKanjiVariationResults)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            if (hasKanjiVariationResults)
                            {
                                Debug.Assert(epwingYomichanKanjiWithWordSchemaVariationResults is not null);
                                BuildEpwingYomichanResultForKanjiWithWordSchema(epwingYomichanKanjiWithWordSchemaVariationResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }

                            if (hasKanjiResults)
                            {
                                Debug.Assert(epwingYomichanKanjiWithWordSchemaResults is not null);
                                BuildEpwingYomichanResultForKanjiWithWordSchema(epwingYomichanKanjiWithWordSchemaResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }
                        }
                    }
                    break;
                }

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo, allTextWithoutLongVowelMark.AsReadOnlySpan(), dict, false, results, null);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        bool profileCustomWordDict = dict.Type is DictType.ProfileCustomWordDictionary;
                        // ReSharper disable once AccessToDisposedClosure
                        BuildCustomWordResult(results, rentedLookupResults, wordFreqs, dbWordFreqs,
                            profileCustomWordDict ? freqConnectionsForProfileCustomWordDict : freqConnectionsForCustomWordDict,
                            dbIsUsedForPitchDict, profileCustomWordDict ? sqliteConnectionForProfileCustomWordPitch : sqliteConnectionForCustomWordPitch, pitchDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo, dict, false, results, null);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildCustomNameResult(results, rentedLookupResults, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.NonspecificKanjiYomichan:
                {
                    if (kanjiExists)
                    {
                        Debug.Assert(kanji is not null);

                        IntermediaryResult? epwingYomichanKanjiResults;
                        IntermediaryResult? epwingYomichanKanjiVariationResults;
                        if (kanjiWithVariationSelector is not null)
                        {
                            GetKanjiResults(kanji, kanjiWithVariationSelector, dict, useDB, YomichanKanjiDBManager.GetRecordsFromDB, out epwingYomichanKanjiResults, out epwingYomichanKanjiVariationResults);
                        }
                        else
                        {
                            GetKanjiResults(kanji, dict, useDB, YomichanKanjiDBManager.GetRecordsFromDB, out epwingYomichanKanjiResults);
                            epwingYomichanKanjiVariationResults = null;
                        }

                        bool hasKanjiResults = epwingYomichanKanjiResults is not null;
                        bool hasKanjiVariationResults = epwingYomichanKanjiVariationResults is not null;
                        if (hasKanjiResults || hasKanjiVariationResults)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            if (hasKanjiVariationResults)
                            {
                                Debug.Assert(epwingYomichanKanjiVariationResults is not null);
                                BuildYomichanKanjiResult(kanji, rentedLookupResults, kanjiCompositions, epwingYomichanKanjiVariationResults, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }

                            if (hasKanjiResults)
                            {
                                Debug.Assert(epwingYomichanKanjiResults is not null);
                                BuildYomichanKanjiResult(kanji, rentedLookupResults, kanjiCompositions, epwingYomichanKanjiResults, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }
                        }
                    }
                    break;
                }

                case DictType.NonspecificNameYomichan:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo, dict, useDB, results, EpwingYomichanDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildEpwingYomichanResult(results, rentedLookupResults, null, null, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo, allTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, EpwingYomichanDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildEpwingYomichanResult(results, rentedLookupResults, wordFreqs, textInfo.FrequencyDicts, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.NonspecificKanjiNazeka:
                {
                    if (kanjiExists)
                    {
                        Debug.Assert(kanji is not null);

                        IntermediaryResult? epwingNazekaKanjiResults;
                        IntermediaryResult? epwingNazekaKanjiVariationResults;
                        if (kanjiWithVariationSelector is not null)
                        {
                            GetKanjiResults(kanji, kanjiWithVariationSelector, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, out epwingNazekaKanjiResults, out epwingNazekaKanjiVariationResults);
                        }
                        else
                        {
                            GetKanjiResults(kanji, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, out epwingNazekaKanjiResults);
                            epwingNazekaKanjiVariationResults = null;
                        }

                        bool hasKanjiResults = epwingNazekaKanjiResults is not null;
                        bool hasKanjiVariationResults = epwingNazekaKanjiVariationResults is not null;
                        if (hasKanjiResults || hasKanjiVariationResults)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            if (hasKanjiVariationResults)
                            {
                                Debug.Assert(epwingNazekaKanjiVariationResults is not null);
                                BuildEpwingNazekaResultForKanji(epwingNazekaKanjiVariationResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }

                            if (hasKanjiResults)
                            {
                                Debug.Assert(epwingNazekaKanjiResults is not null);
                                BuildEpwingNazekaResultForKanji(epwingNazekaKanjiResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                            }
                        }
                    }

                    break;
                }

                case DictType.NonspecificNameNazeka:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo, dict, useDB, results, EpwingNazekaDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildEpwingNazekaResult(results, rentedLookupResults, null, null, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo, allTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, EpwingNazekaDBManager.GetRecordsFromDB);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        BuildEpwingNazekaResult(results, rentedLookupResults, wordFreqs, textInfo.FrequencyDicts, textInfo.PitchAccentDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.PitchAccentYomichan:
                {
                    break;
                }

                default:
                {
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(LookupText), dict.Type);
                    break;
                }
            }
        });

        int lookupResultCount = 0;

        ReadOnlySpan<List<LookupResult>?> resultSlotsSpan = resultSlots.AsSpan(0, dicts.Length);
        foreach (List<LookupResult>? resultSlot in resultSlotsSpan)
        {
            if (resultSlot is not null)
            {
                lookupResultCount += resultSlot.Count;
            }
        }

        if (lookupResultCount is 0)
        {
            ArrayPool<List<LookupResult>?>.Shared.Return(resultSlots);
            return null;
        }

        LookupResult[] lookupResults = new LookupResult[lookupResultCount];
        Span<LookupResult> lookupResultsSpan = lookupResults;
        foreach (List<LookupResult>? resultSlot in resultSlotsSpan)
        {
            if (resultSlot is not null)
            {
                ReadOnlySpan<LookupResult> resultSlotSpan = resultSlot.AsReadOnlySpan();
                resultSlotSpan.CopyTo(lookupResultsSpan);
                lookupResultsSpan = lookupResultsSpan[resultSlotSpan.Length..];
                ObjectPoolManager.s_lookupResultListPool.Return(resultSlot);
            }
        }

        resultSlots.AsSpan(0, dicts.Length).Clear();
        ArrayPool<List<LookupResult>?>.Shared.Return(resultSlots);

        Array.Sort(lookupResults);
        return lookupResults;
    }

    private static void PopulateFreqSqliteConnections(RentedArrayBuffer<SqliteConnection?>? sqliteFreqConnectionsForJmdict, RentedArrayBuffer<SqliteConnection?>? sqliteFreqConnectionsForCustomWordDict, RentedArrayBuffer<SqliteConnection?>? sqliteFreqConnectionsForProfileCustomWordDict, Freq[]? dbWordFreqs)
    {
        bool sqliteFreqConnectionsForJmdictExist = sqliteFreqConnectionsForJmdict is not null;
        bool sqliteFreqConnectionsForCustomWordDictExist = sqliteFreqConnectionsForCustomWordDict is not null;
        bool sqliteFreqConnectionsForProfileCustomWordDictExist = sqliteFreqConnectionsForProfileCustomWordDict is not null;

        if (sqliteFreqConnectionsForJmdictExist || sqliteFreqConnectionsForCustomWordDictExist || sqliteFreqConnectionsForProfileCustomWordDictExist)
        {
            Debug.Assert(dbWordFreqs is not null);
            foreach (Freq dbWordFreq in dbWordFreqs)
            {
                string readOnlyConnectionStringForFreq = dbWordFreq.ReadOnlyConnectionString;
                if (sqliteFreqConnectionsForJmdictExist)
                {
                    Debug.Assert(sqliteFreqConnectionsForJmdict is not null);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    sqliteFreqConnectionsForJmdict.Add(DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForFreq));
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                if (sqliteFreqConnectionsForCustomWordDictExist)
                {
                    Debug.Assert(sqliteFreqConnectionsForCustomWordDict is not null);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    sqliteFreqConnectionsForCustomWordDict.Add(DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForFreq));
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                if (sqliteFreqConnectionsForProfileCustomWordDictExist)
                {
                    Debug.Assert(sqliteFreqConnectionsForProfileCustomWordDict is not null);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    sqliteFreqConnectionsForProfileCustomWordDict.Add(DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForFreq));
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
            }
        }
    }

    private static TextInfo GetTextInfo(string text, bool prepareDeconjugation, bool collectDeconjugatedTexts, bool queryPitchFromDB, Freq[]? dbWordFreqs, Dict? pitchDict)
    {
        int textLength = text.Length;
        List<string> textList = new(textLength);
        List<string> textInHiraganaList = new(textLength);
        List<List<Form>?>? deconjugationResultsList = prepareDeconjugation ? new List<List<Form>?>(textLength) : null;
        List<List<string>?>? textWithoutLongVowelMarksList = null;
        List<List<List<Form>>?>? deconjugatedTextWithoutLongVowelMarksList = null;
        int estimatedDeconjugatedTextCapacity = 0;
        int textWithoutLongVowelMarksCount = 0;
        int estimatedDeconjugatedTextWithoutLongVowelMarksListCount = 0;
        int maxTextInHiraganaLength = 0;
        int maxDeconjugatedTextLength = 0;
        int maxTextWithoutLongVowelMarksLength = 0;

        bool doesNotStartWithLongVowelMark = prepareDeconjugation && !JapaneseUtils.s_longVowelMarkCharsNotNormalized.Contains(text[0]);
        bool countLongVowelMark = doesNotStartWithLongVowelMark;

        for (int i = 0; i < textLength; i++)
        {
            if (char.IsHighSurrogate(text[textLength - i - 1]))
            {
                continue;
            }

            string currentText = text[..^i];

            textList.Add(currentText);

            string textInHiragana = JapaneseUtils.NormalizeText(currentText);
            textInHiraganaList.Add(textInHiragana);
            if (textInHiragana.Length > maxTextInHiraganaLength)
            {
                maxTextInHiraganaLength = textInHiragana.Length;
            }

            if (!prepareDeconjugation)
            {
                continue;
            }

            Debug.Assert(deconjugationResultsList is not null);
            if (textInHiragana.Length <= 35 && (i != textLength - 1 || textInHiragana[0] is not JapaneseUtils.NormalizedFuseji))
            {
                List<Form> deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                estimatedDeconjugatedTextCapacity += deconjugationResults.Count;
                deconjugationResultsList.Add(deconjugationResults);
            }
            else
            {
                deconjugationResultsList.Add(null);
            }

            if (doesNotStartWithLongVowelMark && textInHiragana.Length <= 20)
            {
                int nonConsecutiveLongVowelMarkCount = countLongVowelMark
                    ? JapaneseUtils.CountNonConsecutiveLongVowelMarks(textInHiragana)
                    : 0;

                if (nonConsecutiveLongVowelMarkCount > 0)
                {
                    if (textWithoutLongVowelMarksList is null)
                    {
                        textWithoutLongVowelMarksList = new List<List<string>?>(textLength);
                        deconjugatedTextWithoutLongVowelMarksList = new List<List<List<Form>>?>(textLength);
                        for (int j = 0; j < textList.Count - 1; j++)
                        {
                            textWithoutLongVowelMarksList.Add(null);
                            deconjugatedTextWithoutLongVowelMarksList.Add(null);
                        }
                    }

                    Debug.Assert(deconjugatedTextWithoutLongVowelMarksList is not null);
                    if (nonConsecutiveLongVowelMarkCount < 4)
                    {
                        List<string> textsWithoutLongVowelMarks = JapaneseUtils.NormalizeLongVowelMark(textInHiragana);
                        textWithoutLongVowelMarksCount += textsWithoutLongVowelMarks.Count;
                        textWithoutLongVowelMarksList.Add(textsWithoutLongVowelMarks);

                        List<List<Form>> deconjugatedTextWithoutLongVowelMarks = new(textsWithoutLongVowelMarks.Count);
                        foreach (string textWithoutLongVowelMarks in textsWithoutLongVowelMarks.AsReadOnlySpan())
                        {
                            if (textWithoutLongVowelMarks.Length > maxTextWithoutLongVowelMarksLength)
                            {
                                maxTextWithoutLongVowelMarksLength = textWithoutLongVowelMarks.Length;
                            }

                            List<Form> deconjugationResultsForTextWithoutLongVowelMarks = Deconjugator.Deconjugate(textWithoutLongVowelMarks);
                            estimatedDeconjugatedTextWithoutLongVowelMarksListCount += deconjugationResultsForTextWithoutLongVowelMarks.Count;
                            deconjugatedTextWithoutLongVowelMarks.Add(deconjugationResultsForTextWithoutLongVowelMarks);
                        }

                        deconjugatedTextWithoutLongVowelMarksList.Add(deconjugatedTextWithoutLongVowelMarks);
                    }
                    else
                    {
                        textWithoutLongVowelMarksList.Add(null);
                        deconjugatedTextWithoutLongVowelMarksList.Add(null);
                    }
                }
                else
                {
                    textWithoutLongVowelMarksList?.Add(null);
                    deconjugatedTextWithoutLongVowelMarksList?.Add(null);
                    countLongVowelMark = false;
                }
            }
            else
            {
                textWithoutLongVowelMarksList?.Add(null);
                deconjugatedTextWithoutLongVowelMarksList?.Add(null);
            }
        }

        string[]? deconjugatedTexts = null;
        if (collectDeconjugatedTexts)
        {
            HashSet<string> deconjugatedTextsHashSet = new(Math.Min(estimatedDeconjugatedTextCapacity + estimatedDeconjugatedTextWithoutLongVowelMarksListCount, 256), StringComparer.Ordinal);
            foreach (ref readonly List<Form>? deconjugationResults in deconjugationResultsList.AsReadOnlySpan())
            {
                foreach (ref readonly Form form in deconjugationResults.AsReadOnlySpan())
                {
                    _ = deconjugatedTextsHashSet.Add(form.Text);
                    if (form.Text.Length > maxDeconjugatedTextLength)
                    {
                        maxDeconjugatedTextLength = form.Text.Length;
                    }
                }
            }

            if (deconjugatedTextWithoutLongVowelMarksList is not null)
            {
                foreach (ref readonly List<List<Form>>? deconjugatedTextWithoutLongVowelMarks in deconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan())
                {
                    if (deconjugatedTextWithoutLongVowelMarks is null)
                    {
                        continue;
                    }

                    foreach (ref readonly List<Form> forms in deconjugatedTextWithoutLongVowelMarks.AsReadOnlySpan())
                    {
                        foreach (ref readonly Form form in forms.AsReadOnlySpan())
                        {
                            _ = deconjugatedTextsHashSet.Add(form.Text);
                            if (form.Text.Length > maxDeconjugatedTextLength)
                            {
                                maxDeconjugatedTextLength = form.Text.Length;
                            }
                        }
                    }
                }
            }

            deconjugatedTexts = deconjugatedTextsHashSet.Count > 0 ? deconjugatedTextsHashSet.ToArray() : null;
        }

        HashSet<string>? allSearchKeys = null;
        bool queryWordFrequencies = dbWordFreqs is not null;
        if (queryPitchFromDB || queryWordFrequencies)
        {
            allSearchKeys = new HashSet<string>(textInHiraganaList.Count + (deconjugatedTexts?.Length ?? 0) + textWithoutLongVowelMarksCount, StringComparer.Ordinal);
            allSearchKeys.UnionWith(textInHiraganaList);
            if (deconjugatedTexts is not null)
            {
                allSearchKeys.UnionWith(deconjugatedTexts);
            }

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

        Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (allSearchKeys is not null)
        {
            if (queryWordFrequencies && queryPitchFromDB)
            {
                Parallel.Invoke(
                () =>
                {
                    Debug.Assert(dbWordFreqs is not null);
                    frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, allSearchKeys);
                },
                () =>
                {
                    Debug.Assert(pitchDict is not null);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.ReadOnlyConnectionString, allSearchKeys);
                });
            }
            else if (queryWordFrequencies)
            {
                Debug.Assert(dbWordFreqs is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, allSearchKeys);
                pitchAccentDict = pitchDict?.Contents;
            }
            else // queryPitchFromDB
            {
                Debug.Assert(pitchDict is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.ReadOnlyConnectionString, allSearchKeys);
            }
        }
        else
        {
            pitchAccentDict = pitchDict?.Contents;
        }

        return new TextInfo(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTextWithoutLongVowelMarksList, textWithoutLongVowelMarksList, textWithoutLongVowelMarksCount, deconjugatedTexts, frequencyDicts, pitchAccentDict, maxTextInHiraganaLength, maxDeconjugatedTextLength, maxTextWithoutLongVowelMarksLength);
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
            ref IntermediaryResult? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, textInHiragana, out bool exists);
            if (!exists)
            {
                result = new IntermediaryResult(matchedText, dict, tempResult);
            }
        }

        if (deconjugationResults is not null)
        {
            foreach (ref readonly Form deconjugationResult in deconjugationResults.AsReadOnlySpan())
            {
                Debug.Assert(deconjugationResult.Process is not null);
                if (verbDict.TryGetValue(deconjugationResult.Text, out IList<IDictRecord>? dictResults))
                {
                    List<IDictRecord> resultsList = GetValidDeconjugatedResults(dict, deconjugationResult, dictResults);
                    if (resultsList.Count > 0)
                    {
                        ref IntermediaryResult? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, deconjugationResult.Text, out bool exists);
                        if (exists)
                        {
                            Debug.Assert(result is not null);
                            if (result.Processes is not null && result.MatchedText == matchedText)
                            {
                                foreach (IDictRecord record in resultsList)
                                {
                                    int index = result.Results.FastReferenceIndexOf(record);
                                    if (index >= 0)
                                    {
                                        List<ProcessNode> processes = result.Processes[index];

                                        bool addProcess = true;
                                        foreach (ref readonly ProcessNode process in processes.AsReadOnlySpan())
                                        {
                                            if (process.Equals(deconjugationResult.Process))
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
                                        result.Results.Add(record);

                                        result.Processes.Add([deconjugationResult.Process]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            List<List<ProcessNode>> processNodes = new(resultsList.Count);
                            for (int i = 0; i < resultsList.Count; i++)
                            {
                                processNodes.Add([deconjugationResult.Process]);
                            }

                            result = new IntermediaryResult(matchedText,
                                dict,
                                resultsList,
                                deconjugationResult.Text,
                                processNodes);
                        }
                    }
                }
            }
        }
    }

    private static void GetWordResults(TextInfo textInfo, ReadOnlySpan<string> allTextWithoutLongVowelMark, Dict dict, bool useDB, Dictionary<string, IntermediaryResult> results, GetRecordsFromDB? getRecordsFromDB)
    {
        ReadOnlySpan<string> textList = textInfo.TextList.AsReadOnlySpan();
        ReadOnlySpan<string> textInHiraganaList = textInfo.TextInHiraganaList.AsReadOnlySpan();
        ReadOnlySpan<List<Form>?> deconjugationResultsList = textInfo.DeconjugationResultsList.AsReadOnlySpan();
        string[]? deconjugatedTexts = textInfo.DeconjugatedTexts;
        ReadOnlySpan<List<List<Form>>?> deconjugationResultListForTextWithoutLongVowelMarkList = textInfo.DeconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan();
        ReadOnlySpan<List<string>?> textWithoutLongVowelMarkList = textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan();
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;
        Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = null;

        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            int maxSearchKeyLength = dict.MaxSearchKeyLength;
            // Use 0 to skip filtering when all candidates fit.
            dbWordDict = getRecordsFromDB(dict.ReadOnlyConnectionString, textInHiraganaList, textInfo.MaxTextInHiraganaLength <= maxSearchKeyLength ? 0 : maxSearchKeyLength);

            if (deconjugatedTexts is not null)
            {
                dbVerbDict = getRecordsFromDB(dict.ReadOnlyConnectionString, deconjugatedTexts, textInfo.MaxDeconjugatedTextLength <= maxSearchKeyLength ? 0 : maxSearchKeyLength);
            }

            if (!allTextWithoutLongVowelMark.IsEmpty)
            {
                dbWordDictForLongVowelConversion = getRecordsFromDB(dict.ReadOnlyConnectionString, allTextWithoutLongVowelMark, textInfo.MaxTextWithoutLongVowelMarksLength <= maxSearchKeyLength ? 0 : maxSearchKeyLength);
            }
        }

        for (int i = 0; i < textList.Length; i++)
        {
            ref readonly string text = ref textList[i];
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], text, textInHiraganaList[i], dbWordDict, dbVerbDict);

            ReadOnlySpan<string> textsWithoutLongVowelMark = [];
            ReadOnlySpan<List<Form>> deconjugationResultListForTextWithoutLongVowelMark = [];
            if (textWithoutLongVowelMarkList.Length > i)
            {
                Debug.Assert(textWithoutLongVowelMarkList.Length > i);
                textsWithoutLongVowelMark = textWithoutLongVowelMarkList[i].AsReadOnlySpan();

                Debug.Assert(deconjugationResultListForTextWithoutLongVowelMarkList.Length > i);
                deconjugationResultListForTextWithoutLongVowelMark = deconjugationResultListForTextWithoutLongVowelMarkList[i].AsReadOnlySpan();
            }

            if (!textsWithoutLongVowelMark.IsEmpty)
            {
                Debug.Assert(!deconjugationResultListForTextWithoutLongVowelMark.IsEmpty);
                for (int j = 0; j < textsWithoutLongVowelMark.Length; j++)
                {
                    GetWordResultsHelper(dict, results, deconjugationResultListForTextWithoutLongVowelMark[j], text, textsWithoutLongVowelMark[j], dbWordDictForLongVowelConversion, dbVerbDict);
                }
            }
        }
    }

    private static List<IDictRecord> GetValidDeconjugatedResults(Dict dict, in Form deconjugationResult, IList<IDictRecord> dictResults)
    {
        string lastTag = deconjugationResult.LastTag;
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
                        foreach (ReadOnlySpan<char> wordClass in dictResult.WordClasses)
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
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(GetValidDeconjugatedResults), dict.Type);
                break;
        }

        return resultsList;
    }

    private static void GetNameResults(TextInfo textInfo, Dict dict, bool useDB, Dictionary<string, IntermediaryResult> results, GetRecordsFromDB? getRecordsFromDB)
    {
        ReadOnlySpan<string> textList = textInfo.TextList.AsReadOnlySpan();
        ReadOnlySpan<string> textInHiraganaList = textInfo.TextInHiraganaList.AsReadOnlySpan();
        IDictionary<string, IList<IDictRecord>>? nameDict;
        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            int maxSearchKeyLength = dict.MaxSearchKeyLength;
            nameDict = getRecordsFromDB(dict.ReadOnlyConnectionString, textInHiraganaList, textInfo.MaxTextInHiraganaLength <= maxSearchKeyLength ? 0 : maxSearchKeyLength);
        }
        else
        {
            nameDict = dict.Contents;
        }

        if (nameDict is null)
        {
            return;
        }

        for (int i = 0; i < textList.Length; i++)
        {
            string textInHiragana = textInHiraganaList[i];
            if (nameDict.TryGetValue(textInHiragana, out IList<IDictRecord>? result))
            {
                ref IntermediaryResult? nameResult = ref CollectionsMarshal.GetValueRefOrAddDefault(results, textInHiragana, out bool exists);
                if (!exists)
                {
                    nameResult = new IntermediaryResult(textList[i], dict, result);
                }
            }
        }
    }

    private static void GetKanjiResults(string kanji, Dict dict, bool useDB, GetKanjiRecordsFromDB getKanjiRecordsFromDB, out IntermediaryResult? kanjiResult)
    {
        if (useDB)
        {
            List<IDictRecord>? records = getKanjiRecordsFromDB(dict.ReadOnlyConnectionString, kanji);
            kanjiResult = records is { Count: > 0 } ? new IntermediaryResult(kanji, dict, records) : null;
        }
        else
        {
            kanjiResult = dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? records)
                ? new IntermediaryResult(kanji, dict, records)
                : null;
        }
    }

    private static void GetKanjiResults(string kanji, string kanjiWithVariationSelector, Dict dict, bool useDB, GetKanjiRecordsWithVariationSelectorFromDB getKanjiRecordsFromDB, out IntermediaryResult? kanjiResult, out IntermediaryResult? kanjiVariationResult)
    {
        if (useDB)
        {
            Dictionary<string, IList<IDictRecord>>? results = getKanjiRecordsFromDB(dict.ReadOnlyConnectionString, kanjiWithVariationSelector, kanji);
            kanjiVariationResult = results is not null && results.TryGetValue(kanjiWithVariationSelector, out IList<IDictRecord>? variationRecords)
                ? new IntermediaryResult(kanjiWithVariationSelector, dict, variationRecords)
                : null;
            kanjiResult = results is not null && results.TryGetValue(kanji, out IList<IDictRecord>? records)
                ? new IntermediaryResult(kanji, dict, records)
                : null;
        }
        else
        {
            kanjiVariationResult = dict.Contents.TryGetValue(kanjiWithVariationSelector, out IList<IDictRecord>? variationRecords)
                ? new IntermediaryResult(kanjiWithVariationSelector, dict, variationRecords)
                : null;
            kanjiResult = dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? records)
                ? new IntermediaryResult(kanji, dict, records)
                : null;
        }

        if (kanjiVariationResult is not null
            && kanjiResult is not null
            && dict.Type is DictType.NonspecificKanjiWithWordSchemaYomichan or DictType.NonspecificKanjiNazeka)
        {
            List<IDictRecord> baseRecords = [];
            IList<IDictRecord> records = kanjiResult.Results;
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                IDictRecord record = records[i];
                if (!kanjiVariationResult.Results.Contains(record))
                {
                    baseRecords.Add(record);
                }
            }

            kanjiResult = baseRecords.Count > 0 ? new IntermediaryResult(kanji, dict, baseRecords) : null;
        }
    }

    private static Dictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, RentedArrayBuffer<SqliteConnection?> connections, HashSet<string> searchKeys)
    {
        if (dbFreqs.Length is 1)
        {
            Dictionary<string, Dictionary<string, List<FrequencyRecord>>> singleResult = new(1, StringComparer.Ordinal);
            SqliteConnection? connection = connections[0];
            if (connection is not null)
            {
                Dictionary<string, List<FrequencyRecord>>? records = FreqDBManager.GetRecordsFromDB(connection, searchKeys);
                if (records is not null)
                {
                    singleResult.Add(dbFreqs[0].Name, records);
                }
            }

            return singleResult;
        }

        Dictionary<string, List<FrequencyRecord>>?[] resultsArray = ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Rent(dbFreqs.Length);
        try
        {
            _ = Parallel.For(0, dbFreqs.Length, i =>
            {
                SqliteConnection? connection = connections[i];
                resultsArray[i] = connection is not null
                    ? FreqDBManager.GetRecordsFromDB(connection, searchKeys)
                    : null;
            });

            Dictionary<string, Dictionary<string, List<FrequencyRecord>>> result = new(dbFreqs.Length, StringComparer.Ordinal);
            for (int i = 0; i < dbFreqs.Length; i++)
            {
                Dictionary<string, List<FrequencyRecord>>? resultArrayItem = resultsArray[i];
                if (resultArrayItem is not null)
                {
                    result[dbFreqs[i].Name] = resultArrayItem;
                }
            }

            return result;
        }
        finally
        {
            resultsArray.AsSpan(0, dbFreqs.Length).Clear();
            ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Return(resultsArray);
        }
    }

    private static Dictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, HashSet<string> searchKeys)
    {
        if (dbFreqs.Length is 1)
        {
            Dictionary<string, Dictionary<string, List<FrequencyRecord>>> singleResult = new(1, StringComparer.Ordinal);
            Freq freq = dbFreqs[0];
            Dictionary<string, List<FrequencyRecord>>? records = FreqDBManager.GetRecordsFromDB(freq.ReadOnlyConnectionString, searchKeys);
            if (records is not null)
            {
                singleResult.Add(freq.Name, records);
            }

            return singleResult;
        }

        Dictionary<string, List<FrequencyRecord>>?[] resultsArray = ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Rent(dbFreqs.Length);
        try
        {
            _ = Parallel.For(0, dbFreqs.Length, i =>
            {
                Freq freq = dbFreqs[i];
                resultsArray[i] = FreqDBManager.GetRecordsFromDB(freq.ReadOnlyConnectionString, searchKeys);
            });

            Dictionary<string, Dictionary<string, List<FrequencyRecord>>> result = new(dbFreqs.Length, StringComparer.Ordinal);
            for (int i = 0; i < dbFreqs.Length; i++)
            {
                Dictionary<string, List<FrequencyRecord>>? resultArrayItem = resultsArray[i];
                if (resultArrayItem is not null)
                {
                    result[dbFreqs[i].Name] = resultArrayItem;
                }
            }

            return result;
        }
        finally
        {
            resultsArray.AsSpan(0, dbFreqs.Length).Clear();
            ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Return(resultsArray);
        }
    }

    private static void BuildJmdictResult(Dictionary<string, IntermediaryResult> jmdictResults, List<LookupResult> results, Freq[]? wordFreqs, Freq[]? dbWordFreqs, RentedArrayBuffer<SqliteConnection?>? dbWordFreqConnections, bool dbIsUsedForPitchDict, SqliteConnection? pitchDictConnection, Dict? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(jmdictResults)
            : null;

        bool pitchAccentDictExists = false;
        Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;

        if (dbWordFreqsExist && dbIsUsedForPitchDict)
        {
            Parallel.Invoke(
            () =>
            {
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(dbWordFreqConnections is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);
            },
            () =>
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                Debug.Assert(pitchDictConnection is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDictConnection, searchKeys);
                pitchAccentDictExists = pitchAccentDict is not null;
            });
        }
        else if (dbWordFreqsExist)
        {
            Debug.Assert(dbWordFreqs is not null);
            Debug.Assert(dbWordFreqConnections is not null);
            Debug.Assert(searchKeys is not null);
            frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);

            pitchAccentDict = pitchDict?.Contents;
            pitchAccentDictExists = pitchAccentDict is not null;
        }
        else if (dbIsUsedForPitchDict)
        {
            Debug.Assert(pitchDict is not null);
            Debug.Assert(searchKeys is not null);
            Debug.Assert(pitchDictConnection is not null);
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDictConnection, searchKeys);
            pitchAccentDictExists = pitchAccentDict is not null;
        }
        else
        {
            pitchAccentDict = pitchDict?.Contents;
            pitchAccentDictExists = pitchAccentDict is not null;
        }

        foreach (IntermediaryResult wordResult in jmdictResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<ProcessNode>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            IList<IDictRecord> resultsList = wordResult.Results;
            for (int i = 0; i < resultsList.Count; i++)
            {
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = deconjugatedWord ? GetDeconjugationInfo(processesSpan, i) : (null, 0);
                JmdictRecord jmdictResult = (JmdictRecord)resultsList[i];
                LookupResult result = new
                (
                    primarySpelling: jmdictResult.PrimarySpelling,
                    matchedText: wordResult.MatchedText,
                    dict: wordResult.Dict,
                    readings: jmdictResult.Readings,
                    formattedDefinitions: jmdictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    frequencies: wordFreqsExist ? GetWordFrequencies(jmdictResult, wordFreqs!, frequencyDicts) : null,
                    alternativeSpellings: jmdictResult.AlternativeSpellings,
                    deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                    deconjugationProcess: deconjugationProcess,
                    minDeconjugationProcessStepCount: minDeconjugationProcessStepCount,
                    entryId: jmdictResult.Id,
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(jmdictResult.PrimarySpelling, jmdictResult.Readings, pitchAccentDict!) : null,
                    wordClasses: jmdictResult.WordClassesSharedByAllSenses,
                    jmdictLookupResult: new JmdictLookupResult(jmdictResult.PrimarySpellingOrthographyInfo, jmdictResult.ReadingsOrthographyInfo, jmdictResult.AlternativeSpellingsOrthographyInfo, jmdictResult.MiscSharedByAllSenses, jmdictResult.Misc, jmdictResult.WordClasses)
                );

                // See: ヤンキー座り
                if (!results.Contains(result))
                {
                    results.Add(result);
                }
            }
        }
    }

    private static HashSet<string> GetSearchKeysFromRecords(Dictionary<string, IntermediaryResult> dictResults)
    {
        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        foreach (IntermediaryResult intermediaryResult in dictResults.Values)
        {
            IList<IDictRecord> records = intermediaryResult.Results;
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                IDictRecordWithMultipleReadings record = (IDictRecordWithMultipleReadings)records[i];
                _ = searchKeys.Add(JapaneseUtils.NormalizeText(record.PrimarySpelling));
                if (record.Readings is not null)
                {
                    foreach (string reading in record.Readings)
                    {
                        _ = searchKeys.Add(JapaneseUtils.NormalizeText(reading));
                    }
                }
            }
        }

        return searchKeys;
    }

    private static void BuildJmnedictResult(
        Dictionary<string, IntermediaryResult> jmnedictResults, List<LookupResult> results, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IntermediaryResult nameResult in jmnedictResults.Values)
        {
            IList<IDictRecord> records = nameResult.Results;
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                JmnedictRecord jmnedictRecord = (JmnedictRecord)records[i];
                LookupResult result = new
                (
                    primarySpelling: jmnedictRecord.PrimarySpelling,
                    matchedText: nameResult.MatchedText,
                    dict: nameResult.Dict,
                    readings: jmnedictRecord.Readings,
                    formattedDefinitions: jmnedictRecord.BuildFormattedDefinition(nameResult.Dict.Options),
                    alternativeSpellings: jmnedictRecord.AlternativeSpellings,
                    entryId: jmnedictRecord.Id,
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(jmnedictRecord.PrimarySpelling, jmnedictRecord.Readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }
    }

    private static LookupResult BuildKanjidicResult(string kanji, string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        KanjidicRecord kanjiRecord = (KanjidicRecord)intermediaryResult.Results[0];
        string[]? allReadings = ArrayUtils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        bool pitchAccentDictExists = pitchAccentDict is not null;
        LookupResult result = new
        (
            primarySpelling: kanji,
            matchedText: intermediaryResult.MatchedText,
            dict: intermediaryResult.Dict,
            readings: allReadings,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            frequencies: GetKanjidicFrequencies(kanjiRecord.Frequency, kanjiFrequencyResults),
            // ReSharper disable once NullableWarningSuppressionIsUsed
            pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null,
            kanjiLookupResult: new KanjiLookupResult(kanjiCompositions, kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings, kanjiRecord.RadicalNames, kanjiRecord.StrokeCount, kanjiRecord.Grade)
        );

        return result;
    }

    private static void BuildYomichanKanjiResult(
        string kanji, List<LookupResult> results, string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        IList<IDictRecord> records = intermediaryResult.Results;
        int recordCount = records.Count;
        for (int i = 0; i < recordCount; i++)
        {
            YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)records[i];

            string[]? allReadings = ArrayUtils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);
            LookupResult result = new
            (
                primarySpelling: intermediaryResult.MatchedText,
                matchedText: intermediaryResult.MatchedText,
                dict: intermediaryResult.Dict,
                readings: allReadings,
                formattedDefinitions: yomichanKanjiDictResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                frequencies: kanjiFrequencyResults,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null,
                kanjiLookupResult: new KanjiLookupResult(kanjiCompositions, yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings, kanjiStats: yomichanKanjiDictResult.BuildFormattedStats())
            );

            results.Add(result);
        }
    }

    private static void BuildEpwingYomichanResult(
        Dictionary<string, IntermediaryResult> epwingResults, List<LookupResult> results, Freq[]? freqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool freqsExist = freqs is not null;
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IntermediaryResult wordResult in epwingResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<ProcessNode>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            IList<IDictRecord> resultsList = wordResult.Results;
            for (int i = 0; i < resultsList.Count; i++)
            {
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = deconjugatedWord ? GetDeconjugationInfo(processesSpan, i) : (null, 0);
                EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)resultsList[i];
                string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: epwingResult.PrimarySpelling,
                    matchedText: wordResult.MatchedText,
                    dict: wordResult.Dict,
                    readings: readings,
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    frequencies: freqsExist ? GetWordFrequencies(epwingResult, freqs!, frequencyDicts) : null,
                    deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                    deconjugationProcess: deconjugationProcess,
                    minDeconjugationProcessStepCount: minDeconjugationProcessStepCount,
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                    wordClasses: epwingResult.WordClasses,
                    imageInfos: epwingResult.ImageInfos,
                    popularityScore: epwingResult.PopularityScore
                );

                results.Add(result);
            }
        }
    }

    private static void BuildEpwingYomichanResultForKanjiWithWordSchema(IntermediaryResult intermediaryResult, List<LookupResult> results, string[]? kanjiCompositions, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        IList<IDictRecord> records = intermediaryResult.Results;
        int recordCount = records.Count;
        for (int i = 0; i < recordCount; i++)
        {
            EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)records[i];
            string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
            LookupResult result = new
            (
                primarySpelling: epwingResult.PrimarySpelling,
                matchedText: intermediaryResult.MatchedText,
                dict: intermediaryResult.Dict,
                readings: readings,
                formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                frequencies: kanjiFrequencyResults,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                imageInfos: epwingResult.ImageInfos,
                popularityScore: epwingResult.PopularityScore,
                kanjiLookupResult: new KanjiLookupResult(kanjiCompositions)
            );

            results.Add(result);
        }
    }

    private static void BuildEpwingNazekaResult(
        Dictionary<string, IntermediaryResult> epwingNazekaResults, List<LookupResult> results, Freq[]? freqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        bool freqsExist = freqs is not null;
        foreach (IntermediaryResult wordResult in epwingNazekaResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<ProcessNode>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            IList<IDictRecord> resultsList = wordResult.Results;
            for (int i = 0; i < resultsList.Count; i++)
            {
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = deconjugatedWord ? GetDeconjugationInfo(processesSpan, i) : (null, 0);
                EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)resultsList[i];
                string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: epwingResult.PrimarySpelling,
                    matchedText: wordResult.MatchedText,
                    dict: wordResult.Dict,
                    readings: readings,
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options),
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    frequencies: freqsExist ? GetWordFrequencies(epwingResult, freqs!, frequencyDicts) : null,
                    alternativeSpellings: epwingResult.AlternativeSpellings,
                    deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                    deconjugationProcess: deconjugationProcess,
                    minDeconjugationProcessStepCount: minDeconjugationProcessStepCount,
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                    imageInfos: epwingResult.ImageInfo is not null ? [epwingResult.ImageInfo] : null
                );

                results.Add(result);
            }
        }
    }

    private static bool WordClassDictionaryContainsTag(string primarySpelling, string? reading, string tag)
    {
        if (DictUtils.WordClassDictionary.TryGetValue(JapaneseUtils.NormalizeText(primarySpelling), out IList<JmdictWordClass>? jmdictWcResults))
        {
            bool hasReading = reading is not null;
            int jmdictWcResultsCount = jmdictWcResults.Count;
            for (int i = 0; i < jmdictWcResultsCount; i++)
            {
                JmdictWordClass result = jmdictWcResults[i];
                if (primarySpelling == result.Spelling
                    && ((hasReading && result.Readings is not null && result.Readings.Contains(reading))
                        || (!hasReading && result.Readings is null))
                    && result.WordClasses.Contains(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private static void BuildEpwingNazekaResultForKanji(IntermediaryResult intermediaryResult, List<LookupResult> results, string[]? kanjiCompositions, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        IList<IDictRecord> records = intermediaryResult.Results;
        int recordCount = records.Count;
        for (int i = 0; i < recordCount; i++)
        {
            EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)records[i];
            string[]? readings = epwingResult.Reading is not null ? [epwingResult.Reading] : null;
            LookupResult result = new
            (
                primarySpelling: epwingResult.PrimarySpelling,
                matchedText: intermediaryResult.MatchedText,
                dict: intermediaryResult.Dict,
                readings: readings,
                formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                frequencies: kanjiFrequencyResults,
                alternativeSpellings: epwingResult.AlternativeSpellings,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                imageInfos: epwingResult.ImageInfo is not null ? [epwingResult.ImageInfo] : null,
                kanjiLookupResult: new KanjiLookupResult(kanjiCompositions)
            );
            results.Add(result);
        }
    }

    private static void BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults, List<LookupResult> results, Freq[]? wordFreqs, Freq[]? dbWordFreqs, RentedArrayBuffer<SqliteConnection?>? dbWordFreqConnections, bool dbIsUsedForPitchDict, SqliteConnection? pitchDictConnection, Dict? pitchDict)
    {
        bool wordFreqsExist = wordFreqs is not null;
        bool dbWordFreqsExist = dbWordFreqs is not null;

        HashSet<string>? searchKeys = dbWordFreqsExist || dbIsUsedForPitchDict
            ? GetSearchKeysFromRecords(customWordResults)
            : null;

        bool pitchAccentDictExists = false;
        Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (dbWordFreqsExist && dbIsUsedForPitchDict)
        {
            Parallel.Invoke(
            () =>
            {
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(dbWordFreqConnections is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);
            },
            () =>
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                Debug.Assert(pitchDictConnection is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDictConnection, searchKeys);
                pitchAccentDictExists = pitchAccentDict is not null;
            });
        }
        else if (dbWordFreqsExist)
        {
            Debug.Assert(dbWordFreqs is not null);
            Debug.Assert(dbWordFreqConnections is not null);
            Debug.Assert(searchKeys is not null);
            frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);

            pitchAccentDict = pitchDict?.Contents;
            pitchAccentDictExists = pitchAccentDict is not null;
        }
        else if (dbIsUsedForPitchDict)
        {
            Debug.Assert(pitchDict is not null);
            Debug.Assert(searchKeys is not null);
            Debug.Assert(pitchDictConnection is not null);
            pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDictConnection, searchKeys);
            pitchAccentDictExists = pitchAccentDict is not null;
        }
        else
        {
            pitchAccentDict = pitchDict?.Contents;
            pitchAccentDictExists = pitchAccentDict is not null;
        }

        foreach (IntermediaryResult wordResult in customWordResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<ProcessNode>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            IList<IDictRecord> resultsList = wordResult.Results;
            for (int i = 0; i < resultsList.Count; i++)
            {
                CustomWordRecord customWordDictResult = (CustomWordRecord)resultsList[i];
                string? deconjugationProcess = null;
                int minDeconjugationProcessStepCount = 0;
                if (deconjugatedWord)
                {
                    if (customWordDictResult.HasUserDefinedWordClass)
                    {
                        (deconjugationProcess, minDeconjugationProcessStepCount) = GetDeconjugationInfo(processesSpan, i);
                    }
                    else
                    {
                        minDeconjugationProcessStepCount = GetMinDeconjugationProcessStepCount(processesSpan[i].AsReadOnlySpan());
                    }
                }

                LookupResult result = new
                (
                    primarySpelling: customWordDictResult.PrimarySpelling,
                    matchedText: wordResult.MatchedText,
                    dict: wordResult.Dict,
                    readings: customWordDictResult.Readings,
                    formattedDefinitions: customWordDictResult.BuildFormattedDefinition(wordResult.Dict.Options),
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    frequencies: wordFreqsExist ? GetWordFrequencies(customWordDictResult, wordFreqs!, frequencyDicts) : null,
                    alternativeSpellings: customWordDictResult.AlternativeSpellings,
                    deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                    deconjugationProcess: deconjugationProcess,
                    minDeconjugationProcessStepCount: minDeconjugationProcessStepCount,
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(customWordDictResult.PrimarySpelling, customWordDictResult.Readings, pitchAccentDict!) : null,
                    wordClasses: customWordDictResult.WordClasses
                );

                results.Add(result);
            }
        }
    }

    private static void BuildCustomNameResult(
        Dictionary<string, IntermediaryResult> customNameResults, List<LookupResult> results, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IntermediaryResult customNameResult in customNameResults.Values)
        {
            IList<IDictRecord> records = customNameResult.Results;
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                CustomNameRecord customNameDictResult = (CustomNameRecord)records[i];
                string[]? readings = customNameDictResult.Reading is not null ? [customNameDictResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: customNameDictResult.PrimarySpelling,
                    matchedText: customNameResult.MatchedText,
                    dict: customNameResult.Dict,
                    readings: readings,
                    formattedDefinitions: customNameDictResult.BuildFormattedDefinition(),
                    frequencies: [new LookupFrequencyResult(customNameResult.Dict.Name, -i, false)],
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(customNameDictResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                    imageInfos: customNameDictResult.ImageInfo is not null ? [customNameDictResult.ImageInfo] : null
                );

                results.Add(result);
            }
        }
    }

    private static List<LookupFrequencyResult>? GetWordFrequencies<T>(T record, Freq[] wordFreqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB) where T : IGetFrequency
    {
        List<LookupFrequencyResult> freqsList = new(wordFreqs.Length);
        bool frequencyExists = false;

        foreach (Freq freq in wordFreqs)
        {
            bool useDB = freq.Options.UseDB.Value && freq.Ready && freqDictsFromDB is not null;
            if (useDB)
            {
                Debug.Assert(freqDictsFromDB is not null);
                if (freqDictsFromDB.TryGetValue(freq.Name, out Dictionary<string, List<FrequencyRecord>>? freqDict))
                {
                    int frequency = record.GetFrequency(freqDict);
                    frequencyExists = frequencyExists || frequency is not int.MaxValue;
                    freqsList.Add(new LookupFrequencyResult(freq.Name, frequency, freq.Options.HigherValueMeansHigherFrequency.Value));
                }
            }

            else
            {
                int frequency = record.GetFrequency(freq.Contents);
                frequencyExists = frequencyExists || frequency is not int.MaxValue;
                freqsList.Add(new LookupFrequencyResult(freq.Name, frequency, freq.Options.HigherValueMeansHigherFrequency.Value));
            }
        }

        return frequencyExists
            ? freqsList
            : null;
    }

    private static List<LookupFrequencyResult>? GetKanjiFrequencies(string kanji, string? kanjiWithVariationSelector, Freq[] kanjiFreqs)
    {
        List<LookupFrequencyResult> freqsList = new(kanjiFreqs.Length);
        foreach (Freq kanjiFreq in kanjiFreqs)
        {
            bool useDB = kanjiFreq.Options.UseDB.Value && kanjiFreq.Ready;
            if (useDB)
            {
                int? frequency = kanjiWithVariationSelector is not null
                    ? FreqDBManager.GetKanjiFrequencyFromDB(kanjiFreq.ReadOnlyConnectionString, kanjiWithVariationSelector, kanji)
                    : FreqDBManager.GetKanjiFrequencyFromDB(kanjiFreq.ReadOnlyConnectionString, kanji);
                if (frequency is not null)
                {
                    freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency.Value, kanjiFreq.Options.HigherValueMeansHigherFrequency.Value));
                }

                continue;
            }

            if (kanjiWithVariationSelector is null || !kanjiFreq.Contents.TryGetValue(kanjiWithVariationSelector, out IList<FrequencyRecord>? freqResultList))
            {
                _ = kanjiFreq.Contents.TryGetValue(kanji, out freqResultList);
            }

            if (freqResultList is not null)
            {
                int frequency = freqResultList[0].Frequency;
                freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency, kanjiFreq.Options.HigherValueMeansHigherFrequency.Value));
            }
        }

        return freqsList.Count > 0
            ? freqsList
            : null;
    }

    private static List<LookupFrequencyResult>? GetKanjidicFrequencies(int frequency, List<LookupFrequencyResult>? frequencyResults)
    {
        return frequency is 0
            ? frequencyResults
            : frequencyResults is null
                ? [new LookupFrequencyResult("KANJIDIC2", frequency, false)]
                : [new LookupFrequencyResult("KANJIDIC2", frequency, false), .. frequencyResults];
    }

    private static byte[]? GetPitchPosition(string primarySpelling, string[]? readings, IDictionary<string, IList<IDictRecord>> pitchDictionary)
    {
        if (readings is null)
        {
            if (pitchDictionary.TryGetValue(JapaneseUtils.NormalizeText(primarySpelling), out IList<IDictRecord>? records))
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
            if (pitchDictionary.TryGetValue(JapaneseUtils.NormalizeText(primarySpelling), out IList<IDictRecord>? records))
            {
                int recordsCount = records.Count;
                for (int i = 0; i < readings.Length; i++)
                {
                    byte position = byte.MaxValue;
                    string reading = readings[i];
                    string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                    for (int j = 0; j < recordsCount; j++)
                    {
                        PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)records[j];
                        if (pitchAccentRecord.Reading is not null && readingInHiragana == JapaneseUtils.NormalizeText(pitchAccentRecord.Reading))
                        {
                            if (positions is null)
                            {
                                positions = new byte[readings.Length];
                                for (int k = 0; k < i; k++)
                                {
                                    positions[k] = byte.MaxValue;
                                }
                            }

                            position = pitchAccentRecord.Position;
                            break;
                        }
                    }

                    _ = positions?[i] = position;
                }
            }
            else
            {
                for (int i = 0; i < readings.Length; i++)
                {
                    string reading = readings[i];
                    byte position = byte.MaxValue;
                    if (pitchDictionary.TryGetValue(JapaneseUtils.NormalizeText(reading), out records))
                    {
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
                                    for (int k = 0; k < i; k++)
                                    {
                                        positions[k] = byte.MaxValue;
                                    }
                                }

                                position = pitchAccentRecord.Position;
                                break;
                            }
                        }
                    }

                    _ = positions?[i] = position;
                }
            }

            return positions;
        }
    }

    private static int FastReferenceIndexOf(this IList<IDictRecord> list, IDictRecord record)
    {
        ReadOnlySpan<IDictRecord> span = list is List<IDictRecord> concreteList
            ? concreteList.AsReadOnlySpan()
            : (IDictRecord[])list;

        for (int i = 0; i < span.Length; i++)
        {
            if (ReferenceEquals(span[i], record))
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetMinDeconjugationProcessStepCount(ReadOnlySpan<ProcessNode> processes)
    {
        Debug.Assert(!processes.IsEmpty);
        int min = processes[0].ProperStepCount;
        if (min is 1)
        {
            return 1;
        }

        foreach (ref readonly ProcessNode node in processes[1..])
        {
            int properStepCount = node.ProperStepCount;
            if (properStepCount < min)
            {
                min = properStepCount;
                if (min is 1)
                {
                    return 1;
                }
            }
        }

        return min;
    }

    private static (string? process, int minStepCount) GetDeconjugationInfo(ReadOnlySpan<List<ProcessNode>> processListSpan, int index)
    {
        ReadOnlySpan<ProcessNode> processSpan = processListSpan[index].AsReadOnlySpan();
        string? process = LookupResultUtils.DeconjugationProcessesToText(processSpan);
        if (process is null)
        {
            return (null, 0);
        }

        return (process, GetMinDeconjugationProcessStepCount(processSpan));
    }
}
