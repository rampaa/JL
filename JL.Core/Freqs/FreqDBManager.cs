using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Freqs.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;

internal static class FreqDBManager
{
    public const int Version = 16;
    private const int VariantSearchKeyTransactionBatchSize = 100_000_000;
    private const int VariantSearchKeyBatchSize = 8192;
    private const int MaxVariantSearchKeyWorkers = 8;

    internal const string Record = "record";
    internal const string RowId = "rowid";
    internal const string Spelling = "spelling";
    internal const string Frequency = "frequency";

    internal const string RecordSearchKey = "record_search_key";
    internal const string SearchKey = "search_key";
    internal const string RecordId = "record_id";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{Frequency}
        FROM {Record} r
        JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
        WHERE rsk.{SearchKey} = @{Term};
        """;

    private const string KanjiWithVariationSelectorQuery =
        $"""
        SELECT r.{Frequency}
        FROM {Record} r
        JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
        WHERE rsk.{SearchKey} IN (@1, @2)
        ORDER BY rsk.{SearchKey} DESC
        LIMIT 1;
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
            SELECT r.{Spelling}, r.{Frequency}, rsk.{SearchKey}
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

    private enum ColumnIndex
    {
        Spelling = 0,
        Frequency,
        SearchKey,
        RowId
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
                {Spelling} TEXT NOT NULL,
                {Frequency} INTEGER NOT NULL
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

    public static void ImportFromMemory(Freq freq)
    {
        long rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using FrequencyRecordWriter recordWriter = new(connection);

        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                FrequencyRecord record = records[i];
                recordWriter.InsertRecord(rowId, record.Spelling, record.Frequency);
                recordWriter.InsertSearchKey(rowId, key);

                ++rowId;
            }
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

    public static Dictionary<string, List<FrequencyRecord>>? GetRecordsFromDB(SqliteConnection connection, HashSet<string> terms)
    {
        if (terms.Count is 0)
        {
            return null;
        }

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        using SqliteRecordReader reader = new(connection, GetQuery(terms.Count));
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int index = 1;
        foreach (string term in terms)
        {
            reader.Bind(index, term);
            ++index;
        }

        if (!reader.Read())
        {
            return null;
        }

        Dictionary<string, List<FrequencyRecord>> results = new(StringComparer.Ordinal);
        do
        {
            FrequencyRecord record = GetRecord(reader);
            string searchKey = reader.GetString((int)ColumnIndex.SearchKey);
            ref List<FrequencyRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, searchKey, out bool exists);
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

    public static Dictionary<string, List<FrequencyRecord>>? GetRecordsFromDB(string readOnlyConnectionString, HashSet<string> terms)
    {
        if (terms.Count is 0)
        {
            return null;
        }

        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create a read-only connection to the database for freq dict {DBName}", readOnlyConnectionString);
            return null;
        }

        return GetRecordsFromDB(connection, terms);
    }

    public static int? GetKanjiFrequencyFromDB(string readOnlyConnectionString, string kanji)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        using SqliteRecordReader reader = new(connection, SingleTermQuery);
        reader.Bind(1, kanji);
        return reader.Read()
            ? reader.GetInt32(0)
            : null;
    }

