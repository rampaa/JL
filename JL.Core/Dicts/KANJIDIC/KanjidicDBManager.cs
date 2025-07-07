using System.Collections.Frozen;
using System.Globalization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KANJIDIC;

internal static class KanjidicDBManager
{
    public const int Version = 3;

    private const string SingleTermQuery =
        """
        SELECT r.on_readings, r.kun_readings, r.nanori_readings, r.radical_names, r.glossary, r.stroke_count, r.grade, r.frequency
        FROM record r
        WHERE r.kanji = @term;
        """;

    private enum ColumnIndex
    {
        OnReadings = 0,
        KunReadings,
        NanoriReadings,
        RadicalNames,
        Glossary,
        StrokeCount,
        Grade,
        Frequency,
        Kanji
    }

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                kanji TEXT NOT NULL PRIMARY KEY,
                on_readings BLOB,
                kun_readings BLOB,
                nanori_readings BLOB,
                radical_names BLOB,
                glossary BLOB,
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
        _ = insertRecordCommand.Parameters.Add("@on_readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@kun_readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@nanori_readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@radical_names", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@stroke_count", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@grade", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@frequency", SqliteType.Integer);
        insertRecordCommand.Prepare();

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)records[i];
                _ = insertRecordCommand.Parameters["@kanji"].Value = kanji;
                _ = insertRecordCommand.Parameters["@on_readings"].Value = kanjidicRecord.OnReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.OnReadings) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@kun_readings"].Value = kanjidicRecord.KunReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.KunReadings) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@nanori_readings"].Value = kanjidicRecord.NanoriReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.NanoriReadings) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@radical_names"].Value = kanjidicRecord.RadicalNames is not null ? MessagePackSerializer.Serialize(kanjidicRecord.RadicalNames) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@glossary"].Value = kanjidicRecord.Definitions is not null ? MessagePackSerializer.Serialize(kanjidicRecord.Definitions) : DBNull.Value;
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

        _ = dataReader.Read();
        return [GetRecord(dataReader)];
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.on_readings, r.kun_readings, r.nanori_readings, r.radical_names, r.glossary, r.stroke_count, r.grade, r.frequency, r.kanji
            FROM record r;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            IDictRecord[] record = [GetRecord(dataReader)];
            string kanji = dataReader.GetString((int)ColumnIndex.Kanji);
            dict.Contents[kanji] = record;
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static KanjidicRecord GetRecord(SqliteDataReader dataReader)
    {
        int onReadingsIndex = (int)ColumnIndex.OnReadings;
        string[]? onReadings = !dataReader.IsDBNull(onReadingsIndex)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(onReadingsIndex))
            : null;

        int kunReadingsIndex = (int)ColumnIndex.KunReadings;
        string[]? kunReadings = !dataReader.IsDBNull(kunReadingsIndex)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(kunReadingsIndex))
            : null;

        int nanoriReadingsIndex = (int)ColumnIndex.NanoriReadings;
        string[]? nanoriReadings = !dataReader.IsDBNull(nanoriReadingsIndex)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(nanoriReadingsIndex))
            : null;

        int radicalNamesIndex = (int)ColumnIndex.RadicalNames;
        string[]? radicalNames = !dataReader.IsDBNull(radicalNamesIndex)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(radicalNamesIndex))
            : null;

        int glossaryIndex = (int)ColumnIndex.Glossary;
        string[]? definitions = !dataReader.IsDBNull(glossaryIndex)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(glossaryIndex))
            : null;

        byte strokeCount = dataReader.GetByte((int)ColumnIndex.StrokeCount);
        byte grade = dataReader.GetByte((int)ColumnIndex.Grade);
        int frequency = dataReader.GetInt32((int)ColumnIndex.Frequency);
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
