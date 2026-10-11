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

    private static readonly byte[] s_searchKeysQuery = TextUtils.Utf8NoBom.GetBytes($"SELECT {JmdictDBManager.RecordId}, {JmdictDBManager.SearchKey} FROM {JmdictDBManager.RecordSearchKey}\0");

    public static async Task<bool> Load()
    {
        FrozenDictionary<string, IList<JmdictWordClass>>? dictionary = await JmdictWordClassBinarySerializer.Deserialize(s_partOfSpeechFilePath).ConfigureAwait(false);
        if (dictionary is not null && dictionary.Count is not 0)
        {
            DictUtils.WordClassDictionary = dictionary;
            return true;
        }
        else
        {
            LoggerManager.Logger.Error("Couldn't load word-class data from '{Path}'", s_partOfSpeechFilePath);
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't load word-class data. Check the logs for more details.");
            return false;
        }
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

                string[] wordClasses = wordClassList.ToArray();

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

                    if (keyFromReading)
                    {
                        if (JapaneseUtils.NormalizeText(jmdictRecord.PrimarySpelling) != key)
                        {
                            continue;
                        }

                        if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? prevResults))
                        {
                            bool alreadyAdded = false;
                            foreach (JmdictWordClass wordClass in prevResults.AsReadOnlySpan())
                            {
                                if (wordClass.Spelling == jmdictRecord.PrimarySpelling
                                    && wordClass.Readings.SequenceEqual(jmdictRecord.Readings)
                                    && wordClass.WordClasses.SequenceEqual(wordClasses))
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }

                            if (alreadyAdded)
                            {
                                continue;
                            }
                        }
                    }
                }

                JmdictWordClass record = new(jmdictRecord.PrimarySpelling.GetPooledString(), wordClasses, jmdictRecord.Readings);
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
        using SqliteCommand recordCommand = connection.CreateCommand();
        recordCommand.CommandText =
            $"""
            SELECT {JmdictDBManager.RowId}, {JmdictDBManager.PrimarySpelling}, {JmdictDBManager.Readings}, {JmdictDBManager.PartOfSpeechSharedByAllSenses}, {JmdictDBManager.PartOfSpeech}
            FROM {JmdictDBManager.Record}
            """;

        const int rowIdColumnIndex = 0;
        const int primarySpellingColumnIndex = 1;
        const int readingsColumnIndex = 2;
        const int wordClassesSharedByAllSensesColumnIndex = 3;
        const int wordClassesColumnIndex = 4;

        using SqliteDataReader dataReader = recordCommand.ExecuteReader();
        while (dataReader.Read())
        {
            string[]? wordClassesSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>(wordClassesSharedByAllSensesColumnIndex);
            string[]?[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]?[]>(wordClassesColumnIndex);
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
            string primarySpelling = dataReader.GetString(primarySpellingColumnIndex);
            string[]? readings = dataReader.GetNullableValueFromBlobStream<string[]>(readingsColumnIndex);

            string normalizedPrimarySpelling = JapaneseUtils.NormalizeText(primarySpelling);
            string[]? normalizedReadings;
            if (readings is not null)
            {
                normalizedReadings = new string[readings.Length];
                for (int i = 0; i < readings.Length; i++)
                {
                    normalizedReadings[i] = JapaneseUtils.NormalizeText(readings[i]);
                }
            }
            else
            {
                normalizedReadings = null;
            }

            rowIdToWordClassCandidate[rowId] = new WordClassCandidate(
                primarySpelling,
                normalizedPrimarySpelling,
                readings,
                normalizedReadings,
                wordClassList.ToArray());
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
            if (keyFromReading && jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? prevResults))
            {
                bool alreadyAdded = false;
                foreach (JmdictWordClass wordClass in prevResults.AsReadOnlySpan())
                {
                    if (wordClass.Spelling == data.PrimarySpelling
                        && wordClass.Readings.SequenceEqual(data.Readings)
                        && wordClass.WordClasses.SequenceEqual(data.WordClasses))
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                {
                    continue;
                }
            }

            JmdictWordClass record = new(data.PrimarySpelling.GetPooledString(), data.WordClasses, data.Readings);
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
