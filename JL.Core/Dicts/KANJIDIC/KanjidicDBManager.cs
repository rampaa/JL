using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KANJIDIC;

internal static class KanjidicDBManager
{
    public const int Version = 6;

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

        DBUtils.SetEncodingToUtf16LE(connection);
        DBUtils.SetPageSizeTo64k(connection);

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

    public static async Task ImportFromDisk(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                XmlReaderSettings xmlReaderSettings = new()
                {
                    Async = true,
                    DtdProcessing = DtdProcessing.Parse,
                    IgnoreWhitespace = true
                };

                // ReSharper disable once UseAwaitUsing
                using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
                Debug.Assert(connection is not null);

                DBUtils.ConfigureForBulkWrite(connection);
#pragma warning disable CA1849 // Call async methods when in an async method
                // ReSharper disable once UseAwaitUsing
                using SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

                // ReSharper disable once UseAwaitUsing
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
                insertRecordCommand.Parameters.AddRange(
                 [
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

#pragma warning disable CA1849 // Call async methods when in an async method
                insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

                int kanjiCount = 0;
                using XmlReader xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
                while (xmlReader.ReadToFollowing("literal"))
                {
                    (string kanji, KanjidicRecord kanjidicRecord) = await KanjidicLoader.ReadCharacter(xmlReader).ConfigureAwait(false);

                    kanjiParam.Value = kanji;
                    onReadingsParam.Value = kanjidicRecord.OnReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.OnReadings) : DBNull.Value;
                    kunReadingsParam.Value = kanjidicRecord.KunReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.KunReadings) : DBNull.Value;
                    nanoriReadingsParam.Value = kanjidicRecord.NanoriReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.NanoriReadings) : DBNull.Value;
                    radicalNamesParam.Value = kanjidicRecord.RadicalNames is not null ? MessagePackSerializer.Serialize(kanjidicRecord.RadicalNames) : DBNull.Value;
                    glossaryParam.Value = kanjidicRecord.Definitions is not null ? MessagePackSerializer.Serialize(kanjidicRecord.Definitions) : DBNull.Value;
                    strokeCountParam.Value = kanjidicRecord.StrokeCount;
                    gradeParam.Value = kanjidicRecord.Grade;
                    frequencyParam.Value = kanjidicRecord.Frequency;

#pragma warning disable CA1849 // Call async methods when in an async method
                    _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                    ++kanjiCount;
                }

#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                DBUtils.ConfigureForRead(connection);

                // ReSharper disable once UseAwaitUsing
                using SqliteCommand analyzeCommand = connection.CreateCommand();
                analyzeCommand.CommandText = "ANALYZE;";
#pragma warning disable CA1849 // Call async methods when in an async method
                _ = analyzeCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                // ReSharper disable once UseAwaitUsing
                using SqliteCommand vacuumCommand = connection.CreateCommand();
                vacuumCommand.CommandText = "VACUUM;";
#pragma warning disable CA1849 // Call async methods when in an async method
                _ = vacuumCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                dict.Ready = true;
                dict.Size = kanjiCount;
            }
        }
        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync(
                "Couldn't find kanjidic2.xml. Would you like to download it now?",
                "Download KANJIDIC2?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.Kanjidic), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await ImportFromDisk(dict).ConfigureAwait(false);
                    }
                    finally
                    {
                        dict.Updating = false;
                    }
                }
                else
                {
                    dict.Updating = false;
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    public static void ImportFromMemory(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
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

        DBUtils.ConfigureForRead(connection);

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

        using SqliteRecordReader reader = new(connection, SingleTermQuery);
        reader.Bind(1, term);
        return reader.Read()
            ? [GetRecord(reader)]
            : null;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        const string query =
            $"""
            SELECT r.{OnReadings}, r.{KunReadings}, r.{NanoriReadings}, r.{RadicalNames}, r.{Glossary}, r.{StrokeCount}, r.{Grade}, r.{Frequency}, r.{Kanji}
            FROM {Record} r;
            """;

        using SqliteRecordReader reader = new(connection, query);
        while (reader.Read())
        {
            IDictRecord[] record = [GetRecord(reader)];
            string kanji = reader.GetString((int)ColumnIndex.Kanji);
            dict.Contents[kanji] = record;

            if (kanji.Length > dict.MaxSearchKeyLength)
            {
                dict.MaxSearchKeyLength = kanji.Length;
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static KanjidicRecord GetRecord(SqliteRecordReader reader)
    {
        // The "record" table is created as WITHOUT ROWID because we don't need a numeric primary key.
        // As a result, SqliteBlob cannot be used to read its BLOBs.

        const int onReadingsIndex = (int)ColumnIndex.OnReadings;
        string[]? onReadings = !reader.IsNull(onReadingsIndex)
            ? reader.Deserialize<string[]>(onReadingsIndex)
            : null;

        const int kunReadingsIndex = (int)ColumnIndex.KunReadings;
        string[]? kunReadings = !reader.IsNull(kunReadingsIndex)
            ? reader.Deserialize<string[]>(kunReadingsIndex)
            : null;

        const int nanoriReadingsIndex = (int)ColumnIndex.NanoriReadings;
        string[]? nanoriReadings = !reader.IsNull(nanoriReadingsIndex)
            ? reader.Deserialize<string[]>(nanoriReadingsIndex)
            : null;

        const int radicalNamesIndex = (int)ColumnIndex.RadicalNames;
        string[]? radicalNames = !reader.IsNull(radicalNamesIndex)
            ? reader.Deserialize<string[]>(radicalNamesIndex)
            : null;

        const int glossaryIndex = (int)ColumnIndex.Glossary;
        string[]? definitions = !reader.IsNull(glossaryIndex)
            ? reader.Deserialize<string[]>(glossaryIndex)
            : null;

        byte strokeCount = checked((byte)reader.GetInt64((int)ColumnIndex.StrokeCount));
        byte grade = checked((byte)reader.GetInt64((int)ColumnIndex.Grade));
        int frequency = reader.GetInt32((int)ColumnIndex.Frequency);
        return new KanjidicRecord(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);
    }
}
