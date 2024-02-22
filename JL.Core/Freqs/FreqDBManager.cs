using System.Data.Common;
using System.Globalization;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Data;
using JL.Core.Utilities;
using System.Text.Json;

namespace JL.Core.Freqs;
internal static class FreqDBManager
{
    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(dbName)};"));
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
    }

    public static void InsertRecordsToDB(Freq freq)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(freq.Name)};Mode=ReadWrite"));
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        int id = 1;
        foreach ((string key, IList<FrequencyRecord> records) in freq.Contents)
        {
            for (int i = 0; i < records.Count; i++)
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
        Dictionary<string, List<FrequencyRecord>> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(dbName)};Mode=ReadOnly"));
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

        for (int i = 1; i < terms.Count; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        _ = queryBuilder.Append(')');

        command.CommandText = queryBuilder.ToString();

        for (int i = 0; i < terms.Count; i++)
        {
            _ = command.Parameters.AddWithValue($"@{i + 1}", terms[i]);
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
                results[searchKey] = new List<FrequencyRecord> { record };
            }
        }

        return results;
    }

    public static List<FrequencyRecord> GetRecordsFromDB(string dbName, string term)
    {
        List<FrequencyRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(dbName)};Mode=ReadOnly"));
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
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={FreqUtils.GetDBPath(freq.Name)};Mode=ReadOnly"));
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
                    freq.Contents[searchKey] = new List<FrequencyRecord> { record };
                }
            }
        }

        foreach ((string key, IList<FrequencyRecord> recordList) in freq.Contents)
        {
            freq.Contents[key] = recordList.ToArray();
        }

        freq.Contents.TrimExcess();
    }

    private static FrequencyRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString(nameof(spelling));
        int frequency = dataReader.GetInt32(nameof(frequency));

        return new FrequencyRecord(spelling, frequency);
    }
}
