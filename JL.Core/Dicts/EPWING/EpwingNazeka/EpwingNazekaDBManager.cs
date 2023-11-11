using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.EpwingNazeka;
internal class EpwingNazekaDBManager
{
    public static async Task CreateNazekaWordDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        await connection.OpenAsync().ConfigureAwait(false);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                primary_spelling TEXT NOT NULL,
                primary_spelling_in_hiragana TEXT NOT NULL,
                reading TEXT,
                reading_in_hiragana TEXT,
                alternative_spellings TEXT,
                glossary TEXT NOT NULL
            ) STRICT;
            """;

        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task InsertToNazekaWordDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        await connection.OpenAsync().ConfigureAwait(false);
        using DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        int id = 1;
        HashSet<EpwingNazekaRecord> nazekaWordRecords = dict.Contents.Values.SelectMany(v => v).Select(v => (EpwingNazekaRecord)v).ToHashSet();
        foreach (EpwingNazekaRecord record in nazekaWordRecords)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, primary_spelling, primary_spelling_in_hiragana, reading, reading_in_hiragana, alternative_spellings, glossary)
                VALUES (@id, @primary_spelling, @primary_spelling_in_hiragana, @reading, @reading_in_hiragana, @alternative_spellings, @glossary)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling_in_hiragana", JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling));
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading_in_hiragana", record.Reading is not null ? JapaneseUtils.KatakanaToHiragana(record.Reading) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoWithIndentation) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoWithIndentation));

            _ = await insertRecordCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText =
            """
            CREATE INDEX IF NOT EXISTS ix_record_primary_spelling_in_hiragana ON record(primary_spelling_in_hiragana);
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

    public static bool GetRecordsFromNazekaWordDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.primary_spelling AS primarySpelling, r.reading AS reading, r.alternative_spellings AS alternativeSpellings, r.glossary AS definitions
            FROM record r
            WHERE r.primary_spelling_in_hiragana = @term OR r.reading_in_hiragana = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            string primarySpelling = (string)dataReader["primarySpelling"];

            object readingFromDB = dataReader["reading"];
            string? reading = readingFromDB is not DBNull
                ? (string)readingFromDB
                : null;

            object alternativeSpellingsFromDB = dataReader["alternativeSpellings"];
            string[]? alternativeSpellings = alternativeSpellingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)alternativeSpellingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            string[] definitions = JsonSerializer.Deserialize<string[]>((string)dataReader["definitions"])!;

            records.Add(new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions));
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
