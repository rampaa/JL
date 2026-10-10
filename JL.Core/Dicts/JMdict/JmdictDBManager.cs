using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictDBManager
{
    public const int Version = 26;
    private const int ImportRecordBatchSize = 128;
    private const int VariantSearchKeyTransactionBatchSize = 20_000_000;

    private static readonly ConcurrentDictionary<int, byte[]> s_queryCache = [];

    public const string Record = "record";
    public const string RowId = "rowid";
    internal const string EdictId = "edict_id";
    public const string PrimarySpelling = "primary_spelling";
    internal const string PrimarySpellingOrthographyInfo = "primary_spelling_orthography_info";
    internal const string SpellingRestrictions = "spelling_restrictions";
    internal const string AlternativeSpellings = "alternative_spellings";
    internal const string AlternativeSpellingsOrthographyInfo = "alternative_spellings_orthography_info";
    public const string Readings = "readings";
    internal const string ReadingsOrthographyInfo = "readings_orthography_info";
    internal const string ReadingRestrictions = "reading_restrictions";
    internal const string Glossary = "glossary";
    internal const string GlossaryInfo = "glossary_info";
    public const string PartOfSpeechSharedByAllSenses = "part_of_speech_shared_by_all_senses";
    public const string PartOfSpeech = "part_of_speech";
    internal const string FieldsSharedByAllSenses = "fields_shared_by_all_senses";
    internal const string Fields = "fields";
    internal const string MiscSharedByAllSenses = "misc_shared_by_all_senses";
    internal const string Misc = "misc";
    internal const string DialectsSharedByAllSenses = "dialects_shared_by_all_senses";
    internal const string Dialects = "dialects";
    internal const string LoanwordEtymology = "loanword_etymology";
    internal const string CrossReferences = "cross_references";
    internal const string Info = "info";

    public const string RecordSearchKey = "record_search_key";
    public const string SearchKey = "search_key";
    public const string RecordId = "record_id";

    private static readonly byte[] s_distinctSearchKeyCountQuery = TextUtils.Utf8NoBom.GetBytes(
        $"""
        SELECT COUNT(DISTINCT {SearchKey})
        FROM {RecordSearchKey};{"\0"}
        """);

    private static readonly byte[] s_maxSearchKeyLengthQuery = TextUtils.Utf8NoBom.GetBytes(
        $"""
        SELECT MAX(LENGTH(CAST({SearchKey} AS BLOB)) / 2)
        FROM {RecordSearchKey};{"\0"}
        """);

    private static byte[] GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out byte[]? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{EdictId},
                   r.{PrimarySpelling},
                   r.{PrimarySpellingOrthographyInfo},
                   r.{SpellingRestrictions},
                   r.{AlternativeSpellings},
                   r.{AlternativeSpellingsOrthographyInfo},
                   r.{Readings},
                   r.{ReadingsOrthographyInfo},
                   r.{ReadingRestrictions},
                   r.{Glossary},
                   r.{GlossaryInfo},
                   r.{PartOfSpeechSharedByAllSenses},
                   r.{PartOfSpeech},
                   r.{FieldsSharedByAllSenses},
                   r.{Fields},
                   r.{MiscSharedByAllSenses},
                   r.{Misc},
                   r.{DialectsSharedByAllSenses},
                   r.{Dialects},
                   r.{LoanwordEtymology},
                   r.{CrossReferences},
                   r.{Info},
                   rsk.{SearchKey}
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            WHERE rsk.{SearchKey} IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(',').Append(DBUtils.GetParameterName(i + 1));
        }

        string queryText = queryBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(queryBuilder);
        query = GC.AllocateUninitializedArray<byte>(TextUtils.Utf8NoBom.GetByteCount(queryText) + 1);
        _ = TextUtils.Utf8NoBom.GetBytes(queryText, query);
        query[^1] = 0;
        _ = s_queryCache.TryAdd(termCount, query);
        return query;
    }

    private enum ColumnIndex
    {
        EdictId = 0,
        PrimarySpelling,
        PrimarySpellingOrthographyInfo,
        SpellingRestrictions,
        AlternativeSpellings,
        AlternativeSpellingsOrthographyInfo,
        Readings,
        ReadingsOrthographyInfo,
        ReadingRestrictions,
        Glossary,
        GlossaryInfo,
        WordClassesSharedByAllSenses,
        WordClasses,
        FieldsSharedByAllSenses,
        Fields,
        MiscSharedByAllSenses,
        Misc,
        DialectsSharedByAllSenses,
        Dialects,
        LoanwordEtymology,
        CrossReferences,
        Info,
        SearchKey
    }

    public static void CreateDB(string dbPath)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(dbPath);

        DBUtils.SetEncodingToUtf16LE(connection);
        DBUtils.SetPageSizeTo64K(connection);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            CREATE TABLE IF NOT EXISTS {Record}
            (
                {RowId} INTEGER NOT NULL PRIMARY KEY,
                {EdictId} INTEGER NOT NULL,
                {PrimarySpelling} TEXT NOT NULL,
                {PrimarySpellingOrthographyInfo} BLOB,
                {Readings} BLOB,
                {AlternativeSpellings} BLOB,
                {AlternativeSpellingsOrthographyInfo} BLOB,
                {ReadingsOrthographyInfo} BLOB,
                {ReadingRestrictions} BLOB,
                {Glossary} BLOB NOT NULL,
                {GlossaryInfo} BLOB,
                {PartOfSpeechSharedByAllSenses} BLOB,
                {PartOfSpeech} BLOB,
                {SpellingRestrictions} BLOB,
                {FieldsSharedByAllSenses} BLOB,
                {Fields} BLOB,
                {MiscSharedByAllSenses} BLOB,
                {Misc} BLOB,
                {DialectsSharedByAllSenses} BLOB,
                {Dialects} BLOB,
                {LoanwordEtymology} BLOB,
                {CrossReferences} BLOB,
                {Info} BLOB
            ) STRICT;

            CREATE TABLE IF NOT EXISTS {RecordSearchKey}
            (
                {SearchKey} TEXT NOT NULL,
                {RecordId} INTEGER NOT NULL,
                PRIMARY KEY ({SearchKey}, {RecordId}),
                FOREIGN KEY ({RecordId}) REFERENCES {Record} ({RowId}) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static async Task ImportFromDisk(Dict dict, string dbPath, Dictionary<string, string> entities)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            entities.Clear();

            ProperNameEntriesOption? properNamesEntriesOption = dict.Options.ProperNameEntries;
            Debug.Assert(properNamesEntriesOption is not null);
            bool includeProperNames = properNamesEntriesOption.Value;

            GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
            Debug.Assert(generateMazegakiOption is not null);
            bool generateMazegaki = generateMazegakiOption.Value;

            GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
            Debug.Assert(generateFusejiVariantsOption is not null);
            bool generateFusejiVariants = generateFusejiVariantsOption.Value;

            int maxSearchKeyLengthForFusejiGeneration;
            int maxTotalFuseji;
            if (generateFusejiVariants)
            {
                Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
                maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

                Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
                maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;
            }
            else
            {
                maxSearchKeyLengthForFusejiGeneration = 0;
                maxTotalFuseji = 0;
            }

            // ReSharper disable once UseAwaitUsing
            using FileStream? sourceFileLock = generateFusejiVariants || generateMazegaki
                ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                : null;

            long rowId = 1;

            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dbPath);
            Debug.Assert(connection is not null);

            DBUtils.ConfigureForBulkWrite(connection);

