using System.Buffers;
using System.Text;
using System.Text.Json;

namespace JL.Core.Utilities;

internal static class YomichanBankReader
{
    internal const int WholeFileParsingThreshold = 128 * 1024 * 1024;
    internal const int PooledFileThreshold = 1024 * 1024;

    public static async IAsyncEnumerable<JsonElement> Read(FileStream fileStream)
    {
        if (fileStream.Length <= WholeFileParsingThreshold)
        {
            int jsonLength = (int)fileStream.Length;
            bool pooled = jsonLength <= PooledFileThreshold;
            byte[] jsonBytes = pooled ? ArrayPool<byte>.Shared.Rent(jsonLength) : new byte[jsonLength];
            try
            {
                await fileStream.ReadExactlyAsync(jsonBytes.AsMemory(0, jsonLength)).ConfigureAwait(false);

                ReadOnlyMemory<byte> json = jsonBytes.AsMemory(0, jsonLength);
                ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
                if (json.Span.StartsWith(utf8Preamble))
                {
                    json = json[utf8Preamble.Length..];
                }

                using JsonDocument document = JsonDocument.Parse(json);
                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    yield return item;
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
            await foreach (JsonElement item in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}
