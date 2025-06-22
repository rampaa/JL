using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanDBManager
{
    public const int Version = 18;

    private const int SearchKeyIndex = 5;

    private const string SingleTermQuery =
        """
        SELECT r.primary_spelling AS primarySpelling,
               r.reading AS reading,
               r.glossary AS definitions,
               r.part_of_speech AS wordClasses,
               r.glossary_tags AS definitionTags
        FROM record r
        JOIN record_search_key rsk ON r.id = rsk.record_id
        WHERE rsk.search_key = @term;
        """;

    public static string GetQuery(string parameter)
    {
        return
            $"""
            SELECT r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags,
                   rsk.search_key AS searchKey
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN {parameter}
            """;
    }

    public static string GetQuery(int termCount)
    {
        StringBuilder queryBuilder = new(
            """
            SELECT r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags,
                   rsk.search_key AS searchKey
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        return queryBuilder.Append(");").ToString();
    }

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                primary_spelling TEXT NOT NULL,
                reading TEXT,
                glossary BLOB NOT NULL,
                part_of_speech BLOB,
                glossary_tags BLOB
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                search_key TEXT NOT NULL,
                record_id INTEGER NOT NULL,
                PRIMARY KEY (search_key, record_id),
                FOREIGN KEY (record_id) REFERENCES record (id) ON DELETE CASCADE
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

        HashSet<EpwingYomichanRecord> yomichanWordRecords = new(totalRecordCount);
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = yomichanWordRecords.Add((EpwingYomichanRecord)dictRecords[i]);
            }
        }

        ulong id = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, primary_spelling, reading, glossary, part_of_speech, glossary_tags)
            VALUES (@id, @primary_spelling, @reading, @glossary, @part_of_speech, @glossary_tags);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@reading", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@part_of_speech", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@glossary_tags", SqliteType.Blob);
        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key);
            """;

        _ = insertSearchKeyCommand.Parameters.Add("@record_id", SqliteType.Integer);
        _ = insertSearchKeyCommand.Parameters.Add("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Prepare();

        foreach (EpwingYomichanRecord record in yomichanWordRecords)
        {
            _ = insertRecordCommand.Parameters["@id"].Value = id;
            _ = insertRecordCommand.Parameters["@primary_spelling"].Value = record.PrimarySpelling;
            _ = insertRecordCommand.Parameters["@reading"].Value = record.Reading is not null ? record.Reading : DBNull.Value;
            _ = insertRecordCommand.Parameters["@glossary"].Value = MessagePackSerializer.Serialize(record.Definitions);
            _ = insertRecordCommand.Parameters["@part_of_speech"].Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            _ = insertRecordCommand.Parameters["@glossary_tags"].Value = record.DefinitionTags is not null ? MessagePackSerializer.Serialize(record.DefinitionTags) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            _ = insertSearchKeyCommand.Parameters["@record_id"].Value = id;
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling);
            _ = insertSearchKeyCommand.Parameters["@search_key"].Value = primarySpellingInHiragana;
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    _ = insertSearchKeyCommand.Parameters["@search_key"].Value = readingInHiragana;
                    _ = insertSearchKeyCommand.ExecuteNonQuery();
                }
            }

            ++id;
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
            EpwingYomichanRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString(SearchKeyIndex);
            if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
            {
                result.Add(record);
            }
            else
            {
                results[searchKey] = [record];
            }
        }

        return results;
    }

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
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
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags,
                   json_group_array(rsk.search_key) AS searchKeys
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            EpwingYomichanRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString(SearchKeyIndex), Utils.s_jso).Span;
            foreach (ref readonly string searchKey in searchKeys)
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

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord GetRecord(SqliteDataReader dataReader)
    {
        string primarySpelling = dataReader.GetString(0);

        string? reading = !dataReader.IsDBNull(1)
            ? dataReader.GetString(1)
            : null;

        string[]? definitions = MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(2));
        Debug.Assert(definitions is not null);

        string[]? wordClasses = !dataReader.IsDBNull(3)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(3))
            : null;

        string[]? definitionTags = !dataReader.IsDBNull(4)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(4))
            : null;

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags);
    }
}
