using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using Microsoft.Data.Sqlite;

namespace JL.Core.WordClass;

public static class JmdictWordClassUtils
{
    private static readonly string s_partOfSpeechFilePath = Path.Join(AppInfo.ResourcesPath, "PoS.bin");

    private static readonly byte[] s_recordsQuery = TextUtils.Utf8NoBom.GetBytes($"""
        SELECT {JmdictDBManager.RowId}, {JmdictDBManager.PrimarySpelling}, {JmdictDBManager.Readings}, {JmdictDBManager.PartOfSpeechSharedByAllSenses}, {JmdictDBManager.PartOfSpeech},
               {JmdictDBManager.MiscSharedByAllSenses}, {JmdictDBManager.Misc}, {JmdictDBManager.ReadingsOrthographyInfo},
               {JmdictDBManager.SpellingRestrictions}, {JmdictDBManager.ReadingRestrictions}
        FROM {JmdictDBManager.Record}
        """ + "\0");

    private static readonly byte[] s_searchKeysQuery = TextUtils.Utf8NoBom.GetBytes($"SELECT {JmdictDBManager.RecordId}, {JmdictDBManager.SearchKey} FROM {JmdictDBManager.RecordSearchKey}\0");

    public static async Task<bool> Load()
    {
        FrozenDictionary<string, IList<JmdictWordClass>>? dictionary = await JmdictWordClassBinarySerializer.Deserialize(s_partOfSpeechFilePath).ConfigureAwait(false);
        if (dictionary is not null && dictionary.Count is not 0)
        {
            DictUtils.WordClassDictionary = dictionary;
            return true;
        }

        LoggerManager.Logger.Error("Couldn't load word-class data from '{Path}'", s_partOfSpeechFilePath);
        FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't load word-class data. Check the logs for more details.");
        return false;
    }

    public static bool Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new(StringComparer.Ordinal);
        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        bool useDB = dict.Options.UseDB.Value;
        if (useDB && File.Exists(dict.DBPath))
        {
            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
            if (connection is null)
            {
                FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't generate word-class data. Check the logs for more details.");
                return false;
            }
            PopulateFromDB(jmdictWordClassDictionary, connection);
        }
        else
        {
            PopulateFromJmdictContents(jmdictWordClassDictionary);
        }

        if (jmdictWordClassDictionary.Count is 0)
        {
            LoggerManager.Logger.Error("Couldn't generate word-class data from '{DictName}': no word-class records were found", dict.Name);
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't generate word-class data. Check the logs for more details.");
            return false;
        }

