using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanDBManager
{
    public const int Version = 39;

    public const int Size = 250000;

    internal const string Record = "record";
    internal const string RowId = "rowid";
    internal const string PrimarySpelling = "primary_spelling";
    internal const string Reading = "reading";
    internal const string Glossary = "glossary";
    internal const string PartOfSpeech = "part_of_speech";
    internal const string GlossaryTags = "glossary_tags";
    internal const string ImageInfos = "image_infos";
    internal const string PopularityScore = "popularity_score";
    internal const string RecordSearchKey = "record_search_key";
    internal const string RecordId = "record_id";
    internal const string SearchKey = "search_key";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{PopularityScore}, r.{Glossary}, r.{PartOfSpeech}, r.{GlossaryTags}, r.{ImageInfos}
        FROM {Record} r
        JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
        WHERE rsk.{SearchKey} = @{Term};
        """;

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    private static string GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out string? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{PopularityScore}, r.{Glossary}, r.{PartOfSpeech}, r.{GlossaryTags}, r.{ImageInfos}, rsk.{SearchKey}
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            WHERE rsk.{SearchKey} IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(',').Append(DBUtils.GetParameterName(i + 1));
        }

        query = queryBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(queryBuilder);
        _ = s_queryCache.TryAdd(termCount, query);
        return query;
    }

    private const int ImportRecordBatchSize = 64;
    internal const int VariantSearchKeyRecordBatchSize = 8192;
    private const int VariantSearchKeyTransactionBatchSize = 20_000_000;
    private const long WholeFileParsingThreshold = 32 * 1024 * 1024;

    private static readonly int s_workerCount = Environment.ProcessorCount;
    private static readonly int s_outputChannelCapacity = s_workerCount * 16;
    private static readonly int s_importRecordBatchChannelCapacity = Math.Max(1, s_outputChannelCapacity / ImportRecordBatchSize);

    public static void CreateDB(string dbPath)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(dbPath);

        DBUtils.SetEncodingToUtf16LE(connection);
        DBUtils.SetPageSizeTo64k(connection);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            CREATE TABLE IF NOT EXISTS {Record}
            (
                {RowId} INTEGER NOT NULL PRIMARY KEY,
                {PrimarySpelling} TEXT NOT NULL,
                {Reading} TEXT,
                {PopularityScore} REAL NOT NULL,
                {Glossary} BLOB NOT NULL,
                {PartOfSpeech} BLOB,
                {GlossaryTags} BLOB,
                {ImageInfos} BLOB
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

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<EpwingYomichanRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                EpwingYomichanRecord record = (EpwingYomichanRecord)records[i];
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

        using RecordInserter recordInserter = new(connection);
        using SearchKeyInserter searchKeyInserter = new(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        foreach ((EpwingYomichanRecord record, List<string> keys) in recordToKeysDict)
        {
            byte[] definitions = MessagePackSerializer.Serialize(record.Definitions);
            byte[]? wordClasses = record.WordClasses is not null
                ? MessagePackSerializer.Serialize(record.WordClasses)
                : null;
            byte[]? definitionTags = record.DefinitionTags is not null
                ? MessagePackSerializer.Serialize(record.DefinitionTags)
                : null;
            byte[]? imageInfos = record.ImageInfos is not null
                ? MessagePackSerializer.Serialize(record.ImageInfos)
                : null;

            recordInserter.Insert(rowId, record.PrimarySpelling, record.Reading, record.PopularityScore,
                definitions, wordClasses, definitionTags, imageInfos);

            searchKeyInserter.Insert(rowId, keys.AsReadOnlySpan());
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

    public static async Task ImportFromDisk(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameYomichan;

        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        Debug.Assert(!nonKanjiDict || !nonNameDict || generateMazegakiOption is not null);
        bool generateMazegaki = nonKanjiDict && nonNameDict
                                             && generateMazegakiOption!.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        Debug.Assert(!nonKanjiDict || generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = nonKanjiDict
                                && generateFusejiVariantsOption!.Value;

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

        ImportOptions importOptions = new(nonKanjiDict, nonNameDict, generateMazegaki,
            generateFusejiVariants, maxSearchKeyLengthForFusejiGeneration, maxTotalFuseji);

        ulong rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        using RecordInserter recordInserter = new(connection);
        using SearchKeyInserter searchKeyInserter = new(connection);

        ConcurrentDictionary<string, ImageInfo> imageInfoCache = new();

        int transactionRecordCount = 0;

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        string[] jsonFiles = [.. Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly)];

#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

        try
        {
            await foreach (ImportRecordBatch batch in GetImportRecordBatches(jsonFiles, dict, importOptions, imageInfoCache).ConfigureAwait(false))
            {
                try
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        ref readonly EpwingYomichanImportRecord record = ref batch.Records[i];

                        recordInserter.Insert((long)rowId, in record);
                        searchKeyInserter.Insert((long)rowId, record.SearchKey, record.AdditionalSearchKey);

                        transactionRecordCount += record.AdditionalSearchKey is null ? 1 : 2;
                        ++rowId;

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
                    }
                }
                finally
                {
                    batch.Records.AsSpan(0, batch.Count).Clear();
                    ArrayPool<EpwingYomichanImportRecord>.Shared.Return(batch.Records);
                }
            }

            if (transactionRecordCount > 0)
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                transactionRecordCount = 0;
                dict.Ready = true;
            }
        }
        finally
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        if (rowId > 1)
        {
            RemoveDuplicateRecords(connection);
        }

        if (rowId > 1 && (importOptions.GenerateMazegaki || importOptions.GenerateFusejiVariants))
        {
            DBUtils.FlushWalLog(connection);

            transactionRecordCount = 0;
            long lastVariantSearchKeyRecordRowId = 0;
            VariantSearchKeyRecord[] variantSearchKeyRecords = new VariantSearchKeyRecord[VariantSearchKeyRecordBatchSize];

            using VariantSearchKeyRecordReader variantSearchKeyRecordReader = new(connection);

#pragma warning disable CA1849 // Call async methods when in an async method
            transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            try
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                int variantSearchKeyRecordCount;
                while ((variantSearchKeyRecordCount = variantSearchKeyRecordReader.Read(
                        variantSearchKeyRecords,
                        lastVariantSearchKeyRecordRowId,
                        out long newLastVariantSearchKeyRecordRowId)) > 0)
#pragma warning restore CA1849 // Call async methods when in an async method
                {
                    lastVariantSearchKeyRecordRowId = newLastVariantSearchKeyRecordRowId;

                    await foreach (VariantSearchKeys variantSearchKeys in GetVariantSearchKeysInParallel(
                                       variantSearchKeyRecords,
                                       variantSearchKeyRecordCount,
                                       importOptions).ConfigureAwait(false))
                    {
#pragma warning disable CA1849 // Call async methods when in an async method
                        searchKeyInserter.Insert(variantSearchKeys.RowId, variantSearchKeys.SearchKeys);
#pragma warning restore CA1849 // Call async methods when in an async method

                        transactionRecordCount += variantSearchKeys.SearchKeys.Length;
                        if (transactionRecordCount > VariantSearchKeyTransactionBatchSize)
                        {
#pragma warning disable CA1849 // Call async methods when in an async method
                            transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

#pragma warning disable CA1849 // Call async methods when in an async method
                            // ReSharper disable once MethodHasAsyncOverload
                            transaction.Dispose();
                            transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                            transactionRecordCount = 0;
                        }
                    }
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

    private static void RemoveDuplicateRecords(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            DELETE FROM {Record}
            WHERE {RowId} IN
            (
                SELECT r.{RowId}
                FROM {Record} r
                JOIN
                (
                    SELECT MIN({RowId}) AS {RowId}_to_keep, {PrimarySpelling}, {Reading}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos}
                    FROM {Record}
                    GROUP BY {PrimarySpelling}, {Reading}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos}
                    HAVING COUNT(*) > 1
                ) d ON d.{PrimarySpelling} = r.{PrimarySpelling}
                    AND d.{Reading} IS r.{Reading}
                    AND d.{Glossary} = r.{Glossary}
                    AND d.{PartOfSpeech} IS r.{PartOfSpeech}
                    AND d.{GlossaryTags} IS r.{GlossaryTags}
                    AND d.{ImageInfos} IS r.{ImageInfos}
                WHERE r.{RowId} != d.{RowId}_to_keep
            );

            DELETE FROM {RecordSearchKey}
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM {Record}
                WHERE {Record}.{RowId} = {RecordSearchKey}.{RecordId}
            );
            """;

        _ = command.ExecuteNonQuery();
    }

    private static int GetDistinctSearchKeyCount(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT COUNT(DISTINCT {SearchKey})
            FROM {RecordSearchKey};
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT MAX(LENGTH({SearchKey}))
            FROM {RecordSearchKey};
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, int maxSearchKeyLengthForDict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        int validTermCount = terms.Length > maxSearchKeyLengthForDict && maxSearchKeyLengthForDict > 0
            ? maxSearchKeyLengthForDict
            : terms.Length;

        using YomichanRecordReader reader = new(connection, GetQuery(validTermCount));

        int offset = terms.Length - validTermCount;
        for (int i = 0; i < validTermCount; i++)
        {
            reader.Bind(i + 1, terms[offset + i]);
        }

        Dictionary<string, IList<IDictRecord>>? results = null;
        while (reader.Read())
        {
            results ??= new Dictionary<string, IList<IDictRecord>>(StringComparer.Ordinal);

            EpwingYomichanRecord record = reader.GetRecord();
            string searchKey = reader.GetString(YomichanColumnIndex.SearchKey);
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

        return results;
    }

    public static List<IDictRecord>? GetRecordsFromDB(string readOnlyConnectionString, string term)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        using YomichanRecordReader reader = new(connection, SingleTermQuery);
        reader.Bind(1, term);

        List<IDictRecord>? results = null;
        while (reader.Read())
        {
            results ??= [];
            results.Add(reader.GetRecord());
        }

        return results;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{PopularityScore}, r.{Glossary}, r.{PartOfSpeech}, r.{GlossaryTags}, r.{ImageInfos}, json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            EpwingYomichanRecord record = GetRecord(dataReader);
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString((int)YomichanColumnIndex.SearchKey), JsonOptions.DefaultJso);
            Debug.Assert(searchKeys is not null);

            Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
            Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
            foreach (string searchKey in searchKeys)
            {
                ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, searchKey, out bool exists);
                if (exists)
                {
                    Debug.Assert(result is not null);
                    result.Add(record);
                }
                else
                {
                    result = [record];
                }

                if (searchKey.Length > dict.MaxSearchKeyLength)
                {
                    dict.MaxSearchKeyLength = searchKey.Length;
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord GetRecord(SqliteDataReader dataReader)
    {
        string primarySpelling = dataReader.GetString((int)YomichanColumnIndex.PrimarySpelling);

        const int readingIndex = (int)YomichanColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        double popularityScore = dataReader.GetDouble((int)YomichanColumnIndex.PopularityScore);

        string[] definitions = dataReader.GetValueFromBlobStream<string[]>((int)YomichanColumnIndex.Glossary);
        string[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]>((int)YomichanColumnIndex.PartOfSpeech);
        string[]? definitionTags = dataReader.GetNullableValueFromBlobStream<string[]>((int)YomichanColumnIndex.GlossaryTags);
        ImageInfo[]? imageInfos = dataReader.GetNullableValueFromBlobStream<ImageInfo[]>((int)YomichanColumnIndex.ImageInfos);

        return new EpwingYomichanRecord(primarySpelling, reading, popularityScore, definitions, wordClasses, definitionTags, imageInfos);
    }

    private static async IAsyncEnumerable<ImportRecordBatch> GetImportRecordBatches(string[] jsonFiles, Dict dict,
        ImportOptions importOptions, ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        if (jsonFiles.Length is 0)
        {
            yield break;
        }

        using CancellationTokenSource stopOnConsumerExit = new();
        Channel<ImportRecordBatch> outputChannel = Channel.CreateBounded<ImportRecordBatch>(new BoundedChannelOptions(s_importRecordBatchChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

        ConcurrentQueue<string> jsonFileQueue = new(jsonFiles);

        Task[] workerTasks = new Task[Math.Min(s_workerCount, jsonFiles.Length)];
        for (int i = 0; i < workerTasks.Length; i++)
        {
            workerTasks[i] = Task.Run(() => CreateImportRecords(
                jsonFileQueue, dict, importOptions, outputChannel.Writer, imageInfoCache, stopOnConsumerExit.Token));
        }

        Task outputCompletionTask = CompleteOutputChannel(workerTasks, outputChannel.Writer);

        try
        {
            await foreach (ImportRecordBatch batch in outputChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                yield return batch;
            }
        }
        finally
        {
            // A failed insert can leave producers waiting on a full channel.
            await stopOnConsumerExit.CancelAsync().ConfigureAwait(false);
            try
            {
                await outputCompletionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stopOnConsumerExit.IsCancellationRequested)
            {
            }
            finally
            {
                while (outputChannel.Reader.TryRead(out ImportRecordBatch unusedBatch))
                {
                    unusedBatch.Records.AsSpan(0, unusedBatch.Count).Clear();
                    ArrayPool<EpwingYomichanImportRecord>.Shared.Return(unusedBatch.Records);
                }
            }
        }
    }

    private static async Task CreateImportRecords(ConcurrentQueue<string> jsonFiles, Dict dict,
        ImportOptions importOptions, ChannelWriter<ImportRecordBatch> writer,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, CancellationToken cancellationToken)
    {
        EpwingYomichanImportRecord[] records = ArrayPool<EpwingYomichanImportRecord>.Shared.Rent(ImportRecordBatchSize);
        int recordCount = 0;

        try
        {
            while (jsonFiles.TryDequeue(out string? jsonFile))
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
                await using (fileStream.ConfigureAwait(false))
                {
                    if (fileStream.Length <= WholeFileParsingThreshold)
                    {
                        byte[] jsonBytes = GC.AllocateUninitializedArray<byte>(checked((int)fileStream.Length));
                        await fileStream.ReadExactlyAsync(jsonBytes, cancellationToken).ConfigureAwait(false);

                        int offset = jsonBytes.AsSpan().StartsWith(Encoding.UTF8.Preamble)
                            ? Encoding.UTF8.Preamble.Length
                            : 0;

                        JsonReaderState readerState = EpwingYomichanLoader.InitialJsonReaderState;
                        bool started = false;
                        bool completed = false;

                        while (!completed)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            recordCount += EpwingYomichanLoader.ReadImportRecords(jsonBytes, ref offset, ref readerState,
                                ref started, dict, importOptions.NonKanjiDict, importOptions.NonNameDict, imageInfoCache,
                                records, recordCount, ImportRecordBatchSize - recordCount, out completed);

                            if (recordCount == ImportRecordBatchSize)
                            {
                                await writer.WriteAsync(new ImportRecordBatch(records, recordCount), cancellationToken).ConfigureAwait(false);

                                records = ArrayPool<EpwingYomichanImportRecord>.Shared.Rent(ImportRecordBatchSize);
                                recordCount = 0;
                            }
                        }
                    }
                    else
                    {
                        await foreach (JsonElement jsonElement in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(
                                           fileStream, JsonOptions.DefaultJso, cancellationToken: cancellationToken).ConfigureAwait(false))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!EpwingYomichanLoader.TryGetImportRecord(jsonElement, dict, importOptions.NonKanjiDict,
                                    importOptions.NonNameDict, imageInfoCache, out EpwingYomichanImportRecord record))
                            {
                                continue;
                            }

                            records[recordCount] = record;
                            ++recordCount;
                            if (recordCount == ImportRecordBatchSize)
                            {
                                await writer.WriteAsync(new ImportRecordBatch(records, recordCount), cancellationToken).ConfigureAwait(false);

                                records = ArrayPool<EpwingYomichanImportRecord>.Shared.Rent(ImportRecordBatchSize);
                                recordCount = 0;
                            }
                        }
                    }
                }
            }

            if (recordCount > 0)
            {
                await writer.WriteAsync(new ImportRecordBatch(records, recordCount), cancellationToken).ConfigureAwait(false);

                records = [];
                recordCount = 0;
            }
            else
            {
                ArrayPool<EpwingYomichanImportRecord>.Shared.Return(records);
                records = [];
            }
        }
        catch
        {
            if (records.Length > 0)
            {
                records.AsSpan(0, recordCount).Clear();
                ArrayPool<EpwingYomichanImportRecord>.Shared.Return(records);
            }

            throw;
        }
    }

    private static async IAsyncEnumerable<VariantSearchKeys> GetVariantSearchKeysInParallel(
        VariantSearchKeyRecord[] sources, int sourceCount, ImportOptions importOptions)
    {
        using CancellationTokenSource stopOnConsumerExit = new();
        Channel<VariantSearchKeys> outputChannel = Channel.CreateBounded<VariantSearchKeys>(new BoundedChannelOptions(s_outputChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

        int workerCount = Math.Min(s_workerCount, sourceCount);
        Task[] workerTasks = new Task[workerCount];
        for (int workerIndex = 0; workerIndex < workerTasks.Length; workerIndex++)
        {
            int currentWorkerIndex = workerIndex;
            workerTasks[workerIndex] = Task.Run(() => CreateVariantSearchKeys(
                sources, sourceCount, currentWorkerIndex, workerCount, importOptions,
                outputChannel.Writer, stopOnConsumerExit.Token));
        }

        Task outputCompletionTask = CompleteOutputChannel(workerTasks, outputChannel.Writer);

        try
        {
            await foreach (VariantSearchKeys searchKeys in outputChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                yield return searchKeys;
            }
        }
        finally
        {
            await stopOnConsumerExit.CancelAsync().ConfigureAwait(false);
            try
            {
                await outputCompletionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stopOnConsumerExit.IsCancellationRequested)
            {
            }
        }
    }

    private static async Task CreateVariantSearchKeys(VariantSearchKeyRecord[] sources, int sourceCount,
        int workerIndex, int workerCount, ImportOptions importOptions,
        ChannelWriter<VariantSearchKeys> writer, CancellationToken cancellationToken)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        List<string> variantSearchKeys = [];

        for (int i = workerIndex; i < sourceCount; i += workerCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VariantSearchKeyRecord source = sources[i];

            string[] searchKeys = GenerateVariantSearchKeys(source.PrimarySpelling, source.Reading,
                in importOptions, keys, variantSearchKeys);

            if (searchKeys.Length > 0)
            {
                await writer.WriteAsync(new VariantSearchKeys(source.RowId, searchKeys), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string[] GenerateVariantSearchKeys(string primarySpelling, string? reading,
        in ImportOptions importOptions, HashSet<string> keys, List<string> variantSearchKeys)
    {
        Debug.Assert(keys.Count is 0);
        Debug.Assert(variantSearchKeys.Count is 0);

        string primarySpellingInHiragana = importOptions.NonKanjiDict
            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
            : primarySpelling.GetPooledString();

        string? readingInHiragana = importOptions.NonKanjiDict && importOptions.NonNameDict && reading is not null
            ? JapaneseUtils.NormalizeText(reading).GetPooledString()
            : null;

        _ = keys.Add(primarySpellingInHiragana);

        if (importOptions.NonKanjiDict)
        {
            if (importOptions.GenerateFusejiVariants)
            {
                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana,
                             importOptions.MaxTotalFuseji, importOptions.MaxSearchKeyLengthForFusejiGeneration))
                {
                    _ = TryAddVariantSearchKey(fusejiVariant, readingInHiragana, keys, variantSearchKeys);
                }
            }

            if (readingInHiragana is not null
                && keys.Add(readingInHiragana))
            {
                if (importOptions.GenerateFusejiVariants)
                {
                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana,
                                 importOptions.MaxTotalFuseji, importOptions.MaxSearchKeyLengthForFusejiGeneration))
                    {
                        _ = TryAddVariantSearchKey(fusejiVariant, readingInHiragana, keys, variantSearchKeys);
                    }
                }

                if (importOptions.GenerateMazegaki)
                {
                    foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(
                                 primarySpellingInHiragana, readingInHiragana))
                    {
                        if (TryAddVariantSearchKey(mazegaki, readingInHiragana, keys, variantSearchKeys)
                            && importOptions.GenerateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki,
                                         importOptions.MaxTotalFuseji, importOptions.MaxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = TryAddVariantSearchKey(fusejiVariant, readingInHiragana, keys, variantSearchKeys);
                            }
                        }
                    }
                }
            }
        }

        string[] result = variantSearchKeys.ToArray();
        variantSearchKeys.Clear();
        keys.Clear();
        return result;
    }

    private static bool TryAddVariantSearchKey(string searchKey, string? readingSearchKey,
        HashSet<string> keys, List<string> variantSearchKeys)
    {
        if (!keys.Add(searchKey))
        {
            return false;
        }

        if (searchKey != readingSearchKey)
        {
            variantSearchKeys.Add(searchKey);
        }

        return true;
    }

    private static async Task CompleteOutputChannel<T>(Task[] workerTasks, ChannelWriter<T> writer)
    {
        try
        {
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
            _ = writer.TryComplete();
        }
        catch (Exception ex)
        {
            _ = writer.TryComplete(ex);
            throw;
        }
    }
}
