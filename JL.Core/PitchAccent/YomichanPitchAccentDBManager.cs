using System.Data.Common;
using System.Globalization;
using System.Text;
using JL.Core.Dicts;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.PitchAccent;
internal class YomichanPitchAccentDBManager
{
    public static void CreateYomichanPitchAccentDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                reading TEXT,
                position INTEGER NOT NULL,
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

    public static void InsertToYomichanPitchAccentDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        int id = 1;
        HashSet<PitchAccentRecord> yomichanPitchAccentRecord = dict.Contents.Values.SelectMany(v => v).Select(v => (PitchAccentRecord)v).ToHashSet();
        foreach (PitchAccentRecord record in yomichanPitchAccentRecord)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, spelling, reading, position)
                VALUES (@id, @spelling, @reading, @position)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling", record.Spelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@position", record.Position);

            _ = insertRecordCommand.ExecuteNonQuery();

            using SqliteCommand insertSpellingCommand = connection.CreateCommand();
            insertSpellingCommand.CommandText =
                """
                INSERT INTO record_search_key(record_id, search_key)
                VALUES (@record_id, @search_key)
                """;
            _ = insertSpellingCommand.Parameters.AddWithValue("@record_id", id);

            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Spelling);
            _ = insertSpellingCommand.Parameters.AddWithValue("@search_key", primarySpellingInHiragana);
            _ = insertSpellingCommand.ExecuteNonQuery();

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

    public static Dictionary<string, List<IDictRecord>> GetRecordsFromYomichanPitchAccentDB(string dbName, List<string> terms)
    {
        Dictionary<string, List<IDictRecord>> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey, r.spelling AS spelling, r.reading AS reading, r.position AS position
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
            string searchKey = (string)dataReader["searchKey"];
            string spelling = (string)dataReader["spelling"];

            object readingFromDB = dataReader["reading"];
            string? reading = readingFromDB is not DBNull
                ? (string)readingFromDB
                : null;

            int position = (int)dataReader["spelling"];

            if (results.TryGetValue(searchKey, out List<IDictRecord>? result))
            {
                result.Add(new PitchAccentRecord(spelling, reading, position));
            }

            else
            {
                results[searchKey] = new List<IDictRecord> { new PitchAccentRecord(spelling, reading, position) };
            }
        }

        return results;
    }
}
