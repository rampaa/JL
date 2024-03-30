using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Yomichan;
internal static class EpwingYomichanDBManager
{
    public const int Version = 1;

    private const string GetRecordsQuery =
        """
        SELECT r.primary_spelling AS primarySpelling,
               r.reading AS reading,
               r.glossary AS definitions,
               r.part_of_speech AS wordClasses,
               r.glossary_tags AS definitionTags
        FROM record r
        JOIN record_search_key rsk ON r.id = rsk.record_id
        WHERE rsk.search_key = @term
        """;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                primary_spelling TEXT NOT NULL,
                reading TEXT,
                glossary TEXT NOT NULL,
                part_of_speech TEXT,
                glossary_tags TEXT
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                record_id INTEGER NOT NULL,
                search_key TEXT NOT NULL,
                PRIMARY KEY (record_id, search_key),
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
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadWrite");
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        ulong id = 1;
        HashSet<EpwingYomichanRecord> yomichanWordRecords = dict.Contents.Values.SelectMany(static v => v).Select(static v => (EpwingYomichanRecord)v).ToHashSet();
        foreach (EpwingYomichanRecord record in yomichanWordRecords)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, primary_spelling, reading, glossary, part_of_speech, glossary_tags)
                VALUES (@id, @primary_spelling, @reading, @glossary, @part_of_speech, @glossary_tags)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@part_of_speech", record.WordClasses is not null ? JsonSerializer.Serialize(record.WordClasses, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary_tags", record.DefinitionTags is not null ? JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull) : DBNull.Value);

            _ = insertRecordCommand.ExecuteNonQuery();

            using SqliteCommand insertPrimarySpellingCommand = connection.CreateCommand();
            insertPrimarySpellingCommand.CommandText =
                """
                INSERT INTO record_search_key(record_id, search_key)
                VALUES (@record_id, @search_key)
                """;
            _ = insertPrimarySpellingCommand.Parameters.AddWithValue("@record_id", id);
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling);
            _ = insertPrimarySpellingCommand.Parameters.AddWithValue("@search_key", primarySpellingInHiragana);
            _ = insertPrimarySpellingCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    using SqliteCommand insertReadingCommand = connection.CreateCommand();
                    insertReadingCommand.CommandText =
                        """
                    INSERT INTO record_search_key(record_id, search_key)
                    VALUES (@record_id, @search_key)
                    """;

                    _ = insertReadingCommand.Parameters.AddWithValue("@record_id", id);
                    _ = insertReadingCommand.Parameters.AddWithValue("@search_key", readingInHiragana);

                    _ = insertReadingCommand.ExecuteNonQuery();
                }
            }

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";

        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();

        dict.Ready = true;
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms, string query)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int termCount = terms.Count;
        for (int i = 0; i < termCount; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (dataReader.HasRows)
        {
            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            while (dataReader.Read())
            {
                EpwingYomichanRecord record = GetRecord(dataReader);

                string searchKey = dataReader.GetString(nameof(searchKey));
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

        return null;
    }

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = GetRecordsQuery;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (dataReader.HasRows)
        {
            List<IDictRecord> results = [];
            while (dataReader.Read())
            {
                results.Add(GetRecord(dataReader));
            }
            return results;
        }

        return null;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT json_group_array(rsk.search_key) AS searchKeys,
                   r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            EpwingYomichanRecord record = GetRecord(dataReader);
            string[] searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(searchKeys)), Utils.s_jsoNotIgnoringNull)!;
            for (int i = 0; i < searchKeys.Length; i++)
            {
                string searchKey = searchKeys[i];
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
        string primarySpelling = dataReader.GetString(nameof(primarySpelling));

        string? reading = null;
        if (dataReader[nameof(reading)] is string readingFromDB)
        {
            reading = readingFromDB;
        }

        string[] definitions = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;

        string[]? wordClasses = null;
        if (dataReader[nameof(wordClasses)] is string wordClassesFromDB)
        {
            wordClasses = JsonSerializer.Deserialize<string[]>(wordClassesFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? definitionTags = null;
        if (dataReader[nameof(definitionTags)] is string definitionTagsFromDB)
        {
            definitionTags = JsonSerializer.Deserialize<string[]>(definitionTagsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags);
    }

    public static string GetQuery(string parameter)
    {
        return
            $"""
            SELECT rsk.search_key AS searchKey,
                   r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN {parameter}
            """;
    }

    public static string GetQuery(List<string> terms)
    {
        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey,
                   r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.glossary AS definitions,
                   r.part_of_speech AS wordClasses,
                   r.glossary_tags AS definitionTags
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        int termsCount = terms.Count;
        for (int i = 1; i < termsCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        return queryBuilder.Append(')').ToString();
    }
}
