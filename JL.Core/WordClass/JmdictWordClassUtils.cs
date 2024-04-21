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

        DictUtils.WordClassDictionary = DictUtils.WordClassDictionary.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public static async Task Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new(StringComparer.Ordinal);

        HashSet<string> usedWordClasses =
        [
            "adj-i", "adj-na", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
        ];

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        foreach ((string key, IList<IDictRecord> jmdictRecordList) in dict.Contents)
        {
            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                JmdictRecord jmdictRecord = (JmdictRecord)jmdictRecordList[i];
                string[] wordClasses = usedWordClasses.Intersect(jmdictRecord.WordClasses.SelectMany(static wc => wc)).ToArray();

                if (wordClasses.Length is 0)
                {
                    continue;
                }

                if (jmdictRecord.Readings?.Select(JapaneseUtils.KatakanaToHiragana).Contains(key) ?? false)
                {
                    continue;
                }

                if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? results))
                {
                    if (!results.Any(result => result.Spelling == jmdictRecord.PrimarySpelling
                        && ((result.Readings is not null && jmdictRecord.Readings is not null && result.Readings.SequenceEqual(jmdictRecord.Readings))
                            || (result.Readings is null && jmdictRecord.Readings is null))))
                    {
                        results.Add(new JmdictWordClass(jmdictRecord.PrimarySpelling, jmdictRecord.Readings, wordClasses));
                    }
                }

                else
                {
                    jmdictWordClassDictionary[key] = [new JmdictWordClass(jmdictRecord.PrimarySpelling, jmdictRecord.Readings, wordClasses)];
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
            bool useDB = dict.Options?.UseDB?.Value ?? true;

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

                dict.Contents = new Dictionary<string, IList<IDictRecord>>(dict.Size > 0 ? dict.Size : 450000, StringComparer.Ordinal);
                await Task.Run(async () => await JmdictLoader.Load(dict).ConfigureAwait(false)).ConfigureAwait(false);
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
