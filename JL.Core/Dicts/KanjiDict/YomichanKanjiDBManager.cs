using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiDBManager
{
    public const int Version = 2;

    private const string SingleTermQuery =
        """
        SELECT r.on_readings AS onReadings,
               r.kun_readings AS kunReadings,
               r.glossary AS definitions,
               r.stats AS stats
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
                on_readings TEXT,
                kun_readings TEXT,
                glossary TEXT,
                stats TEXT
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
        _ = insertRecordCommand.Parameters.Add("@on_readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@kun_readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@stats", SqliteType.Text);
        insertRecordCommand.Prepare();

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                YomichanKanjiRecord yomichanKanjiRecord = (YomichanKanjiRecord)records[i];
                _ = insertRecordCommand.Parameters["@id"].Value = id;
                _ = insertRecordCommand.Parameters["@kanji"].Value = kanji;
                _ = insertRecordCommand.Parameters["@on_readings"].Value = yomichanKanjiRecord.OnReadings is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.OnReadings, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@kun_readings"].Value = yomichanKanjiRecord.KunReadings is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.KunReadings, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@glossary"].Value = yomichanKanjiRecord.Definitions is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.Definitions, Utils.s_jso) : DBNull.Value;
                _ = insertRecordCommand.Parameters["@stats"].Value = yomichanKanjiRecord.Stats is not null ? JsonSerializer.Serialize(yomichanKanjiRecord.Stats, Utils.s_jso) : DBNull.Value;
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
            SELECT r.on_readings AS onReadings,
                   r.kun_readings AS kunReadings,
                   r.glossary AS definitions,
                   r.stats AS stats,
                   r.kanji AS kanji
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
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(0), Utils.s_jso)
            : null;

        string[]? kunReadings = !dataReader.IsDBNull(1)
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(1), Utils.s_jso)
            : null;

        string[]? definitions = !dataReader.IsDBNull(2)
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(2), Utils.s_jso)
            : null;

        string[]? stats = !dataReader.IsDBNull(3)
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(3), Utils.s_jso)
            : null;

        return new YomichanKanjiRecord(onReadings, kunReadings, definitions, stats);
    }
}
