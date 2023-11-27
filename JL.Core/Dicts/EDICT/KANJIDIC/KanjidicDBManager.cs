using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EDICT.KANJIDIC;
internal static class KanjidicDBManager
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
                kanji TEXT NOT NULL,
                on_readings TEXT,
                kun_readings TEXT,
                nanori_readings TEXT,
                radical_names TEXT,
                glossary TEXT,
                stroke_count INTEGER NOT NULL,
                grade INTEGER NOT NULL,
                frequency INTEGER NOT NULL,
                jlpt INTEGER
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
        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            foreach (IDictRecord record in records)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)record;

                using SqliteCommand insertRecordCommand = connection.CreateCommand();
                insertRecordCommand.CommandText =
                    """
                    INSERT INTO record (id, kanji, on_readings, kun_readings, nanori_readings, radical_names, glossary, stroke_count, grade, frequency)
                    VALUES (@id, @kanji, @on_readings, @kun_readings, @nanori_readings, @radical_names, @glossary, @stroke_count, @grade, @frequency)
                    """;

                _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
                _ = insertRecordCommand.Parameters.AddWithValue("@kanji", kanji);
                _ = insertRecordCommand.Parameters.AddWithValue("@on_readings", kanjidicRecord.OnReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.OnReadings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@kun_readings", kanjidicRecord.KunReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.KunReadings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@nanori_readings", kanjidicRecord.NanoriReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.NanoriReadings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@radical_names", kanjidicRecord.RadicalNames is not null ? JsonSerializer.Serialize(kanjidicRecord.RadicalNames, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@glossary", kanjidicRecord.Definitions is not null ? JsonSerializer.Serialize(kanjidicRecord.Definitions, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
                _ = insertRecordCommand.Parameters.AddWithValue("@stroke_count", kanjidicRecord.StrokeCount);
                _ = insertRecordCommand.Parameters.AddWithValue("@grade", kanjidicRecord.Grade);
                _ = insertRecordCommand.Parameters.AddWithValue("@frequency", kanjidicRecord.Frequency);

                _ = insertRecordCommand.ExecuteNonQuery();

                ++id;
            }
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_kanji ON record(kanji);";
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

    public static List<IDictRecord> GetRecordsFromDB(string dbName, string term)
    {
        List<IDictRecord> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.on_readings AS onReadings, r.kun_readings AS kunReadings, r.nanori_readings AS nanoriReadings, r.radical_names AS radicalNames, r.glossary AS definitions, r.stroke_count AS strokeCount, r.grade AS grade, r.frequency
            FROM record r
            WHERE r.kanji = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            object onReadingsFromDB = dataReader["onReadings"];
            string[]? onReadings = onReadingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)onReadingsFromDB, Utils.s_jsoNotIgnoringNull)
                : null;

            object kunReadingsFromDB = dataReader["kunReadings"];
            string[]? kunReadings = kunReadingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)kunReadingsFromDB, Utils.s_jsoNotIgnoringNull)
                : null;

            object nanoriReadingsFromDB = dataReader["nanoriReadings"];
            string[]? nanoriReadings = nanoriReadingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)nanoriReadingsFromDB, Utils.s_jsoNotIgnoringNull)
                : null;

            object radicalNamesFromDB = dataReader["radicalNames"];
            string[]? radicalNames = radicalNamesFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)radicalNamesFromDB, Utils.s_jsoNotIgnoringNull)
                : null;

            object definitionsFromDB = dataReader["definitions"];
            string[]? definitions = definitionsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)definitionsFromDB, Utils.s_jsoNotIgnoringNull)
                : null;

            int strokeCount = (int)dataReader["strokeCount"];
            int grade = (int)dataReader["grade"];
            int frequency = (int)dataReader["frequency"];

            results.Add(new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency));
        }

        return results;
    }
}