#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            using JmdictRecordInserter recordInserter = new(connection);

            Dictionary<JmdictRecord, List<string>> recordsToKeys = [];
            int transactionRecordCount = 0;
            Channel<Dictionary<string, JmdictRecord>[]> availableBatches = Channel.CreateUnbounded<Dictionary<string, JmdictRecord>[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
            Channel<(Dictionary<string, JmdictRecord>[] Records, int Count)> readyBatches = Channel.CreateUnbounded<(Dictionary<string, JmdictRecord>[] Records, int Count)>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            _ = availableBatches.Writer.TryWrite(new Dictionary<string, JmdictRecord>[ImportRecordBatchSize]);
            _ = availableBatches.Writer.TryWrite(new Dictionary<string, JmdictRecord>[ImportRecordBatchSize]);

            Task producer = Task.Run(() => CreateImportRecordBatches(fullPath, includeProperNames, availableBatches.Reader, readyBatches.Writer, entities));
            try
            {
                await foreach ((Dictionary<string, JmdictRecord>[] batch, int count) in readyBatches.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    for (int i = 0; i < count; i++)
                    {
                        Dictionary<string, JmdictRecord> recordDictionary = batch[i];
                        foreach ((string key, JmdictRecord record) in recordDictionary)
                        {
                            ref List<string>? keys = ref CollectionsMarshal.GetValueRefOrAddDefault(recordsToKeys, record, out bool exists);
                            if (exists)
                            {
                                Debug.Assert(keys is not null);
                                keys.Add(key);
                            }
                            else
                            {
                                keys = [key];
                            }
                        }

                        foreach ((JmdictRecord record, List<string> keys) in recordsToKeys)
                        {
                            recordInserter.InsertRecord(rowId, record);
                            recordInserter.InsertSearchKeys(rowId, CollectionsMarshal.AsSpan(keys));

                            transactionRecordCount += keys.Count;
                            if (transactionRecordCount > DBUtils.TransactionBatchSize)
                            {
#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

#pragma warning disable CA1849 // Call async methods when in an async method
                                // ReSharper disable once MethodHasAsyncOverload
                                transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

                                dict.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                                transactionRecordCount = 0;
                            }

                            ++rowId;
                        }

                        recordsToKeys.Clear();
                    }

                    Array.Clear(batch, 0, count);
                    _ = availableBatches.Writer.TryWrite(batch);
                }
            }
            catch (Exception exception)
            {
                _ = availableBatches.Writer.TryComplete(exception);
                throw;
            }
            finally
            {
                await producer.ConfigureAwait(false);
            }

            if (transactionRecordCount > 0)
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                dict.Ready = true;
            }

