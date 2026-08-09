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
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanDBManager
{
    public const int Version = 34;

    public const int Size = 250000;

    private const string Record = "record";
    private const string RowId = "rowid";
    private const string PrimarySpelling = "primary_spelling";
    private const string Reading = "reading";
    private const string Glossary = "glossary";
    private const string PartOfSpeech = "part_of_speech";
    private const string GlossaryTags = "glossary_tags";
    private const string ImageInfos = "image_infos";
    private const string PopularityScore = "popularity_score";
    private const string RecordSearchKey = "record_search_key";
    private const string RecordId = "record_id";
    private const string SearchKey = "search_key";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{PopularityScore}, r.{Glossary}, r.{PartOfSpeech}, r.{GlossaryTags}, r.{ImageInfos}
        FROM {Record} r
        JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
        WHERE rsk.{SearchKey} = @{Term};
        """;

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    public static string GetQuery(int termCount)
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

    private enum ColumnIndex
    {
        // ReSharper disable once UnusedMember.Local
        RowId = 0,
        PrimarySpelling,
        Reading,
        PopularityScore,
        Glossary,
        PartOfSpeech,
        GlossaryTags,
        ImageInfos,
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

        ulong rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.SetSynchronousModeToNormal(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {PopularityScore}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos})
            VALUES (@{RowId}, @{PrimarySpelling}, @{Reading}, @{PopularityScore}, @{Glossary}, @{PartOfSpeech}, @{GlossaryTags}, @{ImageInfos});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter popularityScoreParam = new($"@{PopularityScore}", SqliteType.Real);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter partOfSpeechParam = new($"@{PartOfSpeech}", SqliteType.Blob);
        SqliteParameter glossaryTagsParam = new($"@{GlossaryTags}", SqliteType.Blob);
        SqliteParameter imageInfosParam = new($"@{ImageInfos}", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            popularityScoreParam,
            glossaryParam,
            partOfSpeechParam,
            glossaryTagsParam,
            imageInfosParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey}({RecordId}, {SearchKey})
            VALUES (@{RecordId}, @{SearchKey});
            """;

        SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach ((EpwingYomichanRecord record, List<string> keys) in recordToKeysDict)
        {
            rowidParam.Value = rowId;
            primarySpellingParam.Value = record.PrimarySpelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            popularityScoreParam.Value = record.PopularityScore;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            glossaryTagsParam.Value = record.DefinitionTags is not null ? MessagePackSerializer.Serialize(record.DefinitionTags) : DBNull.Value;
            imageInfosParam.Value = record.ImageInfos is not null ? MessagePackSerializer.Serialize(record.ImageInfos) : DBNull.Value;
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
        Debug.Assert(!nonNameDict || nonKanjiDict || generateMazegakiOption is not null);
        bool generateMazegaki = nonKanjiDict && nonNameDict
                                             // ReSharper disable once NullableWarningSuppressionIsUsed
                                             && generateMazegakiOption!.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        Debug.Assert(!nonKanjiDict || generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = nonKanjiDict
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                && generateFusejiVariantsOption!.Value;

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        int maxConsecutiveFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;

            Debug.Assert(dict.Options.MaxConsecutiveFusejiCount is not null);
            maxConsecutiveFuseji = dict.Options.MaxConsecutiveFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
            maxConsecutiveFuseji = 0;
        }

        ulong rowId = 1;

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.SetJournalModeToWal(connection);

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {PopularityScore}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos})
            VALUES (@{RowId}, @{PrimarySpelling}, @{Reading}, @{PopularityScore}, @{Glossary}, @{PartOfSpeech}, @{GlossaryTags}, @{ImageInfos});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter scoreParam = new($"@{PopularityScore}", SqliteType.Real);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter partOfSpeechParam = new($"@{PartOfSpeech}", SqliteType.Blob);
        SqliteParameter glossaryTagsParam = new($"@{GlossaryTags}", SqliteType.Blob);
        SqliteParameter imageInfosParam = new($"@{ImageInfos}", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            scoreParam,
            glossaryParam,
            partOfSpeechParam,
            glossaryTagsParam,
            imageInfosParam
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
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly);
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
                await foreach (JsonElement[]? jsonElements in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonElements is not null);

                    EpwingYomichanRecord? record = EpwingYomichanLoader.GetEpwingYomichanRecord(jsonElements, dict);
                    if (record is not null)
                    {
                        rowidParam.Value = rowId;
                        primarySpellingParam.Value = record.PrimarySpelling;
                        readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
                        scoreParam.Value = record.PopularityScore;
                        glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
                        partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
                        glossaryTagsParam.Value = record.DefinitionTags is not null ? MessagePackSerializer.Serialize(record.DefinitionTags) : DBNull.Value;
                        imageInfosParam.Value = record.ImageInfos is not null ? MessagePackSerializer.Serialize(record.ImageInfos) : DBNull.Value;

#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(record.PrimarySpelling).GetPooledString()
                            : record.PrimarySpelling.GetPooledString();

                        _ = keys.Add(primarySpellingInHiragana);

                        if (nonKanjiDict)
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = keys.Add(fusejiVariant);
                                }
                            }

                            if (nonNameDict && record.Reading is not null)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                                if (primarySpellingInHiragana != readingInHiragana)
                                {
                                    if (keys.Add(readingInHiragana))
                                    {
                                        if (generateFusejiVariants)
                                        {
                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                            {
                                                _ = keys.Add(fusejiVariant);
                                            }
                                        }

                                        if (generateMazegaki)
                                        {
                                            foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, readingInHiragana))
                                            {
                                                if (keys.Add(mazegaki) && generateFusejiVariants)
                                                {
                                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                    {
                                                        _ = keys.Add(fusejiVariant);
                                                    }
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
                        if (transactionRecordCount > 20000)
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

            SqliteConnection.ClearAllPools();
            DBUtils.SetJournalModeToDelete(connection);

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
        }
        else
        {
            dict.Size = 0;
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
                GROUP BY {PrimarySpelling}, {Reading}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos}
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, string query)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < terms.Length; i++)
        {
            _ = command.Parameters.AddWithValue(DBUtils.GetParameterName(i + 1), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            EpwingYomichanRecord record = GetRecord(dataReader);
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

    public static List<IDictRecord>? GetRecordsFromDB(string readOnlyConnectionString, string term)
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

        List<IDictRecord> results = [];
        while (dataReader.Read())
        {
            results.Add(GetRecord(dataReader));
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
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString((int)ColumnIndex.SearchKey), JsonOptions.DefaultJso);
            Debug.Assert(searchKeys is not null);

            foreach (string searchKey in searchKeys)
            {
                if (dict.Contents.TryGetValue(searchKey, out IList<IDictRecord>? result))
                {
                    result.Add(record);
                }
                else
                {
                    dict.Contents[searchKey] = [record];
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord GetRecord(SqliteDataReader dataReader)
    {
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        double popularityScore = dataReader.GetDouble((int)ColumnIndex.PopularityScore);

        string[] definitions = dataReader.GetValueFromBlobStream<string[]>((int)ColumnIndex.Glossary);
        string[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.PartOfSpeech);
        string[]? definitionTags = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.GlossaryTags);
        ImageInfo[]? imageInfos = dataReader.GetNullableValueFromBlobStream<ImageInfo[]>((int)ColumnIndex.ImageInfos);

        return new EpwingYomichanRecord(primarySpelling, reading, popularityScore, definitions, wordClasses, definitionTags, imageInfos);
    }
}
