using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentDBManager
{
    public const int Version = 5;

    private const int SearchKeyIndex = 3;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                reading TEXT,
                position INTEGER NOT NULL
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                search_key TEXT NOT NULL,
                record_id INTEGER NOT NULL,
                PRIMARY KEY (search_key, record_id),
                FOREIGN KEY (record_id) REFERENCES record (id) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict<PitchAccentRecord> dict)
    {
        int totalRecordCount = 0;
        ICollection<IList<PitchAccentRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<PitchAccentRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<PitchAccentRecord> yomichanPitchAccentRecord = new(totalRecordCount);
        foreach (IList<PitchAccentRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = yomichanPitchAccentRecord.Add(dictRecords[i]);
            }
        }

        ulong id = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, spelling, reading, position)
            VALUES (@id, @spelling, @reading, @position)
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@reading", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@position", SqliteType.Integer);
        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key)
            """;

        _ = insertSearchKeyCommand.Parameters.Add("@record_id", SqliteType.Integer);
        _ = insertSearchKeyCommand.Parameters.Add("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Prepare();

        foreach (PitchAccentRecord record in yomichanPitchAccentRecord)
        {
            _ = insertRecordCommand.Parameters["@id"].Value = id;
            _ = insertRecordCommand.Parameters["@spelling"].Value = record.Spelling;
            _ = insertRecordCommand.Parameters["@reading"].Value = record.Reading is not null ? record.Reading : DBNull.Value;
            _ = insertRecordCommand.Parameters["@position"].Value = record.Position;
            _ = insertRecordCommand.ExecuteNonQuery();

            _ = insertSearchKeyCommand.Parameters["@record_id"].Value = id;
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Spelling);
            _ = insertSearchKeyCommand.Parameters["@search_key"].Value = primarySpellingInHiragana;
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    _ = insertSearchKeyCommand.Parameters["@search_key"].Value = readingInHiragana;
                    _ = insertSearchKeyCommand.ExecuteNonQuery();
                }
            }

            ++id;
        }

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<PitchAccentRecord>>? GetRecordsFromDB(string dbName, HashSet<string> terms)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT r.spelling AS spelling,
                   r.reading AS reading,
                   r.position AS position,
                   rsk.search_key AS searchKey
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        int termCount = terms.Count;
        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        _ = queryBuilder.Append(");");

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = queryBuilder.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int index = 1;
        foreach (string term in terms)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{index}"), term);
            ++index;
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        Dictionary<string, IList<PitchAccentRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString(SearchKeyIndex);
            if (results.TryGetValue(searchKey, out IList<PitchAccentRecord>? result))
            {
                result.Add(record);
            }
            else
            {
                results[searchKey] = [record];
            }
        }

        return results;
    }

    public static void LoadFromDB(Dict<PitchAccentRecord> dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling AS spelling,
                   r.reading AS reading,
                   r.position AS position,
                   json_group_array(rsk.search_key) AS searchKeys
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString(SearchKeyIndex), Utils.s_jso).Span;
            foreach (ref readonly string searchKey in searchKeys)
            {
                if (dict.Contents.TryGetValue(searchKey, out IList<PitchAccentRecord>? result))
                {
                    result.Add(record);
                }
                else
                {
                    dict.Contents[searchKey] = [record];
                }
            }
        }

        foreach ((string key, IList<PitchAccentRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static PitchAccentRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString(0);

        string? reading = !dataReader.IsDBNull(1)
            ? dataReader.GetString(1)
            : null;

        byte position = dataReader.GetByte(2);

        return new PitchAccentRecord(spelling, reading, position);
    }
}
