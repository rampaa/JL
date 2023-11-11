using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.YomichanKanji;
internal class YomichanKanjiDBManager
{
    public static async Task CreateYomichanKanjiDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        await connection.OpenAsync().ConfigureAwait(false);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                kanji TEXT NOT NULL,
                on_readings TEXT,
                kun_readings TEXT,
                glossary TEXT,
                stats TEXT,
                tags TEXT
            ) STRICT;
            """;

        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task InsertToYomichanKanjiDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        await connection.OpenAsync().ConfigureAwait(false);
        using DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        int id = 1;
        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            foreach (IDictRecord record in records)
            {
                YomichanKanjiRecord yomichanKanjiRecord = (YomichanKanjiRecord)record;

                using SqliteCommand insertRecordCommand = connection.CreateCommand();
                insertRecordCommand.CommandText =
                    """
                    INSERT INTO record (id, kanji, on_readings, kun_readings, glossary, stats)
                    VALUES (@id, @kanji, @on_readings, @kun_readings, @glossary, @stats)
                    """;

                _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
                _ = insertRecordCommand.Parameters.AddWithValue("@kanji", kanji);
                _ = insertRecordCommand.Parameters.AddWithValue("@on_readings", yomichanKanjiRecord.OnReadings is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.OnReadings, Utils.s_jsoWithIndentation) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@kun_readings", yomichanKanjiRecord.KunReadings is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.KunReadings, Utils.s_jsoWithIndentation) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@glossary", yomichanKanjiRecord.Definitions is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.Definitions, Utils.s_jsoWithIndentation) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@stats", yomichanKanjiRecord.Stats is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.Stats, Utils.s_jsoWithIndentation) : DBNull.Value);

                _ = await insertRecordCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

                ++id;
            }
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText =
            """
            CREATE INDEX IF NOT EXISTS ix_record_kanji ON record(kanji);
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

    public static bool GetRecordsFromYomichanKanjiDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
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
