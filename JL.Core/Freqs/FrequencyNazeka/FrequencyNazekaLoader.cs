using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyNazeka;

internal static class FrequencyNazekaLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        Dictionary<string, JsonElement[][]>? frequencyJson;
        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement[][]>>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            Debug.Assert(frequencyJson is not null);
        }

        foreach ((string reading, JsonElement[][] value) in frequencyJson)
        {
            foreach (JsonElement[] elementList in value)
            {
                int frequencyRank = elementList[1].GetInt32();
                string exactSpelling = elementList[0].GetString()!.GetPooledString();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                FrequencyRecord frequencyRecordWithExactSpelling = new(exactSpelling, frequencyRank);
                FreqUtils.AddOrUpdate(freq.Contents, reading, frequencyRecordWithExactSpelling);

                string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling).GetPooledString();
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
