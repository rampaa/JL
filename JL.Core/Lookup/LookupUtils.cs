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
using JL.Core.Dicts.KanjiComposition;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Array;
using JL.Core.Utilities.Database;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Lookup;

public static class LookupUtils
{
    private delegate Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string parameterOrQuery);
    private delegate List<IDictRecord>? GetKanjiRecordsFromDB(string dbName, string term);

    public static LookupResult[]? LookupText(string text)
    {
        bool dbIsUsedForPitchDict = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict)
            && pitchDict is { Active: true, Options.UseDB.Value: true, Ready: true };

        (string kanji, string[]? kanjiCompositions, List<LookupFrequencyResult>? kanjiFrequencyResults) = GetKanjiInfo(text);
        Freq[]? wordFreqs = FreqUtils.WordFreqs;
        Freq[]? dbWordFreqs = GetDBWordFreqs();

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForJmdict = dbWordFreqs is not null && DictUtils.JmdictIsActive
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        using DisposableItemArrayRefStruct<SqliteConnection> sqliteFreqConnectionsForCustomWordDict = dbWordFreqs is not null && DictUtils.AnyCustomWordDictIsActive
            ? new DisposableItemArrayRefStruct<SqliteConnection>(dbWordFreqs.Length)
            : default;

        (SqliteConnection[]? freqConnectionsForJmdict, SqliteConnection[]? freqConnectionsForCustomWordDict) = GetFreqSqliteConnections(sqliteFreqConnectionsForJmdict.Items, sqliteFreqConnectionsForCustomWordDict.Items, dbWordFreqs);

        TextInfo textInfo = GetTextInfo(text, wordFreqs is not null, dbIsUsedForPitchDict, dbWordFreqs, pitchDict);
        DBParameters dbParameters = GetDBParameters(textInfo);

        using SqliteConnection? sqliteConnectionForJmdictPitch = dbIsUsedForPitchDict && DictUtils.JmdictIsActive
            ? DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(pitchDict!.Name))
            : null;

        using SqliteConnection? sqliteConnectionForCustomWordPitch = dbIsUsedForPitchDict && DictUtils.AnyCustomWordDictIsActive
            ? DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(pitchDict!.Name))
            : null;

        ConcurrentBag<LookupResult> lookupResults = [];
        List<Dict> dicts = GetDicts();

        bool dbIsUsedAtLeastForOneDict = DictUtils.DBIsUsedForAtLeastOneDict;
        _ = Parallel.ForEach(dicts, dict =>
        {
            bool useDB = dbIsUsedAtLeastForOneDict && dict.Options.UseDB.Value && dict.Ready;
            switch (dict.Type)
            {
                case DictType.JMdict:
                    Dictionary<string, IntermediaryResult> jmdictResults = GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList, textInfo.DeconjugationResultsList, textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList, textInfo.TextWithoutLongVowelMarksList, dbParameters.AllTextWithoutLongVowelMark, dict, useDB, JmdictDBManager.GetRecordsFromDB, dbParameters.Parameter, dbParameters.VerbParameter, dbParameters.JmdictTextWithoutLongVowelMarkParameter);
                    if (jmdictResults.Count > 0)
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        lookupResults.AddRange(BuildJmdictResult(jmdictResults, wordFreqs, dbWordFreqs, freqConnectionsForJmdict, dbIsUsedForPitchDict, sqliteConnectionForJmdictPitch, pitchDict));
                    }
                    break;

                case DictType.JMnedict:
                    Dictionary<string, IntermediaryResult>? jmnedictResults = GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, JmnedictDBManager.GetRecordsFromDB, dbParameters.Parameter);
                    if (jmnedictResults is not null)
                    {
                        lookupResults.AddRange(BuildJmnedictResult(jmnedictResults, textInfo.PitchAccentDict));
                    }

                    break;

                case DictType.Kanjidic:
                    IntermediaryResult? kanjidicResult = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, KanjidicDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (kanjidicResult is not null)
                    {
                        lookupResults.Add(BuildKanjidicResult(kanji, kanjiCompositions, kanjidicResult, kanjiFrequencyResults, textInfo.PitchAccentDict));
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
                        lookupResults.AddRange(BuildEpwingYomichanResultForKanjiWithWordSchema(kanjiCompositions, epwingYomichanKanjiWithWordSchemaResults, kanjiFrequencyResults, textInfo.PitchAccentDict));
                    }
                    break;

                case DictType.CustomWordDictionary:
                case DictType.ProfileCustomWordDictionary:
                    Dictionary<string, IntermediaryResult> customWordResults = GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList, textInfo.DeconjugationResultsList, textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList, textInfo.TextWithoutLongVowelMarksList, dbParameters.AllTextWithoutLongVowelMark, dict, false, null, dbParameters.Parameter, dbParameters.VerbParameter, null);
                    if (customWordResults.Count > 0)
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        lookupResults.AddRange(BuildCustomWordResult(customWordResults, wordFreqs, dbWordFreqs, freqConnectionsForCustomWordDict, dbIsUsedForPitchDict, sqliteConnectionForCustomWordPitch, pitchDict));
                    }
                    break;

                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomNameDictionary:
                    Dictionary<string, IntermediaryResult>? customNameResults = GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, false, null, dbParameters.Parameter);
                    if (customNameResults is not null)
                    {
                        lookupResults.AddRange(BuildCustomNameResult(customNameResults, textInfo.PitchAccentDict));
                    }
                    break;

                case DictType.NonspecificKanjiYomichan:
                    IntermediaryResult? epwingYomichanKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, YomichanKanjiDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingYomichanKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildYomichanKanjiResult(kanji, kanjiCompositions, epwingYomichanKanjiResults, kanjiFrequencyResults, textInfo.PitchAccentDict));
                    }
                    break;

                case DictType.NonspecificNameYomichan:
                    Dictionary<string, IntermediaryResult>? epwingYomichanNameResults = GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, dbParameters.YomichanWordQuery);
                    if (epwingYomichanNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanNameResults, null, null, textInfo.PitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificYomichan:
                    Dictionary<string, IntermediaryResult> epwingYomichanWordResults = GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList, textInfo.DeconjugationResultsList, textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList, textInfo.TextWithoutLongVowelMarksList, dbParameters.AllTextWithoutLongVowelMark, dict, useDB, EpwingYomichanDBManager.GetRecordsFromDB, dbParameters.YomichanWordQuery, dbParameters.YomichanVerbQuery, dbParameters.YomichanTextWithoutLongVowelMarkQuery);
                    if (epwingYomichanWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(epwingYomichanWordResults, wordFreqs, textInfo.FrequencyDicts, textInfo.PitchAccentDict));
                    }

                    break;

                case DictType.NonspecificKanjiNazeka:
                    IntermediaryResult? epwingNazekaKanjiResults = useDB
                        ? GetKanjiResultsFromDB(kanji, dict, EpwingNazekaDBManager.GetRecordsFromDB)
                        : GetKanjiResults(kanji, dict);

                    if (epwingNazekaKanjiResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResultForKanji(kanjiCompositions, epwingNazekaKanjiResults, kanjiFrequencyResults, textInfo.PitchAccentDict));
                    }

                    break;

                case DictType.NonspecificNameNazeka:
                    Dictionary<string, IntermediaryResult>? epwingNazekaNameResults = GetNameResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList.AsReadOnlySpan(), dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, dbParameters.NazekaWordQuery);
                    if (epwingNazekaNameResults is not null)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaNameResults, null, null, textInfo.PitchAccentDict));
                    }

                    break;

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificNazeka:
                    Dictionary<string, IntermediaryResult> epwingNazekaWordResults = GetWordResults(textInfo.TextList.AsReadOnlySpan(), textInfo.TextInHiraganaList, textInfo.DeconjugationResultsList, textInfo.DeconjugatedTexts, textInfo.DeconjugatedTextWithoutLongVowelMarksList, textInfo.TextWithoutLongVowelMarksList, dbParameters.AllTextWithoutLongVowelMark, dict, useDB, EpwingNazekaDBManager.GetRecordsFromDB, dbParameters.NazekaWordQuery, dbParameters.NazekaVerbQuery, dbParameters.NazekaTextWithoutLongVowelMarkQuery);
                    if (epwingNazekaWordResults.Count > 0)
                    {
                        lookupResults.AddRange(BuildEpwingNazekaResult(epwingNazekaWordResults, wordFreqs, textInfo.FrequencyDicts, textInfo.PitchAccentDict));
                    }
                    break;

                case DictType.PitchAccentYomichan:
                    break;

                default:
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(LookupUtils), nameof(LookupText), dict.Type);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {dict.Type}");
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

    private static List<Dict> GetDicts()
    {
        LookupCategory lookupType = CoreConfigManager.Instance.LookupCategory;
        List<Dict> dicts = new(DictUtils.Dicts.Count);
        if (lookupType is LookupCategory.All)
        {
            foreach (Dict dict in DictUtils.Dicts.Values)
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
                foreach (Dict dict in DictUtils.Dicts.Values)
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
            foreach (Dict dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_nameDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }
        else if (lookupType is LookupCategory.Word)
        {
            foreach (Dict dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_wordDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }
        else // if (lookupType is LookupCategory.Other)
        {
            foreach (Dict dict in DictUtils.Dicts.Values)
            {
                if (dict.Active && DictUtils.s_otherDictTypes.Contains(dict.Type))
                {
                    dicts.Add(dict);
                }
            }
        }

        return dicts;
    }

    private static Freq[]? GetDBWordFreqs()
    {
        Freq[]? wordFreqs = FreqUtils.WordFreqs;
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
                Freq[] dbWordFreqs = new Freq[validFreqCount];
                int currentIndex = 0;
                foreach (Freq freq in wordFreqs)
                {
                    if (freq.Options.UseDB.Value && freq.Ready)
                    {
                        dbWordFreqs[currentIndex] = freq;
                        ++currentIndex;
                    }
                }

                return dbWordFreqs;
            }
        }

        return null;
    }

    private static (string kanji, string[]? kanjiCompositions, List<LookupFrequencyResult>? kanjiFrequencyResults) GetKanjiInfo(string text)
    {
        string kanji = "";
        string[]? kanjiCompositions = null;
        List<LookupFrequencyResult>? kanjiFrequencyResults = null;
        if (DictUtils.AtLeastOneKanjiDictIsActive)
        {
            kanji = TextUtils.GetFirstCharacter(text);
            kanjiCompositions = KanjiCompositionDBManager.GetRecordsFromDB(kanji);

            Freq[]? kanjiFreqs = FreqUtils.KanjiFreqs;
            kanjiFrequencyResults = kanjiFreqs is not null
                ? GetKanjiFrequencies(kanji, kanjiFreqs)
                : null;
        }

        return (kanji, kanjiCompositions, kanjiFrequencyResults);
    }

    private static (SqliteConnection[]? freqConnectionsForJmdict, SqliteConnection[]? freqConnectionsForCustomWordDict) GetFreqSqliteConnections(SqliteConnection[]? sqliteFreqConnectionsForJmdict, SqliteConnection[]? sqliteFreqConnectionsForCustomWordDict, Freq[]? dbWordFreqs)
    {
        bool sqliteFreqConnectionsForJmdictExist = sqliteFreqConnectionsForJmdict is not null;
        bool sqliteFreqConnectionsForCustomWordDictExist = sqliteFreqConnectionsForCustomWordDict is not null;

        SqliteConnection[]? freqConnectionsForJmdict = null;
        SqliteConnection[]? freqConnectionsForCustomWordDict = null;
        if (sqliteFreqConnectionsForJmdictExist || sqliteFreqConnectionsForCustomWordDictExist)
        {
            Debug.Assert(dbWordFreqs is not null);
            if (sqliteFreqConnectionsForJmdictExist)
            {
                Debug.Assert(sqliteFreqConnectionsForJmdict is not null);
                freqConnectionsForJmdict = sqliteFreqConnectionsForJmdict;
            }
            else
            {
                freqConnectionsForJmdict = null;
            }

            if (sqliteFreqConnectionsForCustomWordDictExist)
            {
                Debug.Assert(sqliteFreqConnectionsForCustomWordDict is not null);
                freqConnectionsForCustomWordDict = sqliteFreqConnectionsForCustomWordDict;
            }
            else
            {
                freqConnectionsForCustomWordDict = null;
            }

            int index = 0;
            foreach (Freq dbWordFreq in dbWordFreqs)
            {
                string freqPath = DBUtils.GetFreqDBPath(dbWordFreq.Name);
                if (sqliteFreqConnectionsForJmdictExist)
                {
                    Debug.Assert(freqConnectionsForJmdict is not null);
                    freqConnectionsForJmdict[index] = DBUtils.CreateReadOnlyDBConnection(freqPath);
                }

                if (sqliteFreqConnectionsForCustomWordDictExist)
                {
                    Debug.Assert(freqConnectionsForCustomWordDict is not null);
                    freqConnectionsForCustomWordDict[index] = DBUtils.CreateReadOnlyDBConnection(freqPath);
                }

                ++index;
            }
        }

        return (freqConnectionsForJmdict, freqConnectionsForCustomWordDict);
    }

    private static DBParameters GetDBParameters(TextInfo textInfo)
    {
        List<string>? allTextWithoutLongVowelMark = null;
        string? parameter = null;
        string? verbParameter = null;
        string? yomichanWordQuery = null;
        string? yomichanVerbQuery = null;
        string? nazekaWordQuery = null;
        string? nazekaVerbQuery = null;
        string? nazekaTextWithoutLongVowelMarkQuery = null;
        string? yomichanTextWithoutLongVowelMarkQuery = null;
        string? jmdictTextWithoutLongVowelMarkParameter = null;

        if (DictUtils.DBIsUsedForAtLeastOneDict)
        {
            parameter = DBUtils.GetParameter(textInfo.TextInHiraganaList.Count);
            if (textInfo.DeconjugatedTexts is not null && textInfo.DeconjugatedTexts.Length > 0)
            {
                verbParameter = DBUtils.GetParameter(textInfo.DeconjugatedTexts.Length);
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

                if (DictUtils.DBIsUsedForJmdict)
                {
                    jmdictTextWithoutLongVowelMarkParameter = DBUtils.GetParameter(textInfo.TextWithoutLongVowelMarksCount);
                }
                if (DictUtils.DBIsUsedForAtLeastOneYomichanDict)
                {
                    yomichanTextWithoutLongVowelMarkQuery = EpwingYomichanDBManager.GetQuery(textInfo.TextWithoutLongVowelMarksCount);
                }
                if (DictUtils.DBIsUsedForAtLeastOneNazekaDict)
                {
                    nazekaTextWithoutLongVowelMarkQuery = EpwingNazekaDBManager.GetQuery(textInfo.TextWithoutLongVowelMarksCount);
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

        return new DBParameters(allTextWithoutLongVowelMark, parameter, verbParameter, yomichanWordQuery, yomichanVerbQuery, nazekaWordQuery, nazekaVerbQuery, nazekaTextWithoutLongVowelMarkQuery, yomichanTextWithoutLongVowelMarkQuery, jmdictTextWithoutLongVowelMarkParameter);
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
                    foreach (char hiragana in textInHiragana)
                    {
                        if (hiragana is 'ー' or '〜')
                        {
                            ++longVowelMarkCount;
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

        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts = null;
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
                new IntermediaryResult(matchedText, dict, [tempResult]));
        }

        if (deconjugationResults is not null)
        {
            foreach (ref readonly Form deconjugationResult in deconjugationResults.AsReadOnlySpan())
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
        List<List<Form>> deconjugationResultsList, string[]? deconjugatedTexts, List<List<List<Form>>?>? deconjugationResultListForTextWithoutLongVowelMarkList, List<List<string>?>? textWithoutLongVowelMarkList, List<string>? allTextWithoutLongVowelMark, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB,
        string? queryOrParameter, string? verbQueryOrParameter, string? longVowelQueryOrParameter)
    {
        Dictionary<string, IList<IDictRecord>>? dbWordDict = null;
        Dictionary<string, IList<IDictRecord>>? dbVerbDict = null;
        Dictionary<string, IList<IDictRecord>>? dbWordDictForLongVowelConversion = null;

        if (useDB)
        {
            Debug.Assert(getRecordsFromDB is not null);
            Debug.Assert(queryOrParameter is not null);
            dbWordDict = getRecordsFromDB(dict.Name, textInHiraganaList.AsReadOnlySpan(), queryOrParameter);

            Debug.Assert(verbQueryOrParameter is not null);
            Debug.Assert(deconjugatedTexts is not null);
            dbVerbDict = getRecordsFromDB(dict.Name, deconjugatedTexts.AsReadOnlySpan(), verbQueryOrParameter);

            if (allTextWithoutLongVowelMark is not null)
            {
                Debug.Assert(longVowelQueryOrParameter is not null);
                dbWordDictForLongVowelConversion = getRecordsFromDB(dict.Name, allTextWithoutLongVowelMark.AsReadOnlySpan(), longVowelQueryOrParameter);
            }
        }

        bool textWithoutLongVowelMarkListExist = textWithoutLongVowelMarkList is not null;
        Dictionary<string, IntermediaryResult> results = new(StringComparer.Ordinal);
        for (int i = 0; i < textList.Length; i++)
        {
            ref readonly string text = ref textList[i];
            GetWordResultsHelper(dict, results, deconjugationResultsList[i], text, textInHiraganaList[i], dbWordDict, dbVerbDict);

            List<string>? textsWithoutLongVowelMark = null;
            List<List<Form>>? deconjugationResultListForTextWithoutLongVowelMark = null;
            if (textWithoutLongVowelMarkListExist)
            {
                Debug.Assert(textWithoutLongVowelMarkList is not null);
                textsWithoutLongVowelMark = textWithoutLongVowelMarkList[i];

                Debug.Assert(deconjugationResultListForTextWithoutLongVowelMarkList is not null);
                deconjugationResultListForTextWithoutLongVowelMark = deconjugationResultListForTextWithoutLongVowelMarkList[i];
            }

            if (textsWithoutLongVowelMark is not null)
            {
                ReadOnlySpan<string> textsWithoutLongVowelMarkSpan = textsWithoutLongVowelMark.AsReadOnlySpan();

                Debug.Assert(deconjugationResultListForTextWithoutLongVowelMark is not null);
                ReadOnlySpan<List<Form>> deconjugationResultListForTextWithoutLongVowelMarkSpan = deconjugationResultListForTextWithoutLongVowelMark.AsReadOnlySpan();
                for (int j = 0; j < textsWithoutLongVowelMarkSpan.Length; j++)
                {
                    GetWordResultsHelper(dict, results, deconjugationResultListForTextWithoutLongVowelMarkSpan[j], text, textsWithoutLongVowelMarkSpan[j], dbWordDictForLongVowelConversion, dbVerbDict);
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
                        && dictResult.WordClassesSharedByAllSenses.AsReadOnlySpan().Contains(lastTag))
                    {
                        resultsList.Add(dictResult);
                    }

                    else if (dictResult.WordClasses is not null)
                    {
                        foreach (string[]? wordClasses in dictResult.WordClasses)
                        {
                            if (wordClasses is not null && wordClasses.AsReadOnlySpan().Contains(lastTag))
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
                    if (dictResult.WordClasses.AsReadOnlySpan().Contains(lastTag))
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
                            if (lastTag.AsSpan().StartsWith(wordClass, StringComparison.Ordinal))
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

    private static Dictionary<string, IntermediaryResult>? GetNameResults(ReadOnlySpan<string> textList, ReadOnlySpan<string> textInHiraganaList, Dict dict, bool useDB, GetRecordsFromDB? getRecordsFromDB, string? queryOrParameter)
    {
        IDictionary<string, IList<IDictRecord>>? nameDict;
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

    private static ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> GetFrequencyDictsFromDB(Freq[] dbFreqs, SqliteConnection[] connections, HashSet<string> searchKeys)
    {
        ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>> frequencyDicts = new(-1, dbFreqs.Length, StringComparer.Ordinal);
        _ = Parallel.For(0, dbFreqs.Length, i =>
        {
            Freq freq = dbFreqs[i];
            SqliteConnection connection = connections[i];

            Dictionary<string, List<FrequencyRecord>>? freqRecords = FreqDBManager.GetRecordsFromDB(connection, searchKeys);
            if (freqRecords is not null)
            {
                _ = frequencyDicts.TryAdd(freq.Name, freqRecords);
            }
        });

        return frequencyDicts;
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
        Dictionary<string, IntermediaryResult> jmdictResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, SqliteConnection[]? dbWordFreqConnections, bool dbIsUsedForPitchDict, SqliteConnection? pitchDictConnection, Dict? pitchDict)
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
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(dbWordFreqConnections is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);
            }
        },
        () =>
        {
            if (dbIsUsedForPitchDict)
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                Debug.Assert(pitchDictConnection is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchDictConnection, searchKeys);
            }
            else
            {
                pitchAccentDict = pitchDict?.Contents;
            }

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in jmdictResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
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
            foreach (ref readonly IList<IDictRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
            {
                int dictRecordsCount = dictRecords.Count;
                for (int j = 0; j < dictRecordsCount; j++)
                {
                    IDictRecordWithMultipleReadings record = (IDictRecordWithMultipleReadings)dictRecords[j];
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
            foreach (ref readonly IList<IDictRecord> dictRecords in nameResult.Results.AsReadOnlySpan())
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

    private static LookupResult BuildKanjidicResult(string kanji, string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        KanjidicRecord kanjiRecord = (KanjidicRecord)intermediaryResult.Results[0][0];

        string[]? allReadings = ArrayUtils.ConcatNullableArrays(kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings);

        bool pitchAccentDictExists = pitchAccentDict is not null;
        LookupResult result = new
        (
            primarySpelling: kanji,
            readings: allReadings,
            kanjiLookupResult: new KanjiLookupResult(kanjiCompositions, kanjiRecord.OnReadings, kanjiRecord.KunReadings, kanjiRecord.NanoriReadings, kanjiRecord.RadicalNames, kanjiRecord.StrokeCount, kanjiRecord.Grade),
            frequencies: GetKanjidicFrequencies(kanjiRecord.Frequency, kanjiFrequencyResults),
            matchedText: intermediaryResult.MatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition(),
            pitchPositions: pitchAccentDictExists && allReadings is not null ? GetPitchPosition(kanji, allReadings, pitchAccentDict!) : null
        );

        return result;
    }

    private static List<LookupResult> BuildYomichanKanjiResult(
        string kanji, string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
        {
            int dictRecordsCount = dictRecords.Count;
            for (int j = 0; j < dictRecordsCount; j++)
            {
                YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)dictRecords[j];

                string[]? allReadings = ArrayUtils.ConcatNullableArrays(yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings);
                LookupResult result = new
                (
                    primarySpelling: kanji,
                    readings: allReadings,
                    kanjiLookupResult: new KanjiLookupResult(kanjiCompositions, yomichanKanjiDictResult.OnReadings, yomichanKanjiDictResult.KunReadings, kanjiStats: yomichanKanjiDictResult.BuildFormattedStats()),
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
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
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
                        wordClasses: epwingResult.WordClasses,
                        imagePaths: epwingResult.ImagePaths
                    );

                    results.Add(result);
                }
            }
        }

        return results;
    }

    private static List<LookupResult> BuildEpwingYomichanResultForKanjiWithWordSchema(string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
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
                    kanjiLookupResult: new KanjiLookupResult(kanjiCompositions),
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null,
                    imagePaths: epwingResult.ImagePaths
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
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
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

    private static List<LookupResult> BuildEpwingNazekaResultForKanji(string[]? kanjiCompositions, IntermediaryResult intermediaryResult, List<LookupFrequencyResult>? kanjiFrequencyResults, IDictionary<string, IList<IDictRecord>>? pitchAccentDict)
    {
        bool pitchAccentDictExists = pitchAccentDict is not null;
        List<LookupResult> results = [];

        foreach (ref readonly IList<IDictRecord> dictRecords in intermediaryResult.Results.AsReadOnlySpan())
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
                    kanjiLookupResult: new KanjiLookupResult(kanjiCompositions),
                    formattedDefinitions: epwingResult.BuildFormattedDefinition(intermediaryResult.Dict.Options),
                    pitchPositions: pitchAccentDictExists ? GetPitchPosition(epwingResult.PrimarySpelling, readings, pitchAccentDict!) : null
                );

                results.Add(result);
            }
        }

        return results;
    }

    private static List<LookupResult> BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults, Freq[]? wordFreqs, Freq[]? dbWordFreqs, SqliteConnection[]? dbWordFreqConnections, bool dbIsUsedForPitchDict, SqliteConnection? pitchConnection, Dict? pitchDict)
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
                Debug.Assert(dbWordFreqs is not null);
                Debug.Assert(dbWordFreqConnections is not null);
                Debug.Assert(searchKeys is not null);
                frequencyDicts = GetFrequencyDictsFromDB(dbWordFreqs, dbWordFreqConnections, searchKeys);
            }
        },
        () =>
        {
            if (dbIsUsedForPitchDict)
            {
                Debug.Assert(pitchDict is not null);
                Debug.Assert(searchKeys is not null);
                Debug.Assert(pitchConnection is not null);
                pitchAccentDict = YomichanPitchAccentDBManager.GetRecordsFromDB(pitchConnection, searchKeys);
            }
            else
            {
                pitchAccentDict = pitchDict?.Contents;
            }

            pitchAccentDictExists = pitchAccentDict is not null;
        });

        List<LookupResult> results = [];
        foreach (IntermediaryResult wordResult in customWordResults.Values)
        {
            bool deconjugatedWord = wordResult.Processes is not null;
            ReadOnlySpan<List<List<string>>> processesSpan = wordResult.Processes.AsReadOnlySpan();
            ReadOnlySpan<IList<IDictRecord>> resultsSpan = wordResult.Results.AsReadOnlySpan();
            for (int i = 0; i < resultsSpan.Length; i++)
            {
                string? deconjugationProcess = deconjugatedWord
                    ? LookupResultUtils.DeconjugationProcessesToText(processesSpan[i].AsReadOnlySpan())
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
            foreach (ref readonly IList<IDictRecord> dictRecords in customNameResult.Results.AsReadOnlySpan())
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
