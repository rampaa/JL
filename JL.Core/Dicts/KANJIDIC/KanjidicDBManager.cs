using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
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
    public const int Version = 7;

    private const int ImportRecordBatchSize = 128;

    internal const string Record = "record";
    internal const string Kanji = "kanji";
    internal const string OnReadings = "on_readings";
    internal const string KunReadings = "kun_readings";
    internal const string NanoriReadings = "nanori_readings";
    internal const string RadicalNames = "radical_names";
    internal const string Glossary = "glossary";
    internal const string StrokeCount = "stroke_count";
    internal const string Grade = "grade";
    internal const string Frequency = "frequency";

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
        DBUtils.SetPageSizeTo64K(connection);

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
            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
            Debug.Assert(connection is not null);

            DBUtils.ConfigureForBulkWrite(connection);
#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once UseAwaitUsing
            using SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            using KanjidicRecordInserter recordInserter = new(connection);

            int kanjiCount = 0;
            Channel<(string Kanji, KanjidicRecord Record)[]> availableBatches = Channel.CreateUnbounded<(string Kanji, KanjidicRecord Record)[]>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            Channel<((string Kanji, KanjidicRecord Record)[] Records, int Count)> readyBatches =
                Channel.CreateUnbounded<((string Kanji, KanjidicRecord Record)[] Records, int Count)>(
                    new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            _ = availableBatches.Writer.TryWrite(new (string Kanji, KanjidicRecord Record)[ImportRecordBatchSize]);
            _ = availableBatches.Writer.TryWrite(new (string Kanji, KanjidicRecord Record)[ImportRecordBatchSize]);
            Task producer = Task.Run(() => CreateImportRecordBatches(fullPath, availableBatches.Reader, readyBatches.Writer));
            try
            {
                await foreach (((string Kanji, KanjidicRecord Record)[] batch, int count) in readyBatches.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    for (int i = 0; i < count; i++)
                    {
                        (string kanji, KanjidicRecord kanjidicRecord) = batch[i];
                        byte[]? onReadings = kanjidicRecord.OnReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.OnReadings) : null;
                        byte[]? kunReadings = kanjidicRecord.KunReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.KunReadings) : null;
                        byte[]? nanoriReadings = kanjidicRecord.NanoriReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.NanoriReadings) : null;
                        byte[]? radicalNames = kanjidicRecord.RadicalNames is not null ? MessagePackSerializer.Serialize(kanjidicRecord.RadicalNames) : null;
                        byte[]? definitions = kanjidicRecord.Definitions is not null ? MessagePackSerializer.Serialize(kanjidicRecord.Definitions) : null;

                        recordInserter.Insert(kanji, onReadings, kunReadings, nanoriReadings, radicalNames, definitions,
                            kanjidicRecord.StrokeCount, kanjidicRecord.Grade, kanjidicRecord.Frequency);

                        ++kanjiCount;
                    }

                    _ = availableBatches.Writer.TryWrite(batch);
                }
            }
            catch (Exception exception)
            {
                _ = availableBatches.Writer.TryComplete(exception);
                throw;
            }
            finally
            {
                await producer.ConfigureAwait(false);
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

    private static async Task CreateImportRecordBatches(string fullPath, ChannelReader<(string Kanji, KanjidicRecord Record)[]> availableBatches,
        ChannelWriter<((string Kanji, KanjidicRecord Record)[] Records, int Count)> readyBatches)
    {
        try
        {
            // ReSharper disable once UseAwaitUsing
            using FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso);
            XmlReaderSettings xmlReaderSettings = new()
            {
                DtdProcessing = DtdProcessing.Parse,
                IgnoreWhitespace = true
            };

            using XmlReader xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
            (string Kanji, KanjidicRecord Record)[] batch = await availableBatches.ReadAsync().ConfigureAwait(false);
            int count = 0;
            while (xmlReader.ReadToFollowing("literal"))
            {
                (string kanji, KanjidicRecord record) = KanjidicLoader.ReadCharacter(xmlReader);
                batch[count] = (kanji, record);
                ++count;
                if (count == batch.Length)
                {
                    _ = readyBatches.TryWrite((batch, count));
                    batch = await availableBatches.ReadAsync().ConfigureAwait(false);
                    count = 0;
                }
            }

            if (count > 0)
            {
                _ = readyBatches.TryWrite((batch, count));
            }

            _ = readyBatches.TryComplete();
        }
        catch (Exception exception)
        {
            _ = readyBatches.TryComplete(exception);
        }
    }

    public static void ImportFromMemory(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using KanjidicRecordInserter recordInserter = new(connection);

        foreach ((string kanji, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                KanjidicRecord kanjidicRecord = (KanjidicRecord)records[i];
                byte[]? onReadings = kanjidicRecord.OnReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.OnReadings) : null;
                byte[]? kunReadings = kanjidicRecord.KunReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.KunReadings) : null;
                byte[]? nanoriReadings = kanjidicRecord.NanoriReadings is not null ? MessagePackSerializer.Serialize(kanjidicRecord.NanoriReadings) : null;
                byte[]? radicalNames = kanjidicRecord.RadicalNames is not null ? MessagePackSerializer.Serialize(kanjidicRecord.RadicalNames) : null;
                byte[]? definitions = kanjidicRecord.Definitions is not null ? MessagePackSerializer.Serialize(kanjidicRecord.Definitions) : null;

                recordInserter.Insert(kanji, onReadings, kunReadings, nanoriReadings, radicalNames, definitions,
                    kanjidicRecord.StrokeCount, kanjidicRecord.Grade, kanjidicRecord.Frequency);
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
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
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
