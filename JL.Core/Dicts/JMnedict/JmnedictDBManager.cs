using System.Globalization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictDBManager
{
    public const int Version = 6;

    private enum ColumnIndex
    {
        // ReSharper disable once UnusedMember.Local
        RowId = 0,
        JmnedictId,
        PrimarySpelling,
        Readings,
        AlternativeSpellings,
        Glossary,
        NameTypes,
        PrimarySpellingInHiragana
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
                jmnedict_id INTEGER NOT NULL,
                primary_spelling TEXT NOT NULL,
                primary_spelling_in_hiragana TEXT NOT NULL,
                readings BLOB,
                alternative_spellings BLOB,
                glossary BLOB NOT NULL,
                name_types BLOB NOT NULL
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
        int totalRecordCount = 0;
        ICollection<IList<IDictRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<JmnedictRecord> jmnedictRecords = new(totalRecordCount);
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = jmnedictRecords.Add((JmnedictRecord)dictRecords[i]);
            }
        }

        ulong rowId = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (rowid, jmnedict_id, primary_spelling, primary_spelling_in_hiragana, readings, alternative_spellings, glossary, name_types)
            VALUES (@rowid, @jmnedict_id, @primary_spelling, @primary_spelling_in_hiragana, @readings, @alternative_spellings, @glossary, @name_types);
            """;

        SqliteParameter rowidParam = new("@rowid", SqliteType.Integer);
        SqliteParameter jmnedictIdParam = new("@jmnedict_id", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new("@primary_spelling", SqliteType.Text);
        SqliteParameter primarySpellingInHiraganaParam = new("@primary_spelling_in_hiragana", SqliteType.Text);
        SqliteParameter readingsParam = new("@readings", SqliteType.Blob);
        SqliteParameter alternativeSpellingsParam = new("@alternative_spellings", SqliteType.Blob);
        SqliteParameter glossaryParam = new("@glossary", SqliteType.Blob);
        SqliteParameter nameTypesParam = new("@name_types", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            jmnedictIdParam,
            primarySpellingParam,
            primarySpellingInHiraganaParam,
            readingsParam,
            alternativeSpellingsParam,
            glossaryParam,
            nameTypesParam
        ]);

        insertRecordCommand.Prepare();

        foreach (JmnedictRecord record in jmnedictRecords)
        {
            rowidParam.Value = rowId;
            jmnedictIdParam.Value = record.Id;
            primarySpellingParam.Value = record.PrimarySpelling;
            primarySpellingInHiraganaParam.Value = JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling);
            readingsParam.Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            nameTypesParam.Value = MessagePackSerializer.Serialize(record.NameTypes);
            _ = insertRecordCommand.ExecuteNonQuery();

            ++rowId;
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string parameter)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
            $"""
            SELECT r.rowid, r.jmnedict_id, r.primary_spelling, r.readings, r.alternative_spellings, r.glossary, r.name_types, r.primary_spelling_in_hiragana
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

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        while (dataReader.Read())
        {
            JmnedictRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.PrimarySpellingInHiragana);
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

    // public static void LoadFromDB(Dict dict)
    // {
    //     using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
    //     using SqliteCommand command = connection.CreateCommand();
    //
    //     command.CommandText =
    //         """
    //         SELECT r.rowid, r.jmnedict_id, r.primary_spelling, r.readings, r.alternative_spellings, r.glossary, r.name_types, r.primary_spelling_in_hiragana
    //         FROM record r;
    //         """;
    //
    //     using SqliteDataReader dataReader = command.ExecuteReader();
    //     while (dataReader.Read())
    //     {
    //         JmnedictRecord record = GetRecord(dataReader);
    //         string searchKey = dataReader.GetString((int)ColumnIndex.PrimarySpellingInHiragana);
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
    // dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord>(entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    // }

    private static JmnedictRecord GetRecord(SqliteDataReader dataReader)
    {
        int jmnedictId = dataReader.GetInt32((int)ColumnIndex.JmnedictId);
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);
        string[]? readings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.Readings);
        string[]? alternativeSpellings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.AlternativeSpellings);
        string[][] definitions = dataReader.GetValueFromBlobStream<string[][]>((int)ColumnIndex.Glossary);
        string[][] nameTypes = dataReader.GetValueFromBlobStream<string[][]>((int)ColumnIndex.NameTypes);

        return new JmnedictRecord(jmnedictId, primarySpelling, alternativeSpellings, readings, definitions, nameTypes);
    }
}
