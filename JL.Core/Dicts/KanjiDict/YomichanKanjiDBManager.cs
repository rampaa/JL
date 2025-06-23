using System.Collections.Frozen;
using System.Globalization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiDBManager
{
    public const int Version = 3;

    private const string SingleTermQuery =
        """
        SELECT r.on_readings,
               r.kun_readings,
               r.glossary,
               r.stats
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
                id INTEGER NOT NULL PRIMARY KEY,
                kanji TEXT NOT NULL,
                on_readings BLOB,
                kun_readings BLOB,
                glossary BLOB,
                stats BLOB
            ) STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict dict)
    {
        ulong id = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, kanji, on_readings, kun_readings, glossary, stats)
            VALUES (@id, @kanji, @on_readings, @kun_readings, @glossary, @stats);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@kanji", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@on_readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@kun_readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@stats", SqliteType.Blob);
        insertRecordCommand.Prepare();

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                YomichanKanjiRecord yomichanKanjiRecord = (YomichanKanjiRecord)records[i];
                _ = insertRecordCommand.Parameters["@id"].Value = id;
                _ = insertRecordCommand.Parameters["@kanji"].Value = kanji;
                _ = insertRecordCommand.Parameters["@on_readings"].Value = yomichanKanjiRecord.OnReadings is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.OnReadings) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@kun_readings"].Value = yomichanKanjiRecord.KunReadings is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.KunReadings) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@glossary"].Value = yomichanKanjiRecord.Definitions is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.Definitions) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@stats"].Value = yomichanKanjiRecord.Stats is not null ? MessagePackSerializer.Serialize(yomichanKanjiRecord.Stats) : DBNull.Value;
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
            SELECT r.on_readings,
                   r.kun_readings,
                   r.glossary,
                   r.stats,
                   r.kanji
            FROM record r;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            YomichanKanjiRecord record = GetRecord(dataReader);
            string kanji = dataReader.GetString(4);
            if (dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result))
            {
                result.Add(record);
            }
            else
            {
                dict.Contents[kanji] = [record];
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static YomichanKanjiRecord GetRecord(SqliteDataReader dataReader)
    {
        string[]? onReadings = !dataReader.IsDBNull(0)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(0))
            : null;

        string[]? kunReadings = !dataReader.IsDBNull(1)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(1))
            : null;

        string[]? definitions = !dataReader.IsDBNull(2)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(2))
            : null;

        string[]? stats = !dataReader.IsDBNull(3)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(3))
            : null;

        return new YomichanKanjiRecord(onReadings, kunReadings, definitions, stats);
    }
}
