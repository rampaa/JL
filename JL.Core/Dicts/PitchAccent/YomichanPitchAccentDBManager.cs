using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentDBManager
{
    public const int Version = 15;
    private const int VariantSearchKeyTransactionBatchSize = 20_000_000;
    private const int VariantSourceBatchSize = 4096;

    public const int Size = 250000;

    internal const string Record = "Record";
    internal const string RowId = "rowid";
    internal const string Spelling = "spelling";
    internal const string Reading = "reading";
    internal const string Position = "position";

    internal const string RecordSearchKey = "record_search_key";
    internal const string RecordId = "record_id";
    internal const string SearchKey = "search_key";

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    private static string GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out string? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{Spelling}, r.{Reading}, r.{Position}, rsk.{SearchKey}
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
        Reading,
        Position,
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
                {Spelling} TEXT NOT NULL,
                {Reading} TEXT,
                {Position} INTEGER NOT NULL
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

    public static async Task ImportFromDisk(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

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

        long rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        using PitchAccentRecordWriter recordWriter = new(connection);

        int transactionRecordCount = 0;

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            try
            {
                FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
                await using (fileStream.ConfigureAwait(false))
                {
                    await foreach (PitchAccentRecordBatch batch in YomichanPitchAccentLoader.ReadRecordBatches(fileStream).ConfigureAwait(false))
                    {
                        for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                        {
                            PitchAccentRecord record = batch.Records[recordIndex];
                            recordWriter.InsertRecord(rowId, record.Spelling, record.Reading, record.Position);

                            string spellingInHiragana = JapaneseUtils.NormalizeText(record.Spelling);
                            recordWriter.InsertSearchKey(rowId, spellingInHiragana);
                            ++transactionRecordCount;

                            if (record.Reading is not null)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading);
                                if (spellingInHiragana != readingInHiragana)
                                {
                                    recordWriter.InsertSearchKey(rowId, readingInHiragana);
                                    ++transactionRecordCount;
                                }
                            }

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
        }

        if (rowId > 1)
        {
            RemoveDuplicateRecords(connection);

            if (generateMazegaki || generateFusejiVariants)
            {
                DBUtils.FlushWalLog(connection);
                InsertVariantSearchKeys(connection, recordWriter, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration);
            }

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
                    SELECT MIN({RowId}) AS {RowId}_to_keep, {Spelling}, {Reading}
                    FROM {Record}
                    GROUP BY {Spelling}, {Reading}
                    HAVING COUNT(*) > 1
                ) d ON d.{Spelling} = r.{Spelling} AND d.{Reading} IS r.{Reading}
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

    private static void InsertVariantSearchKeys(SqliteConnection connection, PitchAccentRecordWriter recordWriter, bool generateMazegaki, bool generateFusejiVariants, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration)
    {
        using PitchAccentVariantSourceReader reader = new(connection);
        PitchAccentVariantSource[] sources = ArrayPool<PitchAccentVariantSource>.Shared.Rent(VariantSourceBatchSize);
        HashSet<string> keys = new(StringComparer.Ordinal);
        try
        {
            int transactionRecordCount = 0;
            SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                int sourceCount = reader.Read(sources, VariantSourceBatchSize);
                while (sourceCount > 0)
                {
                    for (int i = 0; i < sourceCount; i++)
                    {
                        ref readonly PitchAccentVariantSource source = ref sources[i];
                        string spellingInHiragana = JapaneseUtils.NormalizeText(source.Spelling);
                        string? readingInHiragana = source.Reading is not null ? JapaneseUtils.NormalizeText(source.Reading) : null;

                        _ = keys.Add(spellingInHiragana);
                        if (generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(spellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = keys.Add(fusejiVariant);
                            }
                        }

                        if (readingInHiragana is not null && spellingInHiragana != readingInHiragana)
                        {
                            _ = keys.Add(readingInHiragana);
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = keys.Add(fusejiVariant);
                                }
                            }

                            if (generateMazegaki)
                            {
                                foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(spellingInHiragana, readingInHiragana))
                                {
                                    if (keys.Add(mazegaki) && generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            _ = keys.Add(fusejiVariant);
                                        }
                                    }
                                }
                            }
                        }

                        foreach (string key in keys)
                        {
                            if (key == spellingInHiragana || key == readingInHiragana)
                            {
                                continue;
                            }

                            recordWriter.InsertSearchKey(source.RecordId, key);
                            ++transactionRecordCount;
                        }

                        keys.Clear();
                        if (transactionRecordCount > VariantSearchKeyTransactionBatchSize)
                        {
                            transaction.Commit();
                            transaction.Dispose();
                            transaction = connection.BeginTransaction();
                            transactionRecordCount = 0;
                        }
                    }

                    sources.AsSpan(0, sourceCount).Clear();
                    sourceCount = reader.Read(sources, VariantSourceBatchSize);
                }

                if (transactionRecordCount > 0)
                {
                    transaction.Commit();
                }
            }
            finally
            {
                transaction.Dispose();
            }
        }
        finally
        {
            sources.AsSpan(0, VariantSourceBatchSize).Clear();
            ArrayPool<PitchAccentVariantSource>.Shared.Return(sources);
        }
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

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT MAX(LENGTH(CAST({SearchKey} AS BLOB)) / 2)
            FROM {RecordSearchKey};
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<PitchAccentRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                PitchAccentRecord record = (PitchAccentRecord)records[i];
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

        using PitchAccentRecordWriter recordWriter = new(connection);

        foreach ((PitchAccentRecord record, List<string> keys) in recordToKeysDict)
        {
            recordWriter.InsertRecord(rowId, record.Spelling, record.Reading, record.Position);
            foreach (ref readonly string key in keys.AsReadOnlySpan())
            {
                recordWriter.InsertSearchKey(rowId, key);
            }

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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(SqliteConnection connection, HashSet<string> terms)
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

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        do
        {
            PitchAccentRecord record = GetRecord(reader);
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectingString, HashSet<string> terms)
    {
        if (terms.Count is 0)
        {
            return null;
        }

        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectingString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create a read-only connection to the database for dict {DBName}", readOnlyConnectingString);
            // FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Failed to create a read-only connection to the database for dict {dbName}.");
            return null;
        }

        return GetRecordsFromDB(connection, terms);
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);
        using SqliteTransaction transaction = connection.BeginTransaction(deferred: true);

        const string recordQuery = $"SELECT {Spelling}, {Reading}, {Position}, {RowId} FROM {Record};";
        Dictionary<long, PitchAccentRecord> records = [];
        using (SqliteRecordReader reader = new(connection, recordQuery))
        {
            while (reader.Read())
            {
                records.Add(reader.GetInt64(3), GetRecord(reader));
            }
        }

        const string searchKeyQuery =
            $"""
            SELECT {SearchKey}, {RecordId}
            FROM {RecordSearchKey}
            ORDER BY {SearchKey}, {RecordId};
            """;

        Debug.Assert(dict.Contents.Count is 0);
        Dictionary<string, PitchAccentRecords> contents = new(dict.Size > 0 ? dict.Size : Size, StringComparer.Ordinal);
        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
        string? currentSearchKey = null;
        PitchAccentRecords currentRecords = default;
        using (SqliteRecordReader reader = new(connection, searchKeyQuery))
        {
            while (reader.Read())
            {
                long recordId = reader.GetInt64(1);
                Debug.Assert(recordId > 0);
                if (!records.TryGetValue(recordId, out PitchAccentRecord? record))
                {
                    continue;
                }

                ReadOnlySpan<char> searchKey = reader.GetStringSpan(0);
                if (currentSearchKey is not null && searchKey.SequenceEqual(currentSearchKey))
                {
                    currentRecords.Add(record);
                }
                else
                {
                    if (currentSearchKey is not null)
                    {
                        contents.Add(currentSearchKey, currentRecords);
                    }

                    currentSearchKey = searchKey.ToString();
                    currentRecords = new PitchAccentRecords(record);
                    if (searchKey.Length > dict.MaxSearchKeyLength)
                    {
                        dict.MaxSearchKeyLength = searchKey.Length;
                    }
                }
            }
        }

        if (currentSearchKey is not null)
        {
            contents.Add(currentSearchKey, currentRecords);
        }

        transaction.Commit();
        dict.Contents = contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static PitchAccentRecord GetRecord(SqliteRecordReader reader)
    {
        string spelling = reader.GetString((int)ColumnIndex.Spelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !reader.IsNull(readingIndex)
            ? reader.GetString(readingIndex)
            : null;

        byte position = (byte)reader.GetInt32((int)ColumnIndex.Position);

        return new PitchAccentRecord(spelling, reading, position);
    }
}
