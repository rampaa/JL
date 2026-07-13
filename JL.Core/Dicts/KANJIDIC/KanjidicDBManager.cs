using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KANJIDIC;

internal static class KanjidicDBManager
{
    public const int Version = 3;

    private const string Record = "record";
    private const string Kanji = "kanji";
    private const string OnReadings = "on_readings";
    private const string KunReadings = "kun_readings";
    private const string NanoriReadings = "nanori_readings";
    private const string RadicalNames = "radical_names";
    private const string Glossary = "glossary";
    private const string StrokeCount = "stroke_count";
    private const string Grade = "grade";
    private const string Frequency = "frequency";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{OnReadings}, r.{KunReadings}, r.{NanoriReadings}, r.{RadicalNames}, r.{Glossary}, r.{StrokeCount}, r.{Grade}, r.{Frequency}
        FROM {Record} r
        WHERE r.{Kanji} = @{Term};
        """;

    private enum ColumnIndex
    {
        OnReadings = 0,
        KunReadings,
        NanoriReadings,
        RadicalNames,
        Glossary,
        StrokeCount,
        Grade,
        Frequency,
        Kanji
    }

    public static void CreateDB(string dbPath)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(dbPath);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            CREATE TABLE IF NOT EXISTS {Record}
            (
                {Kanji} TEXT NOT NULL PRIMARY KEY,
                {OnReadings} BLOB,
                {KunReadings} BLOB,
                {NanoriReadings} BLOB,
                {RadicalNames} BLOB,
                {Glossary} BLOB,
                {StrokeCount} INTEGER NOT NULL,
                {Grade} INTEGER NOT NULL,
                {Frequency} INTEGER NOT NULL
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
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.SetSynchronousModeToNormal(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({Kanji}, {OnReadings}, {KunReadings}, {NanoriReadings}, {RadicalNames}, {Glossary}, {StrokeCount}, {Grade}, {Frequency})
            VALUES (@{Kanji}, @{OnReadings}, @{KunReadings}, @{NanoriReadings}, @{RadicalNames}, @{Glossary}, @{StrokeCount}, @{Grade}, @{Frequency});
            """;

        SqliteParameter kanjiParam = new($"@{Kanji}", SqliteType.Text);
        SqliteParameter onReadingsParam = new($"@{OnReadings}", SqliteType.Blob);
        SqliteParameter kunReadingsParam = new($"@{KunReadings}", SqliteType.Blob);
        SqliteParameter nanoriReadingsParam = new($"@{NanoriReadings}", SqliteType.Blob);
        SqliteParameter radicalNamesParam = new($"@{RadicalNames}", SqliteType.Blob);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter strokeCountParam = new($"@{StrokeCount}", SqliteType.Integer);
        SqliteParameter gradeParam = new($"@{Grade}", SqliteType.Integer);
        SqliteParameter frequencyParam = new($"@{Frequency}", SqliteType.Integer);
        insertRecordCommand.Parameters.AddRange([
            kanjiParam,
            onReadingsParam,
            kunReadingsParam,
            nanoriReadingsParam,
            radicalNamesParam,
            glossaryParam,
            strokeCountParam,
            gradeParam,
            frequencyParam
        ]);

        insertRecordCommand.Prepare();

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)records[i];
                kanjiParam.Value = kanji;
                onReadingsParam.Value = kanjidicRecord.OnReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.OnReadings) : DBNull.Value;
                kunReadingsParam.Value = kanjidicRecord.KunReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.KunReadings) : DBNull.Value;
                nanoriReadingsParam.Value = kanjidicRecord.NanoriReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.NanoriReadings) : DBNull.Value;
                radicalNamesParam.Value = kanjidicRecord.RadicalNames is not null ? MessagePackSerializer.Serialize(kanjidicRecord.RadicalNames) : DBNull.Value;
                glossaryParam.Value = kanjidicRecord.Definitions is not null ? MessagePackSerializer.Serialize(kanjidicRecord.Definitions) : DBNull.Value;
                strokeCountParam.Value = kanjidicRecord.StrokeCount;
                gradeParam.Value = kanjidicRecord.Grade;
                frequencyParam.Value = kanjidicRecord.Frequency;
                _ = insertRecordCommand.ExecuteNonQuery();
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

    public static List<IDictRecord>? GetRecordsFromDB(string readOnlyConnectionString, string term)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;
        _ = command.Parameters.AddWithValue($"@{Term}", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        _ = dataReader.Read();
        return [GetRecord(dataReader)];
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT r.{OnReadings}, r.{KunReadings}, r.{NanoriReadings}, r.{RadicalNames}, r.{Glossary}, r.{StrokeCount}, r.{Grade}, r.{Frequency}, r.{Kanji}
            FROM {Record} r;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            IDictRecord[] record = [GetRecord(dataReader)];
            string kanji = dataReader.GetString((int)ColumnIndex.Kanji);
            dict.Contents[kanji] = record;
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static KanjidicRecord GetRecord(SqliteDataReader dataReader)
    {
        string[]? onReadings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.OnReadings);
        string[]? kunReadings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.KunReadings);
        string[]? nanoriReadings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.NanoriReadings);
        string[]? radicalNames = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.RadicalNames);
        string[]? definitions = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.Glossary);
        byte strokeCount = dataReader.GetByte((int)ColumnIndex.StrokeCount);
        byte grade = dataReader.GetByte((int)ColumnIndex.Grade);
        int frequency = dataReader.GetInt32((int)ColumnIndex.Frequency);
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
