using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using JL.Core.WordClass;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictDBManager
{
    public const int Version = 24;

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    public const string Record = "record";
    public const string RowId = "rowid";
    private const string EdictId = "edict_id";
    public const string PrimarySpelling = "primary_spelling";
    private const string PrimarySpellingOrthographyInfo = "primary_spelling_orthography_info";
    private const string SpellingRestrictions = "spelling_restrictions";
    private const string AlternativeSpellings = "alternative_spellings";
    private const string AlternativeSpellingsOrthographyInfo = "alternative_spellings_orthography_info";
    public const string Readings = "readings";
    private const string ReadingsOrthographyInfo = "readings_orthography_info";
    private const string ReadingRestrictions = "reading_restrictions";
    private const string Glossary = "glossary";
    private const string GlossaryInfo = "glossary_info";
    public const string PartOfSpeechSharedByAllSenses = "part_of_speech_shared_by_all_senses";
    public const string PartOfSpeech = "part_of_speech";
    private const string FieldsSharedByAllSenses = "fields_shared_by_all_senses";
    private const string Fields = "fields";
    private const string MiscSharedByAllSenses = "misc_shared_by_all_senses";
    private const string Misc = "misc";
    private const string DialectsSharedByAllSenses = "dialects_shared_by_all_senses";
    private const string Dialects = "dialects";
    private const string LoanwordEtymology = "loanword_etymology";
    private const string CrossReferences = "cross_references";
    private const string Info = "info";

    public const string RecordSearchKey = "record_search_key";
    public const string SearchKey = "search_key";
    public const string RecordId = "record_id";

    private static string GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out string? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{RowId},
                   r.{EdictId},
                   r.{PrimarySpelling},
                   r.{PrimarySpellingOrthographyInfo},
                   r.{SpellingRestrictions},
                   r.{AlternativeSpellings},
                   r.{AlternativeSpellingsOrthographyInfo},
                   r.{Readings},
                   r.{ReadingsOrthographyInfo},
                   r.{ReadingRestrictions},
                   r.{Glossary},
                   r.{GlossaryInfo},
                   r.{PartOfSpeechSharedByAllSenses},
                   r.{PartOfSpeech},
                   r.{FieldsSharedByAllSenses},
                   r.{Fields},
                   r.{MiscSharedByAllSenses},
                   r.{Misc},
                   r.{DialectsSharedByAllSenses},
                   r.{Dialects},
                   r.{LoanwordEtymology},
                   r.{CrossReferences},
                   r.{Info},
                   rsk.{SearchKey}
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            WHERE rsk.{SearchKey} IN (@1
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
        EdictId,
        PrimarySpelling,
        PrimarySpellingOrthographyInfo,
        SpellingRestrictions,
        AlternativeSpellings,
        AlternativeSpellingsOrthographyInfo,
        Readings,
        ReadingsOrthographyInfo,
        ReadingRestrictions,
        Glossary,
        GlossaryInfo,
        WordClassesSharedByAllSenses,
        WordClasses,
        FieldsSharedByAllSenses,
        Fields,
        MiscSharedByAllSenses,
        Misc,
        DialectsSharedByAllSenses,
        Dialects,
        LoanwordEtymology,
        CrossReferences,
        Info,
        SearchKey
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
                {RowId} INTEGER NOT NULL PRIMARY KEY,
                {EdictId} INTEGER NOT NULL,
                {PrimarySpelling} TEXT NOT NULL,
                {PrimarySpellingOrthographyInfo} BLOB,
                {Readings} BLOB,
                {AlternativeSpellings} BLOB,
                {AlternativeSpellingsOrthographyInfo} BLOB,
                {ReadingsOrthographyInfo} BLOB,
                {ReadingRestrictions} BLOB,
                {Glossary} BLOB NOT NULL,
                {GlossaryInfo} BLOB,
                {PartOfSpeechSharedByAllSenses} BLOB,
                {PartOfSpeech} BLOB,
                {SpellingRestrictions} BLOB,
                {FieldsSharedByAllSenses} BLOB,
                {Fields} BLOB,
                {MiscSharedByAllSenses} BLOB,
                {Misc} BLOB,
                {DialectsSharedByAllSenses} BLOB,
                {Dialects} BLOB,
                {LoanwordEtymology} BLOB,
                {CrossReferences} BLOB,
                {Info} BLOB
            ) STRICT;

            CREATE TABLE IF NOT EXISTS {RecordSearchKey}
            (
                {SearchKey} TEXT NOT NULL,
                {RecordId} INTEGER NOT NULL,
                PRIMARY KEY ({SearchKey}, {RecordId}),
                FOREIGN KEY ({RecordId}) REFERENCES {Record} ({RowId}) ON DELETE CASCADE
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
            DictUtils.JmdictEntities.Clear();

            // ReSharper disable once UseAwaitUsing
            using FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso);

            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)
            using XmlTextReader xmlReader = new(fileStream);
            xmlReader.DtdProcessing = DtdProcessing.Parse;
            xmlReader.WhitespaceHandling = WhitespaceHandling.None;
            xmlReader.EntityHandling = EntityHandling.ExpandCharEntities;

            ProperNameEntriesOption? properNamesEntriesOption = dict.Options.ProperNameEntries;
            Debug.Assert(properNamesEntriesOption is not null);
            bool includeProperNames = properNamesEntriesOption.Value;

            GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
            Debug.Assert(generateMazegakiOption is not null);
            bool generateMazegaki = generateMazegakiOption.Value;

            GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
            Debug.Assert(generateFusejiVariantsOption is not null);
            bool generateFusejiVariants = generateFusejiVariantsOption.Value;

            int maxSearchKeyLengthForFusejiGeneration;
            int maxTotalFuseji;
            if (generateFusejiVariants)
            {
                Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
                maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

                Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
                maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;
            }
            else
            {
                maxSearchKeyLengthForFusejiGeneration = 0;
                maxTotalFuseji = 0;
            }

            long rowId = 1;

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
                INSERT INTO {Record} ({RowId}, {EdictId}, {PrimarySpelling}, {PrimarySpellingOrthographyInfo}, {AlternativeSpellings}, {AlternativeSpellingsOrthographyInfo}, {Readings}, {ReadingsOrthographyInfo}, {ReadingRestrictions}, {Glossary}, {GlossaryInfo}, {PartOfSpeechSharedByAllSenses}, {PartOfSpeech}, {SpellingRestrictions}, {FieldsSharedByAllSenses}, {Fields}, {MiscSharedByAllSenses}, {Misc}, {DialectsSharedByAllSenses}, {Dialects}, {LoanwordEtymology}, {CrossReferences}, {Info})
                VALUES (@{RowId}, @{EdictId}, @{PrimarySpelling}, @{PrimarySpellingOrthographyInfo}, @{AlternativeSpellings}, @{AlternativeSpellingsOrthographyInfo}, @{Readings}, @{ReadingsOrthographyInfo}, @{ReadingRestrictions}, @{Glossary}, @{GlossaryInfo}, @{PartOfSpeechSharedByAllSenses}, @{PartOfSpeech}, @{SpellingRestrictions}, @{FieldsSharedByAllSenses}, @{Fields}, @{MiscSharedByAllSenses}, @{Misc}, @{DialectsSharedByAllSenses}, @{Dialects}, @{LoanwordEtymology}, @{CrossReferences}, @{Info});
                """;

            SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
            SqliteParameter edictIdParam = new($"@{EdictId}", SqliteType.Integer);
            SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
            SqliteParameter primarySpellingOrthographyInfoParam = new($"@{PrimarySpellingOrthographyInfo}", SqliteType.Blob);
            SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
            SqliteParameter alternativeSpellingsOrthographyInfoParam = new($"@{AlternativeSpellingsOrthographyInfo}", SqliteType.Blob);
            SqliteParameter readingsParam = new($"@{Readings}", SqliteType.Blob);
            SqliteParameter readingsOrthographyInfoParam = new($"@{ReadingsOrthographyInfo}", SqliteType.Blob);
            SqliteParameter readingRestrictionsParam = new($"@{ReadingRestrictions}", SqliteType.Blob);
            SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
            SqliteParameter glossaryInfoParam = new($"@{GlossaryInfo}", SqliteType.Blob);
            SqliteParameter partOfSpeechSharedByAllSensesParam = new($"@{PartOfSpeechSharedByAllSenses}", SqliteType.Blob);
            SqliteParameter partOfSpeechParam = new($"@{PartOfSpeech}", SqliteType.Blob);
            SqliteParameter spellingRestrictionsParam = new($"@{SpellingRestrictions}", SqliteType.Blob);
            SqliteParameter fieldsSharedByAllSensesParam = new($"@{FieldsSharedByAllSenses}", SqliteType.Blob);
            SqliteParameter fieldsParam = new($"@{Fields}", SqliteType.Blob);
            SqliteParameter miscSharedByAllSensesParam = new($"@{MiscSharedByAllSenses}", SqliteType.Blob);
            SqliteParameter miscParam = new($"@{Misc}", SqliteType.Blob);
            SqliteParameter dialectsSharedByAllSensesParam = new($"@{DialectsSharedByAllSenses}", SqliteType.Blob);
            SqliteParameter dialectsParam = new($"@{Dialects}", SqliteType.Blob);
            SqliteParameter loanwordEtymologyParam = new($"@{LoanwordEtymology}", SqliteType.Blob);
            SqliteParameter crossReferencesParam = new($"@{CrossReferences}", SqliteType.Blob);
            SqliteParameter infoParam = new($"@{Info}", SqliteType.Blob);
            insertRecordCommand.Parameters.AddRange([
                rowidParam,
                    edictIdParam,
                    primarySpellingParam,
                    primarySpellingOrthographyInfoParam,
                    alternativeSpellingsParam,
                    alternativeSpellingsOrthographyInfoParam,
                    readingsParam,
                    readingsOrthographyInfoParam,
                    readingRestrictionsParam,
                    glossaryParam,
                    glossaryInfoParam,
                    partOfSpeechSharedByAllSensesParam,
                    partOfSpeechParam,
                    spellingRestrictionsParam,
                    fieldsSharedByAllSensesParam,
                    fieldsParam,
                    miscSharedByAllSensesParam,
                    miscParam,
                    dialectsSharedByAllSensesParam,
                    dialectsParam,
                    loanwordEtymologyParam,
                    crossReferencesParam,
                    infoParam
                ]);

#pragma warning disable CA1849 // Call async methods when in an async method
            insertRecordCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

            // ReSharper disable once UseAwaitUsing
            using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
            insertSearchKeyCommand.CommandText =
                $"""
                INSERT INTO {RecordSearchKey}({RecordId}, {SearchKey})
                VALUES (@{RecordId}, @{SearchKey});
                """;

            SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
            SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
            insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
#pragma warning disable CA1849 // Call async methods when in an async method
            insertSearchKeyCommand.Prepare();
#pragma warning restore CA1849 // Call async methods when in an async method

            Dictionary<JmdictRecord, List<string>> recordsToKeys = [];
            int transactionRecordCount = 0;
            while (xmlReader.ReadToFollowing("entry"))
            {
                Dictionary<string, JmdictRecord>? recordDictionary = JmdictRecordBuilder.GetRecordsFromEntry(JmdictLoader.ReadEntry(xmlReader), includeProperNames);
                if (recordDictionary is not null)
                {
                    foreach ((string key, JmdictRecord record) in recordDictionary)
                    {
                        ref List<string>? keys = ref CollectionsMarshal.GetValueRefOrAddDefault(recordsToKeys, record, out bool exists);
                        if (exists)
                        {
                            Debug.Assert(keys is not null);
                            keys.Add(key);
                        }
                        else
                        {
                            keys = [key];
                        }
                    }

                    foreach ((JmdictRecord record, List<string> keys) in recordsToKeys)
                    {
                        rowidParam.Value = rowId;
                        edictIdParam.Value = record.Id;
                        primarySpellingParam.Value = record.PrimarySpelling;
                        primarySpellingOrthographyInfoParam.Value = record.PrimarySpellingOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.PrimarySpellingOrthographyInfo) : DBNull.Value;
                        alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
                        alternativeSpellingsOrthographyInfoParam.Value = record.AlternativeSpellingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo) : DBNull.Value;
                        readingsParam.Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
                        readingsOrthographyInfoParam.Value = record.ReadingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.ReadingsOrthographyInfo) : DBNull.Value;
                        readingRestrictionsParam.Value = record.ReadingRestrictions is not null ? MessagePackSerializer.Serialize(record.ReadingRestrictions) : DBNull.Value;
                        glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
                        glossaryInfoParam.Value = record.DefinitionInfo is not null ? MessagePackSerializer.Serialize(record.DefinitionInfo) : DBNull.Value;
                        partOfSpeechSharedByAllSensesParam.Value = record.WordClassesSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.WordClassesSharedByAllSenses) : DBNull.Value;
                        partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
                        spellingRestrictionsParam.Value = record.SpellingRestrictions is not null ? MessagePackSerializer.Serialize(record.SpellingRestrictions) : DBNull.Value;
                        fieldsSharedByAllSensesParam.Value = record.FieldsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.FieldsSharedByAllSenses) : DBNull.Value;
                        fieldsParam.Value = record.Fields is not null ? MessagePackSerializer.Serialize(record.Fields) : DBNull.Value;
                        miscSharedByAllSensesParam.Value = record.MiscSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.MiscSharedByAllSenses) : DBNull.Value;
                        miscParam.Value = record.Misc is not null ? MessagePackSerializer.Serialize(record.Misc) : DBNull.Value;
                        dialectsSharedByAllSensesParam.Value = record.DialectsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.DialectsSharedByAllSenses) : DBNull.Value;
                        dialectsParam.Value = record.Dialects is not null ? MessagePackSerializer.Serialize(record.Dialects) : DBNull.Value;
                        loanwordEtymologyParam.Value = record.LoanwordEtymology is not null ? MessagePackSerializer.Serialize(record.LoanwordEtymology) : DBNull.Value;
                        crossReferencesParam.Value = record.CrossReferences is not null ? MessagePackSerializer.Serialize(record.CrossReferences) : DBNull.Value;
                        infoParam.Value = record.Info is not null ? MessagePackSerializer.Serialize(record.Info) : DBNull.Value;
#pragma warning disable CA1849 // Call async methods when in an async method
                        _ = insertRecordCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method

                        HashSet<string> uniqueKeys = [.. keys];
                        if (generateFusejiVariants || generateMazegaki)
                        {
                            foreach (string key in keys)
                            {
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(key, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = uniqueKeys.Add(fusejiVariant);
                                    }
                                }

                                if (generateMazegaki && record.Readings is not null)
                                {
                                    foreach (string reading in record.Readings)
                                    {
                                        string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                                        if (readingInHiragana != key)
                                        {
                                            foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(key, readingInHiragana))
                                            {
                                                if (!recordDictionary.ContainsKey(mazegaki))
                                                {
                                                    if (uniqueKeys.Add(mazegaki) && generateFusejiVariants)
                                                    {
                                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                        {
                                                            if (!recordDictionary.ContainsKey(fusejiVariant))
                                                            {
                                                                _ = uniqueKeys.Add(fusejiVariant);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        recordIdParam.Value = rowId;
                        foreach (string key in uniqueKeys)
                        {
                            searchKeyParam.Value = key;
#pragma warning disable CA1849 // Call async methods when in an async method
                            _ = insertSearchKeyCommand.ExecuteNonQuery();
#pragma warning restore CA1849 // Call async methods when in an async method
                        }

                        transactionRecordCount += uniqueKeys.Count;
                        uniqueKeys.Clear();
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
                            insertSearchKeyCommand.Transaction = transaction;
                        }

                        ++rowId;
                    }

                    recordsToKeys.Clear();
                }
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

                dict.Size = GetDistinctSearchKeyCount(connection);
                dict.MaxSearchKeyLength = GetMaxSearchKeyLength(connection);
            }
            else
            {
                dict.Size = 0;
                dict.MaxSearchKeyLength = 0;
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
                "Couldn't find JMdict.xml. Would you like to download it now?",
                "Download JMdict?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.JMdict), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await ImportFromDisk(dict).ConfigureAwait(false);
                        await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
                        await JmdictWordClassUtils.Load().ConfigureAwait(false);
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

    private static int GetDistinctSearchKeyCount(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT COUNT(DISTINCT {SearchKey})
            FROM {RecordSearchKey};
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT MAX(LENGTH({SearchKey}))
            FROM {RecordSearchKey};
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<JmdictRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                JmdictRecord record = (JmdictRecord)records[i];
                ref List<string>? keys = ref CollectionsMarshal.GetValueRefOrAddDefault(recordToKeysDict, record, out bool exists);
                if (exists)
                {
                    Debug.Assert(keys is not null);
                    keys.Add(key);
                }
                else
                {
                    keys = [key];
                }
            }
        }

        long rowId = 1;

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            $"""
            INSERT INTO {Record} ({RowId}, {EdictId}, {PrimarySpelling}, {PrimarySpellingOrthographyInfo}, {AlternativeSpellings}, {AlternativeSpellingsOrthographyInfo}, {Readings}, {ReadingsOrthographyInfo}, {ReadingRestrictions}, {Glossary}, {GlossaryInfo}, {PartOfSpeechSharedByAllSenses}, {PartOfSpeech}, {SpellingRestrictions}, {FieldsSharedByAllSenses}, {Fields}, {MiscSharedByAllSenses}, {Misc}, {DialectsSharedByAllSenses}, {Dialects}, {LoanwordEtymology}, {CrossReferences}, {Info})
            VALUES (@{RowId}, @{EdictId}, @{PrimarySpelling}, @{PrimarySpellingOrthographyInfo}, @{AlternativeSpellings}, @{AlternativeSpellingsOrthographyInfo}, @{Readings}, @{ReadingsOrthographyInfo}, @{ReadingRestrictions}, @{Glossary}, @{GlossaryInfo}, @{PartOfSpeechSharedByAllSenses}, @{PartOfSpeech}, @{SpellingRestrictions}, @{FieldsSharedByAllSenses}, @{Fields}, @{MiscSharedByAllSenses}, @{Misc}, @{DialectsSharedByAllSenses}, @{Dialects}, @{LoanwordEtymology}, @{CrossReferences}, @{Info});
            """;

        SqliteParameter rowidParam = new($"@{RowId}", SqliteType.Integer);
        SqliteParameter edictIdParam = new($"@{EdictId}", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new($"@{PrimarySpelling}", SqliteType.Text);
        SqliteParameter primarySpellingOrthographyInfoParam = new($"@{PrimarySpellingOrthographyInfo}", SqliteType.Blob);
        SqliteParameter alternativeSpellingsParam = new($"@{AlternativeSpellings}", SqliteType.Blob);
        SqliteParameter alternativeSpellingsOrthographyInfoParam = new($"@{AlternativeSpellingsOrthographyInfo}", SqliteType.Blob);
        SqliteParameter readingsParam = new($"@{Readings}", SqliteType.Blob);
        SqliteParameter readingsOrthographyInfoParam = new($"@{ReadingsOrthographyInfo}", SqliteType.Blob);
        SqliteParameter readingRestrictionsParam = new($"@{ReadingRestrictions}", SqliteType.Blob);
        SqliteParameter glossaryParam = new($"@{Glossary}", SqliteType.Blob);
        SqliteParameter glossaryInfoParam = new($"@{GlossaryInfo}", SqliteType.Blob);
        SqliteParameter partOfSpeechSharedByAllSensesParam = new($"@{PartOfSpeechSharedByAllSenses}", SqliteType.Blob);
        SqliteParameter partOfSpeechParam = new($"@{PartOfSpeech}", SqliteType.Blob);
        SqliteParameter spellingRestrictionsParam = new($"@{SpellingRestrictions}", SqliteType.Blob);
        SqliteParameter fieldsSharedByAllSensesParam = new($"@{FieldsSharedByAllSenses}", SqliteType.Blob);
        SqliteParameter fieldsParam = new($"@{Fields}", SqliteType.Blob);
        SqliteParameter miscSharedByAllSensesParam = new($"@{MiscSharedByAllSenses}", SqliteType.Blob);
        SqliteParameter miscParam = new($"@{Misc}", SqliteType.Blob);
        SqliteParameter dialectsSharedByAllSensesParam = new($"@{DialectsSharedByAllSenses}", SqliteType.Blob);
        SqliteParameter dialectsParam = new($"@{Dialects}", SqliteType.Blob);
        SqliteParameter loanwordEtymologyParam = new($"@{LoanwordEtymology}", SqliteType.Blob);
        SqliteParameter crossReferencesParam = new($"@{CrossReferences}", SqliteType.Blob);
        SqliteParameter infoParam = new($"@{Info}", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            edictIdParam,
            primarySpellingParam,
            primarySpellingOrthographyInfoParam,
            alternativeSpellingsParam,
            alternativeSpellingsOrthographyInfoParam,
            readingsParam,
            readingsOrthographyInfoParam,
            readingRestrictionsParam,
            glossaryParam,
            glossaryInfoParam,
            partOfSpeechSharedByAllSensesParam,
            partOfSpeechParam,
            spellingRestrictionsParam,
            fieldsSharedByAllSensesParam,
            fieldsParam,
            miscSharedByAllSensesParam,
            miscParam,
            dialectsSharedByAllSensesParam,
            dialectsParam,
            loanwordEtymologyParam,
            crossReferencesParam,
            infoParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            $"""
            INSERT INTO {RecordSearchKey}({RecordId}, {SearchKey})
            VALUES (@{RecordId}, @{SearchKey});
            """;

        SqliteParameter recordIdParam = new($"@{RecordId}", SqliteType.Integer);
        SqliteParameter searchKeyParam = new($"@{SearchKey}", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach ((JmdictRecord record, List<string> keys) in recordToKeysDict)
        {
            rowidParam.Value = rowId;
            edictIdParam.Value = record.Id;
            primarySpellingParam.Value = record.PrimarySpelling;
            primarySpellingOrthographyInfoParam.Value = record.PrimarySpellingOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.PrimarySpellingOrthographyInfo) : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            alternativeSpellingsOrthographyInfoParam.Value = record.AlternativeSpellingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo) : DBNull.Value;
            readingsParam.Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
            readingsOrthographyInfoParam.Value = record.ReadingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.ReadingsOrthographyInfo) : DBNull.Value;
            readingRestrictionsParam.Value = record.ReadingRestrictions is not null ? MessagePackSerializer.Serialize(record.ReadingRestrictions) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            glossaryInfoParam.Value = record.DefinitionInfo is not null ? MessagePackSerializer.Serialize(record.DefinitionInfo) : DBNull.Value;
            partOfSpeechSharedByAllSensesParam.Value = record.WordClassesSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.WordClassesSharedByAllSenses) : DBNull.Value;
            partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            spellingRestrictionsParam.Value = record.SpellingRestrictions is not null ? MessagePackSerializer.Serialize(record.SpellingRestrictions) : DBNull.Value;
            fieldsSharedByAllSensesParam.Value = record.FieldsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.FieldsSharedByAllSenses) : DBNull.Value;
            fieldsParam.Value = record.Fields is not null ? MessagePackSerializer.Serialize(record.Fields) : DBNull.Value;
            miscSharedByAllSensesParam.Value = record.MiscSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.MiscSharedByAllSenses) : DBNull.Value;
            miscParam.Value = record.Misc is not null ? MessagePackSerializer.Serialize(record.Misc) : DBNull.Value;
            dialectsSharedByAllSensesParam.Value = record.DialectsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.DialectsSharedByAllSenses) : DBNull.Value;
            dialectsParam.Value = record.Dialects is not null ? MessagePackSerializer.Serialize(record.Dialects) : DBNull.Value;
            loanwordEtymologyParam.Value = record.LoanwordEtymology is not null ? MessagePackSerializer.Serialize(record.LoanwordEtymology) : DBNull.Value;
            crossReferencesParam.Value = record.CrossReferences is not null ? MessagePackSerializer.Serialize(record.CrossReferences) : DBNull.Value;
            infoParam.Value = record.Info is not null ? MessagePackSerializer.Serialize(record.Info) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowId;
            foreach (ref readonly string key in keys.AsReadOnlySpan())
            {
                searchKeyParam.Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();
            }

            ++rowId;
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
            JmdictRecord record = GetRecord(dataReader);
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
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

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT r.{RowId},
                   r.{EdictId},
                   r.{PrimarySpelling},
                   r.{PrimarySpellingOrthographyInfo},
                   r.{SpellingRestrictions},
                   r.{AlternativeSpellings},
                   r.{AlternativeSpellingsOrthographyInfo},
                   r.{Readings},
                   r.{ReadingsOrthographyInfo},
                   r.{ReadingRestrictions},
                   r.{Glossary},
                   r.{GlossaryInfo},
                   r.{PartOfSpeechSharedByAllSenses},
                   r.{PartOfSpeech},
                   r.{FieldsSharedByAllSenses},
                   r.{Fields},
                   r.{MiscSharedByAllSenses},
                   r.{Misc},
                   r.{DialectsSharedByAllSenses},
                   r.{Dialects},
                   r.{LoanwordEtymology},
                   r.{CrossReferences},
                   r.{Info},
                   json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            JmdictRecord record = GetRecord(dataReader);
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString((int)ColumnIndex.SearchKey), JsonOptions.DefaultJso);
            Debug.Assert(searchKeys is not null);

            Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
            Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
            foreach (string searchKey in searchKeys)
            {
                ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, searchKey, out bool exists);
                if (exists)
                {
                    Debug.Assert(result is not null);
                    result.Add(record);
                }
                else
                {
                    result = [record];
                }

                if (searchKey.Length > dict.MaxSearchKeyLength)
                {
                    dict.MaxSearchKeyLength = searchKey.Length;
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static JmdictRecord GetRecord(SqliteDataReader dataReader)
    {
        int edictId = dataReader.GetInt32((int)ColumnIndex.EdictId);
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);
        string[]? primarySpellingOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.PrimarySpellingOrthographyInfo);
        string[]?[]? spellingRestrictions = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.SpellingRestrictions);
        string[]? alternativeSpellings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.AlternativeSpellings);
        string[]?[]? alternativeSpellingsOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.AlternativeSpellingsOrthographyInfo);
        string[]? readings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.Readings);
        string[]?[]? readingsOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.ReadingsOrthographyInfo);
        string[]?[]? readingRestrictions = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.ReadingRestrictions);
        string[][] definitions = dataReader.GetValueFromBlobStream<string[][]>((int)ColumnIndex.Glossary);
        string?[]? definitionInfo = dataReader.GetNullableValueFromBlobStream<string?[]>((int)ColumnIndex.GlossaryInfo);
        string[]? wordClassesSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.WordClassesSharedByAllSenses);
        string[]?[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.WordClasses);
        string[]? fieldsSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.FieldsSharedByAllSenses);
        string[]?[]? fields = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Fields);
        string[]? miscSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.MiscSharedByAllSenses);
        string[]?[]? misc = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Misc);
        string[]? dialectsSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.DialectsSharedByAllSenses);
        string[]?[]? dialects = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Dialects);
        LoanwordSource[]? loanwordEtymology = dataReader.GetNullableValueFromBlobStream<LoanwordSource[]>((int)ColumnIndex.LoanwordEtymology);
        string[]?[]? crossReferences = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.CrossReferences);
        string[]? info = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.Info);

        return new JmdictRecord(edictId, primarySpelling, definitions, wordClasses, wordClassesSharedByAllSenses, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, spellingRestrictions, readingRestrictions, fields, fieldsSharedByAllSenses, misc, miscSharedByAllSenses, definitionInfo, dialects, dialectsSharedByAllSenses, loanwordEtymology, crossReferences, info);
    }
}
