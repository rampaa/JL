using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.YomichanKanji;
using JL.Core.Dicts;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;
internal static class FreqDBManager
{
    public static void CreateFrequencyDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(dbName)};"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                frequency INTEGER NOT NULL
            );

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

    public static void InsertToYomichanKanjiDB(Freq freq)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(freq.Name)};Mode=ReadWrite"));
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        int id = 1;
        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, spelling, frequency)
                VALUES (@id, @spelling, @frequency)
                """;

            for (int i = 0; i < records.Count; i++)
            {
                FrequencyRecord record = records[i];
                _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
                _ = insertRecordCommand.Parameters.AddWithValue("@spelling", record.Spelling);
                _ = insertRecordCommand.Parameters.AddWithValue("@frequency", record.Frequency);
                _ = insertRecordCommand.ExecuteNonQuery();
            }

            using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
            insertSearchKeyCommand.CommandText =
                """
                INSERT INTO record_search_key (record_id, search_key)
                VALUES (@record_id, @search_key)
                """;

            _ = insertSearchKeyCommand.Parameters.AddWithValue("@record_id", id);
            _ = insertSearchKeyCommand.Parameters.AddWithValue("@search_key", key);
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";

        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public static bool GetRecordsFromYomichanKanjiDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.on_readings AS onReadings, r.kun_readings AS kunReadings, r.glossary AS definitions, r.stats AS stats,
            FROM record r
            WHERE r.kanji = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            object onReadingsFromDB = dataReader["onReadings"];
            string[]? onReadings = onReadingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)onReadingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object kunReadingsFromDB = dataReader["kunReadings"];
            string[]? kunReadings = kunReadingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)kunReadingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object definitionsFromDB = dataReader["definitions"];
            string[]? definitions = definitionsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)definitionsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object statsFromDB = dataReader["stats"];
            string[]? stats = statsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)statsFromDB, Utils.s_jsoWithIndentation)
                : null;

            records.Add(new YomichanKanjiRecord(onReadings, kunReadings, definitions, stats));
        }

        if (records.Count > 0)
        {
            value = records;
            return true;
        }

        value = null;
        return false;
    }
}
