using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyYomichan;

internal static class FrequencyYomichanReader
{
    private const int InitialRecordCapacity = 10000;
    private const int RecordBatchSize = 16384;
    private const int PipelineParsingThreshold = 4 * 1024 * 1024;
    private const int WholeFileParsingThreshold = 32 * 1024 * 1024;
    private const int PooledFileThreshold = 4 * 1024 * 1024;
    private const int StreamingBufferSize = 1024 * 1024;

    internal static async IAsyncEnumerable<FrequencyYomichanRecordBatch> ReadRecordBatches(string jsonFile)
    {
        FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
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
                    if (jsonLength >= PipelineParsingThreshold)
                    {
                        await foreach (FrequencyYomichanRecordBatch batch in ReadRecordBatches(fileStream, jsonBytes, jsonLength).ConfigureAwait(false))
                        {
                            yield return batch;
                        }
                    }
                    else
                    {
                        FrequencyYomichanRecord[] records = ReadWholeFile(jsonBytes.AsSpan(0, jsonLength), out int recordCount);
                        yield return new FrequencyYomichanRecordBatch(records, recordCount);
                    }
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
                await foreach (FrequencyYomichanRecordBatch batch in ReadRecordBatches(fileStream, null, 0).ConfigureAwait(false))
                {
                    yield return batch;
                }
            }
        }
    }

    private static async IAsyncEnumerable<FrequencyYomichanRecordBatch> ReadRecordBatches(FileStream fileStream, byte[]? jsonBytes, int jsonLength)
    {
        Channel<FrequencyYomichanRecordBatch> batches = Channel.CreateBounded<FrequencyYomichanRecordBatch>(new BoundedChannelOptions(4)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        using CancellationTokenSource stopProducer = new();
        CancellationToken stopProducerToken = stopProducer.Token;
        Task producer = jsonBytes is not null
            ? Task.Run(() => CreateRecordBatches(jsonBytes, jsonLength, batches.Writer, stopProducerToken), CancellationToken.None)
            : Task.Run(() => CreateStreamedRecordBatches(fileStream, batches.Writer, stopProducerToken), CancellationToken.None);
        try
        {
            await foreach (FrequencyYomichanRecordBatch batch in batches.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    yield return batch;
                }
                finally
                {
                    ReturnRecordBatch(batch.Records, batch.Count);
                }
            }
        }
        finally
        {
            if (!producer.IsCompleted)
            {
                await stopProducer.CancelAsync().ConfigureAwait(false);
            }

            try
            {
                await producer.ConfigureAwait(false);
            }
            finally
            {
                while (batches.Reader.TryRead(out FrequencyYomichanRecordBatch batch))
                {
                    ReturnRecordBatch(batch.Records, batch.Count);
                }
            }
        }
    }

    private static async Task CreateStreamedRecordBatches(FileStream fileStream, ChannelWriter<FrequencyYomichanRecordBatch> writer, CancellationToken stopProducerToken)
    {
        byte[] jsonBytes = ArrayPool<byte>.Shared.Rent(StreamingBufferSize);
        FrequencyYomichanRecord[]? records = ArrayPool<FrequencyYomichanRecord>.Shared.Rent(RecordBatchSize);
        int offset = 0;
        int recordCount = 0;
        bool started = false;
        bool completed = false;
        bool finalBlock = false;
        JsonReaderState readerState = default;
        try
        {
            int bufferedBytes = await fileStream.ReadAtLeastAsync(jsonBytes.AsMemory(), 3, throwOnEndOfStream: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
            if (jsonBytes.AsSpan(0, bufferedBytes).StartsWith(utf8Preamble))
            {
                jsonBytes.AsSpan(utf8Preamble.Length, bufferedBytes - utf8Preamble.Length).CopyTo(jsonBytes);
                bufferedBytes -= utf8Preamble.Length;
            }

            while (!finalBlock || bufferedBytes > 0 || !completed)
            {
                if (stopProducerToken.IsCancellationRequested)
                {
                    break;
                }

                Debug.Assert(records is not null);
                ReadStreamedRecordBatch(jsonBytes.AsSpan(offset, bufferedBytes), finalBlock, records, ref recordCount,
                    ref readerState, ref started, ref completed, out int consumedBytes);
                offset += consumedBytes;
                bufferedBytes -= consumedBytes;
                bool recordBatchFull = recordCount is RecordBatchSize;
                if (recordBatchFull || (completed && recordCount > 0))
                {
                    if (!writer.TryWrite(new FrequencyYomichanRecordBatch(records, recordCount)))
                    {
                        await writer.WriteAsync(new FrequencyYomichanRecordBatch(records, recordCount), stopProducerToken).ConfigureAwait(false);
                    }

                    records = null;
                    recordCount = 0;
                    records = ArrayPool<FrequencyYomichanRecord>.Shared.Rent(RecordBatchSize);
                }

                if (completed && finalBlock)
                {
                    break;
                }

                if (finalBlock && consumedBytes is 0)
                {
                    throw new JsonException("The frequency bank root is incomplete.");
                }

                if (recordBatchFull && bufferedBytes > 0)
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

                int bytesRead = await fileStream.ReadAsync(jsonBytes.AsMemory(bufferedBytes), CancellationToken.None).ConfigureAwait(false);
                bufferedBytes += bytesRead;
                finalBlock = bytesRead is 0;
                if (completed && bufferedBytes is 0 && finalBlock)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            LoggerManager.Logger.Debug("The Yomichan frequency record batch producer was canceled");
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(jsonBytes);
            if (records is not null)
            {
                ReturnRecordBatch(records, recordCount);
            }

            _ = writer.TryComplete();
        }
    }

    private static void ReadStreamedRecordBatch(ReadOnlySpan<byte> json, bool finalBlock, FrequencyYomichanRecord[] records, ref int recordCount,
        ref JsonReaderState readerState, ref bool started, ref bool completed, out int consumedBytes)
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
                    throw new JsonException("The frequency bank root must be an array.");
                }

                started = true;
            }
            else if (completed)
            {
                throw new JsonException("The frequency bank root must contain only an array.");
            }
            else if (reader.TokenType is JsonTokenType.EndArray)
            {
                completed = true;
            }
            else
            {
                if (reader.TokenType is not JsonTokenType.StartArray)
                {
                    throw new JsonException("A frequency bank entry must be an array.");
                }

                if (!reader.Read())
                {
                    reader = entryReader;
                    break;
                }

                Utf8JsonReader spellingReader = reader;
                if (!reader.Read() || !reader.Read())
                {
                    reader = entryReader;
                    break;
                }

                JsonTokenType frequencyTokenType = reader.TokenType;
                int frequencyDepth = reader.CurrentDepth;
                int frequency = ReadFrequency(ref reader, out string? reading);
                if (frequencyTokenType is JsonTokenType.StartObject
                    && (reader.TokenType is not JsonTokenType.EndObject || reader.CurrentDepth != frequencyDepth))
                {
                    reader = entryReader;
                    break;
                }

                bool completeRecord = false;
                while (reader.Read())
                {
                    if (reader.TokenType is JsonTokenType.EndArray)
                    {
                        completeRecord = true;
                        break;
                    }

                    if (!reader.TrySkip())
                    {
                        break;
                    }
                }

                if (!completeRecord)
                {
                    reader = entryReader;
                    break;
                }

                if (frequency > 0)
                {
                    string? spelling = spellingReader.GetString();
                    Debug.Assert(spelling is not null);
                    records[recordCount] = new FrequencyYomichanRecord(spelling, reading, frequency);
                    ++recordCount;
                }
            }
        }

        consumedBytes = (int)reader.BytesConsumed;
        readerState = reader.CurrentState;
    }

    private static void CreateRecordBatches(byte[] jsonBytes, int jsonLength, ChannelWriter<FrequencyYomichanRecordBatch> writer, CancellationToken stopProducerToken)
    {
        try
        {
            _ = ReadWholeFile(jsonBytes.AsSpan(0, jsonLength), out _, writer, stopProducerToken);
            _ = writer.TryComplete();
        }
        catch (OperationCanceledException)
        {
            LoggerManager.Logger.Debug("The Yomichan frequency record batch producer was canceled");
            _ = writer.TryComplete();
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
    }

    private static FrequencyYomichanRecord[] ReadWholeFile(ReadOnlySpan<byte> json, out int recordCount, ChannelWriter<FrequencyYomichanRecordBatch>? writer = null, CancellationToken stopProducerToken = default)
    {
        ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
        if (json.StartsWith(utf8Preamble))
        {
            json = json[utf8Preamble.Length..];
        }

        Utf8JsonReader reader = new(json);
        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException("The frequency bank root must be an array.");
        }

        FrequencyYomichanRecord[] records = writer is null
            ? new FrequencyYomichanRecord[InitialRecordCapacity]
            : ArrayPool<FrequencyYomichanRecord>.Shared.Rent(RecordBatchSize);
        recordCount = 0;
        bool ownsRecordBatch = writer is not null;
        try
        {
            while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
            {
                Debug.Assert(reader.TokenType is JsonTokenType.StartArray);
                _ = reader.Read();
                Utf8JsonReader spellingReader = reader;
                _ = reader.Read();
                _ = reader.Read();
                int frequency = ReadFrequency(ref reader, out string? reading);
                if (frequency > 0)
                {
                    string? spelling = spellingReader.GetString();
                    Debug.Assert(spelling is not null);
                    if (recordCount == records.Length)
                    {
                        if (writer is null)
                        {
                            Array.Resize(ref records, records.Length * 2);
                        }
                        else
                        {
                            if (!writer.TryWrite(new FrequencyYomichanRecordBatch(records, recordCount)))
                            {
                                writer.WriteAsync(new FrequencyYomichanRecordBatch(records, recordCount), stopProducerToken).AsTask().GetAwaiter().GetResult();
                            }

                            ownsRecordBatch = false;
                            records = ArrayPool<FrequencyYomichanRecord>.Shared.Rent(RecordBatchSize);
                            ownsRecordBatch = true;
                            recordCount = 0;
                        }
                    }

                    records[recordCount] = new FrequencyYomichanRecord(spelling, reading, frequency);
                    ++recordCount;
                }

                _ = reader.Read();
                while (reader.TokenType is not JsonTokenType.EndArray)
                {
                    reader.Skip();
                    if (!reader.Read())
                    {
                        throw new JsonException("A frequency bank entry is incomplete.");
                    }
                }
            }

            if (reader.TokenType is not JsonTokenType.EndArray || reader.Read())
            {
                throw new JsonException("The frequency bank root must contain only an array.");
            }

            if (writer is not null && recordCount > 0)
            {
                if (!writer.TryWrite(new FrequencyYomichanRecordBatch(records, recordCount)))
                {
                    writer.WriteAsync(new FrequencyYomichanRecordBatch(records, recordCount), stopProducerToken).AsTask().GetAwaiter().GetResult();
                }

                ownsRecordBatch = false;
            }
            else if (writer is not null)
            {
                ReturnRecordBatch(records, recordCount);
                ownsRecordBatch = false;
            }

            return records;
        }
        finally
        {
            if (ownsRecordBatch)
            {
                ReturnRecordBatch(records, recordCount);
            }
        }
    }

    private static void ReturnRecordBatch(FrequencyYomichanRecord[] records, int count)
    {
        records.AsSpan(0, count).Clear();
        ArrayPool<FrequencyYomichanRecord>.Shared.Return(records);
    }

    private static int ReadFrequency(ref Utf8JsonReader reader, out string? reading)
    {
        reading = null;
        if (reader.TokenType is JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType is JsonTokenType.String)
        {
            return TextUtils.ExtractFirstInt(reader.GetString());
        }

        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
        int value = -1;
        int nestedValue = -1;
        Utf8JsonReader displayValueReader = default;
        Utf8JsonReader readingReader = default;
        string? nestedDisplayValue = null;
        bool hasValue = false;
        bool hasReading = false;
        bool hasDisplayValue = false;
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
        {
            bool valueProperty = reader.ValueTextEquals("value"u8);
            bool readingProperty = !valueProperty && reader.ValueTextEquals("reading"u8);
            bool frequencyProperty = !valueProperty && !readingProperty && reader.ValueTextEquals("frequency"u8);
            bool displayValueProperty = !valueProperty && !readingProperty && !frequencyProperty && reader.ValueTextEquals("displayValue"u8);
            if (!reader.Read())
            {
                return -1;
            }
            if (valueProperty)
            {
                value = reader.GetInt32();
                hasValue = true;
            }
            else if (readingProperty)
            {
                readingReader = reader;
                hasReading = true;
            }
            else if (frequencyProperty)
            {
                JsonTokenType nestedTokenType = reader.TokenType;
                int nestedDepth = reader.CurrentDepth;
                nestedValue = ReadNestedFrequency(ref reader, out nestedDisplayValue);
                if (nestedTokenType is JsonTokenType.StartObject
                    && (reader.TokenType is not JsonTokenType.EndObject || reader.CurrentDepth != nestedDepth))
                {
                    return -1;
                }
            }
            else if (displayValueProperty)
            {
                displayValueReader = reader;
                hasDisplayValue = true;
            }
            else
            {
                if (!reader.TrySkip())
                {
                    return -1;
                }
            }
        }

        if (reader.TokenType is not JsonTokenType.EndObject)
        {
            return -1;
        }

        if (hasValue)
        {
            return value <= 0 && hasDisplayValue ? TextUtils.ExtractFirstInt(displayValueReader.GetString()) : value;
        }

        if (hasReading)
        {
            int frequency = nestedValue <= 0 && nestedDisplayValue is not null ? TextUtils.ExtractFirstInt(nestedDisplayValue) : nestedValue;
            if (frequency > 0)
            {
                reading = readingReader.GetString();
            }

            return frequency;
        }

        return -1;
    }

    private static int ReadNestedFrequency(ref Utf8JsonReader reader, out string? displayValue)
    {
        displayValue = null;
        if (reader.TokenType is JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        if (reader.TokenType is JsonTokenType.String)
        {
            return TextUtils.ExtractFirstInt(reader.GetString());
        }

        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
        Utf8JsonReader displayValueReader = reader;
        int value = -1;
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
        {
            bool valueProperty = reader.ValueTextEquals("value"u8);
            if (!reader.Read())
            {
                return -1;
            }
            if (valueProperty)
            {
                value = reader.GetInt32();
            }
            else
            {
                if (!reader.TrySkip())
                {
                    return -1;
                }
            }
        }

        if (reader.TokenType is not JsonTokenType.EndObject)
        {
            return -1;
        }

        if (value <= 0)
        {
            while (displayValueReader.Read() && displayValueReader.TokenType is not JsonTokenType.EndObject)
            {
                bool displayValueProperty = displayValueReader.ValueTextEquals("displayValue"u8);
                _ = displayValueReader.Read();
                if (displayValueProperty)
                {
                    displayValue = displayValueReader.GetString();
                }
                else
                {
                    if (!displayValueReader.TrySkip())
                    {
                        return -1;
                    }
                }
            }
        }

        return value;
    }
}
