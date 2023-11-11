using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JL.Core.Dicts;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.PitchAccent;
internal class YomichanPitchAccentDBManager
{
    public static async Task CreateYomichanPitchAccentDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        await connection.OpenAsync().ConfigureAwait(false);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                spelling_in_hiragana TEXT NOT NULL,
                reading TEXT,
                reading_in_hiragana TEXT,
                position INTEGER NOT NULL,
            ) STRICT;
            """;

        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task InsertToYomichanPitchAccentDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        await connection.OpenAsync().ConfigureAwait(false);
        using DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        int id = 1;
        HashSet<PitchAccentRecord> yomichanPitchAccentRecord = dict.Contents.Values.SelectMany(v => v).Select(v => (PitchAccentRecord)v).ToHashSet();
        foreach (PitchAccentRecord record in yomichanPitchAccentRecord)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, spelling, spelling_in_hiragana, reading, reading_in_hiragana, position)
                VALUES (@id, @spelling, @spelling_in_hiragana, @reading, @reading_in_hiragana, @position)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling", record.Spelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling_in_hiragana", JapaneseUtils.KatakanaToHiragana(record.Spelling));
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading_in_hiragana", record.Reading is not null ? JapaneseUtils.KatakanaToHiragana(record.Reading) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@position", record.Position);

            _ = await insertRecordCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText =
            """
            CREATE INDEX IF NOT EXISTS ix_record_spelling_in_hiragana ON record(spelling_in_hiragana);
            CREATE INDEX IF NOT EXISTS ix_record_reading_in_hiragana ON record(reading_in_hiragana);
            """;

        _ = await createIndexCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = await analyzeCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = await vacuumCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        dict.Ready = true;
    }

    public static bool GetRecordsFromYomichanPitchAccentDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling AS spelling, r.reading AS reading, r.position AS position
            FROM record r
            WHERE r.spelling_in_hiragana = @term OR r.reading_in_hiragana = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            string spelling = (string)dataReader["spelling"];

            object readingFromDB = dataReader["reading"];
            string? reading = readingFromDB is not DBNull
                ? (string)readingFromDB
                : null;

            int position = (int)dataReader["spelling"];

            records.Add(new PitchAccentRecord(spelling, reading, position));
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
