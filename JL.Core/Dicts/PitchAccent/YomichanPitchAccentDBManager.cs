using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentDBManager
{
    public const int Version = 6;

    private enum ColumnIndex
    {
        Spelling = 0,
        Reading,
        Position,
        SearchKey
    }

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                rowid INTEGER NOT NULL PRIMARY KEY,
                spelling TEXT NOT NULL,
                reading TEXT,
                position INTEGER NOT NULL
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                search_key TEXT NOT NULL,
                record_id INTEGER NOT NULL,
                PRIMARY KEY (search_key, record_id),
                FOREIGN KEY (record_id) REFERENCES record (rowid) ON DELETE CASCADE
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
        int totalRecordCount = 0;
        ICollection<IList<IDictRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<PitchAccentRecord> yomichanPitchAccentRecord = new(totalRecordCount);
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = yomichanPitchAccentRecord.Add((PitchAccentRecord)dictRecords[i]);
            }
        }

        ulong rowId = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (rowid, spelling, reading, position)
            VALUES (@rowid, @spelling, @reading, @position)
            """;

        SqliteParameter rowidParam = new("@rowid", SqliteType.Integer);
        SqliteParameter spellingParam = new("@spelling", SqliteType.Text);
        SqliteParameter readingParam = new("@reading", SqliteType.Text);
        SqliteParameter positionParam = new("@position", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            spellingParam,
            readingParam,
            positionParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key)
            """;

        SqliteParameter recordIdParam = new("@record_id", SqliteType.Integer);
        SqliteParameter searchKeyParam = new("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach (PitchAccentRecord record in yomichanPitchAccentRecord)
        {
            rowidParam.Value = rowId;
            spellingParam.Value = record.Spelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            positionParam.Value = record.Position;
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowId;
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Spelling);
            searchKeyParam.Value = primarySpellingInHiragana;
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    searchKeyParam.Value = readingInHiragana;
                    _ = insertSearchKeyCommand.ExecuteNonQuery();
                }
            }

            ++rowId;
        }

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, HashSet<string> terms)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = Utils.StringBuilderPool.Get().Append(
            """
            SELECT r.spelling, r.reading, r.position, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
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

        Utils.StringBuilderPool.Return(queryBuilder);

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

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
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

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.spelling, r.reading, r.position, json_group_array(rsk.search_key)
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            GROUP BY r.rowid;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            PitchAccentRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString((int)ColumnIndex.SearchKey), Utils.s_jso).Span;
            foreach (ref readonly string searchKey in searchKeys)
            {
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

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static PitchAccentRecord GetRecord(SqliteDataReader dataReader)
    {
        string spelling = dataReader.GetString((int)ColumnIndex.Spelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        byte position = dataReader.GetByte((int)ColumnIndex.Position);

        return new PitchAccentRecord(spelling, reading, position);
    }
}
