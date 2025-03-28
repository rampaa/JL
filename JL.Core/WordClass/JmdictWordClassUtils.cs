using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Utilities;

namespace JL.Core.WordClass;

internal static class JmdictWordClassUtils
{
    private static readonly FrozenSet<string> s_usedWordClasses =
        [
            "adj-i", "adj-na", "v1", "v1-s", "v4r", "v5aru", "v5b", "v5g", "v5k", "v5k-s", "v5m",
            "v5n", "v5r", "v5r-i", "v5s", "v5t", "v5u", "v5u-s", "vk", "vs-c", "vs-i", "vs-s", "vz"
        ];

    internal static async Task Load()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ResourcesPath, "PoS.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            DictUtils.WordClassDictionary = (await JsonSerializer.DeserializeAsync<Dictionary<string, IList<JmdictWordClass>>>(fileStream, Utils.s_jso).ConfigureAwait(false))!;
        }

        IList<JmdictWordClass>[] jmdictWordClasses = DictUtils.WordClassDictionary.Values.ToArray();
        foreach (IList<JmdictWordClass> jmdictWordClassList in jmdictWordClasses)
        {
            int jmdictWordClassListCount = jmdictWordClassList.Count;
            for (int j = 0; j < jmdictWordClassListCount; j++)
            {
                JmdictWordClass jmdictWordClass = jmdictWordClassList[j];

                jmdictWordClass.Readings?.DeduplicateStringsInArray();
                jmdictWordClass.WordClasses.DeduplicateStringsInArray();

                if (jmdictWordClass.Readings is not null)
                {
                    foreach (string reading in jmdictWordClass.Readings)
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
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

    internal static Task Serialize()
    {
        Dictionary<string, List<JmdictWordClass>> jmdictWordClassDictionary = new(StringComparer.Ordinal);

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        foreach ((string key, IList<IDictRecord> jmdictRecordList) in dict.Contents)
        {
            int jmdictRecordListCount = jmdictRecordList.Count;
            for (int i = 0; i < jmdictRecordListCount; i++)
            {
                JmdictRecord jmdictRecord = (JmdictRecord)jmdictRecordList[i];
                string[] wordClasses = s_usedWordClasses
                    .Intersect((jmdictRecord.WordClasses?.Where(static wc => wc is not null).SelectMany(static wc => wc!) ?? [])
                        .Union(jmdictRecord.WordClassesSharedByAllSenses ?? [])).ToArray();

                if (wordClasses.Length is 0)
                {
                    continue;
                }

                if (jmdictRecord.Readings?.Select(JapaneseUtils.KatakanaToHiragana).Contains(key) ?? false)
                {
                    continue;
                }

                JmdictWordClass record = new(jmdictRecord.PrimarySpelling, wordClasses, jmdictRecord.Readings);
                if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? results))
                {
                    if (!CollectionsMarshal.AsSpan(results).Contains(record))
                    {
                        results.Add(record);
                    }
                }

                else
                {
                    jmdictWordClassDictionary[key] = [record];
                }
            }
        }

        return File.WriteAllTextAsync(Path.Join(Utils.ResourcesPath, "PoS.json"),
            JsonSerializer.Serialize(jmdictWordClassDictionary, Utils.s_jsoIgnoringWhenWritingNull));
    }

    internal static async Task Initialize()
    {
        Dict jmdictDict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        string jmdictPath = Path.GetFullPath(jmdictDict.Path, Utils.ApplicationPath);
        string partOfSpeechFilePath = Path.Join(Utils.ResourcesPath, "PoS.json");

        if (!File.Exists(partOfSpeechFilePath)
            || (File.Exists(jmdictPath) && File.GetLastWriteTime(jmdictPath) > File.GetLastWriteTime(partOfSpeechFilePath)))
        {
            bool useDB = jmdictDict.Options.UseDB.Value;
            if (jmdictDict.Active && !useDB)
            {
                await Serialize().ConfigureAwait(false);
            }

            else
            {
                bool deleteJmdictFile = false;
                if (!File.Exists(jmdictPath))
                {
                    deleteJmdictFile = true;
                    bool downloaded = await DictUpdater.DownloadDict(jmdictPath,
                        DictUtils.s_jmdictUrl,
                        jmdictDict.Type.ToString(), false, true).ConfigureAwait(false);

                    if (!downloaded)
                    {
                        return;
                    }
                }

                jmdictDict.Contents = new Dictionary<string, IList<IDictRecord>>(jmdictDict.Size > 0 ? jmdictDict.Size : 450000, StringComparer.Ordinal);
                await JmdictLoader.Load(jmdictDict).ConfigureAwait(false);
                await Serialize().ConfigureAwait(false);
                jmdictDict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;

                if (deleteJmdictFile)
                {
                    File.Delete(jmdictPath);
                }
            }
        }

        if (DictUtils.WordClassDictionary.Count is 0)
        {
            await Load().ConfigureAwait(false);
        }
    }
}
