using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaDBManager
{
    public const int Version = 17;

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

    public static void InsertRecordsToDB(Dict dict)
    {
        Dictionary<EpwingNazekaRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                EpwingNazekaRecord record = (EpwingNazekaRecord)records[i];
                if (recordToKeysDict.TryGetValue(record, out List<string>? keys))
                {
                    keys.Add(key);
                }
                else
                {
                    recordToKeysDict[record] = [key];
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
            INSERT INTO record ({RowId}, {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo})
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
            if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
            {
                result.Add(epwingNazekaRecord);
            }
            else
            {
                results[searchKey] = [epwingNazekaRecord];
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
