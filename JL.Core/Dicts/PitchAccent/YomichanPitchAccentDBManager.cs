using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
    public const int Version = 12;

    public const int Size = 250000;

    private const string Record = "Record";
    private const string RowId = "rowid";
    private const string Spelling = "spelling";
    private const string Reading = "reading";
    private const string Position = "position";

    private const string RecordSearchKey = "record_search_key";
    private const string RecordId = "record_id";
    private const string SearchKey = "search_key";

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

        ulong rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Reading}, {Position})
            VALUES (@{RowId}, @{Spelling}, @{Reading}, @{Position});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter spellingParam = new($"@{Spelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter positionParam = new($"@{Position}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            spellingParam,
            readingParam,
            positionParam
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey}({RecordId}, {SearchKey})
            VALUES (@{RecordId}, @{SearchKey});
            """;

        SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
#pragma warning disable CA1849 // Call async methods when in an async method
        insertSearchKeyCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        HashSet<string> keys = new(StringComparer.Ordinal);
        int transactionRecordCount = 0;

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            insertRecordCommand.Transaction = transaction;
            insertSearchKeyCommand.Transaction = transaction;

            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonObject in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonObject is not null);

                    PitchAccentRecord record = new(jsonObject);
                    if (record.Position is byte.MaxValue || string.IsNullOrWhiteSpace(record.Spelling))
                    {
                        continue;
                    }

                    rowidParam.Value = rowId;
                    spellingParam.Value = record.Spelling;
                    readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
                    positionParam.Value = record.Position;
#pragma warning disable CA1849 // Call async methods when in an async method
                    _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                    string spellingInHiragana = JapaneseUtils.NormalizeText(record.Spelling).GetPooledString();
                    _ = keys.Add(spellingInHiragana);

                    if (generateFusejiVariants)
                    {
                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(spellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                        {
                            _ = keys.Add(fusejiVariant);
                        }
                    }

                    if (record.Reading is not null)
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                        if (spellingInHiragana != readingInHiragana)
                        {
                            if (keys.Add(readingInHiragana))
                            {
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
                        }
                    }

                    recordIdParam.Value = rowId;
                    foreach (string key in keys)
                    {
                        searchKeyParam.Value = key;
#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertSearchKeyCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method
                    }

                    transactionRecordCount += keys.Count;
                    keys.Clear();
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
                        insertRecordCommand.Transaction = transaction;
                        insertSearchKeyCommand.Transaction = transaction;
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
                dict.Ready = true;
            }

#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        if (rowId > 1)
        {
            DBUtils.EnableForeignKeySupport(connection);
            RemoveDuplicateRecords(connection);
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
            WHERE {RowId} NOT IN
            (
                SELECT MIN({RowId})
                FROM {Record}
                GROUP BY {Spelling}, {Reading}, {Position}
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

        ulong rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Reading}, {Position})
            VALUES (@{RowId}, @{Spelling}, @{Reading}, @{Position})
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter spellingParam = new($"@{Spelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter positionParam = new($"@{Position}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            spellingParam,
            readingParam,
            positionParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey}({RecordId}, {SearchKey})
            VALUES (@{RecordId}, @{SearchKey})
            """;

        SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach ((PitchAccentRecord record, List<string> keys) in recordToKeysDict)
        {
            rowidParam.Value = rowId;
            spellingParam.Value = record.Spelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            positionParam.Value = record.Position;
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowId;
            foreach (ref readonly string key in keys.AsReadOnlySpan())
            {
                searchKeyParam.Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();
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
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = GetQuery(terms.Count);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int index = 1;
        foreach (string term in terms)
        {
            _ = command.Parameters.AddWithValue(DBUtils.GetParameterName(index), term);
            ++index;
        }
        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectingString, HashSet<string> terms)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectingString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create a read-only connection to the database for dict {DBName}.", readOnlyConnectingString);
            // FrontendManager.Frontend.Alert(AlertLevel.Error, $"Failed to create a read-only connection to the database for dict {dbName}.");
            return null;
        }

        return GetRecordsFromDB(connection, terms);
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT r.{Spelling}, r.{Reading}, r.{Position}, json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
        Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString((int)ColumnIndex.SearchKey), JsonOptions.DefaultJso);
            Debug.Assert(searchKeys is not null);

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

    private static PitchAccentRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString((int)ColumnIndex.Spelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        byte position = dataReader.GetByte((int)ColumnIndex.Position);

        return new PitchAccentRecord(spelling, reading, position);
    }
}
