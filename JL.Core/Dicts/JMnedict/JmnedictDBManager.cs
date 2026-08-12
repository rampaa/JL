using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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
    public const int Version = 7;

    private const string Record = "record";
    private const string RowId = "rowid";
    private const string JmnedictId = "jmnedict_id";
    private const string PrimarySpelling = "primary_spelling";
    private const string Readings = "readings";
    private const string AlternativeSpellings = "alternative_spellings";
    private const string Glossary = "glossary";
    private const string NameTypes = "name_types";
    private const string PrimarySpellingInHiragana = "primary_spelling_in_hiragana";

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    public static string GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out string? query))
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

        query = queryBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(queryBuilder);
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
        Glossary,
        NameTypes,
        PrimarySpellingInHiragana
    }

    public static void CreateDB(string dbPath)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(dbPath);
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

            int rowId = 1;

            // ReSharper disable once UseAwaitUsing
            using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
            Debug.Assert(connection is not null);

            DBUtils.ConfigureForBulkWrite(connection);

#pragma warning disable CA1849 // Call async methods when in an async method
            SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method

            // ReSharper disable once UseAwaitUsing
            using SqliteCommand insertRecordCommand = connection.CreateCommand();
            insertRecordCommand.CommandText =
                $"""
                INSERT INTO {Record} ({RowId}, {JmnedictId}, {PrimarySpelling}, {PrimarySpellingInHiragana}, {Readings}, {AlternativeSpellings}, {Glossary}, {NameTypes})
                VALUES (@{RowId}, @{JmnedictId}, @{PrimarySpelling}, @{PrimarySpellingInHiragana}, @{Readings}, @{AlternativeSpellings}, @{Glossary}, @{NameTypes});
                """;

            SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
            SqliteParameter jmnedictIdParam = new($"@{JmnedictId}", SqliteType.Integer);
            SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
            SqliteParameter primarySpellingInHiraganaParam = new($"@{PrimarySpellingInHiragana}", SqliteType.Text);
            SqliteParameter readingsParam = new($"@{Readings}", SqliteType.Blob);
            SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
            SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
            SqliteParameter nameTypesParam = new($"@{NameTypes}", SqliteType.Blob);
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

#pragma warning disable CA1849 // Call async methods when in an async method
            insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

            int transactionRecordCount = 0;
            HashSet<JmnedictRecord> jmnedictRecords = [];
            while (xmlTextReader.ReadToFollowing("entry"))
            {
                Dictionary<string, JmnedictRecord> recordDictionary = JmnedictLoader.GetRecordsFromEntry(JmnedictLoader.ReadEntry(xmlTextReader));
                foreach (JmnedictRecord jmnedictRecord in recordDictionary.Values)
                {
                    _ = jmnedictRecords.Add(jmnedictRecord);
                }

                foreach (JmnedictRecord jmnedictRecord in jmnedictRecords)
                {
                    rowidParam.Value = rowId;
                    jmnedictIdParam.Value = jmnedictRecord.Id;
                    primarySpellingParam.Value = jmnedictRecord.PrimarySpelling;
                    primarySpellingInHiraganaParam.Value = JapaneseUtils.NormalizeText(jmnedictRecord.PrimarySpelling);
                    readingsParam.Value = jmnedictRecord.Readings is not null ? MessagePackSerializer.Serialize(jmnedictRecord.Readings) : DBNull.Value;
                    alternativeSpellingsParam.Value = jmnedictRecord.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(jmnedictRecord.AlternativeSpellings) : DBNull.Value;
                    glossaryParam.Value = MessagePackSerializer.Serialize(jmnedictRecord.Definitions);
                    nameTypesParam.Value = MessagePackSerializer.Serialize(jmnedictRecord.NameTypes);

#pragma warning disable CA1849 // Call async methods when in an async method
                    _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                    ++rowId;
                    ++transactionRecordCount;
                }

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
                    insertRecordCommand.Transaction = transaction;
                }

                jmnedictRecords.Clear();
            }

            if (transactionRecordCount > 0)
            {
#pragma warning disable CA1849 // Call async methods when in an async method
                transaction.Commit();
#pragma warning restore CA1849 // Call async methods when in an async method

                dict.Ready = true;
            }

#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method

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

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT MAX(LENGTH({PrimarySpellingInHiragana}))
            FROM {Record};
            """;

        using SqliteDataReader reader = command.ExecuteReader();
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

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {JmnedictId}, {PrimarySpelling}, {PrimarySpellingInHiragana}, {Readings}, {AlternativeSpellings}, {Glossary}, {NameTypes})
            VALUES (@{RowId}, @{JmnedictId}, @{PrimarySpelling}, @{PrimarySpellingInHiragana}, @{Readings}, @{AlternativeSpellings}, @{Glossary}, @{NameTypes});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter jmnedictIdParam = new($"@{JmnedictId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter primarySpellingInHiraganaParam = new($"@{PrimarySpellingInHiragana}", SqliteType.Text);
        SqliteParameter readingsParam = new($"@{Readings}", SqliteType.Blob);
        SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter nameTypesParam = new($"@{NameTypes}", SqliteType.Blob);
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
            primarySpellingInHiraganaParam.Value = JapaneseUtils.NormalizeText(record.PrimarySpelling);
            readingsParam.Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            nameTypesParam.Value = MessagePackSerializer.Serialize(record.NameTypes);
            _ = insertRecordCommand.ExecuteNonQuery();

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
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", readOnlyConnectionString);
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

        int validTermCount = terms.Length > maxSearchKeyLengthForDict && maxSearchKeyLengthForDict > 0
            ? maxSearchKeyLengthForDict
            : terms.Length;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = GetQuery(validTermCount);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int offset = terms.Length - validTermCount;
        for (int i = 0; i < validTermCount; i++)
        {
            _ = command.Parameters.AddWithValue(DBUtils.GetParameterName(i + 1), terms[offset + i]);
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

        return results;
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
