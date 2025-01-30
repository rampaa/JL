using System.Collections.Frozen;
using System.Data;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
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
        WHERE r.kanji = @term;
        """;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
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
        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (kanji, on_readings, kun_readings, nanori_readings, radical_names, glossary, stroke_count, grade, frequency)
            VALUES (@kanji, @on_readings, @kun_readings, @nanori_readings, @radical_names, @glossary, @stroke_count, @grade, @frequency);
            """;

        _ = insertRecordCommand.Parameters.Add("@kanji", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@on_readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@kun_readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@nanori_readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@radical_names", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@stroke_count", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@grade", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@frequency", SqliteType.Integer);
        insertRecordCommand.Prepare();

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            foreach (IDictRecord record in records)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)record;
                _ = insertRecordCommand.Parameters["@kanji"].Value = kanji;
                _ = insertRecordCommand.Parameters["@on_readings"].Value = kanjidicRecord.OnReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.OnReadings, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@kun_readings"].Value = kanjidicRecord.KunReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.KunReadings, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@nanori_readings"].Value = kanjidicRecord.NanoriReadings is not null ? JsonSerializer.Serialize(kanjidicRecord.NanoriReadings, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@radical_names"].Value = kanjidicRecord.RadicalNames is not null ? JsonSerializer.Serialize(kanjidicRecord.RadicalNames, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@glossary"].Value = kanjidicRecord.Definitions is not null ? JsonSerializer.Serialize(kanjidicRecord.Definitions, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@stroke_count"].Value = kanjidicRecord.StrokeCount;
                _ = insertRecordCommand.Parameters["@grade"].Value = kanjidicRecord.Grade;
                _ = insertRecordCommand.Parameters["@frequency"].Value = kanjidicRecord.Frequency;
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
    }

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<IDictRecord> results = [];
        while (dataReader.Read())
        {
            results.Add(GetRecord(dataReader));
        }

        return results;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
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
            FROM record r;
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
            onReadings = JsonSerializer.Deserialize<string[]>(onReadingsFromDB, Utils.s_jso);
        }

        string[]? kunReadings = null;
        if (dataReader[nameof(kunReadings)] is string kunReadingsFromDB)
        {
            kunReadings = JsonSerializer.Deserialize<string[]>(kunReadingsFromDB, Utils.s_jso);
        }

        string[]? nanoriReadings = null;
        if (dataReader[nameof(nanoriReadings)] is string nanoriReadingsFromDB)
        {
            nanoriReadings = JsonSerializer.Deserialize<string[]>(nanoriReadingsFromDB, Utils.s_jso);
        }

        string[]? radicalNames = null;
        if (dataReader[nameof(radicalNames)] is string radicalNamesFromDB)
        {
            radicalNames = JsonSerializer.Deserialize<string[]>(radicalNamesFromDB, Utils.s_jso);
        }

        string[]? definitions = null;
        if (dataReader[nameof(definitions)] is string definitionsFromDB)
        {
            definitions = JsonSerializer.Deserialize<string[]>(definitionsFromDB, Utils.s_jso);
        }

        byte strokeCount = dataReader.GetByte(nameof(strokeCount));
        byte grade = dataReader.GetByte(nameof(grade));
        int frequency = dataReader.GetInt32(nameof(frequency));
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
