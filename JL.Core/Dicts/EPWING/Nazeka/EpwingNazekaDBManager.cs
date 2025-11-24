using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaDBManager
{
    public const int Version = 11;

    private const string SingleTermQuery =
        """
        SELECT r.rowid, r.primary_spelling, r.reading, r.alternative_spellings, r.glossary
        FROM record r
        JOIN record_search_key rsk ON r.rowid = rsk.record_id
        WHERE rsk.search_key = @term;
        """;

    public static string GetQuery(string parameter)
    {
        return
            $"""
            SELECT r.rowid, r.primary_spelling, r.reading, r.alternative_spellings, r.glossary, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            WHERE rsk.search_key IN {parameter}
            """;
    }

    public static string GetQuery(int termCount)
    {
        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            """
            SELECT r.rowid, r.primary_spelling, r.reading, r.alternative_spellings, r.glossary, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        string query = queryBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(queryBuilder);
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
        SearchKey
    }

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                rowid INTEGER NOT NULL PRIMARY KEY,
                primary_spelling TEXT NOT NULL,
                reading TEXT,
                alternative_spellings BLOB,
                glossary BLOB NOT NULL
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                search_key TEXT NOT NULL,
                record_id INTEGER NOT NULL,
                PRIMARY KEY (search_key, record_id),
                FOREIGN KEY (record_id) REFERENCES record (rowid) ON DELETE CASCADE
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
        int totalRecordCount = 0;
        ICollection<IList<IDictRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<EpwingNazekaRecord> nazekaWordRecords = new(totalRecordCount);
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = nazekaWordRecords.Add((EpwingNazekaRecord)dictRecords[i]);
            }
        }

        ulong rowId = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        DBUtils.SetSynchronousModeToNormal(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (rowid, primary_spelling, reading, alternative_spellings, glossary)
            VALUES (@rowid, @primary_spelling, @reading, @alternative_spellings, @glossary);
            """;

        SqliteParameter rowidParam = new("@rowid", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new("@primary_spelling", SqliteType.Text);
        SqliteParameter readingParam = new("@reading", SqliteType.Text);
        SqliteParameter alternativeSpellingsParam = new("@alternative_spellings", SqliteType.Blob);
        SqliteParameter glossaryParam = new("@glossary", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            alternativeSpellingsParam,
            glossaryParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key);
            """;

        SqliteParameter recordIdParam = new("@record_id", SqliteType.Integer);
        SqliteParameter searchKeyParam = new("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach (EpwingNazekaRecord record in nazekaWordRecords)
        {
            rowidParam.Value = rowId;
            primarySpellingParam.Value = record.PrimarySpelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowId;
            string primarySpellingInHiragana = JapaneseUtils.NormalizeText(record.PrimarySpelling);
            searchKeyParam.Value = primarySpellingInHiragana;
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    searchKeyParam.Value = readingInHiragana;
                    _ = insertSearchKeyCommand.ExecuteNonQuery();
                }
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string query)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < terms.Length; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
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

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;

        _ = command.Parameters.AddWithValue("@term", term);

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
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.rowid, r.primary_spelling, r.reading, r.alternative_spellings, r.glossary, json_group_array(rsk.search_key)
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            GROUP BY r.rowid;
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

        return new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions);
    }
}
