using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KANJIDIC;
internal static class KanjidicDBManager
{
    public const int Version = 2;

    private const string SingleTermQuery =
        """
        SELECT r.on_readings AS onReadings,
               r.kun_readings AS kunReadings,
               r.nanori_readings AS nanoriReadings,
               r.radical_names AS radicalNames,
               r.glossary AS definitions,
               r.stroke_count AS strokeCount,
               r.grade AS grade,
               r.frequency AS frequency
        FROM record r
        WHERE r.kanji = @term
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
                kanji TEXT NOT NULL PRIMARY KEY,
                on_readings TEXT,
                kun_readings TEXT,
                nanori_readings TEXT,
                radical_names TEXT,
                glossary TEXT,
                stroke_count INTEGER NOT NULL,
                grade INTEGER NOT NULL,
                frequency INTEGER NOT NULL
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

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            foreach (IDictRecord record in records)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)record;

                using SqliteCommand insertRecordCommand = connection.CreateCommand();
                insertRecordCommand.CommandText =
                    """
                    INSERT INTO record (kanji, on_readings, kun_readings, nanori_readings, radical_names, glossary, stroke_count, grade, frequency)
                    VALUES (@kanji, @on_readings, @kun_readings, @nanori_readings, @radical_names, @glossary, @stroke_count, @grade, @frequency)
                    """;

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
            }
        }

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();

        dict.Ready = true;
    }

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;

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
            SELECT r.kanji AS kanji,
                   r.on_readings AS onReadings,
                   r.kun_readings AS kunReadings,
                   r.nanori_readings AS nanoriReadings,
                   r.radical_names AS radicalNames,
                   r.glossary AS definitions,
                   r.stroke_count AS strokeCount,
                   r.grade AS grade,
                   r.frequency AS frequency
            FROM record r
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            string kanji = dataReader.GetString(nameof(kanji));
            dict.Contents[kanji] = [GetRecord(dataReader)];
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static KanjidicRecord GetRecord(SqliteDataReader dataReader)
    {
        string[]? onReadings = null;
        if (dataReader[nameof(onReadings)] is string onReadingsFromDB)
        {
            onReadings = JsonSerializer.Deserialize<string[]>(onReadingsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? kunReadings = null;
        if (dataReader[nameof(kunReadings)] is string kunReadingsFromDB)
        {
            kunReadings = JsonSerializer.Deserialize<string[]>(kunReadingsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? nanoriReadings = null;
        if (dataReader[nameof(nanoriReadings)] is string nanoriReadingsFromDB)
        {
            nanoriReadings = JsonSerializer.Deserialize<string[]>(nanoriReadingsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? radicalNames = null;
        if (dataReader[nameof(radicalNames)] is string radicalNamesFromDB)
        {
            radicalNames = JsonSerializer.Deserialize<string[]>(radicalNamesFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? definitions = null;
        if (dataReader[nameof(definitions)] is string definitionsFromDB)
        {
            definitions = JsonSerializer.Deserialize<string[]>(definitionsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        byte strokeCount = dataReader.GetByte(nameof(strokeCount));
        byte grade = dataReader.GetByte(nameof(grade));
        int frequency = dataReader.GetInt32(nameof(frequency));
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