        JmdictWordClassBinarySerializer.Serialize(s_partOfSpeechFilePath, jmdictWordClassDictionary);
        return true;
    }

    private static void PopulateFromJmdictContents(Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary)
    {
        FrozenSet<string> validWordClasses = DeconjugatorUtils.ValidWordClasses;
        HashSet<JmdictRecord> processedKanaRecords = new(ReferenceEqualityComparer.Instance);

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        foreach ((string key, IList<IDictRecord> jmdictRecordList) in dict.Contents)
        {
            if (key.ContainsAny(JapaneseUtils.s_fuseji))
            {
                continue;
            }

            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                JmdictRecord jmdictRecord = (JmdictRecord)jmdictRecordList[i];
                List<string> wordClassList = [];
                if (jmdictRecord.WordClasses is not null)
                {
                    foreach (string[]? wordClassArray in jmdictRecord.WordClasses)
                    {
                        if (wordClassArray is not null)
                        {
                            foreach (string wordClass in wordClassArray)
                            {
                                if (validWordClasses.Contains(wordClass) && !wordClassList.Contains(wordClass))
                                {
                                    wordClassList.Add(wordClass);
                                }
                            }
                        }
                    }
                }

                if (jmdictRecord.WordClassesSharedByAllSenses is not null)
                {
                    foreach (string wordClass in jmdictRecord.WordClassesSharedByAllSenses)
                    {
                        if (validWordClasses.Contains(wordClass) && !wordClassList.Contains(wordClass))
                        {
                            wordClassList.Add(wordClass);
                        }
                    }
                }

                if (wordClassList.Count is 0)
                {
                    continue;
                }

                // The primary spelling's key may have been replaced by a search-only key.
                // Generate kana forms once per record, regardless of which key reaches it.
                if (jmdictRecord.Readings is not null
                    && HasUsuallyKanaSenses(jmdictRecord.MiscSharedByAllSenses, jmdictRecord.Misc)
                    && processedKanaRecords.Add(jmdictRecord))
                {
                    AddKanaWordClasses(jmdictWordClassDictionary, jmdictRecord.PrimarySpelling, jmdictRecord.Readings,
                        jmdictRecord.ReadingsOrthographyInfo, jmdictRecord.WordClassesSharedByAllSenses, jmdictRecord.WordClasses,
                        jmdictRecord.MiscSharedByAllSenses, jmdictRecord.Misc, jmdictRecord.SpellingRestrictions, jmdictRecord.ReadingRestrictions);
                }

                if (jmdictRecord.Readings is not null)
                {
                    bool keyFromReading = false;
                    foreach (string reading in jmdictRecord.Readings)
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                        if (readingInHiragana == key)
                        {
                            keyFromReading = true;
                            break;
                        }
                    }

                    if (keyFromReading && JapaneseUtils.NormalizeText(jmdictRecord.PrimarySpelling) != key)
                    {
                        continue;
                    }
                }

                JmdictWordClass record = new(wordClassList.ToArray(), jmdictRecord.Readings);
                ref List<JmdictWordClass>? results = ref CollectionsMarshal.GetValueRefOrAddDefault(jmdictWordClassDictionary, key, out bool exists);
                if (exists)
                {
                    Debug.Assert(results is not null);
                    if (!results.AsReadOnlySpan().Contains(record))
                    {
                        results.Add(record);
                    }
                }
                else
                {
                    results = [record];
                }
            }
        }
    }

    private static void PopulateFromDB(Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary, SqliteConnection connection)
    {
        FrozenSet<string> validWordClasses = DeconjugatorUtils.ValidWordClasses;

        Dictionary<long, WordClassCandidate> rowIdToWordClassCandidate = [];
        const int rowIdColumnIndex = 0;
        const int primarySpellingColumnIndex = 1;
        const int readingsColumnIndex = 2;
        const int wordClassesSharedByAllSensesColumnIndex = 3;
        const int wordClassesColumnIndex = 4;
        const int miscSharedByAllSensesColumnIndex = 5;
        const int miscColumnIndex = 6;
        const int readingsOrthographyInfoColumnIndex = 7;
        const int spellingRestrictionsColumnIndex = 8;
        const int readingRestrictionsColumnIndex = 9;

        using (SqliteRecordReader dataReader = new(connection, s_recordsQuery))
        {
            while (dataReader.Read())
            {
                string[]? wordClassesSharedByAllSenses = dataReader.DeserializeNullable<string[]>(wordClassesSharedByAllSensesColumnIndex);
                string[]?[]? wordClasses = dataReader.DeserializeNullable<string[]?[]>(wordClassesColumnIndex);
                if (wordClasses is null && wordClassesSharedByAllSenses is null)
                {
                    continue;
                }

                List<string> wordClassList = [];
                if (wordClasses is not null)
                {
                    foreach (string[]? wordClassArray in wordClasses)
                    {
                        if (wordClassArray is not null)
                        {
                            foreach (string wordClass in wordClassArray)
                            {
                                if (validWordClasses.TryGetValue(wordClass, out string? internedWordClass) && !wordClassList.Contains(internedWordClass))
                                {
                                    wordClassList.Add(internedWordClass);
                                }
                            }
                        }
                    }
                }

                if (wordClassesSharedByAllSenses is not null)
                {
                    foreach (string wordClass in wordClassesSharedByAllSenses)
                    {
                        if (validWordClasses.TryGetValue(wordClass, out string? internedWordClass) && !wordClassList.Contains(internedWordClass))
                        {
                            wordClassList.Add(internedWordClass);
                        }
                    }
                }

                if (wordClassList.Count is 0)
                {
                    continue;
                }

                long rowId = dataReader.GetInt64(rowIdColumnIndex);
                string[]? readings = dataReader.DeserializeNullable<string[]>(readingsColumnIndex);
                string? normalizedPrimarySpelling = null;
                string[]? normalizedReadings = null;
                if (readings is not null)
                {
                    string primarySpelling = dataReader.GetString(primarySpellingColumnIndex);
                    string[]? miscSharedByAllSenses = dataReader.DeserializeNullable<string[]>(miscSharedByAllSensesColumnIndex);
                    string[]?[]? misc = dataReader.DeserializeNullable<string[]?[]>(miscColumnIndex);
                    if (HasUsuallyKanaSenses(miscSharedByAllSenses, misc))
                    {
                        AddKanaWordClasses(jmdictWordClassDictionary, primarySpelling, readings,
                            dataReader.DeserializeNullable<string[]?[]>(readingsOrthographyInfoColumnIndex),
                            wordClassesSharedByAllSenses, wordClasses, miscSharedByAllSenses, misc,
                            dataReader.DeserializeNullable<string[]?[]>(spellingRestrictionsColumnIndex),
                            dataReader.DeserializeNullable<string[]?[]>(readingRestrictionsColumnIndex));
                    }

                    normalizedPrimarySpelling = JapaneseUtils.NormalizeText(primarySpelling);
                    normalizedReadings = new string[readings.Length];
                    for (int i = 0; i < readings.Length; i++)
                    {
                        normalizedReadings[i] = JapaneseUtils.NormalizeText(readings[i]);
                    }
                }

                rowIdToWordClassCandidate[rowId] = new WordClassCandidate(
                    normalizedPrimarySpelling,
                    readings,
                    normalizedReadings,
                    wordClassList.ToArray());
            }
        }

        if (rowIdToWordClassCandidate.Count is 0)
        {
            return;
        }

        _ = jmdictWordClassDictionary.EnsureCapacity(rowIdToWordClassCandidate.Count);

        const int recordIdColumnIndex = 0;
        const int searchKeyColumnIndex = 1;

        using SqliteRecordReader searchKeyReader = new(connection, s_searchKeysQuery);
        while (searchKeyReader.Read())
        {
            long recordId = searchKeyReader.GetInt64(recordIdColumnIndex);
            if (!rowIdToWordClassCandidate.TryGetValue(recordId, out WordClassCandidate? data))
            {
                continue;
            }

            ReadOnlySpan<char> keySpan = searchKeyReader.GetStringSpan(searchKeyColumnIndex);
            if (keySpan.ContainsAny(JapaneseUtils.s_fuseji))
            {
                continue;
            }

            bool keyFromReading = false;
            if (data.NormalizedReadings is not null)
            {
                foreach (string normalizedReading in data.NormalizedReadings)
                {
                    if (keySpan.SequenceEqual(normalizedReading))
                    {
                        keyFromReading = true;
                        break;
                    }
                }

                if (keyFromReading && !keySpan.SequenceEqual(data.NormalizedPrimarySpelling))
                {
                    continue;
                }
            }

            string key = keySpan.ToString();
            JmdictWordClass record = new(data.WordClasses, data.Readings);
            ref List<JmdictWordClass>? results = ref CollectionsMarshal.GetValueRefOrAddDefault(jmdictWordClassDictionary, key, out bool exists);
            if (exists)
            {
                Debug.Assert(results is not null);
                if (!results.AsReadOnlySpan().Contains(record))
                {
                    results.Add(record);
                }
            }
            else
            {
                results = [record];
            }
        }
    }

    private static bool HasUsuallyKanaSenses(string[]? miscSharedByAllSenses, string[]?[]? misc)
    {
        if (miscSharedByAllSenses is not null && miscSharedByAllSenses.Contains("uk"))
        {
            return true;
        }

        if (misc is not null)
        {
            foreach (string[]? senseMisc in misc)
            {
                if (senseMisc is not null && senseMisc.Contains("uk"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AddKanaWordClasses(Dictionary<string, List<JmdictWordClass>> dictionary, string primarySpelling,
        string[] readings, string[]?[]? readingsOrthographyInfo, string[]? wordClassesSharedByAllSenses, string[]?[]? wordClasses,
        string[]? miscSharedByAllSenses, string[]?[]? misc, string[]?[]? spellingRestrictions, string[]?[]? readingRestrictions)
    {
        bool allSensesUsuallyUseKana = miscSharedByAllSenses is not null && miscSharedByAllSenses.Contains("uk");
        int senseCount = wordClasses?.Length ?? misc?.Length ?? spellingRestrictions?.Length ?? readingRestrictions?.Length ?? 1;
        for (int readingIndex = 0; readingIndex < readings.Length; readingIndex++)
        {
            string[]? readingInfo = readingsOrthographyInfo?[readingIndex];
            bool excludedReading = false;
            if (readingInfo is not null)
            {
                foreach (string tag in readingInfo)
                {
                    if (tag is "rk" or "ik" or "ok" or "sk")
                    {
                        excludedReading = true;
                        break;
                    }
                }
            }

            if (excludedReading)
            {
                continue;
            }

            string reading = readings[readingIndex];
            List<string>? kanaWordClasses = null;
            for (int senseIndex = 0; senseIndex < senseCount; senseIndex++)
            {
                if (!allSensesUsuallyUseKana && misc?[senseIndex]?.Contains("uk") is not true)
                {
                    continue;
                }

                string[]? senseSpellings = spellingRestrictions?[senseIndex];
                string[]? senseReadings = readingRestrictions?[senseIndex];
                if ((senseSpellings is not null && !senseSpellings.Contains(primarySpelling))
                    || (senseReadings is not null && !senseReadings.Contains(reading)))
                {
                    continue;
                }

                if (kanaWordClasses is null)
                {
                    kanaWordClasses = [];
                    AddWordClasses(kanaWordClasses, wordClassesSharedByAllSenses);
                }
                AddWordClasses(kanaWordClasses, wordClasses?[senseIndex]);
            }

            if (kanaWordClasses is not { Count: > 0 })
            {
                continue;
            }

            string key = JapaneseUtils.NormalizeText(reading);
            JmdictWordClass record = new(kanaWordClasses.ToArray());
            ref List<JmdictWordClass>? results = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);
            if (exists)
            {
                Debug.Assert(results is not null);
                if (!results.AsReadOnlySpan().Contains(record))
                {
                    results.Add(record);
                }
            }
            else
            {
                results = [record];
            }
        }
    }

    private static void AddWordClasses(List<string> results, ReadOnlySpan<string> wordClasses)
    {
        FrozenSet<string> validWordClasses = DeconjugatorUtils.ValidWordClasses;
        foreach (string wordClass in wordClasses)
        {
            if (validWordClasses.TryGetValue(wordClass, out string? pooledWordClass) && !results.Contains(pooledWordClass))
            {
                results.Add(pooledWordClass);
            }
        }
    }

    internal static async Task Initialize()
    {
        Dict jmdictDict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        string fullJmdictPath = Path.GetFullPath(jmdictDict.Path, AppInfo.ApplicationPath);

        if (File.Exists(s_partOfSpeechFilePath)
            && (!File.Exists(fullJmdictPath) || File.GetLastWriteTime(fullJmdictPath) <= File.GetLastWriteTime(s_partOfSpeechFilePath)))
        {
            FrozenDictionary<string, IList<JmdictWordClass>>? dictionary = await JmdictWordClassBinarySerializer.Deserialize(s_partOfSpeechFilePath).ConfigureAwait(false);
            if (dictionary is not null && dictionary.Count is not 0)
            {
                DictUtils.WordClassDictionary = dictionary;
                return;
            }
        }

        bool serialized = false;
        if (jmdictDict.Active || (jmdictDict.Options.UseDB.Value && File.Exists(jmdictDict.DBPath)))
        {
            serialized = Serialize();
        }
        else
        {
            bool deleteJmdictFile = false;
            bool deleteDB = !File.Exists(jmdictDict.DBPath);
            if (!File.Exists(fullJmdictPath))
            {
                deleteJmdictFile = true;

                Uri? uri = jmdictDict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullJmdictPath, uri, jmdictDict.Type.ToString(), false, true).ConfigureAwait(false);
                if (!downloaded)
                {
                    return;
                }
            }

            bool useDB = jmdictDict.Options.UseDB.Value;
            try
            {
                if (useDB)
                {
                    JmdictDBManager.CreateDB(jmdictDict.DBPath);
                    await JmdictDBManager.ImportFromDisk(jmdictDict, jmdictDict.DBPath, DictUtils.JmdictEntities).ConfigureAwait(false);
                }
                else
                {
                    jmdictDict.Contents = new Dictionary<string, IList<IDictRecord>>(jmdictDict.Size > 0 ? jmdictDict.Size : JmdictLoader.Size, StringComparer.Ordinal);
                    await JmdictLoader.Load(jmdictDict, DictUtils.JmdictEntities).ConfigureAwait(false);
                }

                serialized = Serialize();
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", jmdictDict.Type.GetDescription(), jmdictDict.Name, fullJmdictPath);
                FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Couldn't import {jmdictDict.Name}. Check the logs for more details.");
            }
            finally
            {
                jmdictDict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                if (deleteJmdictFile)
                {
                    if (File.Exists(fullJmdictPath))
                    {
                        File.Delete(fullJmdictPath);
                    }
                }
                if (deleteDB)
                {
                    if (File.Exists(jmdictDict.DBPath))
                    {
                        DBUtils.DeleteDB(jmdictDict.DBPath);
                    }
                }
            }
        }

        if (serialized)
        {
            _ = await Load().ConfigureAwait(false);
        }
    }
}
