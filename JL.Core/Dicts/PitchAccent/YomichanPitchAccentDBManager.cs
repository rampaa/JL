using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentDBManager
{
    public const int Version = 1;

    private const string SingleTermQuery =
        """
        SELECT rsk.search_key AS searchKey,
               r.spelling AS spelling,
               r.reading AS reading,
               r.position AS position
        FROM record r
        JOIN record_search_key rsk ON r.id = rsk.record_id
        WHERE rsk.search_key = @term;
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
                id INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                reading TEXT,
                position INTEGER NOT NULL
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

    public static void InsertRecordsToDB(Dict dict)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadWrite;");
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        ulong id = 1;
        HashSet<PitchAccentRecord> yomichanPitchAccentRecord = dict.Contents.Values.SelectMany(static v => v).Select(static v => (PitchAccentRecord)v).ToHashSet();
        foreach (PitchAccentRecord record in yomichanPitchAccentRecord)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, spelling, reading, position)
                VALUES (@id, @spelling, @reading, @position)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling", record.Spelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading", record.Reading is not null ? record.Reading : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@position", record.Position);

            _ = insertRecordCommand.ExecuteNonQuery();

            using SqliteCommand insertSpellingCommand = connection.CreateCommand();
            insertSpellingCommand.CommandText =
                """
                INSERT INTO record_search_key(record_id, search_key)
                VALUES (@record_id, @search_key)
                """;
            _ = insertSpellingCommand.Parameters.AddWithValue("@record_id", id);

            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Spelling);
            _ = insertSpellingCommand.Parameters.AddWithValue("@search_key", primarySpellingInHiragana);
            _ = insertSpellingCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    using SqliteCommand insertReadingCommand = connection.CreateCommand();
                    insertReadingCommand.CommandText =
                        """
                        INSERT INTO record_search_key(record_id, search_key)
                        VALUES (@record_id, @search_key)
                        """;

                    _ = insertReadingCommand.Parameters.AddWithValue("@record_id", id);
                    _ = insertReadingCommand.Parameters.AddWithValue("@search_key", readingInHiragana);

                    _ = insertReadingCommand.ExecuteNonQuery();
                }
            }

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";
        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();

        dict.Ready = true;
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly;");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey,
                   r.spelling AS spelling,
                   r.reading AS reading,
                   r.position AS position
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

        for (int i = 0; i < termCount; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (dataReader.HasRows)
        {
            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            while (dataReader.Read())
            {
                PitchAccentRecord record = GetRecord(dataReader);

                string searchKey = dataReader.GetString(nameof(searchKey));
                if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
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

        return null;
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly;");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (dataReader.HasRows)
        {
            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            while (dataReader.Read())
            {
                PitchAccentRecord record = GetRecord(dataReader);

                string searchKey = dataReader.GetString(nameof(searchKey));
                if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
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

        return null;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadOnly;");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT json_group_array(rsk.search_key) AS searchKeys,
                   r.spelling AS spelling,
                   r.reading AS reading,
                   r.position AS position
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            string[] searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(searchKeys)), Utils.s_jsoNotIgnoringNull)!;
            for (int i = 0; i < searchKeys.Length; i++)
            {
                string searchKey = searchKeys[i];
                if (dict.Contents.TryGetValue(searchKey, out IList<IDictRecord>? result))
                {
                    result.Add(record);
                }
                else
                {
                    dict.Contents[searchKey] = [record];
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static PitchAccentRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString(nameof(spelling));

        string? reading = null;
        if (dataReader[nameof(reading)] is string readingFromDB)
        {
            reading = readingFromDB;
        }

        byte position = dataReader.GetByte(nameof(position));

        return new PitchAccentRecord(spelling, reading, position);
    }
}
