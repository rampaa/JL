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
                .DeserializeAsync<Dictionary<string, ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>>(fileStream, Utils.s_jso)
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
                if (freq.Contents.TryGetValue(reading, out IList<FrequencyRecord>? readingFreqResult))
                {
                    int index = readingFreqResult.IndexOf(frequencyRecordWithExactSpelling);
                    if (index < 0)
                    {
                        readingFreqResult.Add(frequencyRecordWithExactSpelling);
                    }
                    else
                    {
                        FrequencyRecord record = readingFreqResult[index];
                        if (record.Frequency > frequencyRank)
                        {
                            readingFreqResult[index] = frequencyRecordWithExactSpelling;
                        }
                    }
                }
                else
                {
                    freq.Contents[reading.GetPooledString()] = [frequencyRecordWithExactSpelling];
                }

                string exactSpellingInHiragana = JapaneseUtils.KatakanaToHiragana(exactSpelling).GetPooledString();
                if (exactSpellingInHiragana != reading)
                {
                    FrequencyRecord frequencyRecordWithReading = new(reading, frequencyRank);
                    if (freq.Contents.TryGetValue(exactSpellingInHiragana, out IList<FrequencyRecord>? exactSpellingFreqResult))
                    {
                        int index = exactSpellingFreqResult.IndexOf(frequencyRecordWithReading);
                        if (index < 0)
                        {
                            exactSpellingFreqResult.Add(frequencyRecordWithReading);
                        }
                        else
                        {
                            FrequencyRecord record = exactSpellingFreqResult[index];
                            if (record.Frequency > frequencyRank)
                            {
                                exactSpellingFreqResult[index] = frequencyRecordWithReading;
                            }
                        }
                    }
                    else
                    {
                        freq.Contents[exactSpellingInHiragana] = [frequencyRecordWithReading];
                    }
                }
            }
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(entry => entry.Key, entry => (IList<FrequencyRecord>)entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
