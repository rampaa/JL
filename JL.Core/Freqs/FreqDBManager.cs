using System.Collections.Frozen;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;

internal static class FreqDBManager
{
    public const int Version = 6;

    private const int SearchKeyIndex = 2;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetFreqDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                frequency INTEGER NOT NULL
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

    public static void InsertRecordsToDB(Freq freq)
    {
        ulong id = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetFreqDBPath(freq.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, spelling, frequency)
            VALUES (@id, @spelling, @frequency);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@frequency", SqliteType.Integer);
        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key (record_id, search_key)
            VALUES (@record_id, @search_key);
            """;

        _ = insertSearchKeyCommand.Parameters.Add("@record_id", SqliteType.Integer);
        _ = insertSearchKeyCommand.Parameters.Add("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Prepare();

        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                FrequencyRecord record = records[i];
                _ = insertRecordCommand.Parameters["@id"].Value = id;
                _ = insertRecordCommand.Parameters["@spelling"].Value = record.Spelling;
                _ = insertRecordCommand.Parameters["@frequency"].Value = record.Frequency;
                _ = insertRecordCommand.ExecuteNonQuery();

                _ = insertSearchKeyCommand.Parameters["@record_id"].Value = id;
                _ = insertSearchKeyCommand.Parameters["@search_key"].Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();

                ++id;
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

    public static Dictionary<string, List<FrequencyRecord>>? GetRecordsFromDB(string dbName, HashSet<string> terms)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetFreqDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT r.spelling, r.frequency, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        int termsCount = terms.Count;
        for (int i = 1; i < termsCount; i++)
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

        Dictionary<string, List<FrequencyRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString(SearchKeyIndex);
            if (results.TryGetValue(searchKey, out List<FrequencyRecord>? result))
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

    public static List<FrequencyRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetFreqDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling, r.frequency
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term;
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<FrequencyRecord> records = [];
        while (dataReader.Read())
        {
            records.Add(GetRecord(dataReader));
        }
        return records;
    }

    public static void SetMaxFrequencyValue(Freq freq)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetFreqDBPath(freq.Name));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT MAX(frequency)
            FROM record
            """;

        using SqliteDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);
        _ = reader.Read();
        freq.MaxValue = !reader.IsDBNull(0)
            ? reader.GetInt32(0)
            : 0;
    }

    public static void LoadFromDB(Freq freq)
    {
        SetMaxFrequencyValue(freq);

        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetFreqDBPath(freq.Name));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling, r.frequency, json_group_array(rsk.search_key)
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString(SearchKeyIndex), Utils.s_jso).Span;
            foreach (ref readonly string searchKey in searchKeys)
            {
                if (freq.Contents.TryGetValue(searchKey, out IList<FrequencyRecord>? result))
                {
                    result.Add(record);
                }
                else
                {
                    freq.Contents[searchKey] = [record];
                }
            }
        }

        foreach ((string key, IList<FrequencyRecord> recordList) in freq.Contents)
        {
            freq.Contents[key] = recordList.ToArray();
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrequencyRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString(0);
        int frequency = dataReader.GetInt32(1);

        return new FrequencyRecord(spelling, frequency);
    }
}
