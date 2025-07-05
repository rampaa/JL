using System.Collections.Frozen;
using System.Data;
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

        using SqliteDataReader dataReader = command.ExecuteReader(CommandBehavior.SingleRow);
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
            string kanji = dataReader.GetString(8);
            dict.Contents[kanji] = [GetRecord(dataReader)];
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static KanjidicRecord GetRecord(SqliteDataReader dataReader)
    {
        string[]? onReadings = !dataReader.IsDBNull(0)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(0))
            : null;

        string[]? kunReadings = !dataReader.IsDBNull(1)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(1))
            : null;

        string[]? nanoriReadings = !dataReader.IsDBNull(2)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(2))
            : null;

        string[]? radicalNames = !dataReader.IsDBNull(3)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(3))
            : null;

        string[]? definitions = !dataReader.IsDBNull(4)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(4))
            : null;

        byte strokeCount = dataReader.GetByte(5);
        byte grade = dataReader.GetByte(6);
        int frequency = dataReader.GetInt32(7);
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
