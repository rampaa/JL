using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictDBManager
{
    public const int Version = 12;

    internal const string Record = "record";
    internal const string RowId = "rowid";
    internal const string JmnedictId = "jmnedict_id";
    internal const string PrimarySpelling = "primary_spelling";
    internal const string Readings = "readings";
    internal const string AlternativeSpellings = "alternative_spellings";
    internal const string Glossary = "glossary";
    internal const string NameTypes = "name_types";
    internal const string PrimarySpellingInHiragana = "primary_spelling_in_hiragana";

    private static readonly ConcurrentDictionary<int, byte[]> s_queryCache = [];

    private static readonly byte[] s_maxSearchKeyLengthQuery = TextUtils.s_utf8NoBom.GetBytes(
        $"""
        SELECT MAX(LENGTH(CAST({PrimarySpellingInHiragana} AS BLOB)) / 2)
        FROM {Record};{"\0"}
        """);

    private static byte[] GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out byte[]? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{RowId}, r.{JmnedictId}, r.{PrimarySpelling}, r.{Readings}, r.{AlternativeSpellings}, r.{Glossary}, r.{NameTypes}, r.{PrimarySpellingInHiragana}
            FROM {Record} r
            WHERE r.{PrimarySpellingInHiragana} IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(',').Append(DBUtils.GetParameterName(i + 1));
        }

        string queryText = queryBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(queryBuilder);
        query = GC.AllocateUninitializedArray<byte>(TextUtils.s_utf8NoBom.GetByteCount(queryText) + 1);
        _ = TextUtils.s_utf8NoBom.GetBytes(queryText, query);
        query[^1] = 0;
        _ = s_queryCache.TryAdd(termCount, query);
        return query;
    }

    private enum ColumnIndex
    {
        // ReSharper disable once UnusedMember.Local
        RowId = 0,
        JmnedictId,
        PrimarySpelling,
        Readings,
        AlternativeSpellings,
        // ReSharper disable once UnusedMember.Local
        Glossary,
        // ReSharper disable once UnusedMember.Local
        NameTypes,
        PrimarySpellingInHiragana
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
                {RowId} INTEGER NOT NULL PRIMARY KEY,
                {JmnedictId} INTEGER NOT NULL,
                {PrimarySpelling} TEXT NOT NULL,
                {PrimarySpellingInHiragana} TEXT NOT NULL,
                {Readings} BLOB,
                {AlternativeSpellings} BLOB,
                {Glossary} BLOB NOT NULL,
                {NameTypes} BLOB NOT NULL
            ) STRICT;
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
            DictUtils.JmnedictEntities.Clear();

            int rowId = 1;

            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
            Debug.Assert(connection is not null);

            DBUtils.ConfigureForBulkWrite(connection);

