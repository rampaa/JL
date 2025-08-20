using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyNazeka;

internal static class FrequencyNazekaLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        Dictionary<string, ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>? frequencyJson;

        FileStream fileStream = File.OpenRead(fullPath);
        await using (fileStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer
                .DeserializeAsync<Dictionary<string, ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>>(fileStream, Utils.Jso)
                .ConfigureAwait(false);
        }

        Debug.Assert(frequencyJson is not null);
        foreach ((string reading, ReadOnlyMemory<ReadOnlyMemory<JsonElement>> value) in frequencyJson)
        {
            foreach (ref readonly ReadOnlyMemory<JsonElement> elementListMemory in value.Span)
            {
                ReadOnlySpan<JsonElement> elementList = elementListMemory.Span;

                string exactSpelling = elementList[0].GetString()!.GetPooledString();
                int frequencyRank = elementList[1].GetInt32();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                FrequencyRecord frequencyRecordWithExactSpelling = new(exactSpelling, frequencyRank);
                FreqUtils.AddOrUpdate(freq.Contents, reading, frequencyRecordWithExactSpelling);

                string exactSpellingInHiragana = JapaneseUtils.KatakanaToHiragana(exactSpelling).GetPooledString();
                if (exactSpellingInHiragana != reading)
                {
                    FrequencyRecord frequencyRecordWithReading = new(reading, frequencyRank);
                    FreqUtils.AddOrUpdate(freq.Contents, exactSpellingInHiragana, frequencyRecordWithReading);
                }
            }
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
