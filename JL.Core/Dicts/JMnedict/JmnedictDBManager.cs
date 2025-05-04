using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictDBManager
{
    public const int Version = 2;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                jmnedict_id INTEGER NOT NULL,
                primary_spelling TEXT NOT NULL,
                primary_spelling_in_hiragana TEXT NOT NULL,
                readings TEXT,
                alternative_spellings TEXT,
                glossary TEXT NOT NULL,
                name_types TEXT NOT NULL
            ) STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict<JmnedictRecord> dict)
    {
        int totalRecordCount = 0;
        ICollection<IList<JmnedictRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<JmnedictRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<JmnedictRecord> jmnedictRecords = new(totalRecordCount);
        foreach (IList<JmnedictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = jmnedictRecords.Add(dictRecords[i]);
            }
        }

        ulong id = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, jmnedict_id, primary_spelling, primary_spelling_in_hiragana, readings, alternative_spellings, glossary, name_types)
            VALUES (@id, @jmnedict_id, @primary_spelling, @primary_spelling_in_hiragana, @readings, @alternative_spellings, @glossary, @name_types);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@jmnedict_id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling_in_hiragana", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@alternative_spellings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@name_types", SqliteType.Text);
        insertRecordCommand.Prepare();

        foreach (JmnedictRecord record in jmnedictRecords)
        {
            _ = insertRecordCommand.Parameters["@id"].Value = id;
            _ = insertRecordCommand.Parameters["@jmnedict_id"].Value = record.Id;
            _ = insertRecordCommand.Parameters["@primary_spelling"].Value = record.PrimarySpelling;
            _ = insertRecordCommand.Parameters["@primary_spelling_in_hiragana"].Value = JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling);
            _ = insertRecordCommand.Parameters["@readings"].Value = record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jso) : DBNull.Value;
            _ = insertRecordCommand.Parameters["@alternative_spellings"].Value = record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jso) : DBNull.Value;
            _ = insertRecordCommand.Parameters["@glossary"].Value = JsonSerializer.Serialize(record.Definitions, Utils.s_jso);
            _ = insertRecordCommand.Parameters["@name_types"].Value = JsonSerializer.Serialize(record.NameTypes, Utils.s_jso);
            _ = insertRecordCommand.ExecuteNonQuery();

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_primary_spelling_in_hiragana ON record(primary_spelling_in_hiragana);";
        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<JmnedictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string parameter)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
            $"""
            SELECT r.jmnedict_id AS id,
                   r.primary_spelling AS primarySpelling,
                   r.readings AS readings,
                   r.alternative_spellings AS alternativeSpellings,
                   r.glossary AS definitions,
                   r.name_types AS nameTypes,
                   r.primary_spelling_in_hiragana AS searchKey
            FROM record r
            WHERE r.primary_spelling_in_hiragana IN {parameter}
            """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < terms.Length; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        Dictionary<string, IList<JmnedictRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            JmnedictRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString(6);
            if (results.TryGetValue(searchKey, out IList<JmnedictRecord>? result))
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

    // public static void LoadFromDB(Dict dict)
    // {
    //     using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
    //     using SqliteCommand command = connection.CreateCommand();
    //
    //     command.CommandText =
    //         """
    //         SELECT r.jmnedict_id AS id,
    //                r.primary_spelling AS primarySpelling,
    //                r.readings AS readings,
    //                r.alternative_spellings AS alternativeSpellings,
    //                r.glossary AS definitions,
    //                r.name_types AS nameTypes,
    //                r.primary_spelling_in_hiragana AS searchKey
    //         FROM record r;
    //         """;
    //
    //     using SqliteDataReader dataReader = command.ExecuteReader();
    //     while (dataReader.Read())
    //     {
    //         JmnedictRecord record = GetRecord(dataReader);
    //         string searchKey = dataReader.GetString(6);
    //         if (dict.Contents.TryGetValue(searchKey, out IList<IDictRecord>? result))
    //         {
    //             result.Add(record);
    //         }
    //         else
    //         {
    //             dict.Contents[searchKey] = new List<IDictRecord> { record };
    //         }
    //     }
    //
    //     foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
    //     {
    //         dict.Contents[key] = recordList.ToArray();
    //     }
    //
    //     dict.Contents.TrimExcess();
    // }

    private static JmnedictRecord GetRecord(SqliteDataReader dataReader)
    {
        int id = dataReader.GetInt32(0);
        string primarySpelling = dataReader.GetString(1);

        string[]? readings = !dataReader.IsDBNull(2)
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(2), Utils.s_jso)
            : null;

        string[]? alternativeSpellings = !dataReader.IsDBNull(3)
            ? JsonSerializer.Deserialize<string[]>(dataReader.GetString(3), Utils.s_jso)
            : null;

        string[][]? definitions = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(4), Utils.s_jso);
        Debug.Assert(definitions is not null);

        string[][]? nameTypes = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(5), Utils.s_jso);
        Debug.Assert(nameTypes is not null);

        return new JmnedictRecord(id, primarySpelling, alternativeSpellings, readings, definitions, nameTypes);
    }
}
