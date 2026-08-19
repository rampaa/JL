using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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

    private const string Record = "record";
    private const string RowId = "rowid";
    private const string Spelling = "spelling";
    private const string Frequency = "frequency";

    private const string RecordSearchKey = "record_search_key";
    private const string SearchKey = "search_key";
    private const string RecordId = "record_id";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{Spelling}, r.{Frequency}
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
        SearchKey
    }

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
        ulong rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Frequency})
            VALUES (@{RowId}, @{Spelling}, @{Frequency});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter spellingParam = new($"@{Spelling}", SqliteType.Text);
        SqliteParameter frequencyParam = new($"@{Frequency}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            spellingParam,
            frequencyParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey} ({RecordId}, {SearchKey})
            VALUES (@{RecordId}, @{SearchKey});
            """;

        SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                FrequencyRecord record = records[i];
                rowidParam.Value = rowId;
                spellingParam.Value = record.Spelling;
                frequencyParam.Value = record.Frequency;
                _ = insertRecordCommand.ExecuteNonQuery();

                recordIdParam.Value = rowId;
                searchKeyParam.Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();

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

        Dictionary<string, List<FrequencyRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
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

        return results;
    }

    public static Dictionary<string, List<FrequencyRecord>>? GetRecordsFromDB(string readOnlyConnectionString, HashSet<string> terms)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create a read-only connection to the database for freq dict {DBName}.", readOnlyConnectionString);
            return null;
        }

        return GetRecordsFromDB(connection, terms);
    }

    public static List<FrequencyRecord>? GetRecordsFromDB(string readOnlyConnectionString, string term)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;
        _ = command.Parameters.AddWithValue($"@{Term}", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<FrequencyRecord> records = [];
        while (dataReader.Read())
        {
            records.Add(GetRecord(dataReader));
        }
        return records;
    }

    public static void SetMaxFrequencyValue(Freq freq)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(freq.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT MAX({Frequency})
            FROM {Record}
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        freq.MaxValue = !reader.IsDBNull(0)
            ? reader.GetInt32(0)
            : 0;
    }

    public static void LoadFromDB(Freq freq)
    {
        SetMaxFrequencyValue(freq);

        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(freq.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT r.{Spelling}, r.{Frequency}, json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        Debug.Assert(freq.Contents is Dictionary<string, IList<FrequencyRecord>>);
        Dictionary<string, IList<FrequencyRecord>> contents = (Dictionary<string, IList<FrequencyRecord>>)freq.Contents;
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString((int)ColumnIndex.SearchKey), JsonOptions.DefaultJso);
            Debug.Assert(searchKeys is not null);

            foreach (string searchKey in searchKeys)
            {
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
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private const int RowIdColumnIndex = 0;
    private const int FrequencyColumnIndex = 1;

    public static async Task ImportYomichanFreqFromDisk(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = freq.Type is not FreqType.YomichanKanji;

        GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
        Debug.Assert(nonKanjiDict || generateMazegakiOption is not null);
        bool generateMazegaki = nonKanjiDict
            // ReSharper disable once NullableWarningSuppressionIsUsed
            && generateMazegakiOption!.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
        Debug.Assert(!nonKanjiDict || generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = nonKanjiDict
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                && generateFusejiVariantsOption!.Value;

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

        ulong rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Frequency})
            VALUES (@{RowId}, @{Spelling}, @{Frequency});
            """;

        SqliteParameter rowIdParamForInsertRecordCommand = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter spellingParamForInsertRecordCommand = new($"@{Spelling}", SqliteType.Text);
        SqliteParameter frequencyParamForInsertRecordCommand = new($"@{Frequency}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowIdParamForInsertRecordCommand,
            spellingParamForInsertRecordCommand,
            frequencyParamForInsertRecordCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordSearchKeyCommand = connection.CreateCommand();
        insertRecordSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey} ({SearchKey}, {RecordId})
            VALUES (@{SearchKey}, @{RecordId});
            """;

        SqliteParameter searchKeyParamForInsertRecordSearchKeyCommand = new($"@{SearchKey}", SqliteType.Text);
        SqliteParameter recordIdParamForInsertRecordSearchKeyCommand = new($"@{RecordId}", SqliteType.Integer);

        insertRecordSearchKeyCommand.Parameters.AddRange([
            searchKeyParamForInsertRecordSearchKeyCommand,
            recordIdParamForInsertRecordSearchKeyCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        insertRecordSearchKeyCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand selectSameRecordsCommand = connection.CreateCommand();
        selectSameRecordsCommand.CommandText =
            $"""
            SELECT r.{RowId}, r.{Frequency}
            FROM {Record} AS r
            JOIN {RecordSearchKey} AS rs ON rs.{RecordId} = r.{RowId}
            WHERE rs.{SearchKey} = @{SearchKey} AND r.{Spelling} = @{Spelling};
            """;

        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        SqliteParameter spellingParam = new($"@{Spelling}", SqliteType.Text);
        selectSameRecordsCommand.Parameters.AddRange([
            searchKeyParam,
            spellingParam
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        selectSameRecordsCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand updateRecordCommand = connection.CreateCommand();
        updateRecordCommand.CommandText =
            $"""
            UPDATE {Record}
            SET {Frequency} = @{Frequency}
            WHERE {RowId} = @{RowId};
            """;

        SqliteParameter frequencyParamForUpdateCommand = new($"@{Frequency}", SqliteType.Integer);
        SqliteParameter rowIdParamForUpdateCommand = new($"@{RowId}", SqliteType.Integer);
        updateRecordCommand.Parameters.AddRange([
            frequencyParamForUpdateCommand,
            rowIdParamForUpdateCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        updateRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        CommandsAndParameters commandsAndParameters = new(selectSameRecordsCommand, searchKeyParam, spellingParam, updateRecordCommand, frequencyParamForUpdateCommand, rowIdParamForUpdateCommand, insertRecordCommand, rowIdParamForInsertRecordCommand, spellingParamForInsertRecordCommand, frequencyParamForInsertRecordCommand, insertRecordSearchKeyCommand, searchKeyParamForInsertRecordSearchKeyCommand, recordIdParamForInsertRecordSearchKeyCommand);

        int transactionRecordCount = 0;

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, freq.Type is FreqType.Yomichan ? "term_meta_bank_*.json" : "kanji_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            insertRecordCommand.Transaction = transaction;
            insertRecordSearchKeyCommand.Transaction = transaction;
            selectSameRecordsCommand.Transaction = transaction;
            updateRecordCommand.Transaction = transaction;

            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonElements in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonElements is not null);

                    string primarySpelling = jsonElements[0]
                        // ReSharper disable once NullableWarningSuppressionIsUsed
                        .GetString()!.GetPooledString();

                    string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling).GetPooledString();
                    string? reading = null;
                    int frequency = -1;
                    ref readonly JsonElement thirdElement = ref jsonElements[2];

                    if (thirdElement.ValueKind is JsonValueKind.Number)
                    {
                        frequency = thirdElement.GetInt32();
                    }
                    else if (thirdElement.ValueKind is JsonValueKind.Object)
                    {
                        if (thirdElement.TryGetProperty("value", out JsonElement freqValue))
                        {
                            frequency = freqValue.GetInt32();
                            if (frequency <= 0 && thirdElement.TryGetProperty("displayValue", out JsonElement displayValue))
                            {
                                frequency = TextUtils.ExtractFirstInt(displayValue.GetString());
                            }
                        }
                        else if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                        {
                            reading = readingValue
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                .GetString()!.GetPooledString();
                            JsonElement frequencyElement = thirdElement.GetProperty("frequency");

                            if (frequencyElement.ValueKind is JsonValueKind.Number)
                            {
                                frequency = frequencyElement.GetInt32();
                            }
                            else if (frequencyElement.ValueKind is JsonValueKind.Object)
                            {
                                frequency = frequencyElement.GetProperty("value").GetInt32();
                                if (frequency <= 0 && frequencyElement.TryGetProperty("displayValue", out JsonElement displayValue))
                                {
                                    frequency = TextUtils.ExtractFirstInt(displayValue.GetString());
                                }
                            }
                            else // if (frequencyElement.ValueKind is JsonValueKind.String)
                            {
                                frequency = TextUtils.ExtractFirstInt(frequencyElement.GetString());
                            }
                        }
                    }
                    else // if (thirdElement.ValueKind is JsonValueKind.String)
                    {
                        string? freqStr = thirdElement.GetString();
                        Debug.Assert(freqStr is not null);

                        frequency = TextUtils.ExtractFirstInt(freqStr);
                    }

                    if (frequency <= 0)
                    {
                        continue;
                    }

                    if (frequency > freq.MaxValue)
                    {
                        freq.MaxValue = frequency;
                    }

                    if (primarySpelling == reading)
                    {
                        reading = null;
                    }

                    FrequencyRecord frequencyRecordWithPrimarySpelling = new(primarySpelling, frequency);
                    if (reading is null)
                    {
                        if (AddOrUpdate(primarySpellingInHiragana, rowId, frequencyRecordWithPrimarySpelling, true, commandsAndParameters))
                        {
                            ++transactionRecordCount;

                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithPrimarySpelling, false, commandsAndParameters))
                                    {
                                        ++transactionRecordCount;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
                        if (AddOrUpdate(readingInHiragana, rowId, frequencyRecordWithPrimarySpelling, true, commandsAndParameters))
                        {
                            ++transactionRecordCount;

                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithPrimarySpelling, false, commandsAndParameters))
                                    {
                                        ++transactionRecordCount;
                                    }
                                }
                            }
                        }

                        FrequencyRecord frequencyRecordWithReading = new(reading, frequency);
                        ++rowId;

                        if (AddOrUpdate(primarySpellingInHiragana, rowId, frequencyRecordWithReading, true, commandsAndParameters))
                        {
                            ++transactionRecordCount;

                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                    {
                                        ++transactionRecordCount;
                                    }
                                }
                            }

                            if (generateMazegaki)
                            {
                                foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, reading))
                                {
                                    if (AddOrUpdate(mazegakiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                    {
                                        ++transactionRecordCount;
                                    }

                                    if (generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                            {
                                                ++transactionRecordCount;
                                            }
                                        }
                                    }
                                }
                            }
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

                        freq.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                        transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                        transactionRecordCount = 0;

                        insertRecordCommand.Transaction = transaction;
                        insertRecordSearchKeyCommand.Transaction = transaction;
                        selectSameRecordsCommand.Transaction = transaction;
                        updateRecordCommand.Transaction = transaction;
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

#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
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

            freq.Size = GetDistinctSearchKeyCount(connection);
        }
        else
        {
            freq.Size = 0;
        }
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

        ulong rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(freq.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Frequency})
            VALUES (@{RowId}, @{Spelling}, @{Frequency});
            """;

        SqliteParameter rowIdParamForInsertRecordCommand = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter spellingParamForInsertRecordCommand = new($"@{Spelling}", SqliteType.Text);
        SqliteParameter frequencyParamForInsertRecordCommand = new($"@{Frequency}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowIdParamForInsertRecordCommand,
            spellingParamForInsertRecordCommand,
            frequencyParamForInsertRecordCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordSearchKeyCommand = connection.CreateCommand();
        insertRecordSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey} ({SearchKey}, {RecordId})
            VALUES (@{SearchKey}, @{RecordId});
            """;

        SqliteParameter searchKeyParamForInsertRecordSearchKeyCommand = new($"@{SearchKey}", SqliteType.Text);
        SqliteParameter recordIdParamForInsertRecordSearchKeyCommand = new($"@{RecordId}", SqliteType.Integer);

        insertRecordSearchKeyCommand.Parameters.AddRange([
            searchKeyParamForInsertRecordSearchKeyCommand,
            recordIdParamForInsertRecordSearchKeyCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        insertRecordSearchKeyCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand selectSameRecordsCommand = connection.CreateCommand();
        selectSameRecordsCommand.CommandText =
            $"""
            SELECT r.{RowId}, r.{Frequency}
            FROM {Record} AS r
            JOIN {RecordSearchKey} AS rs ON rs.{RecordId} = r.{RowId}
            WHERE rs.{SearchKey} = @{SearchKey} AND r.{Spelling} = @{Spelling};
            """;

        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        SqliteParameter spellingParam = new($"@{Spelling}", SqliteType.Text);
        selectSameRecordsCommand.Parameters.AddRange([
            searchKeyParam,
            spellingParam
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        selectSameRecordsCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand updateRecordCommand = connection.CreateCommand();
        updateRecordCommand.CommandText =
            $"""
            UPDATE {Record}
            SET {Frequency} = @{Frequency}
            WHERE {RowId} = @{RowId};
            """;

        SqliteParameter frequencyParamForUpdateCommand = new($"@{Frequency}", SqliteType.Integer);
        SqliteParameter rowIdParamForUpdateCommand = new($"@{RowId}", SqliteType.Integer);
        updateRecordCommand.Parameters.AddRange([
            frequencyParamForUpdateCommand,
            rowIdParamForUpdateCommand
        ]);

#pragma warning disable CA1849 // Call async methods when in an async method
        updateRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

        CommandsAndParameters commandsAndParameters = new(selectSameRecordsCommand, searchKeyParam, spellingParam, updateRecordCommand, frequencyParamForUpdateCommand, rowIdParamForUpdateCommand, insertRecordCommand, rowIdParamForInsertRecordCommand, spellingParamForInsertRecordCommand, frequencyParamForInsertRecordCommand, insertRecordSearchKeyCommand, searchKeyParamForInsertRecordSearchKeyCommand, recordIdParamForInsertRecordSearchKeyCommand);

        int transactionRecordCount = 0;

        Dictionary<string, JsonElement[][]>? frequencyJson;
        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement[][]>>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            Debug.Assert(frequencyJson is not null);
        }

#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

        insertRecordCommand.Transaction = transaction;
        insertRecordSearchKeyCommand.Transaction = transaction;
        selectSameRecordsCommand.Transaction = transaction;
        updateRecordCommand.Transaction = transaction;

        foreach ((string reading, JsonElement[][] value) in frequencyJson)
        {
            foreach (JsonElement[] elementList in value)
            {
                int frequencyRank = elementList[1].GetInt32();
                string exactSpelling = elementList[0]
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    .GetString()!.GetPooledString();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                FrequencyRecord frequencyRecordWithExactSpelling = new(exactSpelling, frequencyRank);
                if (AddOrUpdate(reading, rowId, frequencyRecordWithExactSpelling, true, commandsAndParameters))
                {
                    ++transactionRecordCount;

                    if (generateFusejiVariants)
                    {
                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(reading, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                        {
                            if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithExactSpelling, false, commandsAndParameters))
                            {
                                ++transactionRecordCount;
                            }
                        }
                    }
                }

                string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling).GetPooledString();
                if (exactSpellingInHiragana != reading)
                {
                    FrequencyRecord frequencyRecordWithReading = new(reading, frequencyRank);
                    ++rowId;

                    if (AddOrUpdate(exactSpellingInHiragana, rowId, frequencyRecordWithReading, true, commandsAndParameters))
                    {
                        ++transactionRecordCount;

                        if (generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(exactSpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                {
                                    ++transactionRecordCount;
                                }
                            }
                        }

                        if (generateMazegaki)
                        {
                            foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(exactSpellingInHiragana, reading))
                            {
                                if (AddOrUpdate(mazegakiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                {
                                    ++transactionRecordCount;

                                    if (generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            if (AddOrUpdate(fusejiVariant, rowId, frequencyRecordWithReading, false, commandsAndParameters))
                                            {
                                                ++transactionRecordCount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
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

                    freq.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                    transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                    transactionRecordCount = 0;

                    insertRecordCommand.Transaction = transaction;
                    insertRecordSearchKeyCommand.Transaction = transaction;
                    selectSameRecordsCommand.Transaction = transaction;
                    updateRecordCommand.Transaction = transaction;
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

#pragma warning disable CA1849 // Call async methods when in an async method
        // ReSharper disable once MethodHasAsyncOverload
        transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

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

            freq.Size = GetDistinctSearchKeyCount(connection);
        }
        else
        {
            freq.Size = 0;
        }
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

    internal sealed record class CommandsAndParameters(SqliteCommand SelectSameRecordsCommand,
        SqliteParameter SearchKeyParam,
        SqliteParameter SpellingParam,
        SqliteCommand UpdateRecordCommand,
        SqliteParameter FrequencyParamForUpdateCommand,
        SqliteParameter RowIdParamForUpdateCommand,
        SqliteCommand InsertRecordCommand,
        SqliteParameter RowIdParamForInsertRecordCommand,
        SqliteParameter SpellingParamForInsertRecordCommand,
        SqliteParameter FrequencyParamForInsertRecordCommand,
        SqliteCommand InsertRecordSearchKeyCommand,
        SqliteParameter SearchKeyParamForInsertRecordSearchKeyCommand,
        SqliteParameter RecordIdParamForInsertRecordSearchKeyCommand);

    internal static bool AddOrUpdate(string searchKey,
        ulong recordId,
        FrequencyRecord record,
        bool newRecord,
        CommandsAndParameters commandsAndParameters)
    {
        int existingFrequency = 0;

        commandsAndParameters.SearchKeyParam.Value = searchKey;
        commandsAndParameters.SpellingParam.Value = record.Spelling;

        using SqliteDataReader reader = commandsAndParameters.SelectSameRecordsCommand.ExecuteReader();
        long rowId = 0;
        if (reader.Read())
        {
            rowId = reader.GetInt64(RowIdColumnIndex);
            existingFrequency = reader.GetInt32(FrequencyColumnIndex);
        }

        if (rowId is not 0)
        {
            if (existingFrequency > record.Frequency)
            {
                commandsAndParameters.FrequencyParamForUpdateCommand.Value = record.Frequency;
                commandsAndParameters.RowIdParamForUpdateCommand.Value = rowId;
                _ = commandsAndParameters.UpdateRecordCommand.ExecuteNonQuery();

                return true;
            }

            return false;
        }

        if (newRecord)
        {
            commandsAndParameters.RowIdParamForInsertRecordCommand.Value = recordId;
            commandsAndParameters.SpellingParamForInsertRecordCommand.Value = record.Spelling;
            commandsAndParameters.FrequencyParamForInsertRecordCommand.Value = record.Frequency;
            _ = commandsAndParameters.InsertRecordCommand.ExecuteNonQuery();
        }

        commandsAndParameters.SearchKeyParamForInsertRecordSearchKeyCommand.Value = searchKey;
        commandsAndParameters.RecordIdParamForInsertRecordSearchKeyCommand.Value = recordId;
        _ = commandsAndParameters.InsertRecordSearchKeyCommand.ExecuteNonQuery();

        return true;
    }

    private static FrequencyRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString((int)ColumnIndex.Spelling);
        int frequency = dataReader.GetInt32((int)ColumnIndex.Frequency);

        return new FrequencyRecord(spelling, frequency);
    }
}
