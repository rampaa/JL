using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaDBManager
{
    public const int Version = 18;

    private const string Record = "record";
    private const string RowId = "rowid";
    private const string PrimarySpelling = "primary_spelling";
    private const string Reading = "reading";
    private const string Glossary = "glossary";
    private const string AlternativeSpellings = "alternative_spellings";
    private const string ImageInfo = "image_info";

    private const string RecordSearchKey = "record_search_key";
    private const string RecordId = "record_id";
    private const string SearchKey = "search_key";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}
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
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}, rsk.{SearchKey}
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
        AlternativeSpellings,
        Glossary,
        ImageInfo,
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
                {AlternativeSpellings} BLOB,
                {Glossary} BLOB NOT NULL,
                {ImageInfo} BLOB
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
        if (!File.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameNazeka;

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

        DBUtils.ConfigureForBulkWrite(connection);

        // ReSharper disable once UseAwaitUsing
        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
        $"""
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo})
            VALUES (@{RowId}, @{PrimarySpelling}, @{Reading}, @{AlternativeSpellings}, @{Glossary}, @{ImageInfo});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter imageInfoParam = new($"@{ImageInfo}", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            alternativeSpellingsParam,
            glossaryParam,
            imageInfoParam
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

#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

        insertRecordCommand.Transaction = transaction;
        insertSearchKeyCommand.Transaction = transaction;

        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            IAsyncEnumerator<JsonElement> enumerator = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).GetAsyncEnumerator();
            await using (enumerator.ConfigureAwait(false))
            {
                _ = await enumerator.MoveNextAsync().ConfigureAwait(false);
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    JsonElement jsonObj = enumerator.Current;
                    string reading = jsonObj.GetProperty("r")
                        // ReSharper disable once NullableWarningSuppressionIsUsed
                        .GetString()!.GetPooledString();

                    JsonElement spellingJsonArray = jsonObj.GetProperty("s");
                    List<string>? spellingList = new(spellingJsonArray.GetArrayLength());
                    foreach (JsonElement spellingJsonElement in spellingJsonArray.EnumerateArray())
                    {
                        string? spelling = spellingJsonElement.GetString();
                        if (!string.IsNullOrWhiteSpace(spelling))
                        {
                            spellingList.Add(spelling.GetPooledString());
                        }
                    }

                    if (spellingList.Count is 0)
                    {
                        spellingList = null;
                    }

                    JsonElement definitionJsonArray = jsonObj.GetProperty("l");
                    List<string> definitionList = new(definitionJsonArray.GetArrayLength());
                    foreach (JsonElement definitionJsonElement in definitionJsonArray.EnumerateArray())
                    {
                        string? definition = definitionJsonElement.GetString();
                        if (!string.IsNullOrWhiteSpace(definition))
                        {
                            definitionList.Add(definition.GetPooledString());
                        }
                    }

                    if (definitionList.Count is 0)
                    {
                        continue;
                    }

                    string[] definitions = definitionList.ToArray();
                    definitions.DeduplicateStringsInArray();

                    if (spellingList is not null)
                    {
                        string primarySpelling = spellingList[0];
                        if (primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                        {
                            continue;
                        }

                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
                            : primarySpelling.GetPooledString();

                        ImageInfo? imageInfo = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            string? imagePath = imagePathProperty.GetString();
                            if (imagePath is not null)
                            {
                                imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
                            }
                        }

                        string[]? alternativeSpellings = spellingList.RemoveAtToArray(0);
                        rowidParam.Value = rowId;
                        primarySpellingParam.Value = primarySpelling;
                        readingParam.Value = reading;
                        alternativeSpellingsParam.Value = alternativeSpellings is not null ? MessagePackSerializer.Serialize(alternativeSpellings) : DBNull.Value;
                        glossaryParam.Value = MessagePackSerializer.Serialize(definitions);
                        imageInfoParam.Value = imageInfo is not null ? MessagePackSerializer.Serialize(imageInfo) : DBNull.Value;

#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                        recordIdParam.Value = rowId;
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

                            if (nonNameDict)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
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
                        ++rowId;

                        ReadOnlySpan<string> spellingListSpan = spellingList.AsReadOnlySpan();
                        for (int j = 1; j < spellingListSpan.Length; j++)
                        {
                            ref readonly string alternativeSpelling = ref spellingListSpan[j];
                            if (alternativeSpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                            {
                                continue;
                            }

                            string alternativeSpellingInHiragana = nonKanjiDict
                                ? JapaneseUtils.NormalizeText(alternativeSpelling).GetPooledString()
                                : alternativeSpelling.GetPooledString();

                            if (primarySpellingInHiragana != alternativeSpellingInHiragana)
                            {
                                string[]? altSpellings = spellingList.RemoveAtToArray(j);
                                rowidParam.Value = rowId;
                                primarySpellingParam.Value = alternativeSpelling;
                                readingParam.Value = reading;
                                alternativeSpellingsParam.Value = altSpellings is not null ? MessagePackSerializer.Serialize(altSpellings) : DBNull.Value;
                                glossaryParam.Value = MessagePackSerializer.Serialize(definitions);
                                imageInfoParam.Value = imageInfo is not null ? MessagePackSerializer.Serialize(imageInfo) : DBNull.Value;

#pragma warning disable CA1849 // Call async methods when in an async method
                                _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                                recordIdParam.Value = rowId;
                                searchKeyParam.Value = alternativeSpellingInHiragana;
#pragma warning disable CA1849 // Call async methods when in an async method
                                _ = insertSearchKeyCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                                ++rowId;
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
                            insertRecordCommand.Transaction = transaction;
                            insertSearchKeyCommand.Transaction = transaction;
                        }
                    }

                    else if (!reading.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                    {
                        ImageInfo? imageInfo = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            string? imagePath = imagePathProperty.GetString();
                            if (imagePath is not null)
                            {
                                imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
                            }
                        }

                        rowidParam.Value = rowId;
                        primarySpellingParam.Value = reading;
                        readingParam.Value = DBNull.Value;
                        alternativeSpellingsParam.Value = DBNull.Value;
                        glossaryParam.Value = MessagePackSerializer.Serialize(definitions);
                        imageInfoParam.Value = imageInfo is not null ? MessagePackSerializer.Serialize(imageInfo) : DBNull.Value;

#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                        recordIdParam.Value = rowId;
                        searchKeyParam.Value = nonKanjiDict ? JapaneseUtils.NormalizeText(reading).GetPooledString() : reading;
#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertSearchKeyCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                        ++rowId;
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

                            dict.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                            transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                            transactionRecordCount = 0;
                            insertRecordCommand.Transaction = transaction;
                            insertSearchKeyCommand.Transaction = transaction;
                        }
                    }
                }
            }
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

        if (rowId > 1)
        {
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
        }
        else
        {
            DBUtils.DeleteDB(dict.DBPath);
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
                GROUP BY {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo}
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

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<EpwingNazekaRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                EpwingNazekaRecord record = (EpwingNazekaRecord)records[i];
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
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo})
            VALUES (@{RowId}, @{PrimarySpelling}, @{Reading}, @{AlternativeSpellings}, @{Glossary}, @{ImageInfo});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter readingParam = new($"@{Reading}", SqliteType.Text);
        SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter imageInfoParam = new($"@{ImageInfo}", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            alternativeSpellingsParam,
            glossaryParam,
            imageInfoParam
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

        foreach ((EpwingNazekaRecord record, List<string> keys) in recordToKeysDict)
        {
            rowidParam.Value = rowId;
            primarySpellingParam.Value = record.PrimarySpelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            imageInfoParam.Value = record.ImageInfo is not null ? MessagePackSerializer.Serialize(record.ImageInfo) : DBNull.Value;
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
            EpwingNazekaRecord epwingNazekaRecord = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
            ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, searchKey, out bool exists);
            if (exists)
            {
                Debug.Assert(result is not null);
                result.Add(epwingNazekaRecord);
            }
            else
            {
                result = [epwingNazekaRecord];
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
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}, json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            EpwingNazekaRecord record = GetRecord(dataReader);
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

    private static EpwingNazekaRecord GetRecord(SqliteDataReader dataReader)
    {
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        string[]? alternativeSpellings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.AlternativeSpellings);
        string[] definitions = dataReader.GetValueFromBlobStream<string[]>((int)ColumnIndex.Glossary);

        const int imageInfoIndex = (int)ColumnIndex.ImageInfo;
        ImageInfo? imageInfo = !dataReader.IsDBNull(imageInfoIndex)
            ? dataReader.GetValueFromBlobStream<ImageInfo>((int)ColumnIndex.ImageInfo)
            : null;

        return new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions, imageInfo);
    }
}
