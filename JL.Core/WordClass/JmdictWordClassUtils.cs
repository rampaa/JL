using System.Collections.Frozen;
using System.Diagnostics;
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
            Dictionary<string, IList<JmdictWordClass>>? wordClassDictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, IList<JmdictWordClass>>>(fileStream, Utils.s_jso).ConfigureAwait(false);
            Debug.Assert(wordClassDictionary is not null);
            DictUtils.WordClassDictionary = wordClassDictionary;
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

        DictUtils.WordClassDictionary = DictUtils.WordClassDictionary.ToFrozenDictionary(static entry => entry.Key, static IList<JmdictWordClass> (kvp) => kvp.Value.ToArray(), StringComparer.Ordinal);
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
                List<string> wordClassList = [];
                if (jmdictRecord.WordClasses is not null)
                {
                    foreach (string[]? wordClassArray in jmdictRecord.WordClasses)
                    {
                        if (wordClassArray is not null)
                        {
                            foreach (string wordClass in wordClassArray)
                            {
                                if (s_usedWordClasses.Contains(wordClass))
                                {
                                    wordClassList.Add(wordClass);
                                }
                            }
                        }
                    }
                }

                if (jmdictRecord.WordClassesSharedByAllSenses is not null)
                {
                    foreach (string wordClass in jmdictRecord.WordClassesSharedByAllSenses)
                    {
                        if (s_usedWordClasses.Contains(wordClass))
                        {
                            wordClassList.Add(wordClass);
                        }
                    }
                }

                if (wordClassList.Count is 0)
                {
                    continue;
                }

                string[] wordClasses = wordClassList.ToArray();

                if (jmdictRecord.Readings is not null)
                {
                    bool keyFromReading = false;
                    foreach (string reading in jmdictRecord.Readings)
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading);
                        if (readingInHiragana == key)
                        {
                            keyFromReading = true;
                            break;
                        }
                    }

                    if (keyFromReading)
                    {
                        if (JapaneseUtils.KatakanaToHiragana(jmdictRecord.PrimarySpelling) != key)
                        {
                            continue;
                        }

                        if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? prevResults))
                        {
                            bool alreadyAdded = false;
                            foreach (JmdictWordClass wordClass in prevResults.AsReadOnlySpan())
                            {
                                if (wordClass.Spelling == jmdictRecord.PrimarySpelling
                                    && wordClass.Readings.AsReadOnlySpan().SequenceEqual(jmdictRecord.Readings)
                                    && wordClass.WordClasses.AsReadOnlySpan().SequenceEqual(wordClasses))
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }

                            if (alreadyAdded)
                            {
                                continue;
                            }
                        }
                    }
                }

                JmdictWordClass record = new(jmdictRecord.PrimarySpelling, wordClasses, jmdictRecord.Readings);
                if (jmdictWordClassDictionary.TryGetValue(key, out List<JmdictWordClass>? results))
                {
                    if (!results.AsReadOnlySpan().Contains(record))
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
