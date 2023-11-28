using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Nazeka;
internal static class EpwingNazekaDBManager
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
                alternative_spellings TEXT,
                glossary TEXT NOT NULL
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
        HashSet<EpwingNazekaRecord> nazekaWordRecords = dict.Contents.Values.SelectMany(static v => v).Select(static v => (EpwingNazekaRecord)v).ToHashSet();
        foreach (EpwingNazekaRecord record in nazekaWordRecords)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, primary_spelling, reading, alternative_spellings, glossary)
                VALUES (@id, @primary_spelling, @reading, @alternative_spellings, @glossary)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull));

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

    public static Dictionary<string, List<IDictRecord>> GetRecordsFromDB(string dbName, List<string> terms)
    {
        Dictionary<string, List<IDictRecord>> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey,
                   r.primary_spelling AS primarySpelling,
                   r.reading AS reading,
                   r.alternative_spellings AS alternativeSpellings,
                   r.glossary AS definitions
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term1
            """);

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

            string[]? alternativeSpellings = null;
            if (dataReader[nameof(alternativeSpellings)] is string alternativeSpellingsFromDB)
            {
                alternativeSpellings = JsonSerializer.Deserialize<string[]>(alternativeSpellingsFromDB, Utils.s_jsoNotIgnoringNull);
            }

            string[] definitions = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;

            if (results.TryGetValue(searchKey, out List<IDictRecord>? result))
            {
                result.Add(new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions));
            }

            else
            {
                results[searchKey] = new List<IDictRecord> { new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions) };
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
            SELECT r.primary_spelling AS primarySpelling, r.reading AS reading, r.alternative_spellings AS alternativeSpellings, r.glossary AS definitions
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

            string[]? alternativeSpellings = null;
            if (dataReader[nameof(alternativeSpellings)] is string alternativeSpellingsFromDB)
            {
                alternativeSpellings = JsonSerializer.Deserialize<string[]>(alternativeSpellingsFromDB, Utils.s_jsoNotIgnoringNull);
            }

            string[] definitions = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;

            results.Add(new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions));
        }

        return results;
    }
}
