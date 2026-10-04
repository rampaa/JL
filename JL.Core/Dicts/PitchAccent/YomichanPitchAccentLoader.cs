using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentLoader
{
    public const int Size = 434991;
    private const int InitialRecordCapacity = 10000;
    private const int RecordBatchSize = 16384;
    private const int WholeFileParsingThreshold = 32 * 1024 * 1024;
    private const int PooledFileThreshold = 4 * 1024 * 1024;
    private const int StreamingBufferSize = 64 * 1024;

    internal static async IAsyncEnumerable<PitchAccentRecordBatch> ReadRecordBatches(FileStream fileStream)
    {
        long fileLength = fileStream.Length;
        if (fileLength <= WholeFileParsingThreshold)
        {
            int jsonLength = (int)fileLength;
            bool pooled = jsonLength <= PooledFileThreshold;
            byte[] jsonBytes = pooled ? ArrayPool<byte>.Shared.Rent(jsonLength) : GC.AllocateUninitializedArray<byte>(jsonLength);
            try
            {
                await fileStream.ReadExactlyAsync(jsonBytes.AsMemory(0, jsonLength)).ConfigureAwait(false);
                PitchAccentRecord[] records = ReadWholeFile(jsonBytes.AsSpan(0, jsonLength), out int recordCount);
                yield return new PitchAccentRecordBatch(records, recordCount);
            }
            finally
            {
                if (pooled)
                {
                    ArrayPool<byte>.Shared.Return(jsonBytes);
                }
            }
        }
        else
        {
            await foreach (PitchAccentRecordBatch batch in ReadStreamedRecordBatches(fileStream).ConfigureAwait(false))
            {
                yield return batch;
            }
        }
    }

    private static async IAsyncEnumerable<PitchAccentRecordBatch> ReadStreamedRecordBatches(FileStream fileStream)
    {
        byte[] jsonBytes = ArrayPool<byte>.Shared.Rent(StreamingBufferSize);
        PitchAccentRecord[] records = ArrayPool<PitchAccentRecord>.Shared.Rent(RecordBatchSize);
        int recordCount = 0;
        int offset = 0;
        bool started = false;
        bool completed = false;
        bool finalBlock = false;
        JsonReaderState readerState = default;
        try
        {
            int bufferedBytes = await fileStream.ReadAtLeastAsync(jsonBytes.AsMemory(), 3, throwOnEndOfStream: false).ConfigureAwait(false);
            ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
            if (jsonBytes.AsSpan(0, bufferedBytes).StartsWith(utf8Preamble))
            {
                offset = utf8Preamble.Length;
                bufferedBytes -= offset;
            }

            while (!finalBlock || bufferedBytes > 0 || !completed)
            {
                ReadStreamedRecordBatch(jsonBytes.AsSpan(offset, bufferedBytes), finalBlock, records, ref recordCount, ref readerState, ref started, ref completed, out int consumedBytes);
                offset += consumedBytes;
                bufferedBytes -= consumedBytes;
                bool batchFull = recordCount is RecordBatchSize;
                if (batchFull || (completed && recordCount > 0))
                {
                    yield return new PitchAccentRecordBatch(records, recordCount);

                    records.AsSpan(0, recordCount).Clear();
                    recordCount = 0;
                }

                if (completed && finalBlock)
                {
                    break;
                }

                if (finalBlock && consumedBytes is 0)
                {
                    throw new JsonException("The pitch bank root is incomplete.");
                }

                if (batchFull && bufferedBytes > 0)
                {
                    continue;
                }

                jsonBytes.AsSpan(offset, bufferedBytes).CopyTo(jsonBytes);
                offset = 0;
                if (bufferedBytes == jsonBytes.Length)
                {
                    byte[] largerBuffer = ArrayPool<byte>.Shared.Rent(jsonBytes.Length * 2);
                    jsonBytes.AsSpan(0, bufferedBytes).CopyTo(largerBuffer);
                    ArrayPool<byte>.Shared.Return(jsonBytes);
                    jsonBytes = largerBuffer;
                }

                int bytesRead = await fileStream.ReadAsync(jsonBytes.AsMemory(bufferedBytes)).ConfigureAwait(false);
                bufferedBytes += bytesRead;
                finalBlock = bytesRead is 0;
            }
        }
        finally
        {
            records.AsSpan(0, recordCount).Clear();
            ArrayPool<PitchAccentRecord>.Shared.Return(records);
            ArrayPool<byte>.Shared.Return(jsonBytes);
        }
    }

    private static void ReadStreamedRecordBatch(ReadOnlySpan<byte> json, bool finalBlock, PitchAccentRecord[] records, ref int recordCount, ref JsonReaderState readerState, ref bool started, ref bool completed, out int consumedBytes)
    {
        Utf8JsonReader reader = new(json, finalBlock, readerState);
        while (recordCount < RecordBatchSize)
        {
            Utf8JsonReader entryReader = reader;
            if (!reader.Read())
            {
                break;
            }

            if (!started)
            {
                if (reader.TokenType is not JsonTokenType.StartArray)
                {
                    throw new JsonException("The pitch bank root must be an array.");
                }

                started = true;
            }
            else if (completed)
            {
                throw new JsonException("The pitch bank root must contain only an array.");
            }
            else if (reader.TokenType is JsonTokenType.EndArray)
            {
                completed = true;
            }
            else
            {
                if (reader.TokenType is not JsonTokenType.StartArray)
                {
                    throw new JsonException("A pitch bank entry must be an array.");
                }

                // Skip requires a final reader. Keep partial entries buffered and give the converter only a complete entry.
                Utf8JsonReader completeEntryReader = reader;
                if (!completeEntryReader.TrySkip())
                {
                    reader = entryReader;
                    break;
                }

                int entryOffset = (int)reader.TokenStartIndex;
                int entryLength = (int)completeEntryReader.BytesConsumed - entryOffset;
                Utf8JsonReader recordReader = new(json.Slice(entryOffset, entryLength));
                _ = recordReader.Read();
                PitchAccentRecord? record = ReadRecord(ref recordReader);
                reader = completeEntryReader;
                if (record is not null)
                {
                    records[recordCount] = record;
                    ++recordCount;
                }
            }
        }

        consumedBytes = (int)reader.BytesConsumed;
        readerState = reader.CurrentState;
    }

    private static PitchAccentRecord[] ReadWholeFile(ReadOnlySpan<byte> json, out int recordCount)
    {
        ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
        if (json.StartsWith(utf8Preamble))
        {
            json = json[utf8Preamble.Length..];
        }

        Utf8JsonReader reader = new(json);
        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException("The pitch bank root must be an array.");
        }

        PitchAccentRecord[] records = new PitchAccentRecord[InitialRecordCapacity];
        recordCount = 0;
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
        {
            PitchAccentRecord? record = ReadRecord(ref reader);
            if (record is null)
            {
                continue;
            }

            if (recordCount == records.Length)
            {
                Array.Resize(ref records, records.Length * 2);
            }

            records[recordCount] = record;
            ++recordCount;
        }

        ValidateEndOfFile(ref reader);
        return records;
    }

    private static PitchAccentRecord? ReadRecord(ref Utf8JsonReader reader)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartArray);
        _ = reader.Read();
        Utf8JsonReader spellingReader = reader;
        _ = reader.Read();
        _ = reader.Read();

        Utf8JsonReader readingReader = default;
        byte position = byte.MaxValue;
        bool hasReading = false;
        bool hasPitches = false;
        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
        {
            bool readingProperty = reader.ValueTextEquals("reading"u8);
            bool pitchesProperty = !readingProperty && reader.ValueTextEquals("pitches"u8);
            _ = reader.Read();
            if (readingProperty)
            {
                readingReader = reader;
                hasReading = true;
            }
            else if (pitchesProperty)
            {
                hasPitches = true;
                position = ReadPitchPosition(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }

        if (!hasReading || !hasPitches)
        {
            throw new JsonException("A pitch entry is missing its reading or pitches.");
        }

        _ = reader.Read();
        while (reader.TokenType is not JsonTokenType.EndArray)
        {
            reader.Skip();
            if (!reader.Read())
            {
                throw new JsonException("A pitch bank entry is incomplete.");
            }
        }

        if (position is byte.MaxValue)
        {
            return null;
        }

        string? spelling = spellingReader.GetString();
        Debug.Assert(spelling is not null);
        string? reading = readingReader.GetString();
        Debug.Assert(reading is not null);
        if (string.IsNullOrWhiteSpace(spelling))
        {
            if (string.IsNullOrWhiteSpace(reading))
            {
                return null;
            }

            spelling = reading;
            reading = null;
        }
        else
        {
            reading = spelling == reading ? null : reading;
        }

        return new PitchAccentRecord(spelling.GetPooledString(), reading?.GetPooledString(), position);
    }

    private static byte ReadPitchPosition(ref Utf8JsonReader reader)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartArray);
        byte position = byte.MaxValue;
        bool foundPosition = false;
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
        {
            if (foundPosition)
            {
                reader.Skip();
                continue;
            }

            Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
            byte candidatePosition = byte.MaxValue;
            bool validCandidatePosition = false;
            bool hasPosition = false;
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                bool positionProperty = reader.ValueTextEquals("position"u8);
                _ = reader.Read();
                if (positionProperty)
                {
                    hasPosition = true;
                    validCandidatePosition = false;
                    if (reader.TokenType is JsonTokenType.Number)
                    {
                        validCandidatePosition = reader.TryGetByte(out candidatePosition);
                    }
                    else if (reader.TokenType is JsonTokenType.String)
                    {
                        string? positionStr = reader.GetString();
                        Debug.Assert(positionStr is not null);
                        candidatePosition = PitchAccentRecord.GetPositionFromPitchString(positionStr);
                        validCandidatePosition = true;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            if (!hasPosition)
            {
                throw new JsonException("A pitch entry is missing its position.");
            }

            if (validCandidatePosition)
            {
                position = candidatePosition;
                foundPosition = true;
            }
        }

        return position;
    }

    private static void ValidateEndOfFile(ref Utf8JsonReader reader)
    {
        if (reader.TokenType is not JsonTokenType.EndArray || reader.Read())
        {
            throw new JsonException("The pitch bank root must contain only an array.");
        }
    }

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

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

        Debug.Assert(dict.Contents.Count is 0);
        Dictionary<string, PitchAccentRecords> contents = new(dict.Size > 0 ? dict.Size : Size, StringComparer.Ordinal);
        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (PitchAccentRecordBatch batch in ReadRecordBatches(fileStream).ConfigureAwait(false))
                {
                    for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                    {
                        PitchAccentRecord record = batch.Records[recordIndex];
                        string spellingInHiragana = JapaneseUtils.NormalizeText(record.Spelling).GetPooledString();
                        if (AddRecordToDictionary(spellingInHiragana, record, contents, dict))
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(spellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = AddRecordToDictionary(fusejiVariant, record, contents, dict);
                                }
                            }

                            if (record.Reading is not null)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                                if (spellingInHiragana != readingInHiragana)
                                {
                                    if (AddRecordToDictionary(readingInHiragana, record, contents, dict))
                                    {
                                        if (generateFusejiVariants)
                                        {
                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                            {
                                                _ = AddRecordToDictionary(fusejiVariant, record, contents, dict);
                                            }
                                        }

                                        if (generateMazegaki)
                                        {
                                            foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(spellingInHiragana, readingInHiragana))
                                            {
                                                if (AddRecordToDictionary(mazegaki, record, contents, dict))
                                                {
                                                    if (generateFusejiVariants)
                                                    {
                                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                        {
                                                            _ = AddRecordToDictionary(fusejiVariant, record, contents, dict);
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
                }
            }
        }

        dict.Contents = contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static bool AddRecordToDictionary(string normalizedKey, PitchAccentRecord record, Dictionary<string, PitchAccentRecords> contents, Dict dict)
    {
        ref PitchAccentRecords records = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, normalizedKey, out bool exists);
        if (exists)
        {
            return records.AddIfNotExists(record);
        }

        records = new PitchAccentRecords(record);
        if (normalizedKey.Length > dict.MaxSearchKeyLength)
        {
            dict.MaxSearchKeyLength = normalizedKey.Length;
        }

        return true;
    }
}