#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            using JmnedictRecordInserter recordInserter = new(connection);

            int transactionRecordCount = 0;
            HashSet<string> searchKeys = new(StringComparer.Ordinal);
            Channel<JmnedictImportEntryBatch> availableBatches = Channel.CreateUnbounded<JmnedictImportEntryBatch>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            Channel<JmnedictImportEntryBatch> readyBatches = Channel.CreateUnbounded<JmnedictImportEntryBatch>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            _ = availableBatches.Writer.TryWrite(new JmnedictImportEntryBatch());
            _ = availableBatches.Writer.TryWrite(new JmnedictImportEntryBatch());
            Task producer = Task.Run(() => CreateImportEntryBatches(fullPath, availableBatches.Reader, readyBatches.Writer));
            try
            {
                await foreach (JmnedictImportEntryBatch batch in readyBatches.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    try
                    {
                        for (int i = 0; i < batch.EntryCount; i++)
                        {
                            JmnedictEntry entry = new(batch.EntryIds[i], batch.SpellingLists[i], batch.ReadingArrays[i],
                                batch.TranslationLists[i]);
                            int recordsInserted = InsertRecordsFromEntry(in entry, recordInserter, rowId, searchKeys);
                            rowId += recordsInserted;
                            transactionRecordCount += recordsInserted;

                            if (transactionRecordCount > DBUtils.TransactionBatchSize)
                            {
#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

#pragma warning disable CA1849 // Call async methods when in an async method
                                // ReSharper disable once MethodHasAsyncOverload
                                transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

                                dict.Ready = true;

#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
                                transactionRecordCount = 0;
                            }
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < batch.EntryCount; i++)
                        {
                            batch.SpellingLists[i].Clear();
                            batch.TranslationLists[i].Clear();
                        }

                        batch.EntryCount = 0;
                        _ = availableBatches.Writer.TryWrite(batch);
                    }
                }

                if (transactionRecordCount > 0)
                {
#pragma warning disable CA1849 // Call async methods when in an async method
                    transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                    dict.Ready = true;
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

#pragma warning disable CA1849 // Call async methods when in an async method
                // ReSharper disable once MethodHasAsyncOverload
                transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
            }

            if (rowId > 1)
            {
                // ReSharper disable once UseAwaitUsing
                using SqliteCommand createIndexCommand = connection.CreateCommand();
                createIndexCommand.CommandText = $"CREATE INDEX IF NOT EXISTS ix_record_{PrimarySpellingInHiragana} ON record({PrimarySpellingInHiragana});";
#pragma warning disable CA1849 // Call async methods when in an async method
                _ = createIndexCommand.ExecuteNonQuery();
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
            }

            dict.Size = rowId - 1;
            dict.MaxSearchKeyLength = GetMaxSearchKeyLength(connection);
        }
        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync("Couldn't find JMnedict.xml. Would you like to download it now?",
                "Download JMnedict?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.JMnedict), false, false).ConfigureAwait(false);

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

    private static int InsertRecordsFromEntry(in JmnedictEntry entry, JmnedictRecordInserter recordInserter,
        int firstRowId, HashSet<string> searchKeys)
    {
        ReadOnlySpan<Translation> translations = entry.TranslationList.AsReadOnlySpan();
        Debug.Assert(translations.Length > 0);

        ReadOnlySpan<string> spellings = entry.KebList.AsReadOnlySpan();
        bool hasSpellings = spellings.Length > 0;
        if (!hasSpellings)
        {
            spellings = entry.RebArray;
        }

        if (spellings.IsEmpty)
        {
            return 0;
        }

        string[][] definitionsArray = new string[translations.Length][];
        string[]?[] nameTypesArray = new string[translations.Length][];
        for (int i = 0; i < translations.Length; i++)
        {
            definitionsArray[i] = translations[i].TransDetArray;
            nameTypesArray[i] = translations[i].NameTypeArray;
        }
        byte[] definitionBytes = MessagePackSerializer.Serialize(definitionsArray);
        byte[] nameTypeBytes = MessagePackSerializer.Serialize(nameTypesArray.TrimNullableArray());

        byte[]? readingBytes = hasSpellings ? MessagePackSerializer.Serialize(entry.RebArray) : null;
        int recordsInserted = 0;
        for (int i = 0; i < spellings.Length; i++)
        {
            string spelling = spellings[i];
            string searchKey = JapaneseUtils.NormalizeText(spelling);
            if (spellings.Length > 1 && !searchKeys.Add(searchKey))
            {
                continue;
            }

            string[]? alternativeSpellings = hasSpellings
                ? entry.KebList.RemoveAtToArray(i)
                : entry.RebArray.RemoveAt(i);
            byte[]? alternativeSpellingBytes = alternativeSpellings is not null
                ? MessagePackSerializer.Serialize(alternativeSpellings)
                : null;

            recordInserter.Insert(firstRowId + recordsInserted, entry.Id, spelling, searchKey, readingBytes,
                alternativeSpellingBytes, definitionBytes, nameTypeBytes);
            ++recordsInserted;
        }

        if (spellings.Length > 1)
        {
            searchKeys.Clear();
        }

        return recordsInserted;
    }

    private static async Task CreateImportEntryBatches(string fullPath, ChannelReader<JmnedictImportEntryBatch> availableBatches, ChannelWriter<JmnedictImportEntryBatch> readyBatches)
    {
        List<string> rebList = [];
        List<string> nameTypeList = [];
        List<string> transDetList = [];
        try
        {
            // ReSharper disable once UseAwaitUsing
            using FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso);

            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)
            using XmlTextReader xmlTextReader = new(fileStream);
            xmlTextReader.DtdProcessing = DtdProcessing.Parse;
            xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
            xmlTextReader.EntityHandling = EntityHandling.ExpandCharEntities;

            JmnedictImportEntryBatch batch = await availableBatches.ReadAsync().ConfigureAwait(false);
            while (xmlTextReader.ReadToFollowing("entry"))
            {
                int i = batch.EntryCount;
                JmnedictEntry entry = JmnedictLoader.ReadEntry(xmlTextReader, batch.SpellingLists[i], rebList,
                    batch.TranslationLists[i], nameTypeList, transDetList);
                batch.EntryIds[i] = entry.Id;
                batch.ReadingArrays[i] = entry.RebArray;
                ++batch.EntryCount;
                rebList.Clear();

                if (batch.EntryCount == JmnedictImportEntryBatch.MaxEntriesPerBatch)
                {
                    _ = readyBatches.TryWrite(batch);
                    batch = await availableBatches.ReadAsync().ConfigureAwait(false);
                }
            }

            if (batch.EntryCount > 0)
            {
                _ = readyBatches.TryWrite(batch);
            }

            _ = readyBatches.TryComplete();
        }
        catch (Exception exception)
        {
            _ = readyBatches.TryComplete(exception);
        }
    }

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        using SqliteRecordReader reader = new(connection, s_maxSearchKeyLengthQuery);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
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

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        using JmnedictRecordInserter recordInserter = new(connection);

        foreach (JmnedictRecord record in jmnedictRecords)
        {
            byte[]? readings = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : null;
            byte[]? alternativeSpellings = record.AlternativeSpellings is not null
                ? MessagePackSerializer.Serialize(record.AlternativeSpellings)
                : null;
            byte[] definitions = MessagePackSerializer.Serialize(record.Definitions);
            byte[] nameTypes = MessagePackSerializer.Serialize(record.NameTypes);

            recordInserter.Insert((long)rowId, record.Id, record.PrimarySpelling,
                JapaneseUtils.NormalizeText(record.PrimarySpelling), readings, alternativeSpellings, definitions, nameTypes);

            ++rowId;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = $"CREATE INDEX IF NOT EXISTS ix_record_{PrimarySpellingInHiragana} ON record({PrimarySpellingInHiragana});";
        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        DBUtils.ConfigureForRead(connection);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, ReadOnlySpan<string> terms, int maxSearchKeyLengthForDict)
    {
        int validTermCount = terms.Length;
        if (maxSearchKeyLengthForDict > 0)
        {
            validTermCount = 0;
            foreach (string term in terms)
            {
                if (term.Length <= maxSearchKeyLengthForDict)
                {
                    ++validTermCount;
                }
            }
        }

        if (validTermCount is 0)
        {
            return null;
        }

        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        try
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            using SqliteRecordReader reader = new(connection, GetQuery(validTermCount));
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            if (validTermCount == terms.Length)
            {
                for (int i = 0; i < terms.Length; i++)
                {
                    reader.Bind(i + 1, terms[i]);
                }
            }
            else
            {
                int parameterIndex = 1;
                foreach (string term in terms)
                {
                    if (term.Length <= maxSearchKeyLengthForDict)
                    {
                        reader.Bind(parameterIndex, term);
                        ++parameterIndex;
                    }
                }
            }

            if (!reader.Read())
            {
                return null;
            }

            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            do
            {
                JmnedictRecord record = GetRecord(reader);
                string searchKey = reader.GetString((int)ColumnIndex.PrimarySpellingInHiragana);
                ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, searchKey, out bool exists);
                if (exists)
                {
                    Debug.Assert(result is not null);
                    result.Add(record);
                }
                else
                {
                    result = [record];
                }
            }
            while (reader.Read());

            return results;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is SQLitePCL.raw.SQLITE_BUSY)
        {
            LoggerManager.Logger.Error(ex, "Database is locked for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }
    }

    //public static void LoadFromDB(Dict dict)
    //{
    //    using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
    //    Debug.Assert(connection is not null);
    //
    //    using SqliteCommand command = connection.CreateCommand();
    //
    //    command.CommandText =
    //        $"""
    //        SELECT r.{RowId}, r.{JmnedictId}, r.{PrimarySpelling}, r.{Readings}, r.{AlternativeSpellings}, r.{Glossary}, r.{NameTypes}, r.{PrimarySpellingInHiragana}
    //        FROM {Record} r;
    //        """;
    //
    //    using SqliteDataReader dataReader = command.ExecuteReader();
    //    while (dataReader.Read())
    //    {
    //        JmnedictRecord record = GetRecord(dataReader);
    //        string searchKey = dataReader.GetString((int)ColumnIndex.PrimarySpellingInHiragana);
    //        if (dict.Contents.TryGetValue(searchKey, out IList<IDictRecord>? result))
    //        {
    //            result.Add(record);
    //        }
    //        else
    //        {
    //            dict.Contents[searchKey] = [record];
    //        }
    //    }
    //
    //    dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    //}

    private static JmnedictRecord GetRecord(SqliteRecordReader reader)
    {
        long rowId = reader.GetInt64((int)ColumnIndex.RowId);
        int jmnedictId = reader.GetInt32((int)ColumnIndex.JmnedictId);
        string primarySpelling = reader.GetString((int)ColumnIndex.PrimarySpelling);
        string[]? readings = reader.DeserializeNullable<string[]>((int)ColumnIndex.Readings, Record, Readings, rowId);
        string[]? alternativeSpellings = reader.DeserializeNullable<string[]>((int)ColumnIndex.AlternativeSpellings, Record, AlternativeSpellings, rowId);
        string[][] definitions = reader.Deserialize<string[][]>(Record, Glossary, rowId);
        string[][] nameTypes = reader.Deserialize<string[][]>(Record, NameTypes, rowId);

        return new JmnedictRecord(jmnedictId, primarySpelling, alternativeSpellings, readings, definitions, nameTypes);
    }
}
