using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT.JMdict;

namespace JL.Core.WordClass;

public static class JmdictWordClassLoader
{
    public static async Task Load()
    {
        FileStream openStream = File.OpenRead($"{Storage.ResourcesPath}/PoS.json");
        await using (openStream.ConfigureAwait(false))
        {
            Storage.WordClassDictionary = (await JsonSerializer.DeserializeAsync<Dictionary<string, List<JmdictWordClass>>>(openStream))!;
        }

        foreach (List<JmdictWordClass> jmdictWordClassList in Storage.WordClassDictionary.Values.ToList())
        {
            int jmdictWordClassListCount = jmdictWordClassList.Count;
            for (int i = 0; i < jmdictWordClassListCount; i++)
            {
                JmdictWordClass jmdictWordClass = jmdictWordClassList[i];

                if (jmdictWordClass.Readings != null)
                {
                    int readingCount = jmdictWordClass.Readings.Count;
                    for (int j = 0; j < readingCount; j++)
                    {
                        string reading = jmdictWordClass.Readings[j];

                        if (Storage.WordClassDictionary.TryGetValue(reading, out List<JmdictWordClass>? result))
                        {
                            result.Add(jmdictWordClass);
                        }

                        else
                        {
                            Storage.WordClassDictionary.Add(reading, new List<JmdictWordClass> { jmdictWordClass });
                        }
                    }
                }
            }
        }

        Storage.WordClassDictionary.TrimExcess();
    }

    public static async Task JmdictWordClassSerializer()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new();

        string[] usedWordClasses =
        {
            "adj-i", "adj-na", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
        };

        foreach (List<IDictRecord> jmdictRecordList in Storage.Dicts.Values.First(dict => dict.Type == DictType.JMdict).Contents.Values.ToList())
        {
            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                var value = (JmdictRecord)jmdictRecordList[i];

                if ((!value.WordClasses?.Any()) ?? true)
                    continue;

                List<string> wordClasses = value.WordClasses?.Where(wc => wc != null).SelectMany(wc => wc!).ToHashSet().Intersect(usedWordClasses).ToList() ?? new();

                if (!wordClasses.Any())
                    continue;

                if (jmdictWordClassDictionary.TryGetValue(value.PrimarySpelling, out List<JmdictWordClass>? psr))
                {
                    if (!psr.Any(r =>
                            r.Readings?.SequenceEqual(value.Readings ?? new List<string>()) ??
                            value.Readings == null && r.Spelling == value.PrimarySpelling))
                        psr.Add(new JmdictWordClass(value.PrimarySpelling, value.Readings, wordClasses));
                }

                else
                {
                    jmdictWordClassDictionary.Add(value.PrimarySpelling,
                        new List<JmdictWordClass> { new(value.PrimarySpelling, value.Readings, wordClasses) });
                }

                if (value.AlternativeSpellings != null)
                {
                    int alternativeSpellingCount = value.AlternativeSpellings.Count;
                    for (int j = 0; j < alternativeSpellingCount; j++)
                    {
                        string spelling = value.AlternativeSpellings[j];

                        if (jmdictWordClassDictionary.TryGetValue(spelling, out List<JmdictWordClass>? asr))
                        {
                            if (!asr.Any(r =>
                                    r.Readings?.SequenceEqual(value.Readings ?? new List<string>()) ??
                                    value.Readings == null && r.Spelling == spelling))
                                asr.Add(new JmdictWordClass(spelling, value.Readings, wordClasses));
                        }

                        else
                        {
                            jmdictWordClassDictionary.Add(spelling,
                                new List<JmdictWordClass> { new(spelling, value.Readings, wordClasses) });
                        }
                    }
                }
            }
        }

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        await File.WriteAllBytesAsync($"{Storage.ResourcesPath}/PoS.json",
            JsonSerializer.SerializeToUtf8Bytes(jmdictWordClassDictionary, options)).ConfigureAwait(false);
    }
}
