using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
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
    private static readonly string s_partOfSpeechFilePath = Path.Join(AppInfo.ResourcesPath, "PoS.json");

    public static async Task Load()
    {
        if (!File.Exists(s_partOfSpeechFilePath))
        {
            return;
        }

        FileStream fileStream = new(s_partOfSpeechFilePath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            Dictionary<string, IList<JmdictWordClass>>? wordClassDictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, IList<JmdictWordClass>>>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            Debug.Assert(wordClassDictionary is not null);
            DictUtils.WordClassDictionary = wordClassDictionary;
        }

        IList<JmdictWordClass>[] jmdictWordClasses = DictUtils.WordClassDictionary.Values.ToArray();

        Debug.Assert(DictUtils.WordClassDictionary is Dictionary<string, IList<JmdictWordClass>>);
        Dictionary<string, IList<JmdictWordClass>> dictionary = (Dictionary<string, IList<JmdictWordClass>>)DictUtils.WordClassDictionary;
        foreach (IList<JmdictWordClass> jmdictWordClassList in jmdictWordClasses)
        {
            int jmdictWordClassListCount = jmdictWordClassList.Count;
            for (int j = 0; j < jmdictWordClassListCount; j++)
            {
                JmdictWordClass jmdictWordClass = jmdictWordClassList[j];

                jmdictWordClass.Readings?.DeduplicateStringsInArray();
                jmdictWordClass.WordClasses.DeduplicateStringsInArray();

                if (jmdictWordClass.Readings is not null)
                {
                    foreach (string reading in jmdictWordClass.Readings)
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
                        ref IList<JmdictWordClass>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, readingInHiragana, out bool exists);
                        if (exists)
                        {
                            Debug.Assert(result is not null);
                            result.Add(jmdictWordClass);
                        }
                        else
                        {
                            result = [jmdictWordClass];
                        }
                    }
                }
            }
        }

        DictUtils.WordClassDictionary = DictUtils.WordClassDictionary.ToFrozenDictionary(static entry => entry.Key, static IList<JmdictWordClass> (kvp) => kvp.Value.ToArray(), StringComparer.Ordinal);
    }

    public static async Task Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new(StringComparer.Ordinal);
        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        bool useDB = dict.Options.UseDB.Value;
        if (useDB && File.Exists(dict.DBPath))
        {
            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
            Debug.Assert(connection is not null);
            PopulateFromDB(jmdictWordClassDictionary, connection);
        }
        else
        {
            PopulateFromJmdictContents(jmdictWordClassDictionary);
        }

        string tempPartOfSpeechFilePath = PathUtils.GetTempPath(s_partOfSpeechFilePath);
        FileStream fileStream = new(tempPartOfSpeechFilePath, FileStreamOptionsPresets.s_asyncCreate64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, jmdictWordClassDictionary, JsonOptions.s_jsoIgnoringWhenWritingNull).ConfigureAwait(false);
        }

        PathUtils.ReplaceFileAtomicallyOnSameVolume(s_partOfSpeechFilePath, tempPartOfSpeechFilePath);
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

                JmdictWordClass record = new(jmdictRecord.PrimarySpelling, wordClasses, jmdictRecord.Readings);
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
            LoggerManager.Logger.Error("PopulateFromDB failed unexpectedly, rowIdToRecord.Count is 0!");
            return;
        }

        _ = jmdictWordClassDictionary.EnsureCapacity(rowIdToWordClassCandidate.Count);

        using SqliteCommand searchKeyCommand = connection.CreateCommand();
        searchKeyCommand.CommandText = $"SELECT {JmdictDBManager.RecordId}, {JmdictDBManager.SearchKey} FROM {JmdictDBManager.RecordSearchKey}";
        const int recordIdColumnIndex = 0;
        const int searchKeyColumnIndex = 1;

        using SqliteDataReader searchKeyReader = searchKeyCommand.ExecuteReader();
        while (searchKeyReader.Read())
        {
            long recordId = searchKeyReader.GetInt64(recordIdColumnIndex);
            if (!rowIdToWordClassCandidate.TryGetValue(recordId, out WordClassCandidate? data))
            {
                continue;
            }

            string key = searchKeyReader.GetString(searchKeyColumnIndex);
            if (key.ContainsAny(JapaneseUtils.s_fuseji))
            {
                continue;
            }

            if (data.NormalizedReadings is not null)
            {
                bool keyFromReading = false;
                foreach (string normalizedReading in data.NormalizedReadings)
                {
                    if (normalizedReading == key)
                    {
                        keyFromReading = true;
                        break;
                    }
                }

                if (keyFromReading)
                {
                    if (data.NormalizedPrimarySpelling != key)
                    {
                        continue;
                    }

                    if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? prevResults))
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
                }
            }

            JmdictWordClass record = new(data.PrimarySpelling, data.WordClasses, data.Readings);
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

        if (!File.Exists(s_partOfSpeechFilePath)
            || (File.Exists(fullJmdictPath) && File.GetLastWriteTime(fullJmdictPath) > File.GetLastWriteTime(s_partOfSpeechFilePath)))
        {
            if (jmdictDict.Active)
            {
                await Serialize().ConfigureAwait(false);
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
                        await JmdictDBManager.ImportFromDisk(jmdictDict).ConfigureAwait(false);
                    }
                    else
                    {
                        jmdictDict.Contents = new Dictionary<string, IList<IDictRecord>>(jmdictDict.Size > 0 ? jmdictDict.Size : JmdictLoader.Size, StringComparer.Ordinal);
                        await JmdictLoader.Load(jmdictDict).ConfigureAwait(false);
                    }

                    await Serialize().ConfigureAwait(false);
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
                            File.Delete(jmdictDict.DBPath);
                        }
                    }
                }
            }
        }

        await Load().ConfigureAwait(false);
    }
}