    public static int? GetKanjiFrequencyFromDB(string readOnlyConnectionString, string kanjiWithVariationSelector, string kanji)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        using SqliteRecordReader reader = new(connection, KanjiWithVariationSelectorQuery);
        reader.Bind(1, kanjiWithVariationSelector);
        reader.Bind(2, kanji);
        return reader.Read()
            ? reader.GetInt32(0)
            : null;
    }

    public static void SetMaxFrequencyValue(Freq freq)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(freq.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        SetMaxFrequencyValue(freq, connection);
    }

    private static void SetMaxFrequencyValue(Freq freq, SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT MAX({Frequency})
            FROM {Record}
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        freq.MaxValue = !reader.IsNull(0)
            ? reader.GetInt32(0)
            : 0;
    }

    public static void LoadFromDB(Freq freq)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(freq.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        SetMaxFrequencyValue(freq, connection);

        const string query =
            $"""
            SELECT r.{Spelling}, r.{Frequency}, rsk.{SearchKey}, r.{RowId}
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            ORDER BY r.{RowId};
            """;

        using SqliteRecordReader reader = new(connection, query);
        Debug.Assert(freq.Contents is Dictionary<string, IList<FrequencyRecord>>);
        Dictionary<string, IList<FrequencyRecord>> contents = (Dictionary<string, IList<FrequencyRecord>>)freq.Contents;
        long previousRowId = 0;
        FrequencyRecord record = default;
        while (reader.Read())
        {
            long rowId = reader.GetInt64((int)ColumnIndex.RowId);
            Debug.Assert(rowId > 0);
            if (rowId != previousRowId)
            {
                record = GetRecord(reader);
                previousRowId = rowId;
            }

            string searchKey = reader.GetString((int)ColumnIndex.SearchKey);
            ref IList<FrequencyRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, searchKey, out bool exists);
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

        freq.Contents = freq.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    public static async Task ImportYomichanFreqFromDisk(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = freq.Type is not FreqType.YomichanKanji;

        bool generateMazegaki = false;
        bool generateFusejiVariants = false;
        if (nonKanjiDict)
        {
            GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
            Debug.Assert(generateMazegakiOption is not null);
            generateMazegaki = generateMazegakiOption.Value;

            GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
            Debug.Assert(generateFusejiVariantsOption is not null);
            generateFusejiVariants = generateFusejiVariantsOption.Value;
        }

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(freq.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = freq.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(freq.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = freq.Options.MaxTotalFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
        }

        long rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        using FrequencyRecordWriter recordWriter = new(connection);

        int transactionRecordCount = 0;

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        string[] jsonFiles = Directory.GetFiles(fullPath, freq.Type is FreqType.Yomichan ? "term_meta_bank_*.json" : "kanji_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
            try
            {
                await foreach (FrequencyYomichanRecordBatch batch in FrequencyYomichanReader.ReadRecordBatches(jsonFile, nonKanjiDict).ConfigureAwait(false))
                {
                    for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                    {
                        ref readonly FrequencyYomichanRecord record = ref batch.Records[recordIndex];
                        string primarySpelling = record.Spelling;
                        int frequency = record.Frequency;
                        string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling);
                        string? reading = record.Reading;

                        if (frequency > freq.MaxValue)
                        {
                            freq.MaxValue = frequency;
                        }

                        if (primarySpelling == reading)
                        {
                            reading = null;
                        }

                        if (reading is null)
                        {
                            recordWriter.InsertRecord(rowId, primarySpelling, frequency);
                        }
                        else
                        {
                            string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                            recordWriter.InsertRecord(rowId, primarySpelling, frequency);
                            recordWriter.InsertSearchKey(rowId, readingInHiragana);
                            ++transactionRecordCount;

                            ++rowId;
                            recordWriter.InsertRecord(rowId, reading, frequency);
                        }

                        recordWriter.InsertSearchKey(rowId, primarySpellingInHiragana);
                        ++transactionRecordCount;

                        if (transactionRecordCount > DBUtils.TransactionBatchSize)
                        {
#pragma warning disable CA1849 // Call async methods when in an async method
                            transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

#pragma warning disable CA1849 // Call async methods when in an async method
                            // ReSharper disable once MethodHasAsyncOverload
                            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

                            freq.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                            transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                            transactionRecordCount = 0;
                        }

                        ++rowId;
                    }
                }

                if (transactionRecordCount > 0)
                {
#pragma warning disable CA1849 // Call async methods when in an async method
                    transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                    transactionRecordCount = 0;
                    freq.Ready = true;
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

        if (rowId > 1 && (generateFusejiVariants || generateMazegaki))
        {
            DBUtils.FlushWalLog(connection);
            await InsertVariantSearchKeysInParallel(jsonFiles, null, nonKanjiDict, generateFusejiVariants, generateMazegaki,
                maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, recordWriter, connection).ConfigureAwait(false);
        }

        if (rowId > 1)
        {
            RemoveDuplicateYomichanFrequencyRecords(connection, freq.Options.HigherValueMeansHigherFrequency.Value);
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

            freq.Size = GetDistinctSearchKeyCount(connection);
        }
        else
        {
            freq.Size = 0;
        }
    }

    private static async Task InsertVariantSearchKeysInParallel(string[]? jsonFiles, string? nazekaFilePath, bool nonKanjiDict, bool generateFusejiVariants, bool generateMazegaki,
        int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration, FrequencyRecordWriter recordWriter, SqliteConnection connection)
    {
        Debug.Assert(generateFusejiVariants || generateMazegaki);
        int workerCount = Math.Min(Environment.ProcessorCount, MaxVariantSearchKeyWorkers);
        Channel<(FrequencyVariantSource[] Sources, int Count)> sourceChannel = Channel.CreateBounded<(FrequencyVariantSource[] Sources, int Count)>(new BoundedChannelOptions(workerCount * 2)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        Channel<((long RowId, string SearchKey)[] SearchKeys, int Count)> outputChannel = Channel.CreateBounded<((long RowId, string SearchKey)[] SearchKeys, int Count)>(new BoundedChannelOptions(workerCount * 2)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
        using CancellationTokenSource stopOnConsumerExit = new();
        CancellationToken stopOnConsumerExitToken = stopOnConsumerExit.Token;
        Task sourceProducer;
        if (nazekaFilePath is not null)
        {
            sourceProducer = Task.Run(() => CreateNazekaVariantSourceBatches(nazekaFilePath, generateFusejiVariants, sourceChannel.Writer, stopOnConsumerExitToken), CancellationToken.None);
        }
        else
        {
            Debug.Assert(jsonFiles is not null);
            sourceProducer = Task.Run(() => CreateVariantSourceBatches(jsonFiles, nonKanjiDict, generateFusejiVariants, sourceChannel.Writer, stopOnConsumerExitToken), CancellationToken.None);
        }
        Task[] workers = new Task[workerCount];
        for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            workers[workerIndex] = Task.Run(() => CreateVariantSearchKeyBatches(sourceChannel.Reader, generateFusejiVariants,
                generateMazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, outputChannel.Writer, stopOnConsumerExitToken), CancellationToken.None);
        }

        Task outputCompletionTask = CompleteVariantSearchKeyChannel(workers, outputChannel.Writer);
        int transactionRecordCount = 0;
        try
        {
            await foreach (((long RowId, string SearchKey)[] searchKeys, int count) in outputChannel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        (long recordId, string searchKey) = searchKeys[i];
                        recordWriter.InsertSearchKey(recordId, searchKey);
                    }

                    transactionRecordCount += count;
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
                finally
                {
                    ReturnVariantSearchKeyBatch(searchKeys, count);
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
            try
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                // ReSharper disable once MethodHasAsyncOverload
                transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
            }
            finally
            {
                if (!sourceProducer.IsCompleted || !outputCompletionTask.IsCompleted)
                {
                    await stopOnConsumerExit.CancelAsync().ConfigureAwait(false);
                }

                try
                {
                    await Task.WhenAll(sourceProducer, outputCompletionTask).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    LoggerManager.Logger.Debug("Variant search key workers were canceled due to consumer exit");
                }
                finally
                {
                    while (sourceChannel.Reader.TryRead(out (FrequencyVariantSource[] Sources, int Count) sourceBatch))
                    {
                        ReturnVariantSourceBatch(sourceBatch.Sources, sourceBatch.Count);
                    }

                    while (outputChannel.Reader.TryRead(out ((long RowId, string SearchKey)[] SearchKeys, int Count) batch))
                    {
                        ReturnVariantSearchKeyBatch(batch.SearchKeys, batch.Count);
                    }
                }
            }
        }
    }

    private static async Task CreateVariantSearchKeyBatches(ChannelReader<(FrequencyVariantSource[] Sources, int Count)> reader, bool generateFusejiVariants,
        bool generateMazegaki, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration,
        ChannelWriter<((long RowId, string SearchKey)[] SearchKeys, int Count)> writer, CancellationToken cancellationToken)
    {
        HashSet<string>? keys = generateMazegaki ? new HashSet<string>(StringComparer.Ordinal) : null;
        (long RowId, string SearchKey)[]? batch = ArrayPool<(long RowId, string SearchKey)>.Shared.Rent(VariantSearchKeyBatchSize);
        int count = 0;
        try
        {
            await foreach ((FrequencyVariantSource[] sources, int sourceCount) in reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        ref readonly FrequencyVariantSource source = ref sources[sourceIndex];
                        if (!generateMazegaki || source.Reading is null)
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(source.SearchKey, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    AddVariantSearchKey(source.RowId, fusejiVariant, writer, ref batch, ref count, cancellationToken);
                                }
                            }
                        }
                        else
                        {
                            Debug.Assert(keys is not null);
                            keys.Clear();
                            _ = keys.Add(source.SearchKey);
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(source.SearchKey, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    if (keys.Add(fusejiVariant))
                                    {
                                        AddVariantSearchKey(source.RowId, fusejiVariant, writer, ref batch, ref count, cancellationToken);
                                    }
                                }
                            }

                            foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(source.SearchKey, source.Reading))
                            {
                                if (!keys.Add(mazegakiVariant))
                                {
                                    continue;
                                }

                                AddVariantSearchKey(source.RowId, mazegakiVariant, writer, ref batch, ref count, cancellationToken);
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        if (keys.Add(fusejiVariant))
                                        {
                                            AddVariantSearchKey(source.RowId, fusejiVariant, writer, ref batch, ref count, cancellationToken);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    ReturnVariantSourceBatch(sources, sourceCount);
                }
            }

            if (count > 0)
            {
                Debug.Assert(batch is not null);
                if (!writer.TryWrite((batch, count)))
                {
                    await writer.WriteAsync((batch, count), cancellationToken).ConfigureAwait(false);
                }

                batch = null;
            }
        }
        finally
        {
            if (batch is not null)
            {
                ReturnVariantSearchKeyBatch(batch, count);
            }
        }
    }

    private static void AddVariantSearchKey(long rowId, string searchKey, ChannelWriter<((long RowId, string SearchKey)[] SearchKeys, int Count)> writer,
        ref (long RowId, string SearchKey)[]? batch, ref int count, CancellationToken cancellationToken)
    {
        Debug.Assert(batch is not null);
        batch[count] = (rowId, searchKey);
        ++count;
        if (count is VariantSearchKeyBatchSize)
        {
            if (!writer.TryWrite((batch, count)))
            {
                // Fuseji enumeration uses spans, so a full batch must be sent before enumeration can continue.
                writer.WriteAsync((batch, count), cancellationToken).AsTask().GetAwaiter().GetResult();
            }

            batch = null;
            count = 0;
            batch = ArrayPool<(long RowId, string SearchKey)>.Shared.Rent(VariantSearchKeyBatchSize);
        }
    }

    private static void ReturnVariantSearchKeyBatch((long RowId, string SearchKey)[] searchKeys, int count)
    {
        searchKeys.AsSpan(0, count).Clear();
        ArrayPool<(long RowId, string SearchKey)>.Shared.Return(searchKeys);
    }

    private static async Task CompleteVariantSearchKeyChannel(Task[] workers, ChannelWriter<((long RowId, string SearchKey)[] SearchKeys, int Count)> writer)
    {
        try
        {
            await Task.WhenAll(workers).ConfigureAwait(false);
            _ = writer.TryComplete();
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
            throw;
        }
    }

    private static async Task CreateVariantSourceBatches(string[] jsonFiles, bool nonKanjiDict, bool generateFusejiVariants,
        ChannelWriter<(FrequencyVariantSource[] Sources, int Count)> writer, CancellationToken cancellationToken)
    {
        FrequencyVariantSource[]? sources = ArrayPool<FrequencyVariantSource>.Shared.Rent(VariantSearchKeyBatchSize);
        int count = 0;
        long rowId = 1;
        try
        {
            foreach (string jsonFile in jsonFiles)
            {
                // ReSharper disable once UseCancellationTokenForIAsyncEnumerable
                await foreach (FrequencyYomichanRecordBatch batch in FrequencyYomichanReader.ReadRecordBatches(jsonFile, nonKanjiDict).ConfigureAwait(false))
                {
                    for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                    {
                        ref readonly FrequencyYomichanRecord record = ref batch.Records[recordIndex];
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        string primarySpelling = record.Spelling;
                        string? reading = record.Reading;
                        if (primarySpelling == reading)
                        {
                            reading = null;
                        }

                        if (reading is null)
                        {
                            if (generateFusejiVariants)
                            {
                                string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling);
                                Debug.Assert(sources is not null);
                                sources[count] = new FrequencyVariantSource(rowId, primarySpellingInHiragana, null);
                                ++count;
                            }
                        }
                        else
                        {
                            if (generateFusejiVariants)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                                Debug.Assert(sources is not null);
                                sources[count] = new FrequencyVariantSource(rowId, readingInHiragana, null);
                                ++count;
                            }

                            ++rowId;
                            string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling);
                            Debug.Assert(sources is not null);
                            sources[count] = new FrequencyVariantSource(rowId, primarySpellingInHiragana, reading);
                            ++count;
                        }

                        if (count >= VariantSearchKeyBatchSize - 1)
                        {
                            Debug.Assert(sources is not null);
                            if (!writer.TryWrite((sources, count)))
                            {
                                await writer.WriteAsync((sources, count), cancellationToken).ConfigureAwait(false);
                            }

                            sources = null;
                            sources = ArrayPool<FrequencyVariantSource>.Shared.Rent(VariantSearchKeyBatchSize);
                            count = 0;
                        }

                        ++rowId;
                    }
                }
            }

            if (count > 0)
            {
                Debug.Assert(sources is not null);
                if (!writer.TryWrite((sources, count)))
                {
                    await writer.WriteAsync((sources, count), cancellationToken).ConfigureAwait(false);
                }

                sources = null;
            }
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
        finally
        {
            if (sources is not null)
            {
                ReturnVariantSourceBatch(sources, count);
            }

            _ = writer.TryComplete();
        }
    }

    private static async Task CreateNazekaVariantSourceBatches(string jsonFile, bool generateFusejiVariants,
        ChannelWriter<(FrequencyVariantSource[] Sources, int Count)> writer, CancellationToken cancellationToken)
    {
        FrequencyVariantSource[]? sources = ArrayPool<FrequencyVariantSource>.Shared.Rent(VariantSearchKeyBatchSize);
        int count = 0;
        long rowId = 1;
        try
        {
            // ReSharper disable once UseCancellationTokenForIAsyncEnumerable
            await foreach (FrequencyNazekaRecordBatch batch in FrequencyNazekaReader.ReadRecordBatches(jsonFile).ConfigureAwait(false))
            {
                for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                {
                    ref readonly FrequencyNazekaRecord record = ref batch.Records[recordIndex];
                    string reading = record.Reading;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    string exactSpelling = record.Spelling;

                    if (generateFusejiVariants)
                    {
                        Debug.Assert(sources is not null);
                        sources[count] = new FrequencyVariantSource(rowId, reading, null);
                        ++count;
                    }

                    string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling);
                    if (exactSpellingInHiragana != reading)
                    {
                        ++rowId;
                        Debug.Assert(sources is not null);
                        sources[count] = new FrequencyVariantSource(rowId, exactSpellingInHiragana, reading);
                        ++count;
                    }

                    if (count >= VariantSearchKeyBatchSize - 1)
                    {
                        Debug.Assert(sources is not null);
                        if (!writer.TryWrite((sources, count)))
                        {
                            await writer.WriteAsync((sources, count), cancellationToken).ConfigureAwait(false);
                        }

                        sources = null;
                        sources = ArrayPool<FrequencyVariantSource>.Shared.Rent(VariantSearchKeyBatchSize);
                        count = 0;
                    }

                    ++rowId;
                }
            }

            if (count > 0)
            {
                Debug.Assert(sources is not null);
                if (!writer.TryWrite((sources, count)))
                {
                    await writer.WriteAsync((sources, count), cancellationToken).ConfigureAwait(false);
                }

                sources = null;
            }
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
        finally
        {
            if (sources is not null)
            {
                ReturnVariantSourceBatch(sources, count);
            }

            _ = writer.TryComplete();
        }
    }

    private static void ReturnVariantSourceBatch(FrequencyVariantSource[] sources, int count)
    {
        sources.AsSpan(0, count).Clear();
        ArrayPool<FrequencyVariantSource>.Shared.Return(sources);
    }

    public static async Task ImportNazekaFreqFromDisk(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
        Debug.Assert(generateMazegakiOption is not null);
        bool generateMazegaki = generateMazegakiOption.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
        Debug.Assert(generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = generateFusejiVariantsOption.Value;

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(freq.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = freq.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(freq.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = freq.Options.MaxTotalFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
        }

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using FrequencyRecordWriter recordWriter = new(connection);

        long rowId = 1;
        int transactionRecordCount = 0;
#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
        try
        {
            await foreach (FrequencyNazekaRecordBatch batch in FrequencyNazekaReader.ReadRecordBatches(fullPath).ConfigureAwait(false))
            {
                for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                {
                    ref readonly FrequencyNazekaRecord record = ref batch.Records[recordIndex];
                    string reading = record.Reading;
                    int frequencyRank = record.Frequency;
                    string exactSpelling = record.Spelling;

                    if (frequencyRank > freq.MaxValue)
                    {
                        freq.MaxValue = frequencyRank;
                    }

                    recordWriter.InsertRecord(rowId, exactSpelling, frequencyRank);
                    recordWriter.InsertSearchKey(rowId, reading);
                    ++transactionRecordCount;

                    string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling);
                    if (exactSpellingInHiragana != reading)
                    {
                        ++rowId;
                        recordWriter.InsertRecord(rowId, reading, frequencyRank);
                        recordWriter.InsertSearchKey(rowId, exactSpellingInHiragana);
                        ++transactionRecordCount;
                    }

                    if (transactionRecordCount > DBUtils.TransactionBatchSize)
                    {
#pragma warning disable CA1849 // Call async methods when in an async method
                        transaction.Commit();
                        // ReSharper disable once MethodHasAsyncOverload
                        transaction.Dispose();
                        transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                        freq.Ready = true;
                        transactionRecordCount = 0;
                    }

                    ++rowId;
                }
            }

            if (transactionRecordCount > 0)
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method
                freq.Ready = true;
            }
        }
        finally
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        if (rowId > 1 && (generateFusejiVariants || generateMazegaki))
        {
            DBUtils.FlushWalLog(connection);
            await InsertVariantSearchKeysInParallel(null, fullPath, true, generateFusejiVariants, generateMazegaki,
                maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, recordWriter, connection).ConfigureAwait(false);
        }

        if (rowId > 1)
        {
            RemoveDuplicateNazekaFrequencyRecords(connection, freq.Options.HigherValueMeansHigherFrequency.Value);
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

            freq.Size = GetDistinctSearchKeyCount(connection);
        }
        else
        {
            freq.Size = 0;
        }
    }

    private static void RemoveDuplicateYomichanFrequencyRecords(SqliteConnection connection, bool higherValueMeansHigherFrequency)
    {
        string frequencyOrder = higherValueMeansHigherFrequency ? "DESC" : "ASC";
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE TEMP TABLE losing_links AS
            WITH duplicate_keys AS MATERIALIZED
            (
                SELECT {SearchKey}
                FROM {RecordSearchKey}
                GROUP BY {SearchKey}
                HAVING COUNT(*) > 1
            ), ranked_search_keys AS
            (
                SELECT rsk.{SearchKey}, rsk.{RecordId}, ROW_NUMBER() OVER (PARTITION BY rsk.{SearchKey}, r.{Spelling} ORDER BY r.{Frequency} {frequencyOrder}) AS rank
                FROM duplicate_keys dk
                JOIN {RecordSearchKey} rsk ON rsk.{SearchKey} = dk.{SearchKey}
                JOIN {Record} r ON r.{RowId} = rsk.{RecordId}
            )
            SELECT {SearchKey}, {RecordId}
            FROM ranked_search_keys
            WHERE rank > 1;

            DELETE FROM {RecordSearchKey}
            WHERE ({SearchKey}, {RecordId}) IN (SELECT {SearchKey}, {RecordId} FROM losing_links);

            CREATE TEMP TABLE losing_record_ids ({RecordId} INTEGER PRIMARY KEY) WITHOUT ROWID;
            INSERT INTO losing_record_ids
            SELECT DISTINCT {RecordId}
            FROM losing_links;

            DELETE FROM {Record}
            WHERE {RowId} IN (SELECT {RecordId} FROM losing_record_ids) AND {RowId} NOT IN
            (
                SELECT DISTINCT rsk.{RecordId}
                FROM {RecordSearchKey} rsk
                CROSS JOIN losing_record_ids lri
                WHERE lri.{RecordId} = rsk.{RecordId}
            );

            DROP TABLE losing_record_ids;
            DROP TABLE losing_links;
            """;

        _ = command.ExecuteNonQuery();
    }

    private static void RemoveDuplicateNazekaFrequencyRecords(SqliteConnection connection, bool higherValueMeansHigherFrequency)
    {
        string frequencyOrder = higherValueMeansHigherFrequency ? "DESC" : "ASC";
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE TEMP TABLE losing_links AS
            WITH duplicate_keys AS MATERIALIZED
            (
                SELECT rsk.{SearchKey}, r.{Spelling}
                FROM {RecordSearchKey} rsk
                JOIN {Record} r ON r.{RowId} = rsk.{RecordId}
                GROUP BY rsk.{SearchKey}, r.{Spelling}
                HAVING COUNT(*) > 1
            ), ranked_search_keys AS
            (
                SELECT rsk.{SearchKey}, rsk.{RecordId}, ROW_NUMBER() OVER (PARTITION BY rsk.{SearchKey}, r.{Spelling} ORDER BY r.{Frequency} {frequencyOrder}) AS rank
                FROM duplicate_keys dk
                JOIN {RecordSearchKey} rsk ON rsk.{SearchKey} = dk.{SearchKey}
                JOIN {Record} r ON r.{RowId} = rsk.{RecordId}
                WHERE r.{Spelling} = dk.{Spelling}
            )
            SELECT {SearchKey}, {RecordId}
            FROM ranked_search_keys
            WHERE rank > 1;

            DELETE FROM {RecordSearchKey}
            WHERE ({SearchKey}, {RecordId}) IN (SELECT {SearchKey}, {RecordId} FROM losing_links);

            CREATE TEMP TABLE losing_record_ids ({RecordId} INTEGER PRIMARY KEY) WITHOUT ROWID;
            INSERT INTO losing_record_ids
            SELECT DISTINCT {RecordId}
            FROM losing_links;

            DELETE FROM {Record}
            WHERE {RowId} IN (SELECT {RecordId} FROM losing_record_ids) AND {RowId} NOT IN
            (
                SELECT DISTINCT rsk.{RecordId}
                FROM {RecordSearchKey} rsk
                CROSS JOIN losing_record_ids lri
                WHERE lri.{RecordId} = rsk.{RecordId}
            );

            DROP TABLE losing_record_ids;
            DROP TABLE losing_links;
            """;

        _ = command.ExecuteNonQuery();
    }

    private static int GetDistinctSearchKeyCount(SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT COUNT(DISTINCT {SearchKey})
            FROM {RecordSearchKey};
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    private static FrequencyRecord GetRecord(SqliteRecordReader reader)
    {
        string spelling = reader.GetString((int)ColumnIndex.Spelling);
        int frequency = reader.GetInt32((int)ColumnIndex.Frequency);

        return new FrequencyRecord(spelling, frequency);
    }
}
