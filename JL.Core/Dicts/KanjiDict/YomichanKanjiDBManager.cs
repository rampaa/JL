using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiDBManager
{
    public const int Version = 8;

    public const int Size = 20000;
    private const int ImportRecordBatchSize = 128;
    private const int ImportBatchChannelCapacity = 256;

    internal const string Record = "record";
    internal const string RowId = "rowid";
    internal const string Kanji = "kanji";
    internal const string OnReadings = "on_readings";
    internal const string KunReadings = "kun_readings";
    internal const string Glossary = "glossary";
    internal const string Stats = "stats";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{RowId}, r.{OnReadings}, r.{KunReadings}, r.{Glossary}, r.{Stats}
        FROM {Record} r
        WHERE r.{Kanji} = @{Term};
        """;

    private const string KanjiWithVariationSelectorQuery =
        $"""
        SELECT r.{RowId}, r.{OnReadings}, r.{KunReadings}, r.{Glossary}, r.{Stats}, r.{Kanji}
        FROM {Record} r
        WHERE r.{Kanji} IN (@1, @2);
        """;

    private enum ColumnIndex
    {
        // ReSharper disable once UnusedMember.Local
        RowId = 0,
        OnReadings,
        KunReadings,
        Glossary,
        Stats,
        Kanji
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
                {Kanji} TEXT NOT NULL,
                {OnReadings} BLOB,
                {KunReadings} BLOB,
                {Glossary} BLOB,
                {Stats} BLOB
            ) STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static async Task ImportFromDisk(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "kanji_bank_*.json", SearchOption.TopDirectoryOnly);

        int rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        using YomichanKanjiRecordInserter recordInserter = new(connection);

        int transactionRecordCount = 0;
        foreach (string jsonFile in jsonFiles)
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            if (new FileInfo(jsonFile).Length <= YomichanKanjiLoader.WholeFileParsingThreshold)
            {
                byte[] jsonBytes = await File.ReadAllBytesAsync(jsonFile).ConfigureAwait(false);
                Channel<YomichanKanjiSerializedRecord[]> batches = Channel.CreateBounded<YomichanKanjiSerializedRecord[]>(new BoundedChannelOptions(ImportBatchChannelCapacity)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });
                Task producer = Task.Run(() => CreateSerializedRecordBatches(jsonBytes, batches.Writer));
                try
                {
                    await foreach (YomichanKanjiSerializedRecord[] batch in batches.Reader.ReadAllAsync().ConfigureAwait(false))
                    {
                        for (int i = 0; i < batch.Length; i++)
                        {
                            ref readonly YomichanKanjiSerializedRecord record = ref batch[i];
                            InsertSerializedRecord(connection, recordInserter, dict, record.Kanji, record.OnReadings, record.KunReadings, record.Definitions, record.Stats, ref rowId, ref transactionRecordCount, ref transaction);
                        }
                    }
                }
                finally
                {
                    _ = batches.Writer.TryComplete();
                    await producer.ConfigureAwait(false);
                }
            }
            else
            {
                FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
                await using (fileStream.ConfigureAwait(false))
                {
                    await foreach (JsonElement[]? jsonObj in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                    {
                        Debug.Assert(jsonObj is not null);
                        string? kanji = jsonObj[0].GetString();
                        Debug.Assert(kanji is not null);
                        if (string.IsNullOrWhiteSpace(kanji))
                        {
                            continue;
                        }

                        YomichanKanjiRecord.ReadFields(jsonObj, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats);
                        InsertRecord(connection, recordInserter, dict, kanji, onReadings, kunReadings, definitions, stats, ref rowId, ref transactionRecordCount, ref transaction);
                    }
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

#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        if (rowId > 1)
        {
            RemoveDuplicateRecords(connection);

            // ReSharper disable once UseAwaitUsing
            using SqliteCommand createIndexCommand = connection.CreateCommand();
            createIndexCommand.CommandText = $"CREATE INDEX IF NOT EXISTS ix_record_kanji ON {Record}({Kanji});";
#pragma warning disable CA1849 // Call async methods when in an async method
            _ = createIndexCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

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

            dict.Size = GetDistinctKanjiCount(connection);
        }
        else
        {
            dict.Size = 0;
            dict.MaxSearchKeyLength = 0;
        }
    }

    private static void CreateSerializedRecordBatches(byte[] jsonBytes, ChannelWriter<YomichanKanjiSerializedRecord[]> writer)
    {
        try
        {
            Utf8JsonReader reader = YomichanKanjiLoader.CreateJsonReader(jsonBytes);
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
            {
                throw new JsonException("The Yomichan kanji bank root must be an array.");
            }

            YomichanKanjiSerializedRecord[] batch = new YomichanKanjiSerializedRecord[ImportRecordBatchSize];
            int recordCount = 0;
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
            {
                YomichanKanjiLoader.ReadRecord(ref reader, out string kanji, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats);
                if (string.IsNullOrWhiteSpace(kanji))
                {
                    continue;
                }

                batch[recordCount] = new YomichanKanjiSerializedRecord(kanji,
                    onReadings is not null ? MessagePackSerializer.Serialize(onReadings) : null,
                    kunReadings is not null ? MessagePackSerializer.Serialize(kunReadings) : null,
                    definitions is not null ? MessagePackSerializer.Serialize(definitions) : null,
                    stats is not null ? MessagePackSerializer.Serialize(stats) : null);
                ++recordCount;
                if (recordCount == batch.Length)
                {
                    if (!writer.TryWrite(batch))
                    {
                        writer.WriteAsync(batch).AsTask().GetAwaiter().GetResult();
                    }
                    batch = new YomichanKanjiSerializedRecord[ImportRecordBatchSize];
                    recordCount = 0;
                }
            }

            if (reader.TokenType is not JsonTokenType.EndArray || reader.Read())
            {
                throw new JsonException("Unexpected content after the Yomichan kanji bank array.");
            }

            if (recordCount > 0)
            {
                Array.Resize(ref batch, recordCount);
                if (!writer.TryWrite(batch))
                {
                    writer.WriteAsync(batch).AsTask().GetAwaiter().GetResult();
                }
            }

            _ = writer.TryComplete();
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
    }

    private static void InsertRecord(SqliteConnection connection, YomichanKanjiRecordInserter recordInserter, Dict dict, string kanji, string[]? onReadings, string[]? kunReadings, string[]? definitions, string[]? stats, ref int rowId, ref int transactionRecordCount, ref SqliteTransaction transaction)
    {
        //if (definitions is null && kunReadings is null && onReadings is null && stats is null)
        //{
        //    return;
        //}

        byte[]? onReadingsBytes = onReadings is not null ? MessagePackSerializer.Serialize(onReadings) : null;
        byte[]? kunReadingsBytes = kunReadings is not null ? MessagePackSerializer.Serialize(kunReadings) : null;
        byte[]? definitionsBytes = definitions is not null ? MessagePackSerializer.Serialize(definitions) : null;
        byte[]? statsBytes = stats is not null ? MessagePackSerializer.Serialize(stats) : null;
        InsertSerializedRecord(connection, recordInserter, dict, kanji, onReadingsBytes, kunReadingsBytes, definitionsBytes, statsBytes, ref rowId, ref transactionRecordCount, ref transaction);
    }

    private static void InsertSerializedRecord(SqliteConnection connection, YomichanKanjiRecordInserter recordInserter, Dict dict, string kanji, byte[]? onReadings, byte[]? kunReadings, byte[]? definitions, byte[]? stats, ref int rowId, ref int transactionRecordCount, ref SqliteTransaction transaction)
    {
        if (kanji.Length > dict.MaxSearchKeyLength)
        {
            dict.MaxSearchKeyLength = kanji.Length;
        }

        recordInserter.Insert(rowId, kanji, onReadings, kunReadings, definitions, stats);

        ++transactionRecordCount;
        if (transactionRecordCount > DBUtils.TransactionBatchSize)
        {
            transaction.Commit();
            transaction.Dispose();
            dict.Ready = true;
            transaction = connection.BeginTransaction();
            transactionRecordCount = 0;
        }

        ++rowId;
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
                    SELECT MIN({RowId}) AS {RowId}_to_keep, {Kanji}, {OnReadings}, {KunReadings}, {Glossary}, {Stats}
                    FROM {Record}
                    GROUP BY {Kanji}, {OnReadings}, {KunReadings}, {Glossary}, {Stats}
                    HAVING COUNT(*) > 1
                ) d ON d.{Kanji} = r.{Kanji}
                    AND d.{OnReadings} IS r.{OnReadings}
                    AND d.{KunReadings} IS r.{KunReadings}
                    AND d.{Glossary} IS r.{Glossary}
                    AND d.{Stats} IS r.{Stats}
                WHERE r.{RowId} != d.{RowId}_to_keep
            );
            """;

        _ = command.ExecuteNonQuery();
    }

    private static int GetDistinctKanjiCount(SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT COUNT(DISTINCT {Kanji})
            FROM {Record};
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
    {
        long rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using YomichanKanjiRecordInserter recordInserter = new(connection);

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                YomichanKanjiRecord yomichanKanjiRecord = (YomichanKanjiRecord)records[i];
                byte[]? onReadings = yomichanKanjiRecord.OnReadings is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.OnReadings) : null;
                byte[]? kunReadings = yomichanKanjiRecord.KunReadings is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.KunReadings) : null;
                byte[]? definitions = yomichanKanjiRecord.Definitions is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.Definitions) : null;
                byte[]? stats = yomichanKanjiRecord.Stats is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.Stats) : null;
                recordInserter.Insert(rowId, kanji, onReadings, kunReadings, definitions, stats);

                ++rowId;
            }
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = $"CREATE INDEX IF NOT EXISTS ix_record_kanji ON {Record}({Kanji});";
        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        DBUtils.ConfigureForRead(connection);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static List<IDictRecord>? GetRecordsFromDB(string readOnlyConnectionString, string term)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        using SqliteRecordReader reader = new(connection, SingleTermQuery);
        reader.Bind(1, term);
        if (!reader.Read())
        {
            return null;
        }

        List<IDictRecord> results = [];
        do
        {
            results.Add(GetRecord(reader));
        }
        while (reader.Read());

        return results;
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, string kanjiWithVariationSelector, string kanji)
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

        Dictionary<string, IList<IDictRecord>>? results = null;
        while (reader.Read())
        {
            results ??= new Dictionary<string, IList<IDictRecord>>(StringComparer.Ordinal);
            YomichanKanjiRecord record = GetRecord(reader);
            string searchKey = reader.GetString((int)ColumnIndex.Kanji);
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

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        const string query =
            $"""
            SELECT r.{RowId}, r.{OnReadings}, r.{KunReadings}, r.{Glossary}, r.{Stats}, r.{Kanji}
            FROM {Record} r;
            """;

        using SqliteRecordReader reader = new(connection, query);
        Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
        Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
        while (reader.Read())
        {
            YomichanKanjiRecord record = GetRecord(reader);
            string kanji = reader.GetString((int)ColumnIndex.Kanji);
            ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, kanji, out bool exists);
            if (exists)
            {
                Debug.Assert(result is not null);
                result.Add(record);
            }
            else
            {
                result = [record];
            }

            if (kanji.Length > dict.MaxSearchKeyLength)
            {
                dict.MaxSearchKeyLength = kanji.Length;
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static YomichanKanjiRecord GetRecord(SqliteRecordReader reader)
    {
        long rowId = reader.GetInt64((int)ColumnIndex.RowId);
        string[]? onReadings = reader.DeserializeNullable<string[]>((int)ColumnIndex.OnReadings, Record, OnReadings, rowId);
        string[]? kunReadings = reader.DeserializeNullable<string[]>((int)ColumnIndex.KunReadings, Record, KunReadings, rowId);
        string[]? definitions = reader.DeserializeNullable<string[]>((int)ColumnIndex.Glossary, Record, Glossary, rowId);
        string[]? stats = reader.DeserializeNullable<string[]>((int)ColumnIndex.Stats, Record, Stats, rowId);

        return new YomichanKanjiRecord(onReadings, kunReadings, definitions, stats);
    }
}
