using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.JMdict;
using JL.Core.Utilities;

namespace JL.Core.WordClass;

internal static class JmdictWordClassUtils
{
    public static async Task Load()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "PoS.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            DictUtils.WordClassDictionary = (await JsonSerializer.DeserializeAsync<Dictionary<string, IList<JmdictWordClass>>>(fileStream).ConfigureAwait(false))!;
        }

        foreach (IList<JmdictWordClass> jmdictWordClassList in DictUtils.WordClassDictionary.Values.ToList())
        {
            int jmdictWordClassListCount = jmdictWordClassList.Count;
            for (int i = 0; i < jmdictWordClassListCount; i++)
            {
                JmdictWordClass jmdictWordClass = jmdictWordClassList[i];

                jmdictWordClass.Spelling = jmdictWordClass.Spelling.GetPooledString();
                jmdictWordClass.Readings?.DeduplicateStringsInArray();
                jmdictWordClass.WordClasses.DeduplicateStringsInArray();

                if (jmdictWordClass.Readings is not null)
                {
                    for (int j = 0; j < jmdictWordClass.Readings.Length; j++)
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(jmdictWordClass.Readings[j]).GetPooledString();
                        if (DictUtils.WordClassDictionary.TryGetValue(readingInHiragana, out IList<JmdictWordClass>? result))
                        {
                            result.Add(jmdictWordClass);
                        }
                        else
                        {
                            DictUtils.WordClassDictionary[readingInHiragana] = [jmdictWordClass];
                        }
                    }
                }
            }
        }

        foreach ((string key, IList<JmdictWordClass> recordList) in DictUtils.WordClassDictionary)
        {
            DictUtils.WordClassDictionary[key] = recordList.ToArray();
        }

        DictUtils.WordClassDictionary = DictUtils.WordClassDictionary.ToFrozenDictionary();
    }

    public static async Task Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = [];

        HashSet<string> usedWordClasses =
        [
            "adj-i", "adj-na", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
        ];

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        foreach (IList<IDictRecord> jmdictRecordList in dict.Contents.Values.ToList())
        {
            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                JmdictRecord value = (JmdictRecord)jmdictRecordList[i];

                string[] wordClasses = usedWordClasses.Intersect(value.WordClasses.SelectMany(static wc => wc)).ToArray();

                if (wordClasses.Length is 0)
                {
                    continue;
                }

                if (jmdictWordClassDictionary.TryGetValue(value.PrimarySpelling, out List<JmdictWordClass>? psr))
                {
                    if (!psr.Any(r =>
                            r.Readings?.SequenceEqual(value.Readings ?? []) ??
                            (value.Readings is null && r.Spelling == value.PrimarySpelling)))
                    {
                        psr.Add(new JmdictWordClass(value.PrimarySpelling, value.Readings, wordClasses));
                    }
                }

                else
                {
                    jmdictWordClassDictionary[value.PrimarySpelling] = [new JmdictWordClass(value.PrimarySpelling, value.Readings, wordClasses)];
                }

                if (value.AlternativeSpellings is not null)
                {
                    for (int j = 0; j < value.AlternativeSpellings.Length; j++)
                    {
                        string spelling = value.AlternativeSpellings[j];

                        if (jmdictWordClassDictionary.TryGetValue(spelling, out List<JmdictWordClass>? asr))
                        {
                            if (!asr.Any(r =>
                                    r.Readings?.SequenceEqual(value.Readings ?? []) ??
                                    (value.Readings is null && r.Spelling == spelling)))
                            {
                                asr.Add(new JmdictWordClass(spelling, value.Readings, wordClasses));
                            }
                        }

                        else
                        {
                            jmdictWordClassDictionary[spelling] = [new JmdictWordClass(spelling, value.Readings, wordClasses)];
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
        if (!File.Exists(Path.Join(Utils.ResourcesPath, "PoS.json")))
        {
            Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
            bool useDB = dict.Options?.UseDB?.Value ?? false;

            if (dict.Active && !useDB)
            {
                await Serialize().ConfigureAwait(false);
            }

            else
            {
                bool deleteJmdictFile = false;
                string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
                if (!File.Exists(fullPath))
                {
                    deleteJmdictFile = true;
                    bool downloaded = await DictUpdater.UpdateDict(fullPath,
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
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;

                if (deleteJmdictFile)
                {
                    File.Delete(fullPath);
                }
            }
        }

        if (DictUtils.WordClassDictionary.Count is 0)
        {
            await Load().ConfigureAwait(false);
        }
    }
}