#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

            if (rowId > 1 && (generateFusejiVariants || generateMazegaki))
            {
                DBUtils.FlushWalLog(connection);
                await InsertVariantSearchKeys(fullPath, connection, recordInserter, rowId, includeProperNames, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, entities).ConfigureAwait(false);
            }

            if (rowId > 1)
            {
                DBUtils.ConfigureForRead(connection);

                // ReSharper disable once UseAwaitUsing
                using SqliteCommand analyzeCommand = connection.CreateCommand();
                analyzeCommand.CommandText = "ANALYZE;";
#pragma warning disable CA1849 // Call async methods when in an async method
                _ = analyzeCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                // ReSharper disable once UseAwaitUsing
                using SqliteCommand vacuumCommand = connection.CreateCommand();
                vacuumCommand.CommandText = "VACUUM;";
#pragma warning disable CA1849 // Call async methods when in an async method
                _ = vacuumCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                dict.Size = GetDistinctSearchKeyCount(connection);
                dict.MaxSearchKeyLength = GetMaxSearchKeyLength(connection);
            }
            else
            {
                dict.Size = 0;
                dict.MaxSearchKeyLength = 0;
            }
        }
        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync(
                "Couldn't find JMdict.xml. Would you like to download it now?",
                "Download JMdict?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.JMdict), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await ImportFromDisk(dict, dict.DBPath, entities).ConfigureAwait(false);
                        await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
                        await JmdictWordClassUtils.Load().ConfigureAwait(false);
                    }
                    finally
                    {
                        dict.Updating = false;
                    }
                }
                else
                {
                    dict.Updating = false;
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    private static async Task CreateImportRecordBatches(string fullPath, bool includeProperNames, ChannelReader<Dictionary<string, JmdictRecord>[]> availableBatches, ChannelWriter<(Dictionary<string, JmdictRecord>[] Records, int Count)> readyBatches, Dictionary<string, string> entities)
    {
        List<KanjiElement> kanjiElements = [];
        List<ReadingElement> readingElements = [];
        List<Sense> senseList = [];
        List<string> glossList = [];
        List<string> posList = [];
        try
        {
            // ReSharper disable once UseAwaitUsing
            using FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso);

            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)
            using XmlTextReader xmlReader = new(fileStream);
            xmlReader.DtdProcessing = DtdProcessing.Parse;
            xmlReader.WhitespaceHandling = WhitespaceHandling.None;
            xmlReader.EntityHandling = EntityHandling.ExpandCharEntities;

            Dictionary<string, JmdictRecord>[] batch = await availableBatches.ReadAsync().ConfigureAwait(false);
            int count = 0;
            while (xmlReader.ReadToFollowing("entry"))
            {
                Dictionary<string, JmdictRecord>? recordDictionary = JmdictRecordBuilder.GetRecordsFromEntry(JmdictLoader.ReadEntry(xmlReader, includeProperNames, kanjiElements, readingElements, senseList, glossList, posList, entities), includeProperNames);
                kanjiElements.Clear();
                readingElements.Clear();
                senseList.Clear();
                if (recordDictionary is not null && recordDictionary.Count > 0)
                {
                    batch[count] = recordDictionary;
                    ++count;
                    if (count == batch.Length)
                    {
                        _ = readyBatches.TryWrite((batch, count));
                        batch = await availableBatches.ReadAsync().ConfigureAwait(false);
                        count = 0;
                    }
                }
            }

            if (count > 0)
            {
                _ = readyBatches.TryWrite((batch, count));
            }

            _ = readyBatches.TryComplete();
        }
        catch (Exception exception)
        {
            _ = readyBatches.TryComplete(exception);
        }
    }

    private static async Task InsertVariantSearchKeys(string fullPath, SqliteConnection connection, JmdictRecordInserter recordInserter, long expectedNextRowId, bool includeProperNames, bool generateMazegaki, bool generateFusejiVariants, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration, Dictionary<string, string> entities)
    {
        int transactionRecordCount = 0;
        long rowId = 1;

        Channel<Dictionary<string, JmdictRecord>[]> availableBatches = Channel.CreateUnbounded<Dictionary<string, JmdictRecord>[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        Channel<(Dictionary<string, JmdictRecord>[] Records, int Count)> readyBatches = Channel.CreateUnbounded<(Dictionary<string, JmdictRecord>[] Records, int Count)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _ = availableBatches.Writer.TryWrite(new Dictionary<string, JmdictRecord>[ImportRecordBatchSize]);
        _ = availableBatches.Writer.TryWrite(new Dictionary<string, JmdictRecord>[ImportRecordBatchSize]);

        Channel<(List<string> Keys, List<int> EndOffsets)> availableKeyBatches = Channel.CreateUnbounded<(List<string> Keys, List<int> EndOffsets)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        Channel<(List<string> Keys, List<int> EndOffsets)> readyKeyBatches = Channel.CreateUnbounded<(List<string> Keys, List<int> EndOffsets)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _ = availableKeyBatches.Writer.TryWrite(([], []));
        _ = availableKeyBatches.Writer.TryWrite(([], []));

        Task producer = Task.Run(() => CreateImportRecordBatches(fullPath, includeProperNames, availableBatches.Reader, readyBatches.Writer, entities));
        Task keyProducer = Task.Run(() => CreateVariantSearchKeyBatches(readyBatches.Reader, availableBatches.Writer, availableKeyBatches.Reader, readyKeyBatches.Writer, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration));
#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
        try
        {
            try
            {
                await foreach ((List<string> keys, List<int> endOffsets) in readyKeyBatches.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    int startOffset = 0;
                    ReadOnlySpan<string> searchKeys = CollectionsMarshal.AsSpan(keys);
                    foreach (int endOffset in endOffsets)
                    {
                        int keyCount = endOffset - startOffset;
                        if (keyCount > 0)
                        {
                            recordInserter.InsertSearchKeys(rowId, searchKeys.Slice(startOffset, keyCount));
                            transactionRecordCount += keyCount;

                            if (transactionRecordCount > VariantSearchKeyTransactionBatchSize)
                            {
#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction.Commit();

                                // ReSharper disable once MethodHasAsyncOverload
                                transaction.Dispose();

                                transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
                                transactionRecordCount = 0;
                            }
                        }

                        ++rowId;
                        startOffset = endOffset;
                    }

                    keys.Clear();
                    endOffsets.Clear();
                    _ = availableKeyBatches.Writer.TryWrite((keys, endOffsets));
                }
            }
            catch (Exception exception)
            {
                _ = availableBatches.Writer.TryComplete(exception);
                _ = availableKeyBatches.Writer.TryComplete(exception);
                throw;
            }
            finally
            {
                await keyProducer.ConfigureAwait(false);
                await producer.ConfigureAwait(false);
            }

            if (rowId != expectedNextRowId)
            {
                throw new InvalidOperationException("JMdict changed while generating variant search keys.");
            }

            if (transactionRecordCount > 0)
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method
            }
        }
        finally
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }
    }

    private static async Task CreateVariantSearchKeyBatches(ChannelReader<(Dictionary<string, JmdictRecord>[] Records, int Count)> recordBatches, ChannelWriter<Dictionary<string, JmdictRecord>[]> availableRecordBatches, ChannelReader<(List<string> Keys, List<int> EndOffsets)> availableKeyBatches, ChannelWriter<(List<string> Keys, List<int> EndOffsets)> readyKeyBatches, bool generateMazegaki, bool generateFusejiVariants, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration)
    {
        Dictionary<JmdictRecord, List<string>> recordsToKeys = [];
        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        try
        {
            await foreach ((Dictionary<string, JmdictRecord>[] batch, int count) in recordBatches.ReadAllAsync().ConfigureAwait(false))
            {
                (List<string> keys, List<int> endOffsets) = await availableKeyBatches.ReadAsync().ConfigureAwait(false);
                for (int i = 0; i < count; i++)
                {
                    Dictionary<string, JmdictRecord> recordDictionary = batch[i];
                    foreach ((string key, JmdictRecord record) in recordDictionary)
                    {
                        ref List<string>? recordKeys = ref CollectionsMarshal.GetValueRefOrAddDefault(recordsToKeys, record, out bool exists);
                        if (exists)
                        {
                            Debug.Assert(recordKeys is not null);
                            recordKeys.Add(key);
                        }
                        else
                        {
                            recordKeys = [key];
                        }
                    }

                    foreach ((JmdictRecord record, List<string> recordKeys) in recordsToKeys)
                    {
                        foreach (string key in recordKeys)
                        {
                            _ = uniqueKeys.Add(key);
                        }

                        foreach (string key in recordKeys)
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(key, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = uniqueKeys.Add(fusejiVariant);
                                }
                            }

                            if (generateMazegaki && record.Readings is not null)
                            {
                                foreach (string reading in record.Readings)
                                {
                                    string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                                    if (readingInHiragana != key)
                                    {
                                        foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(key, readingInHiragana))
                                        {
                                            if (!recordDictionary.ContainsKey(mazegaki))
                                            {
                                                if (uniqueKeys.Add(mazegaki) && generateFusejiVariants)
                                                {
                                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                    {
                                                        if (!recordDictionary.ContainsKey(fusejiVariant))
                                                        {
                                                            _ = uniqueKeys.Add(fusejiVariant);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        foreach (string key in recordKeys)
                        {
                            _ = uniqueKeys.Remove(key);
                        }

                        keys.AddRange(uniqueKeys);
                        endOffsets.Add(keys.Count);
                        uniqueKeys.Clear();
                    }

                    recordsToKeys.Clear();
                }

                Array.Clear(batch, 0, count);
                _ = availableRecordBatches.TryWrite(batch);
                _ = readyKeyBatches.TryWrite((keys, endOffsets));
            }

            _ = readyKeyBatches.TryComplete();
        }
        catch (Exception exception)
        {
            _ = availableRecordBatches.TryComplete(exception);
            _ = readyKeyBatches.TryComplete(exception);
        }
    }

    private static int GetDistinctSearchKeyCount(SqliteConnection connection)
    {
        using SqliteRecordReader reader = new(connection, s_distinctSearchKeyCountQuery);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        using SqliteRecordReader reader = new(connection, s_maxSearchKeyLengthQuery);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<JmdictRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                JmdictRecord record = (JmdictRecord)records[i];
                ref List<string>? keys = ref CollectionsMarshal.GetValueRefOrAddDefault(recordToKeysDict, record, out bool exists);
                if (exists)
                {
                    Debug.Assert(keys is not null);
                    keys.Add(key);
                }
                else
                {
                    keys = [key];
                }
            }
        }

        long rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using JmdictRecordInserter recordInserter = new(connection);

        foreach ((JmdictRecord record, List<string> keys) in recordToKeysDict)
        {
            recordInserter.InsertRecord(rowId, record);
            recordInserter.InsertSearchKeys(rowId, CollectionsMarshal.AsSpan(keys));

            ++rowId;
        }

        transaction.Commit();

        DBUtils.ConfigureForRead(connection);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, int maxSearchKeyLengthForDict)
    {
        int validTermCount = terms.Length;
        if (maxSearchKeyLengthForDict > 0)
        {
            validTermCount = 0;
            foreach (string term in terms)
            {
                if (term.Length <= maxSearchKeyLengthForDict)
                {
                    ++validTermCount;
                }
            }
        }

        if (validTermCount is 0)
        {
            return null;
        }

        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        try
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using SqliteRecordReader reader = new(connection, GetQuery(validTermCount));
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            if (validTermCount == terms.Length)
            {
                for (int i = 0; i < terms.Length; i++)
                {
                    reader.Bind(i + 1, terms[i]);
                }
            }
            else
            {
                int parameterIndex = 1;
                foreach (string term in terms)
                {
                    if (term.Length <= maxSearchKeyLengthForDict)
                    {
                        reader.Bind(parameterIndex, term);
                        ++parameterIndex;
                    }
                }
            }

            if (!reader.Read())
            {
                return null;
            }

            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            do
            {
                JmdictRecord record = GetRecord(reader);
                string searchKey = reader.GetString((int)ColumnIndex.SearchKey);
                ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, searchKey, out bool exists);
                if (exists)
                {
                    Debug.Assert(result is not null);
                    result.Add(record);
                }
                else
                {
                    result = [record];
                }
            }
            while (reader.Read());

            return results;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is SQLitePCL.raw.SQLITE_BUSY)
        {
            LoggerManager.Logger.Error(ex, "Database is locked for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }
    }

    private static JmdictRecord GetRecord(SqliteRecordReader reader)
    {
        int edictId = reader.GetInt32((int)ColumnIndex.EdictId);
        string primarySpelling = reader.GetString((int)ColumnIndex.PrimarySpelling);
        string[]? primarySpellingOrthographyInfo = reader.DeserializeNullable<string[]>((int)ColumnIndex.PrimarySpellingOrthographyInfo);
        string[]?[]? spellingRestrictions = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.SpellingRestrictions);
        string[]? alternativeSpellings = reader.DeserializeNullable<string[]>((int)ColumnIndex.AlternativeSpellings);
        string[]?[]? alternativeSpellingsOrthographyInfo = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.AlternativeSpellingsOrthographyInfo);
        string[]? readings = reader.DeserializeNullable<string[]>((int)ColumnIndex.Readings);
        string[]?[]? readingsOrthographyInfo = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.ReadingsOrthographyInfo);
        string[]?[]? readingRestrictions = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.ReadingRestrictions);
        string[][] definitions = reader.Deserialize<string[][]>((int)ColumnIndex.Glossary);
        string?[]? definitionInfo = reader.DeserializeNullable<string?[]>((int)ColumnIndex.GlossaryInfo);
        string[]? wordClassesSharedByAllSenses = reader.DeserializeNullable<string[]>((int)ColumnIndex.WordClassesSharedByAllSenses);
        string[]?[]? wordClasses = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.WordClasses);
        string[]? fieldsSharedByAllSenses = reader.DeserializeNullable<string[]>((int)ColumnIndex.FieldsSharedByAllSenses);
        string[]?[]? fields = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.Fields);
        string[]? miscSharedByAllSenses = reader.DeserializeNullable<string[]>((int)ColumnIndex.MiscSharedByAllSenses);
        string[]?[]? misc = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.Misc);
        string[]? dialectsSharedByAllSenses = reader.DeserializeNullable<string[]>((int)ColumnIndex.DialectsSharedByAllSenses);
        string[]?[]? dialects = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.Dialects);
        LoanwordSource[]? loanwordEtymology = reader.DeserializeNullable<LoanwordSource[]>((int)ColumnIndex.LoanwordEtymology);
        string[]?[]? crossReferences = reader.DeserializeNullable<string[]?[]>((int)ColumnIndex.CrossReferences);
        string[]? info = reader.DeserializeNullable<string[]>((int)ColumnIndex.Info);

        return new JmdictRecord(edictId, primarySpelling, definitions, wordClasses, wordClassesSharedByAllSenses, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, spellingRestrictions, readingRestrictions, fields, fieldsSharedByAllSenses, misc, miscSharedByAllSenses, definitionInfo, dialects, dialectsSharedByAllSenses, loanwordEtymology, crossReferences, info);
    }
}
