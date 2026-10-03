using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyNazeka;

internal static class FrequencyNazekaReader
{
    private const int WholeFileParsingThreshold = 32 * 1024 * 1024;
    private const int StreamingBufferSize = 64 * 1024;
    private const int RecordBatchSize = 4096;

    internal static async IAsyncEnumerable<FrequencyNazekaRecordBatch> ReadRecordBatches(string jsonFile, bool parseInParallel = false)
    {
        FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            IAsyncEnumerable<FrequencyNazekaRecordBatch> batches = parseInParallel
                ? ReadRecordBatchesInParallel(fileStream)
                : ReadRecordBatches(fileStream, reuseRecordBatch: true);
            await foreach (FrequencyNazekaRecordBatch batch in batches.ConfigureAwait(false))
            {
                yield return batch;
            }
        }
    }

    private static async IAsyncEnumerable<FrequencyNazekaRecordBatch> ReadRecordBatchesInParallel(FileStream fileStream)
    {
        Channel<FrequencyNazekaRecordBatch> batches = Channel.CreateBounded<FrequencyNazekaRecordBatch>(new BoundedChannelOptions(4)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        using CancellationTokenSource stopProducer = new();
        CancellationToken stopProducerToken = stopProducer.Token;
        Task producer = Task.Run(() => CreateRecordBatches(fileStream, batches.Writer, stopProducerToken), CancellationToken.None);
        try
        {
            await foreach (FrequencyNazekaRecordBatch batch in batches.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                try
                {
                    yield return batch;
                }
                finally
                {
                    ReturnRecordBatch(batch);
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
                while (batches.Reader.TryRead(out FrequencyNazekaRecordBatch batch))
                {
                    ReturnRecordBatch(batch);
                }
            }
        }
    }

    private static async Task CreateRecordBatches(FileStream fileStream, ChannelWriter<FrequencyNazekaRecordBatch> writer, CancellationToken stopProducerToken)
    {
        try
        {
            // ReSharper disable once UseCancellationTokenForIAsyncEnumerable
            await foreach (FrequencyNazekaRecordBatch batch in ReadRecordBatches(fileStream, reuseRecordBatch: false).ConfigureAwait(false))
            {
                if (stopProducerToken.IsCancellationRequested)
                {
                    ReturnRecordBatch(batch);
                    return;
                }

                try
                {
                    if (!writer.TryWrite(batch))
                    {
                        await writer.WriteAsync(batch, stopProducerToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    ReturnRecordBatch(batch);
                    throw;
                }
            }
        }
        catch (OperationCanceledException)
        {
            LoggerManager.Logger.Debug("The Nazeka frequency record batch producer was canceled");
        }
        catch (Exception exception)
        {
            _ = writer.TryComplete(exception);
        }
        finally
        {
            _ = writer.TryComplete();
        }
    }

    private static void ReturnRecordBatch(FrequencyNazekaRecordBatch batch)
    {
        batch.Records.AsSpan(0, batch.Count).Clear();
        ArrayPool<FrequencyNazekaRecord>.Shared.Return(batch.Records);
    }

    private static async IAsyncEnumerable<FrequencyNazekaRecordBatch> ReadRecordBatches(FileStream fileStream, bool reuseRecordBatch)
    {
        bool wholeFile = fileStream.Length <= WholeFileParsingThreshold;
        byte[] jsonBytes = wholeFile ? new byte[(int)fileStream.Length] : ArrayPool<byte>.Shared.Rent(StreamingBufferSize);
        FrequencyNazekaRecord[]? records = ArrayPool<FrequencyNazekaRecord>.Shared.Rent(RecordBatchSize);
        int offset = 0;
        int recordCount = 0;
        bool started = false;
        bool completed = false;
        bool finalBlock = wholeFile;
        bool readingArray = false;
        string? reading = null;
        JsonReaderState readerState = default;
        try
        {
            int bufferedBytes;
            if (wholeFile)
            {
                await fileStream.ReadExactlyAsync(jsonBytes).ConfigureAwait(false);
                bufferedBytes = jsonBytes.Length;
            }
            else
            {
                bufferedBytes = await fileStream.ReadAtLeastAsync(jsonBytes.AsMemory(), 3, throwOnEndOfStream: false).ConfigureAwait(false);
            }

            ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
            if (jsonBytes.AsSpan(0, bufferedBytes).StartsWith(utf8Preamble))
            {
                offset = utf8Preamble.Length;
                bufferedBytes -= offset;
            }

            while (!finalBlock || bufferedBytes > 0 || !completed)
            {
                Debug.Assert(records is not null);
                ReadRecordBatch(jsonBytes.AsSpan(offset, bufferedBytes), finalBlock, records, ref recordCount,
                    ref readerState, ref started, ref completed, ref readingArray, ref reading, out int consumedBytes);
                offset += consumedBytes;
                bufferedBytes -= consumedBytes;
                bool recordBatchFull = recordCount is RecordBatchSize;
                if (reuseRecordBatch)
                {
                    try
                    {
                        yield return new FrequencyNazekaRecordBatch(records, recordCount);
                    }
                    finally
                    {
                        records.AsSpan(0, recordCount).Clear();
                        recordCount = 0;
                    }
                }
                else
                {
                    FrequencyNazekaRecordBatch batch = new(records, recordCount);
                    // The pipeline returns the batch after the consumer finishes with it.
                    records = null;
                    recordCount = 0;
                    yield return batch;

                    records = ArrayPool<FrequencyNazekaRecord>.Shared.Rent(RecordBatchSize);
                }

                if (completed && finalBlock)
                {
                    break;
                }

                if (finalBlock)
                {
                    if (consumedBytes is 0)
                    {
                        throw new JsonException("The Nazeka frequency root is incomplete.");
                    }

                    continue;
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

                int bytesRead = await fileStream.ReadAsync(jsonBytes.AsMemory(bufferedBytes)).ConfigureAwait(false);
                bufferedBytes += bytesRead;
                finalBlock = bytesRead is 0;
            }
        }
        finally
        {
            if (!wholeFile)
            {
                ArrayPool<byte>.Shared.Return(jsonBytes);
            }

            if (records is not null)
            {
                records.AsSpan(0, recordCount).Clear();
                ArrayPool<FrequencyNazekaRecord>.Shared.Return(records);
            }
        }
    }

    private static void ReadRecordBatch(ReadOnlySpan<byte> json, bool finalBlock, FrequencyNazekaRecord[] records, ref int recordCount, ref JsonReaderState readerState,
        ref bool started, ref bool completed, ref bool readingArray, ref string? reading, out int consumedBytes)
    {
        Utf8JsonReader reader = new(json, finalBlock, readerState);
        while (recordCount < RecordBatchSize)
        {
            Utf8JsonReader previousReader = reader;
            if (!reader.Read())
            {
                break;
            }

            if (!started)
            {
                if (reader.TokenType is not JsonTokenType.StartObject)
                {
                    throw new JsonException("The Nazeka frequency root must be an object.");
                }

                started = true;
            }
            else if (completed)
            {
                throw new JsonException("The Nazeka frequency root must contain only an object.");
            }
            else if (!readingArray && reader.TokenType is JsonTokenType.EndObject)
            {
                completed = true;
            }
            else if (!readingArray && reader.TokenType is JsonTokenType.PropertyName)
            {
                reading = reader.GetString();
            }
            else if (!readingArray && reader.TokenType is JsonTokenType.StartArray)
            {
                readingArray = true;
            }
            else if (readingArray && reader.TokenType is JsonTokenType.EndArray)
            {
                readingArray = false;
                reading = null;
            }
            else if (readingArray && reader.TokenType is JsonTokenType.StartArray)
            {
                if (!reader.Read())
                {
                    reader = previousReader;
                    break;
                }

                Utf8JsonReader spellingReader = reader;
                if (!reader.Read())
                {
                    reader = previousReader;
                    break;
                }

                int frequency = reader.GetInt32();
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
                    reader = previousReader;
                    break;
                }

                string? spelling = spellingReader.GetString();
                Debug.Assert(spelling is not null);
                Debug.Assert(reading is not null);
                records[recordCount] = new FrequencyNazekaRecord(reading, spelling, frequency);
                ++recordCount;
            }
            else
            {
                throw new JsonException("A Nazeka frequency reading must contain an array of records.");
            }
        }

        consumedBytes = (int)reader.BytesConsumed;
        readerState = reader.CurrentState;
    }
}
