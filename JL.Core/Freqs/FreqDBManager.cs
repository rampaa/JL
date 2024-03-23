using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;
internal static class FreqDBManager
{
    public const int Version = 1;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetFreqDBPath(dbName)};");
        connection.Open();
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
                record_id INTEGER NOT NULL,
                search_key TEXT NOT NULL,
                PRIMARY KEY (record_id, search_key),
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
        using SqliteConnection connection = new($"Data Source={DBUtils.GetFreqDBPath(freq.Name)};Mode=ReadWrite");
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        ulong id = 1;
        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                using SqliteCommand insertRecordCommand = connection.CreateCommand();

                insertRecordCommand.CommandText =
                    """
                    INSERT INTO record (id, spelling, frequency)
                    VALUES (@id, @spelling, @frequency)
                    """;

                FrequencyRecord record = records[i];
                _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
                _ = insertRecordCommand.Parameters.AddWithValue("@spelling", record.Spelling);
                _ = insertRecordCommand.Parameters.AddWithValue("@frequency", record.Frequency);
                _ = insertRecordCommand.ExecuteNonQuery();

                using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
                insertSearchKeyCommand.CommandText =
                    """
                    INSERT INTO record_search_key (record_id, search_key)
                    VALUES (@record_id, @search_key)
                    """;

                _ = insertSearchKeyCommand.Parameters.AddWithValue("@record_id", id);
                _ = insertSearchKeyCommand.Parameters.AddWithValue("@search_key", key);
                _ = insertSearchKeyCommand.ExecuteNonQuery();

                ++id;
            }
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";

        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    public static Dictionary<string, List<FrequencyRecord>> GetRecordsFromDB(string dbName, List<string> terms)
    {
        Dictionary<string, List<FrequencyRecord>> results = [];

        using SqliteConnection connection = new($"Data Source={DBUtils.GetFreqDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey,
                   r.spelling as spelling,
                   r.frequency AS frequency
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        int termCount = terms.Count;
        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        _ = queryBuilder.Append(')');

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = queryBuilder.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < termCount; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);

            string searchKey = dataReader.GetString(nameof(searchKey));
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

    public static List<FrequencyRecord> GetRecordsFromDB(string dbName, string term)
    {
        List<FrequencyRecord> records = [];

        using SqliteConnection connection = new($"Data Source={DBUtils.GetFreqDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling as spelling, r.frequency AS frequency
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            records.Add(GetRecord(dataReader));
        }

        return records;
    }

    public static void LoadFromDB(Freq freq)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetFreqDBPath(freq.Name)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT json_group_array(rsk.search_key) AS searchKeys, r.spelling as spelling, r.frequency AS frequency
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            FrequencyRecord record = GetRecord(dataReader);

            string[] searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(searchKeys)), Utils.s_jsoNotIgnoringNull)!;
            for (int i = 0; i < searchKeys.Length; i++)
            {
                string searchKey = searchKeys[i];
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

        freq.Contents = freq.Contents.ToFrozenDictionary();
    }

    private static FrequencyRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString(nameof(spelling));
        int frequency = dataReader.GetInt32(nameof(frequency));

        return new FrequencyRecord(spelling, frequency);
    }
}
