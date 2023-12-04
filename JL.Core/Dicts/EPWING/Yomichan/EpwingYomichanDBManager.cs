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
    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
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
            ) STRICT;
            """;

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        int id = 1;
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

    public static Dictionary<string, IList<IDictRecord>> GetRecordsFromDB(string dbName, List<string> terms)
    {
        Dictionary<string, IList<IDictRecord>> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

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
            WHERE rsk.search_key = @term1
            """
            );

        for (int i = 1; i < terms.Count; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $"\nOR rsk.search_key = @term{i + 1}");
        }

        command.CommandText = queryBuilder.ToString();

        for (int i = 0; i < terms.Count; i++)
        {
            _ = command.Parameters.AddWithValue($"@term{i + 1}", terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            string searchKey = dataReader.GetString(nameof(searchKey));
            string primarySpelling = dataReader.GetString(nameof(primarySpelling));

            string? reading = null;
            if (dataReader[nameof(reading)] is string readingFromDB)
            {
                reading = readingFromDB;
            }

            string[] definitions = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;

            string[]? wordClasses = null;
            if (dataReader[nameof(wordClasses)] is string wordClassFromDB)
            {
                wordClasses = JsonSerializer.Deserialize<string[]>(wordClassFromDB, Utils.s_jsoNotIgnoringNull);
            }

            string[]? definitionTags = null;
            if (dataReader[nameof(definitionTags)] is string definitionTagsFromDB)
            {
                definitionTags = JsonSerializer.Deserialize<string[]>(definitionTagsFromDB, Utils.s_jsoNotIgnoringNull);
            }

            if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
            {
                result.Add(new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags));
            }

            else
            {
                results[searchKey] = new List<IDictRecord> { new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags) };
            }
        }

        return results;
    }

    public static List<IDictRecord> GetRecordsFromDB(string dbName, string term)
    {
        List<IDictRecord> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.primary_spelling AS primarySpelling, r.reading AS reading, r.glossary AS definitions, r.part_of_speech AS wordClasses, r.glossary_tags AS definitionTags
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
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

            results.Add(new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags));
        }

        return results;
    }
}
