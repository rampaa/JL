using System.Buffers;
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
using JL.Core.Dicts.KanjiComposition;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Array;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.Japanese;
using JL.Core.Utilities.ObjectPool;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Lookup;

public static class LookupUtils
{
    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, string query);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string dbName, string term);

    private static List<LookupResult>?[] s_resultSlots = [];

    public static LookupResult[]? LookupText(string text)
    {
        string? kanji = null;
        string[]? kanjiCompositions = null;
        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        bool kanjiExists = false;
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = JapaneseUtils.GetFirstCharacterIfKanji(text);
            if (kanji is not null)
            {
                kanjiExists = true;
                kanjiCompositions = KanjiCompositionDBManager.GetRecordsFromDB(kanji);

                Freq[]? kanjiFreqs = FreqUtils.KanjiFreqs;
                kanjiFrequencyResults = kanjiFreqs is not null
                    ? GetKanjiFrequencies(kanji, kanjiFreqs)
                    : null;
            }
        }

        Freq[]? wordFreqs = FreqUtils.WordFreqs;
        Freq[]? dbWordFreqs = FreqUtils.DBWordFreqs;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForJmdict = dbWordFreqs is not null && DictUtils.JmdictIsActive
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForCustomWordDict = dbWordFreqs is not null && DictUtils.AnyCustomWordDictIsActive
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        PopulateFreqSqliteConnections(sqliteFreqConnectionsForJmdict.Items, sqliteFreqConnectionsForCustomWordDict.Items, dbWordFreqs);
        RentedArrayBuffer<SqliteConnection?>? freqConnectionsForJmdict = sqliteFreqConnectionsForJmdict.Items;
        RentedArrayBuffer<SqliteConnection?>? freqConnectionsForCustomWordDict = sqliteFreqConnectionsForCustomWordDict.Items;

        Dict? pitchDict = DictUtils.PitchDict;
        bool dbIsUsedForPitchDict = DictUtils.DBIsUsedForPitchDict
            // ReSharper disable once NullableWarningSuppressionIsUsed
            && pitchDict!.Ready;

        TextInfo textInfo = GetTextInfo(text, wordFreqs is not null, dbIsUsedForPitchDict, dbWordFreqs, pitchDict);

        DBParameters dbParameters = GetDBParameters(textInfo);

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

        using SqliteConnection? sqliteConnectionForJmdictPitch = readOnlyConnectionStringForPitchDict is not null && DictUtils.JmdictIsActive
            ? DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForPitchDict)
            : null;

        using SqliteConnection? sqliteConnectionForCustomWordPitch = readOnlyConnectionStringForPitchDict is not null && DictUtils.AnyCustomWordDictIsActive
            ? DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionStringForPitchDict)
            : null;

        Dict[] dicts = DictUtils.GetDictForLookupCategoryType(CoreConfigManager.Instance.LookupCategory);
        bool dbIsUsedAtLeastForOneDict = DictUtils.DBIsUsedForAtLeastOneDict;

        List<LookupResult>?[] resultSlots;
        if (dicts.Length == s_resultSlots.Length)
        {
            resultSlots = s_resultSlots;
            resultSlots.AsSpan().Clear();
        }
        else
        {
            resultSlots = new List<LookupResult>?[dicts.Length];
            s_resultSlots = resultSlots;
        }

        _ = Parallel.For(0, dicts.Length, i =>
        {
            Dict dict = dicts[i];
            bool useDB = dbIsUsedAtLeastForOneDict && dict is { Options.UseDB.Value: true, Ready: true, Active: true };
            switch (dict.Type)
            {
                case DictType.JMdict:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), textInfo.DeconjugationResultsList.AsReadOnlySpan(), textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan(), textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan(), dbParameters.AllTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, JmdictDBManager.GetRecordsFromDB, dbParameters.JmdictWordQuery, dbParameters.JmdictVerbQuery, dbParameters.JmdictTextWithoutLongVowelMarkParameter);
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
                    GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, results, JmnedictDBManager.GetRecordsFromDB, dbParameters.JmnedictQuery);
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
                        IntermediaryResult? kanjidicResult = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, KanjidicDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

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
                        IntermediaryResult? epwingYomichanKanjiWithWordSchemaResults = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, EpwingYomichanDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (epwingYomichanKanjiWithWordSchemaResults is not null)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            BuildEpwingYomichanResultForKanjiWithWordSchema(epwingYomichanKanjiWithWordSchemaResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                        }
                    }
                    break;
                }

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), textInfo.DeconjugationResultsList.AsReadOnlySpan(), textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan(), textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan(), dbParameters.AllTextWithoutLongVowelMark.AsReadOnlySpan(), dict, false, results, null, null, null, null);
                    if (results.Count > 0)
                    {
                        List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                        // ReSharper disable once AccessToDisposedClosure
                        resultSlots[i] = rentedLookupResults;
                        // ReSharper disable once AccessToDisposedClosure
                        BuildCustomWordResult(results, rentedLookupResults, wordFreqs, dbWordFreqs, freqConnectionsForCustomWordDict, dbIsUsedForPitchDict, sqliteConnectionForCustomWordPitch, pitchDict);
                    }

                    ObjectPoolManager.s_intermediaryResultPool.Return(results);
                    break;
                }

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, false, results, null, null);
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

                        IntermediaryResult? epwingYomichanKanjiResults = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, YomichanKanjiDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (epwingYomichanKanjiResults is not null)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            BuildYomichanKanjiResult(kanji, rentedLookupResults, kanjiCompositions, epwingYomichanKanjiResults, kanjiFrequencyResults, textInfo.PitchAccentDict);
                        }
                    }
                    break;
                }

                case DictType.NonspecificNameYomichan:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, results, EpwingYomichanDBManager.GetRecordsFromDB, dbParameters.YomichanWordQuery);
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
                    GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), textInfo.DeconjugationResultsList.AsReadOnlySpan(), textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan(), textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan(), dbParameters.AllTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, EpwingYomichanDBManager.GetRecordsFromDB, dbParameters.YomichanWordQuery, dbParameters.YomichanVerbQuery, dbParameters.YomichanTextWithoutLongVowelMarkQuery);
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

                        IntermediaryResult? epwingNazekaKanjiResults = useDB
                            ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                            : GetKanjiResults(kanji, dict);

                        if (epwingNazekaKanjiResults is not null)
                        {
                            List<LookupResult> rentedLookupResults = ObjectPoolManager.s_lookupResultListPool.Get();
                            // ReSharper disable once AccessToDisposedClosure
                            resultSlots[i] = rentedLookupResults;
                            BuildEpwingNazekaResultForKanji(epwingNazekaKanjiResults, rentedLookupResults, kanjiCompositions, kanjiFrequencyResults, textInfo.PitchAccentDict);
                        }
                    }

                    break;
                }

                case DictType.NonspecificNameNazeka:
                {
                    Dictionary<string, IntermediaryResult> results = ObjectPoolManager.s_intermediaryResultPool.Get();
                    GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, results, EpwingNazekaDBManager.GetRecordsFromDB, dbParameters.NazekaWordQuery);
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
                    GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), textInfo.DeconjugationResultsList.AsReadOnlySpan(), textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan(), textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan(), dbParameters.AllTextWithoutLongVowelMark.AsReadOnlySpan(), dict, useDB, results, EpwingNazekaDBManager.GetRecordsFromDB, dbParameters.NazekaWordQuery, dbParameters.NazekaVerbQuery, dbParameters.NazekaTextWithoutLongVowelMarkQuery);
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
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                    break;
                }
            }
        });

        int lookupResultCount = 0;
        foreach (List<LookupResult>? resultSlot in resultSlots)
        {
            if (resultSlot is not null)
            {
                lookupResultCount += resultSlot.Count;
            }
        }

        if (lookupResultCount is 0)
        {
            return null;
        }

        LookupResult[] lookupResults = new LookupResult[lookupResultCount];
        Span<LookupResult> lookupResultsSpan = lookupResults;

        foreach (List<LookupResult>? resultSlot in resultSlots)
        {
            if (resultSlot is not null)
            {
                ReadOnlySpan<LookupResult> resultSlotSpan = resultSlot.AsReadOnlySpan();
                resultSlotSpan.CopyTo(lookupResultsSpan);
                lookupResultsSpan = lookupResultsSpan[resultSlotSpan.Length..];

                ObjectPoolManager.s_lookupResultListPool.Return(resultSlot);
            }
        }

        Array.Sort(lookupResults);
        return lookupResults;
    }

    private static DBParameters GetDBParameters(TextInfo textInfo)
    {
        if (!DictUtils.DBIsUsedForAtLeastOneDict)
        {
            return new DBParameters(null, null, null, null, null, null, null, null, null, null, null);
        }

        List<string>? allTextWithoutLongVowelMark = null;
        string? jmdictWordQuery = null;
        string? jmdictVerbQuery = null;
        string? jmnedictQuery = null;
        string? yomichanWordQuery = null;
        string? yomichanVerbQuery = null;
        string? nazekaWordQuery = null;
        string? nazekaVerbQuery = null;
        string? nazekaTextWithoutLongVowelMarkQuery = null;
        string? yomichanTextWithoutLongVowelMarkQuery = null;
        string? jmdictTextWithoutLongVowelMarkParameter = null;

        int textInHiraganaListCount = textInfo.TextInHiraganaList.Count;

        bool dbIsUsedForJmdict = DictUtils.DBIsUsedForJmdict;
        if (dbIsUsedForJmdict)
        {
            jmdictWordQuery = JmdictDBManager.GetQuery(textInHiraganaListCount);
        }

        if (DictUtils.DBIsUsedForJmnedict)
        {
            jmnedictQuery = JmnedictDBManager.GetQuery(textInHiraganaListCount);
        }

        bool dbIsUsedForAtLeastOneYomichanDict = DictUtils.DBIsUsedForAtLeastOneYomichanDict;
        if (dbIsUsedForAtLeastOneYomichanDict)
        {
            yomichanWordQuery = EpwingYomichanDBManager.GetQuery(textInHiraganaListCount);
        }

        bool dbIsUsedForAtLeastOneNazekaDict = DictUtils.DBIsUsedForAtLeastOneNazekaDict;
        if (dbIsUsedForAtLeastOneNazekaDict)
        {
            nazekaWordQuery = EpwingNazekaDBManager.GetQuery(textInHiraganaListCount);
        }

        if (textInfo.DeconjugatedTexts is not null && textInfo.DeconjugatedTexts.Length > 0)
        {
            int deconjugatedTextsLength = textInfo.DeconjugatedTexts.Length;
            if (dbIsUsedForJmdict)
            {
                jmdictVerbQuery = JmdictDBManager.GetQuery(deconjugatedTextsLength);
            }

            if (dbIsUsedForAtLeastOneYomichanDict)
            {
                yomichanVerbQuery = EpwingYomichanDBManager.GetQuery(deconjugatedTextsLength);
            }

            if (dbIsUsedForAtLeastOneNazekaDict)
            {
                nazekaVerbQuery = EpwingNazekaDBManager.GetQuery(deconjugatedTextsLength);
            }
        }

        if (textInfo.TextWithoutLongVowelMarksList is not null)
        {
            allTextWithoutLongVowelMark = new List<string>(textInfo.TextWithoutLongVowelMarksCount);
            foreach (ref readonly List<string>? textWithoutLongVowelMark in textInfo.TextWithoutLongVowelMarksList.AsReadOnlySpan())
            {
                if (textWithoutLongVowelMark is not null)
                {
                    allTextWithoutLongVowelMark.AddRange(textWithoutLongVowelMark.AsReadOnlySpan());
                }
            }

            if (dbIsUsedForJmdict)
            {
                jmdictTextWithoutLongVowelMarkParameter = JmdictDBManager.GetQuery(textInfo.TextWithoutLongVowelMarksCount);
            }

            if (dbIsUsedForAtLeastOneYomichanDict)
            {
                yomichanTextWithoutLongVowelMarkQuery = EpwingYomichanDBManager.GetQuery(textInfo.TextWithoutLongVowelMarksCount);
            }

            if (dbIsUsedForAtLeastOneNazekaDict)
            {
                nazekaTextWithoutLongVowelMarkQuery = EpwingNazekaDBManager.GetQuery(textInfo.TextWithoutLongVowelMarksCount);
            }
        }

        return new DBParameters(allTextWithoutLongVowelMark, jmdictWordQuery, jmdictVerbQuery, jmnedictQuery, yomichanWordQuery, yomichanVerbQuery, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQuery, yomichanTextWithoutLongVowelMarkQuery, jmdictTextWithoutLongVowelMarkParameter);
    }

    private static void PopulateFreqSqliteConnections(RentedArrayBuffer<SqliteConnection?>? sqliteFreqConnectionsForJmdict, RentedArrayBuffer<SqliteConnection?>? sqliteFreqConnectionsForCustomWordDict, Freq[]? dbWordFreqs)
    {
        bool sqliteFreqConnectionsForJmdictExist = sqliteFreqConnectionsForJmdict is not null;
        bool sqliteFreqConnectionsForCustomWordDictExist = sqliteFreqConnectionsForCustomWordDict is not null;

        if (sqliteFreqConnectionsForJmdictExist || sqliteFreqConnectionsForCustomWordDictExist)
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
            }
        }
    }

    private static TextInfo GetTextInfo(string text, bool dbIsUsedForAtLeastOneWordFreqDict, bool dbIsUsedForPitchDict, Freq[]? dbWordFreqs, Dict? pitchDict)
    {
        int textLength = text.Length;
        List<string> textList = new(textLength);
        List<string> textInHiraganaList = new(textLength);
        List<List<Form>> deconjugationResultsList = new(textLength);
        List<List<string>?>? textWithoutLongVowelMarksList = null;
        List<List<List<Form>>?>? deconjugatedTextWithoutLongVowelMarksList = null;
        int estimatedDeconjugatedTextCapacity = 0;
        int textWithoutLongVowelMarksCount = 0;
        int estimatedDeconjugatedTextWithoutLongVowelMarksListCount = 0;

        bool doesNotStartWithLongVowelMark = text[0] is not 'ー' and not '〜';
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

            List<Form> deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
            estimatedDeconjugatedTextCapacity += deconjugationResults.Count;
            deconjugationResultsList.Add(deconjugationResults);

            if (doesNotStartWithLongVowelMark)
            {
                int longVowelMarkCount = 0;
                if (countLongVowelMark)
                {
                    int firstLongVowelMarkIndex = textInHiragana.IndexOfAny(DictUtils.s_longVowelMarkChars);
                    if (firstLongVowelMarkIndex is not -1)
                    {
                        int lastLongVowelMarkIndex = textInHiragana.LastIndexOfAny(DictUtils.s_longVowelMarkChars);
                        if (firstLongVowelMarkIndex != lastLongVowelMarkIndex)
                        {
                            longVowelMarkCount = 2;
                            for (int j = firstLongVowelMarkIndex + 1; j < lastLongVowelMarkIndex; j++)
                            {
                                char character = textInHiragana[j];
                                if (character is 'ー' or '〜')
                                {
                                    ++longVowelMarkCount;
                                }

                                if (longVowelMarkCount is 4)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            longVowelMarkCount = 1;
                        }
                    }
                }

                if (longVowelMarkCount > 0)
                {
                    textWithoutLongVowelMarksList ??= new List<List<string>?>(textLength);
                    deconjugatedTextWithoutLongVowelMarksList ??= new List<List<List<Form>>?>(textLength);
                    if (longVowelMarkCount < 4)
                    {
                        List<string> textsWithoutLongVowelMarks = JapaneseUtils.NormalizeLongVowelMark(textInHiragana);
                        textWithoutLongVowelMarksCount += textsWithoutLongVowelMarks.Count;
                        textWithoutLongVowelMarksList.Add(textsWithoutLongVowelMarks);

                        List<List<Form>> deconjugatedTextWithoutLongVowelMarks = [];
                        foreach (string textWithoutLongVowelMarks in textsWithoutLongVowelMarks.AsReadOnlySpan())
                        {
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
        }

        string[]? deconjugatedTexts = null;
        if (DictUtils.DBIsUsedForAtLeastOneWordDict || dbIsUsedForPitchDict || dbIsUsedForAtLeastOneWordFreqDict)
        {
            HashSet<string> deconjugatedTextsHashSet = new(Math.Min(estimatedDeconjugatedTextCapacity + estimatedDeconjugatedTextWithoutLongVowelMarksListCount, 256), StringComparer.Ordinal);
            foreach (ref readonly List<Form> deconjugationResults in deconjugationResultsList.AsReadOnlySpan())
            {
                foreach (ref readonly Form form in deconjugationResults.AsReadOnlySpan())
                {
                    _ = deconjugatedTextsHashSet.Add(form.Text);
                }
            }

            if (deconjugatedTextWithoutLongVowelMarksList is not null)
            {
                foreach (ref readonly List<List<Form>>? deconjugatedTextWithoutLongVowelMarks in deconjugatedTextWithoutLongVowelMarksList.AsReadOnlySpan())
                {
                    if (deconjugatedTextWithoutLongVowelMarks is null)
                    {
                        break;
                    }

                    foreach (ref readonly List<Form> forms in deconjugatedTextWithoutLongVowelMarks.AsReadOnlySpan())
                    {
                        foreach (ref readonly Form form in forms.AsReadOnlySpan())
                        {
                            _ = deconjugatedTextsHashSet.Add(form.Text);
                        }
                    }
                }
            }

            deconjugatedTexts = deconjugatedTextsHashSet.ToArray();
        }

        HashSet<string>? allSearchKeys = null;
        bool dbIsUsedForAtLeastOneYomichanOrNazekaWordDict = DictUtils.DBIsUsedForAtLeastOneYomichanOrNazekaWordDict;
        if (dbIsUsedForPitchDict || dbIsUsedForAtLeastOneYomichanOrNazekaWordDict)
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

        Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
        IDictionary<string, IList<IDictRecord>>? pitchAccentDict = null;
        if (allSearchKeys is not null)
        {
            Parallel.Invoke(
            () =>
            {
                frequencyDicts = dbWordFreqs is not null && dbIsUsedForAtLeastOneYomichanOrNazekaWordDict
                    ? GetFrequencyDictsFromDB(dbWordFreqs, allSearchKeys)
                    : null;
            },
            () =>
            {
                if (dbIsUsedForPitchDict)
                {
                    Debug.Assert(pitchDict is not null);
                    pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDict.ReadOnlyConnectionString, allSearchKeys);
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

        return new TextInfo(textList, textInHiraganaList, deconjugationResultsList, deconjugatedTextWithoutLongVowelMarksList, textWithoutLongVowelMarksList, textWithoutLongVowelMarksCount, deconjugatedTexts, frequencyDicts, pitchAccentDict);
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
                new IntermediaryResult(matchedText, dict, tempResult));
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
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? result))
                        {
                            if (result.MatchedText == deconjugationResult.OriginalText)
                            {
                                foreach (IDictRecord record in resultsList)
                                {
                                    int index = result.Results.FastReferenceIndexOf(record);
                                    if (index >= 0)
                                    {
                                        Debug.Assert(result.Processes is not null);
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

                                        Debug.Assert(result.Processes is not null);
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

                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult(matchedText,
                                    dict,
                                    resultsList,
                                    deconjugationResult.Text,
                                    processNodes));
                        }
                    }
                }
            }
        }
    }

    private static void GetWordResults(ReadOnlySpan<string> textList, ReadOnlySpan<string> textInHiraganaList,
        ReadOnlySpan<List<Form>> deconjugationResultsList, string[]? deconjugatedTexts, ReadOnlySpan<List<List<Form>>?> deconjugationResultListForTextWithoutLongVowelMarkList, ReadOnlySpan<List<string>?> textWithoutLongVowelMarkList, ReadOnlySpan<string> allTextWithoutLongVowelMark, Dict dict, bool useDB, Dictionary<string, IntermediaryResult> results, GetRecordsFromDB? getRecordsFromDB, string? query, string? verbQuery, string? textWithoutLongVowelMarkQuery)
    {
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;
        Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = null;

        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            Debug.Assert(query is not null);
            dbWordDict = getRecordsFromDB(dict.ReadOnlyConnectionString, textInHiraganaList, query);

            Debug.Assert(deconjugatedTexts is not null);
            Debug.Assert(verbQuery is not null);
            dbVerbDict = getRecordsFromDB(dict.ReadOnlyConnectionString, deconjugatedTexts, verbQuery);
            if (!allTextWithoutLongVowelMark.IsEmpty)
            {
                Debug.Assert(textWithoutLongVowelMarkQuery is not null);
                dbWordDictForLongVowelConversion = getRecordsFromDB(dict.ReadOnlyConnectionString, allTextWithoutLongVowelMark, textWithoutLongVowelMarkQuery);
            }
        }

        bool textWithoutLongVowelMarkListExist = !textWithoutLongVowelMarkList.IsEmpty;
        for (int i = 0; i < textList.Length; i++)
        {
            ref readonly string text = ref textList[i];
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], text, textInHiraganaList[i], dbWordDict, dbVerbDict);

            ReadOnlySpan<string> textsWithoutLongVowelMark = [];
            ReadOnlySpan<List<Form>> deconjugationResultListForTextWithoutLongVowelMark = [];
            if (textWithoutLongVowelMarkListExist)
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
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
                break;
        }

        return resultsList;
    }

    private static void GetNameResults(ReadOnlySpan<string> textList, ReadOnlySpan<string> textInHiraganaList, Dict dict, bool useDB, Dictionary<string, IntermediaryResult> results, GetRecordsFromDB? getRecordsFromDB, string? query)
    {
        IDictionary<string, IList<IDictRecord>>? nameDict;
        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            Debug.Assert(query is not null);
            nameDict = getRecordsFromDB(dict.ReadOnlyConnectionString, textInHiraganaList, query);
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
                results.Add(textInHiragana,
                    new IntermediaryResult(textList[i], dict, result));
            }
        }
    }

    private static IntermediaryResult? GetKanjiResults(string kanji, Dict dict)
    {
        return dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result)
            ? new IntermediaryResult(kanji, dict, result)
            : null;
    }

    private static IntermediaryResult? GetKanjiResultsFromDB(string kanji, Dict dict, GetKanjiRecordsFromDB getKanjiRecordsFromDB)
    {
        List<IDictRecord>? results = getKanjiRecordsFromDB(dict.ReadOnlyConnectionString, kanji);

        return results is not null && results.Count > 0
            ? new IntermediaryResult(kanji, dict, results)
            : null;
    }

    private static Dictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, RentedArrayBuffer<SqliteConnection?> connections, HashSet<string> searchKeys)
    {
        Dictionary<string, List<FrequencyRecord>>?[] resultsArray = ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Rent(dbFreqs.Length);
        _ = Parallel.For(0, dbFreqs.Length, i =>
        {
            SqliteConnection? connection = connections.Array[i];
            if (connection is not null)
            {
                resultsArray[i] = FreqDBManager.GetRecordsFromDB(connection, searchKeys);
            }
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

        ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Return(resultsArray);

        return result;
    }

    private static Dictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, HashSet<string> searchKeys)
    {
        Dictionary<string, List<FrequencyRecord>>?[] resultsArray = ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Rent(dbFreqs.Length);

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

        ArrayPool<Dictionary<string, List<FrequencyRecord>>?>.Shared.Return(resultsArray);
        return result;
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
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = GetDeconjugationInfo(deconjugatedWord, processesSpan, i);
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
            foreach (IDictRecord dictRecord in intermediaryResult.Results)
            {
                IDictRecordWithMultipleReadings record = (IDictRecordWithMultipleReadings)dictRecord;
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
            foreach (IDictRecord dictRecord in nameResult.Results)
            {
                JmnedictRecord jmnedictRecord = (JmnedictRecord)dictRecord;
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
        foreach (IDictRecord dictRecord in intermediaryResult.Results)
        {
            YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)dictRecord;

            string[]? allReadings = ArrayUtils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);
            LookupResult result = new
            (
                primarySpelling: kanji,
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
        IDictionary<string, IntermediaryResult> epwingResults, List<LookupResult> results, Freq[]? freqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
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
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = GetDeconjugationInfo(deconjugatedWord, processesSpan, i);
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
                    imageInfos: epwingResult.ImageInfos
                );

                results.Add(result);
            }
        }
    }

    private static void BuildEpwingYomichanResultForKanjiWithWordSchema(IntermediaryResult intermediaryResult, List<LookupResult> results, string[]? kanjiCompositions, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        foreach (IDictRecord dictRecords in intermediaryResult.Results)
        {
            EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)dictRecords;
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
                kanjiLookupResult: new KanjiLookupResult(kanjiCompositions)
            );

            results.Add(result);
        }
    }

    private static void BuildEpwingNazekaResult(
        IDictionary<string, IntermediaryResult> epwingNazekaResults, List<LookupResult> results, Freq[]? freqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
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
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = GetDeconjugationInfo(deconjugatedWord, processesSpan, i);
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
        if (DictUtils.WordClassDictionary.TryGetValue(primarySpelling, out IList<JmdictWordClass>? jmdictWcResults))
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
        foreach (IDictRecord dictRecords in intermediaryResult.Results)
        {
            EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)dictRecords;
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
                (string? deconjugationProcess, int minDeconjugationProcessStepCount) = GetDeconjugationInfo(deconjugatedWord, processesSpan, i);
                CustomWordRecord customWordDictResult = (CustomWordRecord)resultsList[i];
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
                    deconjugationProcess: customWordDictResult.HasUserDefinedWordClass ? deconjugationProcess : null,
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
            int freq = 0;
            foreach (IDictRecord dictRecord in customNameResult.Results)
            {
                CustomNameRecord customNameDictResult = (CustomNameRecord)dictRecord;
                string[]? readings = customNameDictResult.Reading is not null ? [customNameDictResult.Reading] : null;
                LookupResult result = new
                (
                    primarySpelling: customNameDictResult.PrimarySpelling,
                    matchedText: customNameResult.MatchedText,
                    dict: customNameResult.Dict,
                    readings: readings,
                    formattedDefinitions: customNameDictResult.BuildFormattedDefinition(),
                    frequencies: [new LookupFrequencyResult(customNameResult.Dict.Name, -freq, false)],
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(customNameDictResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                    imageInfos: customNameDictResult.ImageInfo is not null ? [customNameDictResult.ImageInfo] : null
                );

                ++freq;
                results.Add(result);
            }
        }
    }

    private static List<LookupFrequencyResult>? GetWordFrequencies<T>(T record, Freq[] wordFreqs, Dictionary<string, Dictionary<string, List<FrequencyRecord>>>? freqDictsFromDB) where T : IGetFrequency
    {
        List<LookupFrequencyResult> freqsList = new(wordFreqs.Length);
        bool frequencyExists = false;

        // TODO: Precompute this? Can we make the return type an array if we do this without allocating more?
        foreach (Freq freq in wordFreqs)
        {
            bool useDB = freq.Options.UseDB.Value && freq.Ready;
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

    private static List<LookupFrequencyResult>? GetKanjiFrequencies(string kanji, Freq[] kanjiFreqs)
    {
        List<LookupFrequencyResult> freqsList = new(kanjiFreqs.Length);
        foreach (Freq kanjiFreq in kanjiFreqs)
        {
            bool useDB = kanjiFreq.Options.UseDB.Value && kanjiFreq.Ready;
            IList<FrequencyRecord>? freqResultList;

            if (useDB)
            {
                freqResultList = FreqDBManager.GetRecordsFromDB(kanjiFreq.ReadOnlyConnectionString, kanji);
            }
            else
            {
                _ = kanjiFreq.Contents.TryGetValue(kanji, out freqResultList);
            }

            if (freqResultList is not null)
            {
                int frequency = freqResultList[0].Frequency;
                freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency, false));
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
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    : [new LookupFrequencyResult("KANJIDIC2", frequency, false), .. frequencyResults!];
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
                for (int i = 0; i < readings.Length; i++)
                {
                    byte position = byte.MaxValue;
                    string reading = readings[i];
                    string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                    int recordsCount = records.Count;
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

                    _ = (positions?[i] = position);
                }
            }
            else
            {
                for (int i = 0; i < readings.Length; i++)
                {
                    string reading = readings[i];
                    if (pitchDictionary.TryGetValue(JapaneseUtils.NormalizeText(reading), out records))
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
                                    for (int k = 0; k < i; k++)
                                    {
                                        positions[k] = byte.MaxValue;
                                    }
                                }

                                position = pitchAccentRecord.Position;
                                break;
                            }
                        }

                        _ = (positions?[i] = position);
                    }
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

    private static (string? process, int minStepCount) GetDeconjugationInfo(bool deconjugated, ReadOnlySpan<List<ProcessNode>> processListSpan, int index)
    {
        if (!deconjugated)
        {
            return (null, 0);
        }

        ReadOnlySpan<ProcessNode> processSpan = processListSpan[index].AsReadOnlySpan();
        string? process = LookupResultUtils.DeconjugationProcessesToText(processSpan);
        if (process is null)
        {
            return (null, 0);
        }

        if (processListSpan.Length is 1)
        {
            return (process, processSpan[0].ProperStepCount);
        }

        int min = int.MaxValue;
        foreach (ref readonly ProcessNode node in processSpan)
        {
            if (node.ProperStepCount < min)
            {
                min = node.ProperStepCount;
            }
        }

        return (process, min);
    }
}
