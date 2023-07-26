using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Utilities;

namespace JL.Core.WordClass;

internal static class JmdictWordClassUtils
{
    public static async Task Load()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "PoS.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            DictUtils.WordClassDictionary = (await JsonSerializer.DeserializeAsync<Dictionary<string, List<JmdictWordClass>>>(fileStream).ConfigureAwait(false))!;
        }

        foreach (List<JmdictWordClass> jmdictWordClassList in DictUtils.WordClassDictionary.Values.ToList())
        {
            int jmdictWordClassListCount = jmdictWordClassList.Count;
            for (int i = 0; i < jmdictWordClassListCount; i++)
            {
                JmdictWordClass jmdictWordClass = jmdictWordClassList[i];

                if (jmdictWordClass.Readings is not null)
                {
                    for (int j = 0; j < jmdictWordClass.Readings.Length; j++)
                    {
                        string reading = jmdictWordClass.Readings[j];

                        if (DictUtils.WordClassDictionary.TryGetValue(reading, out List<JmdictWordClass>? result))
                        {
                            result.Add(jmdictWordClass);
                        }

                        else
                        {
                            DictUtils.WordClassDictionary.Add(reading, new List<JmdictWordClass> { jmdictWordClass });
                        }
                    }
                }
            }
        }

        DictUtils.WordClassDictionary.TrimExcess();
    }

    public static async Task Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new();

        HashSet<string> usedWordClasses = new()
        {
            "adj-i", "adj-na", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
        };

        foreach (List<IDictRecord> jmdictRecordList in DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.JMdict).Contents.Values.ToList())
        {
            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                var value = (JmdictRecord)jmdictRecordList[i];

                string[] wordClasses = usedWordClasses.Intersect(value.WordClasses.SelectMany(static wc => wc)).ToArray();

                if (wordClasses.Length is 0)
                {
                    continue;
                }

                if (jmdictWordClassDictionary.TryGetValue(value.PrimarySpelling, out List<JmdictWordClass>? psr))
                {
                    if (!psr.Any(r =>
                            r.Readings?.SequenceEqual(value.Readings ?? Enumerable.Empty<string>()) ??
                            (value.Readings is null && r.Spelling == value.PrimarySpelling)))
                    {
                        psr.Add(new JmdictWordClass(value.PrimarySpelling, value.Readings, wordClasses));
                    }
                }

                else
                {
                    jmdictWordClassDictionary.Add(value.PrimarySpelling,
                        new List<JmdictWordClass> { new(value.PrimarySpelling, value.Readings, wordClasses) });
                }

                if (value.AlternativeSpellings is not null)
                {
                    for (int j = 0; j < value.AlternativeSpellings.Length; j++)
                    {
                        string spelling = value.AlternativeSpellings[j];

                        if (jmdictWordClassDictionary.TryGetValue(spelling, out List<JmdictWordClass>? asr))
                        {
                            if (!asr.Any(r =>
                                    r.Readings?.SequenceEqual(value.Readings ?? Enumerable.Empty<string>()) ??
                                    (value.Readings is null && r.Spelling == spelling)))
                            {
                                asr.Add(new JmdictWordClass(spelling, value.Readings, wordClasses));
                            }
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

        await File.WriteAllTextAsync(Path.Join(Utils.ResourcesPath, "PoS.json"),
            JsonSerializer.Serialize(jmdictWordClassDictionary, Utils.s_defaultJso)).ConfigureAwait(false);
    }

    internal static async Task Initialize()
    {
        Dict dict = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.JMdict);
        if (!File.Exists(Path.Join(Utils.ResourcesPath, "PoS.json")))
        {
            if (dict.Active)
            {
                await Serialize().ConfigureAwait(false);
            }

            else
            {
                bool deleteJmdictFile = false;
                if (!File.Exists(dict.Path))
                {
                    deleteJmdictFile = true;
                    bool downloaded = await ResourceUpdater.UpdateResource(dict.Path,
                        DictUtils.s_jmdictUrl,
                        dict.Type.ToString(), false, true).ConfigureAwait(false);

                    if (!downloaded)
                    {
                        return;
                    }
                }

                await Task.Run(async () =>
                    await JmdictLoader.Load(dict).ConfigureAwait(false)).ConfigureAwait(false);
                await Serialize().ConfigureAwait(false);
                dict.Contents.Clear();
                dict.Contents.TrimExcess();

                if (deleteJmdictFile)
                {
                    File.Delete(dict.Path);
                }
            }
        }

        await Load().ConfigureAwait(false);
    }
}
