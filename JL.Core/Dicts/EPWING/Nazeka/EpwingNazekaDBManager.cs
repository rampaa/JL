using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaDBManager
{
    public const int Version = 24;

    private const int ImportRecordBatchSize = 64;
    private const int VariantSearchKeyRecordBatchSize = 8192;
    private const int VariantSearchKeyTransactionBatchSize = 20_000_000;
    private const long WholeFileParsingThreshold = 32 * 1024 * 1024;
    private static readonly int s_workerCount = Environment.ProcessorCount;
    private static readonly int s_importRecordBatchChannelCapacity = Math.Max(1, s_workerCount * 16 / ImportRecordBatchSize);
    private static readonly JsonReaderState s_initialJsonReaderState = CreateJsonReaderState();

    internal const string Record = "record";
    internal const string RowId = "rowid";
    internal const string PrimarySpelling = "primary_spelling";
    internal const string Reading = "reading";
    internal const string Glossary = "glossary";
    internal const string AlternativeSpellings = "alternative_spellings";
    internal const string ImageInfo = "image_info";

    internal const string RecordSearchKey = "record_search_key";
    internal const string RecordId = "record_id";
    internal const string SearchKey = "search_key";

    private const string Term = "term";
    private const string SingleTermQuery =
        $"""
        SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}
        FROM {Record} r
        JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
        WHERE rsk.{SearchKey} = @{Term};
        """;

    private static readonly ConcurrentDictionary<int, string> s_queryCache = [];

    private static string GetQuery(int termCount)
    {
        if (s_queryCache.TryGetValue(termCount, out string? query))
        {
            return query;
        }

        StringBuilder queryBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(
            $"""
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}, rsk.{SearchKey}
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
        PrimarySpelling,
        Reading,
        AlternativeSpellings,
        // ReSharper disable once UnusedMember.Local
        Glossary,
        ImageInfo,
        SearchKey
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
                {PrimarySpelling} TEXT NOT NULL,
                {Reading} TEXT,
                {AlternativeSpellings} BLOB,
                {Glossary} BLOB NOT NULL,
                {ImageInfo} BLOB
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
        if (!File.Exists(fullPath))
        {
            return;
        }

        byte[]? json = null;
        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            if (fileStream.Length <= WholeFileParsingThreshold)
            {
                json = GC.AllocateUninitializedArray<byte>((int)fileStream.Length);
                await fileStream.ReadExactlyAsync(json).ConfigureAwait(false);
            }

            await ImportFromJson(dict, json, fileStream).ConfigureAwait(false);
        }
    }

    private static async Task ImportFromJson(Dict dict, byte[]? json, FileStream fileStream)
    {
        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameNazeka;

        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        bool generateMazegaki = false;
        if (nonKanjiDict && nonNameDict)
        {
            Debug.Assert(generateMazegakiOption is not null);
            generateMazegaki = generateMazegakiOption.Value;
        }

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        bool generateFusejiVariants = false;
        if (nonKanjiDict)
        {
            Debug.Assert(generateFusejiVariantsOption is not null);
            generateFusejiVariants = generateFusejiVariantsOption.Value;
        }

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

        // ReSharper disable once UseAwaitUsing
        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);

        using EpwingNazekaRecordInserter recordInserter = new(connection);
        using EpwingNazekaSearchKeyInserter searchKeyInserter = new(connection);

        ConcurrentDictionary<string, byte[]> imageInfoCache = new(StringComparer.Ordinal);
        int transactionRecordCount = 0;
        long rowId = 1;
        List<long>? entryRowIds = generateMazegaki || generateFusejiVariants
            ? []
            : null;

#pragma warning disable CA1849 // Call async methods when in an async method
        SqliteTransaction transaction = connection.BeginTransaction();
#pragma warning restore CA1849 // Call async methods when in an async method
        try
        {
            Channel<EpwingNazekaImportEntryBatch> inputChannel = Channel.CreateBounded<EpwingNazekaImportEntryBatch>(
                new BoundedChannelOptions(s_importRecordBatchChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleWriter = true
                });
            Channel<EpwingNazekaPreparedRecordBatch> outputChannel = Channel.CreateBounded<EpwingNazekaPreparedRecordBatch>(
                new BoundedChannelOptions(s_importRecordBatchChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true
                });

            Task producer = Task.Run(() => CreateImportBatches(json, fileStream, inputChannel.Writer));

            Task[] workers = new Task[s_workerCount];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(() => CreatePreparedRecords(inputChannel.Reader, inputChannel.Writer, outputChannel.Writer, nonKanjiDict, nonNameDict, imageInfoCache));
            }

            Task completeOutputChannel = CompleteOutputChannel(producer, workers, outputChannel.Writer);
            try
            {
                await foreach (EpwingNazekaPreparedRecordBatch batch in outputChannel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    try
                    {
                        for (int i = 0; i < batch.RecordCount; i++)
                        {
                            ref readonly EpwingNazekaPreparedRecord record = ref batch.Records[i];
                            if (entryRowIds is not null && record.IsFirstInEntry)
                            {
                                entryRowIds.Add(rowId);
                            }

                            recordInserter.Insert(rowId, record.PrimarySpelling, record.Reading, record.AlternativeSpellings, record.Definitions, record.ImageInfo);
                            searchKeyInserter.Insert(rowId, batch.SearchKeys.AsSpan(record.SearchKeyOffset, record.SearchKeyCount));
                            transactionRecordCount += record.SearchKeyCount;
                            ++rowId;

                            if (transactionRecordCount > DBUtils.TransactionBatchSize)
                            {
#pragma warning disable CA1849 // Call async methods when in an async method
                                transaction.Commit();

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
                        ReturnPreparedRecordBatch(batch);
                    }
                }
            }
            catch (Exception exception)
            {
                _ = inputChannel.Writer.TryComplete(exception);
                _ = outputChannel.Writer.TryComplete(exception);
                throw;
            }
            finally
            {
                await completeOutputChannel.ConfigureAwait(false);

                while (inputChannel.Reader.TryRead(out EpwingNazekaImportEntryBatch remainingBatch))
                {
                    remainingBatch.Entries.AsSpan(0, remainingBatch.Count).Clear();
                    ArrayPool<EpwingNazekaImportEntry>.Shared.Return(remainingBatch.Entries);
                }

                while (outputChannel.Reader.TryRead(out EpwingNazekaPreparedRecordBatch remainingBatch))
                {
                    ReturnPreparedRecordBatch(remainingBatch);
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
        finally
        {
#pragma warning disable CA1849 // Call async methods when in an async method
            // ReSharper disable once MethodHasAsyncOverload
            transaction.Dispose();
#pragma warning restore CA1849 // Call async methods when in an async method
        }

        if (rowId > 1)
        {
            RemoveDuplicateRecords(connection);
        }

        if (rowId > 1 && (generateMazegaki || generateFusejiVariants))
        {
            DBUtils.FlushWalLog(connection);

            Debug.Assert(entryRowIds is not null);
            InsertVariantSearchKeys(connection, searchKeyInserter, entryRowIds, nonKanjiDict, nonNameDict, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration);
        }

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
            DBUtils.DeleteDB(dict.DBPath);
            dict.Size = 0;
            dict.MaxSearchKeyLength = 0;
        }
    }

    private static async Task CreateImportBatches(byte[]? json, FileStream fileStream, ChannelWriter<EpwingNazekaImportEntryBatch> writer)
    {
        try
        {
            if (json is not null)
            {
                int offset = json.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
                JsonReaderState readerState = s_initialJsonReaderState;
                bool started = false;
                bool completed = false;

                while (!completed)
                {
                    EpwingNazekaImportEntry[]? entries = ArrayPool<EpwingNazekaImportEntry>.Shared.Rent(ImportRecordBatchSize);
                    int entriesToClear = entries.Length;
                    try
                    {
                        int entryCount = ReadImportBatch(json, ref offset, ref readerState, ref started, entries, out completed);
                        entriesToClear = entryCount;
                        if (entryCount is 0)
                        {
                            ArrayPool<EpwingNazekaImportEntry>.Shared.Return(entries);
                            entries = null;
                            continue;
                        }

                        await writer.WriteAsync(new EpwingNazekaImportEntryBatch(entries, entryCount)).ConfigureAwait(false);
                        entries = null;
                    }
                    finally
                    {
                        if (entries is not null)
                        {
                            entries.AsSpan(0, entriesToClear).Clear();
                            ArrayPool<EpwingNazekaImportEntry>.Shared.Return(entries);
                        }
                    }
                }
            }
            else
            {
                await CreateImportBatchesFromStream(fileStream, writer).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
            throw;
        }

        _ = writer.TryComplete();
    }

    private static async Task CreateImportBatchesFromStream(FileStream fileStream, ChannelWriter<EpwingNazekaImportEntryBatch> writer)
    {
        EpwingNazekaImportEntry[] entries = ArrayPool<EpwingNazekaImportEntry>.Shared.Rent(ImportRecordBatchSize);
        int entryCount = 0;
        try
        {
            IAsyncEnumerator<JsonElement> enumerator = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).GetAsyncEnumerator();
            await using (enumerator.ConfigureAwait(false))
            {
                _ = await enumerator.MoveNextAsync().ConfigureAwait(false);
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    JsonElement jsonObject = enumerator.Current;
                    string? reading = jsonObject.GetProperty("r").GetString();
                    Debug.Assert(reading is not null);

                    JsonElement spellingArray = jsonObject.GetProperty("s");
                    List<string> spellings = new(spellingArray.GetArrayLength());
                    foreach (JsonElement spellingElement in spellingArray.EnumerateArray())
                    {
                        string? spelling = spellingElement.GetString();
                        if (!string.IsNullOrWhiteSpace(spelling))
                        {
                            spellings.Add(spelling);
                        }
                    }

                    JsonElement definitionArray = jsonObject.GetProperty("l");
                    List<string> definitions = new(definitionArray.GetArrayLength());
                    foreach (JsonElement definitionElement in definitionArray.EnumerateArray())
                    {
                        string? definition = definitionElement.GetString();
                        if (!string.IsNullOrWhiteSpace(definition))
                        {
                            definitions.Add(definition);
                        }
                    }

                    string? imagePath = jsonObject.TryGetProperty("i", out JsonElement imageElement)
                        ? imageElement.GetString()
                        : null;

                    entries[entryCount] = new EpwingNazekaImportEntry(reading,
                        spellings.Count > 0 ? spellings : null,
                        definitions,
                        imagePath);

                    ++entryCount;

                    if (entryCount is ImportRecordBatchSize)
                    {
                        await writer.WriteAsync(new EpwingNazekaImportEntryBatch(entries, entryCount)).ConfigureAwait(false);
                        entries = [];
                        entryCount = 0;
                        entries = ArrayPool<EpwingNazekaImportEntry>.Shared.Rent(ImportRecordBatchSize);
                    }
                }
            }

            if (entryCount > 0)
            {
                await writer.WriteAsync(new EpwingNazekaImportEntryBatch(entries, entryCount)).ConfigureAwait(false);
                entries = [];
                entryCount = 0;
            }
        }
        finally
        {
            if (entries.Length > 0)
            {
                entries.AsSpan(0, entryCount).Clear();
                ArrayPool<EpwingNazekaImportEntry>.Shared.Return(entries);
            }
        }
    }

    private static int ReadImportBatch(byte[] json, ref int offset, ref JsonReaderState readerState, ref bool started, EpwingNazekaImportEntry[] entries, out bool completed)
    {
        ReadOnlySpan<byte> jsonBytes = json;
        Utf8JsonReader reader = new(jsonBytes[offset..], true, readerState);
        if (!started)
        {
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray || !reader.Read())
            {
                throw new JsonException("The Nazeka dictionary JSON root must be an array with a header.");
            }

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                if (reader.Read())
                {
                    throw new JsonException("Unexpected JSON content after the Nazeka dictionary array.");
                }

                offset += (int)reader.BytesConsumed;
                readerState = reader.CurrentState;
                completed = true;
                return 0;
            }

            reader.Skip();
            started = true;
        }

        int entryCount = 0;
        completed = false;
        while (entryCount < entries.Length)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of Nazeka dictionary JSON.");
            }

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                completed = true;
                if (reader.Read())
                {
                    throw new JsonException("Unexpected JSON content after the Nazeka dictionary array.");
                }

                break;
            }

            string? reading = null;
            List<string>? spellings = null;
            List<string>? definitions = null;
            string? imagePath = null;

            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    reader.Skip();
                    continue;
                }

                if (reader.ValueTextEquals("r"u8))
                {
                    _ = reader.Read();
                    reading = reader.GetString();
                }
                else if (reader.ValueTextEquals("s"u8))
                {
                    _ = reader.Read();
                    spellings = ReadStringArray(ref reader);
                }
                else if (reader.ValueTextEquals("l"u8))
                {
                    _ = reader.Read();
                    definitions = ReadStringArray(ref reader);
                }
                else if (reader.ValueTextEquals("i"u8))
                {
                    _ = reader.Read();
                    imagePath = reader.GetString();
                }
                else
                {
                    _ = reader.Read();
                    reader.Skip();
                }
            }

            Debug.Assert(reading is not null);
            Debug.Assert(definitions is not null);
            entries[entryCount] = new EpwingNazekaImportEntry(reading,
                spellings is { Count: > 0 } ? spellings : null,
                definitions,
                imagePath);

            ++entryCount;
        }

        offset += (int)reader.BytesConsumed;
        readerState = reader.CurrentState;
        return entryCount;
    }

    private static async Task CreatePreparedRecords(ChannelReader<EpwingNazekaImportEntryBatch> inputReader, ChannelWriter<EpwingNazekaImportEntryBatch> inputWriter, ChannelWriter<EpwingNazekaPreparedRecordBatch> outputWriter, bool nonKanjiDict, bool nonNameDict, ConcurrentDictionary<string, byte[]> imageInfoCache)
    {
        HashSet<string> alternativeSpellingsInHiragana = [];
        EpwingNazekaPreparedRecord[]? records = null;
        string[]? searchKeyBuffer = null;
        int searchKeyCount = 0;
        int recordCount = 0;

        try
        {
            await foreach (EpwingNazekaImportEntryBatch batch in inputReader
                               .ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        if (batch.Entries[i].Definitions.Count is 0)
                        {
                            continue;
                        }

                        int maximumEntryRecordCount = batch.Entries[i].Spellings?.Count ?? 1;
                        if (records is not null && maximumEntryRecordCount > records.Length - recordCount)
                        {
                            if (recordCount > 0)
                            {
                                Debug.Assert(searchKeyBuffer is not null);
                                await outputWriter.WriteAsync(new EpwingNazekaPreparedRecordBatch(
                                    records, recordCount, searchKeyBuffer, searchKeyCount)).ConfigureAwait(false);
                            }
                            else
                            {
                                ReturnPreparedRecords(records, recordCount, searchKeyBuffer, searchKeyCount);
                            }

                            records = null;
                            searchKeyBuffer = null;
                            recordCount = 0;
                            searchKeyCount = 0;
                        }

                        records ??= ArrayPool<EpwingNazekaPreparedRecord>.Shared.Rent(Math.Max(64, maximumEntryRecordCount));

                        ref readonly EpwingNazekaImportEntry entry = ref batch.Entries[i];
                        PrepareEntry(in entry, records, ref recordCount, ref searchKeyBuffer, ref searchKeyCount, alternativeSpellingsInHiragana, nonKanjiDict, nonNameDict, imageInfoCache);

                        if (recordCount == records.Length)
                        {
                            Debug.Assert(searchKeyBuffer is not null);
                            await outputWriter.WriteAsync(new EpwingNazekaPreparedRecordBatch(records, recordCount, searchKeyBuffer, searchKeyCount)).ConfigureAwait(false);
                            records = null;
                            searchKeyBuffer = null;
                            recordCount = 0;
                            searchKeyCount = 0;
                        }
                    }
                }
                finally
                {
                    batch.Entries.AsSpan(0, batch.Count).Clear();
                    ArrayPool<EpwingNazekaImportEntry>.Shared.Return(batch.Entries);
                }
            }

            if (recordCount > 0)
            {
                Debug.Assert(records is not null);
                Debug.Assert(searchKeyBuffer is not null);
                await outputWriter.WriteAsync(new EpwingNazekaPreparedRecordBatch(records, recordCount, searchKeyBuffer, searchKeyCount)).ConfigureAwait(false);
                records = null;
                searchKeyBuffer = null;
                recordCount = 0;
                searchKeyCount = 0;
            }
        }
        catch (Exception exception)
        {
            _ = inputWriter.TryComplete(exception);
            _ = outputWriter.TryComplete(exception);
            throw;
        }
        finally
        {
            if (records is not null)
            {
                ReturnPreparedRecords(records, recordCount, searchKeyBuffer, searchKeyCount);
            }
            else if (searchKeyBuffer is not null)
            {
                searchKeyBuffer.AsSpan(0, searchKeyCount).Clear();
                ArrayPool<string>.Shared.Return(searchKeyBuffer);
            }
        }
    }

    private static void PrepareEntry(in EpwingNazekaImportEntry entry, EpwingNazekaPreparedRecord[] records, ref int recordCount, ref string[]? searchKeyBuffer, ref int searchKeyCount, HashSet<string> alternativeSpellingsInHiragana, bool nonKanjiDict, bool nonNameDict, ConcurrentDictionary<string, byte[]> imageInfoCache)
    {
        if (entry.Spellings is not { Count: > 0 } spellingList)
        {
            string reading = entry.Reading;
            if (!reading.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
            {
                byte[] definitionBytes = MessagePackSerializer.Serialize(entry.Definitions);
                byte[]? imageInfoBytes = GetSerializedImageInfo(entry.ImagePath, imageInfoCache);
                string searchKey = nonKanjiDict ? JapaneseUtils.NormalizeText(reading) : reading;
                records[recordCount] = CreatePreparedRecord(reading, null, null, definitionBytes, imageInfoBytes, true, searchKey, null, ref searchKeyBuffer, ref searchKeyCount);
                ++recordCount;
            }

            return;
        }

        string primarySpelling = spellingList[0];
        if (primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
        {
            return;
        }

        byte[] serializedDefinitions = MessagePackSerializer.Serialize(entry.Definitions);
        byte[]? serializedImageInfo = GetSerializedImageInfo(entry.ImagePath, imageInfoCache);

        string readingText = entry.Reading;
        string readingInHiragana = nonKanjiDict && nonNameDict
            ? JapaneseUtils.NormalizeText(readingText)
            : "";

        string primarySpellingInHiragana = nonKanjiDict
            ? JapaneseUtils.NormalizeText(primarySpelling)
            : primarySpelling;

        string[]? alternativeSpellings = spellingList.RemoveAtToArray(0);
        byte[]? alternativeSpellingBytes = alternativeSpellings is not null
            ? MessagePackSerializer.Serialize(alternativeSpellings)
            : null;

        records[recordCount] = CreatePreparedRecord(primarySpelling,
            readingText,
            alternativeSpellingBytes,
            serializedDefinitions,
            serializedImageInfo,
            true,
            primarySpellingInHiragana,
            nonKanjiDict && nonNameDict && primarySpellingInHiragana != readingInHiragana
                ? readingInHiragana
                : null,
            ref searchKeyBuffer,
            ref searchKeyCount);

        ++recordCount;

        ReadOnlySpan<string> spellingListSpan = spellingList.AsReadOnlySpan();
        for (int j = 1; j < spellingListSpan.Length; j++)
        {
            ref readonly string alternativeSpelling = ref spellingListSpan[j];
            if (alternativeSpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
            {
                continue;
            }

            string alternativeSpellingInHiragana = nonKanjiDict
                ? JapaneseUtils.NormalizeText(alternativeSpelling)
                : alternativeSpelling;

            if (nonKanjiDict && nonNameDict && alternativeSpellingInHiragana == readingInHiragana)
            {
                continue;
            }

            if (primarySpellingInHiragana == alternativeSpellingInHiragana || !alternativeSpellingsInHiragana.Add(alternativeSpellingInHiragana))
            {
                continue;
            }

            string[]? altSpellings = spellingList.RemoveAtToArray(j);
            byte[]? altSpellingBytes = altSpellings is not null ? MessagePackSerializer.Serialize(altSpellings) : null;
            records[recordCount] = CreatePreparedRecord(alternativeSpelling, readingText, altSpellingBytes, serializedDefinitions, serializedImageInfo, false, alternativeSpellingInHiragana, null, ref searchKeyBuffer, ref searchKeyCount);
            ++recordCount;
        }

        alternativeSpellingsInHiragana.Clear();
    }

    private static EpwingNazekaPreparedRecord CreatePreparedRecord(string primarySpelling, string? reading, byte[]? alternativeSpellings, byte[] definitions, byte[]? imageInfo, bool isFirstInEntry, string searchKey, string? additionalSearchKey, ref string[]? searchKeyBuffer, ref int searchKeyCount)
    {
        int searchKeyOffset = searchKeyCount;
        int recordSearchKeyCount = additionalSearchKey is null ? 1 : 2;
        int requiredSearchKeyCount = searchKeyCount + recordSearchKeyCount;
        if (searchKeyBuffer is null || requiredSearchKeyCount > searchKeyBuffer.Length)
        {
            int minimumLength = searchKeyBuffer is null ? 64 : Math.Max(searchKeyBuffer.Length * 2, requiredSearchKeyCount);
            string[] largerSearchKeyBuffer = ArrayPool<string>.Shared.Rent(minimumLength);
            if (searchKeyBuffer is not null)
            {
                searchKeyBuffer.AsSpan(0, searchKeyCount).CopyTo(largerSearchKeyBuffer);
                searchKeyBuffer.AsSpan(0, searchKeyCount).Clear();
                ArrayPool<string>.Shared.Return(searchKeyBuffer);
            }

            searchKeyBuffer = largerSearchKeyBuffer;
        }

        searchKeyBuffer[searchKeyCount] = searchKey;
        ++searchKeyCount;
        if (additionalSearchKey is not null)
        {
            searchKeyBuffer[searchKeyCount] = additionalSearchKey;
            ++searchKeyCount;
        }

        return new EpwingNazekaPreparedRecord(primarySpelling, reading, alternativeSpellings, definitions, imageInfo, isFirstInEntry, searchKeyOffset, recordSearchKeyCount);
    }

    private static byte[]? GetSerializedImageInfo(string? imagePath, ConcurrentDictionary<string, byte[]> imageInfoCache)
    {
        if (imagePath is null)
        {
            return null;
        }

        if (imageInfoCache.TryGetValue(imagePath, out byte[]? imageInfoBytes))
        {
            return imageInfoBytes.Length is 0 ? null : imageInfoBytes;
        }

        ImageInfo? imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
        imageInfoBytes = imageInfo is not null ? MessagePackSerializer.Serialize(imageInfo) : [];
        if (!imageInfoCache.TryAdd(imagePath, imageInfoBytes) && imageInfoCache.TryGetValue(imagePath, out byte[]? cachedImageInfoBytes))
        {
            imageInfoBytes = cachedImageInfoBytes;
        }

        return imageInfoBytes.Length is 0
            ? null
            : imageInfoBytes;
    }

    private static JsonReaderState CreateJsonReaderState()
    {
        JsonSerializerOptions serializerOptions = JsonOptions.DefaultJso;

        return new JsonReaderState(new JsonReaderOptions
        {
            AllowTrailingCommas = serializerOptions.AllowTrailingCommas,

            CommentHandling = serializerOptions.ReadCommentHandling is JsonCommentHandling.Allow
                ? JsonCommentHandling.Skip
                : serializerOptions.ReadCommentHandling,

            MaxDepth = serializerOptions.MaxDepth
        });
    }

    private static async Task CompleteOutputChannel(Task producer, Task[] workers,
        ChannelWriter<EpwingNazekaPreparedRecordBatch> outputWriter)
    {
        Exception? completionException = null;
        try
        {
            await producer.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completionException = exception;
        }

        try
        {
            await Task.WhenAll(workers).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completionException ??= exception;
        }

        _ = outputWriter.TryComplete(completionException);
    }

    private static void ReturnPreparedRecords(EpwingNazekaPreparedRecord[] records, int recordCount, string[]? searchKeys, int searchKeyCount)
    {
        if (searchKeys is not null)
        {
            searchKeys.AsSpan(0, searchKeyCount).Clear();
            ArrayPool<string>.Shared.Return(searchKeys);
        }

        records.AsSpan(0, recordCount).Clear();
        ArrayPool<EpwingNazekaPreparedRecord>.Shared.Return(records);
    }

    private static void ReturnPreparedRecordBatch(EpwingNazekaPreparedRecordBatch batch)
    {
        ReturnPreparedRecords(batch.Records, batch.RecordCount, batch.SearchKeys, batch.SearchKeyCount);
    }

    private static List<string> ReadStringArray(ref Utf8JsonReader reader)
    {
        List<string> values = [];
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
        {
            string? value = reader.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static void InsertVariantSearchKeys(SqliteConnection connection, EpwingNazekaSearchKeyInserter searchKeyInserter, List<long> entryRowIds, bool nonKanjiDict, bool nonNameDict, bool generateMazegaki, bool generateFusejiVariants, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration)
    {
        const string query = $"SELECT {RowId}, {PrimarySpelling}, {Reading} FROM {Record} ORDER BY {RowId};";
        using SqliteRecordReader reader = new(connection, query);
        EpwingNazekaVariantSource[] sources = new EpwingNazekaVariantSource[VariantSearchKeyRecordBatchSize];
        int sourceCount = 0;
        int entryIndex = 0;
        long lastEntryRowId = 0;
        int transactionRecordCount = 0;

        SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            while (reader.Read())
            {
                long rowId = reader.GetInt64(0);
                while (entryIndex + 1 < entryRowIds.Count && rowId >= entryRowIds[entryIndex + 1])
                {
                    ++entryIndex;
                }

                long entryRowId = entryRowIds[entryIndex];
                if (sourceCount == sources.Length && entryRowId != lastEntryRowId)
                {
                    InsertVariantSearchKeyBatch(sources, sourceCount, searchKeyInserter, nonKanjiDict, nonNameDict, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, ref transactionRecordCount, ref transaction, connection);
                    sourceCount = 0;
                }
                else if (sourceCount == sources.Length)
                {
                    Array.Resize(ref sources, sources.Length * 2);
                }

                string? reading = null;
                if (!reader.IsNull(2))
                {
                    reading = nonNameDict ? reader.GetString(2) : "";
                }

                sources[sourceCount] = new EpwingNazekaVariantSource(rowId, entryRowId, reader.GetString(1), reading);
                ++sourceCount;
                lastEntryRowId = entryRowId;
            }

            if (sourceCount > 0)
            {
                InsertVariantSearchKeyBatch(sources, sourceCount, searchKeyInserter, nonKanjiDict, nonNameDict, generateMazegaki, generateFusejiVariants, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration, ref transactionRecordCount, ref transaction, connection);
            }

            if (transactionRecordCount > 0)
            {
                transaction.Commit();
            }
        }
        finally
        {
            transaction.Dispose();
        }
    }

    private static void InsertVariantSearchKeyBatch(EpwingNazekaVariantSource[] sources, int sourceCount, EpwingNazekaSearchKeyInserter searchKeyInserter, bool nonKanjiDict, bool nonNameDict, bool generateMazegaki, bool generateFusejiVariants, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration, ref int transactionRecordCount, ref SqliteTransaction transaction, SqliteConnection connection)
    {
        List<int> entryOffsets = [0];
        for (int i = 1; i < sourceCount; i++)
        {
            if (sources[i].EntryRowId != sources[i - 1].EntryRowId)
            {
                entryOffsets.Add(i);
            }
        }

        entryOffsets.Add(sourceCount);
        string[]?[] variantSearchKeys = new string[]?[sourceCount];
        string[] normalizedSpellings = new string[sourceCount];
        int workerCount = Math.Min(s_workerCount, entryOffsets.Count - 1);
        _ = Parallel.For(0, workerCount, workerIndex =>
        {
            HashSet<string> keys = new(StringComparer.Ordinal);
            List<string> variants = [];
            for (int entryIndex = workerIndex; entryIndex < entryOffsets.Count - 1; entryIndex += workerCount)
            {
                int start = entryOffsets[entryIndex];
                int end = entryOffsets[entryIndex + 1];
                string? readingText = sources[start].Reading;
                if (readingText is null)
                {
                    continue;
                }

                string? reading = nonKanjiDict && nonNameDict
                    ? JapaneseUtils.NormalizeText(readingText)
                    : null;
                if (reading is not null)
                {
                    _ = keys.Add(reading);
                }

                for (int i = start; i < end; i++)
                {
                    ref readonly EpwingNazekaVariantSource source = ref sources[i];

                    string searchKey = nonKanjiDict
                        ? JapaneseUtils.NormalizeText(source.PrimarySpelling)
                        : source.PrimarySpelling;

                    normalizedSpellings[i] = searchKey;
                    _ = keys.Add(searchKey);
                }

                for (int i = start; i < end; i++)
                {
                    ref readonly EpwingNazekaVariantSource source = ref sources[i];
                    string spelling = normalizedSpellings[i];

                    if (generateFusejiVariants)
                    {
                        foreach (string variant in FusejiUtils.CreateFusejiVariants(spelling, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                        {
                            if (keys.Add(variant))
                            {
                                variants.Add(variant);
                            }
                        }
                    }

                    if (reading is not null && spelling != reading)
                    {
                        if (source.RowId == source.EntryRowId && generateFusejiVariants)
                        {
                            foreach (string variant in FusejiUtils.CreateFusejiVariants(reading, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                if (keys.Add(variant))
                                {
                                    variants.Add(variant);
                                }
                            }
                        }

                        if (generateMazegaki)
                        {
                            foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(spelling, reading))
                            {
                                if (keys.Add(mazegaki))
                                {
                                    variants.Add(mazegaki);
                                    if (generateFusejiVariants)
                                    {
                                        foreach (string variant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            if (keys.Add(variant))
                                            {
                                                variants.Add(variant);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (variants.Count > 0)
                    {
                        variantSearchKeys[i] = variants.ToArray();
                        variants.Clear();
                    }
                }

                keys.Clear();
            }
        });

        for (int i = 0; i < sourceCount; i++)
        {
            if (variantSearchKeys[i] is not { Length: > 0 } searchKeys)
            {
                continue;
            }

            searchKeyInserter.Insert(sources[i].RowId, searchKeys);
            transactionRecordCount += searchKeys.Length;
            if (transactionRecordCount > VariantSearchKeyTransactionBatchSize)
            {
                transaction.Commit();
                transaction.Dispose();
                transaction = connection.BeginTransaction();
                transactionRecordCount = 0;
            }
        }
    }

    private static void RemoveDuplicateRecords(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            DELETE FROM {Record}
            WHERE {RowId} IN
            (
                SELECT r.{RowId}
                FROM {Record} r
                JOIN
                (
                    SELECT MIN({RowId}) AS {RowId}_to_keep, {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo}
                    FROM {Record}
                    GROUP BY {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {ImageInfo}
                    HAVING COUNT(*) > 1
                ) d ON d.{PrimarySpelling} = r.{PrimarySpelling}
                    AND d.{Reading} IS r.{Reading}
                    AND d.{AlternativeSpellings} IS r.{AlternativeSpellings}
                    AND d.{Glossary} = r.{Glossary}
                    AND d.{ImageInfo} IS r.{ImageInfo}
                WHERE r.{RowId} != d.{RowId}_to_keep
            );

            DELETE FROM {RecordSearchKey}
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM {Record}
                WHERE {Record}.{RowId} = {RecordSearchKey}.{RecordId}
            );
            """;

        _ = command.ExecuteNonQuery();
    }

    private static int GetDistinctSearchKeyCount(SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT COUNT(DISTINCT {SearchKey})
            FROM {RecordSearchKey};
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetMaxSearchKeyLength(SqliteConnection connection)
    {
        const string query =
            $"""
            SELECT MAX(LENGTH(CAST({SearchKey} AS BLOB)) / 2)
            FROM {RecordSearchKey};
            """;

        using SqliteRecordReader reader = new(connection, query);
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static void ImportFromMemory(Dict dict)
    {
        Dictionary<EpwingNazekaRecord, List<string>> recordToKeysDict = new(ReferenceEqualityComparer.Instance);
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
            {
                EpwingNazekaRecord record = (EpwingNazekaRecord)records[i];
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

        using SqliteConnection? connection = DBUtils.CreateReadWriteDBConnection(dict.DBPath);
        Debug.Assert(connection is not null);

        DBUtils.ConfigureForBulkWrite(connection);
        using EpwingNazekaRecordInserter recordInserter = new(connection);
        using EpwingNazekaSearchKeyInserter searchKeyInserter = new(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        long rowId = 1;
        foreach ((EpwingNazekaRecord record, List<string> keys) in recordToKeysDict)
        {
            byte[]? alternativeSpellings = record.AlternativeSpellings is not null
                ? MessagePackSerializer.Serialize(record.AlternativeSpellings)
                : null;

            byte[] definitions = MessagePackSerializer.Serialize(record.Definitions);

            byte[]? imageInfo = record.ImageInfo is not null
                ? MessagePackSerializer.Serialize(record.ImageInfo)
                : null;

            recordInserter.Insert(rowId, record.PrimarySpelling, record.Reading, alternativeSpellings, definitions, imageInfo);
            searchKeyInserter.Insert(rowId, keys.AsReadOnlySpan());

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
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        int validTermCount = terms.Length > maxSearchKeyLengthForDict && maxSearchKeyLengthForDict > 0
            ? maxSearchKeyLengthForDict
            : terms.Length;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        using SqliteRecordReader reader = new(connection, GetQuery(validTermCount));
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int offset = terms.Length - validTermCount;
        for (int i = 0; i < validTermCount; i++)
        {
            reader.Bind(i + 1, terms[offset + i]);
        }

        if (!reader.Read())
        {
            return null;
        }

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        do
        {
            EpwingNazekaRecord epwingNazekaRecord = GetRecord(reader);
            string searchKey = reader.GetString((int)ColumnIndex.SearchKey);
            ref IList<IDictRecord>? result = ref CollectionsMarshal.GetValueRefOrAddDefault(results, searchKey, out bool exists);
            if (exists)
            {
                Debug.Assert(result is not null);
                result.Add(epwingNazekaRecord);
            }
            else
            {
                result = [epwingNazekaRecord];
            }
        }
        while (reader.Read());

        return results;
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string readOnlyConnectionString, string kanjiWithVariationSelector, string kanji)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", readOnlyConnectionString);
            return null;
        }

        using SqliteRecordReader reader = new(connection, GetQuery(2));
        reader.Bind(1, kanjiWithVariationSelector);
        reader.Bind(2, kanji);

        if (!reader.Read())
        {
            return null;
        }

        Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
        do
        {
            EpwingNazekaRecord record = GetRecord(reader);
            string searchKey = reader.GetString((int)ColumnIndex.SearchKey);
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
        if (!reader.Read())
        {
            return null;
        }

        List<IDictRecord> results = [];
        do
        {
            results.Add(GetRecord(reader));
        }
        while (reader.Read());

        return results;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(dict.ReadOnlyConnectionString);
        Debug.Assert(connection is not null);

        const string query =
            $"""
            SELECT r.{RowId}, r.{PrimarySpelling}, r.{Reading}, r.{AlternativeSpellings}, r.{Glossary}, r.{ImageInfo}, json_group_array(rsk.{SearchKey})
            FROM {Record} r
            JOIN {RecordSearchKey} rsk ON r.{RowId} = rsk.{RecordId}
            GROUP BY r.{RowId};
            """;

        using SqliteRecordReader reader = new(connection, query);
        while (reader.Read())
        {
            EpwingNazekaRecord record = GetRecord(reader);
            string[]? searchKeys = JsonSerializer.Deserialize<string[]>(reader.GetString((int)ColumnIndex.SearchKey), JsonOptions.DefaultJso);
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

    private static EpwingNazekaRecord GetRecord(SqliteRecordReader reader)
    {
        long rowId = reader.GetInt64((int)ColumnIndex.RowId);
        string primarySpelling = reader.GetString((int)ColumnIndex.PrimarySpelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !reader.IsNull(readingIndex)
            ? reader.GetString(readingIndex)
            : null;

        string[]? alternativeSpellings = reader.DeserializeNullable<string[]>((int)ColumnIndex.AlternativeSpellings, Record, AlternativeSpellings, rowId);
        string[] definitions = reader.Deserialize<string[]>(Record, Glossary, rowId);
        ImageInfo? imageInfo = reader.DeserializeNullable<ImageInfo>((int)ColumnIndex.ImageInfo, Record, ImageInfo, rowId);

        return new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions, imageInfo);
    }
}
