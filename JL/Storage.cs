using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Threading.Tasks;
using JL.Dicts;
using JL.Dicts.CustomDict;
using JL.Dicts.EDICT;
using JL.Dicts.EDICT.JMdict;
using JL.Dicts.EDICT.JMnedict;
using JL.Dicts.EDICT.KANJIDIC;
using JL.Dicts.EPWING;
using JL.Frequency;
using JL.PoS;

namespace JL
{
    public class Storage
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();
        public static readonly HttpClient Client = new(new HttpClientHandler() { UseProxy = false });
        public static readonly Version Version = new(1, 2);
        public static readonly string RepoUrl = "https://github.com/rampaa/JL/";

        public static Dictionary<string, List<JmdictWc>> WcDict { get; set; } = new();
        public static Dictionary<string, Dictionary<string, List<FrequencyEntry>>> FreqDicts { get; set; } = new();

        public static readonly Dictionary<DictType, Dict> Dicts = new();

        public static readonly Dictionary<string, Dict> BuiltInDicts =
            new()
            {
                { "JMdict", new Dict(DictType.JMdict, "Resources\\JMdict.xml", true, 0) },
                { "JMnedict", new Dict(DictType.JMnedict, "Resources\\JMnedict.xml", true, 1) },
                { "Kanjidic", new Dict(DictType.Kanjidic, "Resources\\kanjidic2.xml", true, 2) },
                {
                    "CustomWordDictionary",
                    new Dict(DictType.CustomWordDictionary, "Resources\\custom_words.txt", true, 3)
                },
                {
                    "CustomNameDictionary",
                    new Dict(DictType.CustomNameDictionary, "Resources\\custom_names.txt", true, 4)
                }
            };

        public static readonly Dictionary<string, string> FrequencyLists = new()
        {
            { "VN", "Resources/freqlist_vns.json" },
            { "Novel", "Resources/freqlist_novels.json" },
            { "Narou", "Resources/freqlist_narou.json" },
            { "None", "" }
        };

        public static async Task LoadDictionaries()
        {
            ConfigManager.Ready = false;

            List<Task> tasks = new();
            bool dictRemoved = false;

            foreach (Dict dict in Dicts.Values.ToList())
            {
                switch (dict.Type)
                {
                    case DictType.JMdict:
                        // initial jmdict load
                        if (dict.Active && !Dicts[DictType.JMdict].Contents.Any())
                        {
                            Task jMDictTask = Task.Run(async () =>
                                await JMdictLoader.Load(dict.Path).ConfigureAwait(false));

                            tasks.Add(jMDictTask);
                        }

                        else if (!dict.Active && Dicts[DictType.JMdict].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.JMnedict:
                        // JMnedict
                        if (dict.Active && !Dicts[DictType.JMnedict].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () => await JMnedictLoader.Load(dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.JMnedict].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Kanjidic:
                        // KANJIDIC
                        if (dict.Active && !Dicts[DictType.Kanjidic].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await KanjiInfoLoader.Load(dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Kanjidic].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Kenkyuusha:
                        if (dict.Active && !Dicts[DictType.Kenkyuusha].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Kenkyuusha].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Daijirin:
                        if (dict.Active && !Dicts[DictType.Daijirin].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Daijirin].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Daijisen:
                        if (dict.Active && !Dicts[DictType.Daijisen].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Daijisen].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Koujien:
                        if (dict.Active && !Dicts[DictType.Koujien].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Koujien].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Meikyou:
                        if (dict.Active && !Dicts[DictType.Meikyou].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Meikyou].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.Gakken:
                        if (dict.Active && !Dicts[DictType.Gakken].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Gakken].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.Kotowaza:
                        if (dict.Active && !Dicts[DictType.Kotowaza].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingJsonLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Kotowaza].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.CustomWordDictionary:
                        if (dict.Active && !Dicts[DictType.CustomWordDictionary].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () => await CustomWordLoader
                                .Load(Dicts[DictType.CustomWordDictionary].Path)
                                .ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.CustomWordDictionary].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.CustomNameDictionary:
                        if (dict.Active && !Dicts[DictType.CustomNameDictionary].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await CustomNameLoader.Load(Dicts[DictType.CustomNameDictionary].Path)
                                    .ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.CustomNameDictionary].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (tasks.Any() || dictRemoved)
            {
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }

            ConfigManager.Ready = true;
        }

        public static async Task InitializePoS()
        {
            if (!File.Exists(Path.Join(Storage.ApplicationPath, "Resources/PoS.json")))
            {
                if (Dicts[DictType.JMdict].Active)
                {
                    await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
                }

                else
                {
                    bool deleteJmdictFile = false;
                    if (!File.Exists(Path.Join(Storage.ApplicationPath, Dicts[DictType.JMdict].Path)))
                    {
                        deleteJmdictFile = true;
                        await ResourceUpdater.UpdateResource(Dicts[DictType.JMdict].Path,
                            new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                            DictType.JMdict.ToString(), false, true).ConfigureAwait(false);
                    }

                    await Task.Run(async () =>
                        await JMdictLoader.Load(Dicts[DictType.JMdict].Path).ConfigureAwait(false));
                    await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
                    Dicts[DictType.JMdict].Contents.Clear();

                    if (deleteJmdictFile)
                        File.Delete(Path.Join(Storage.ApplicationPath, Dicts[DictType.JMdict].Path));
                }
            }

            await JmdictWcLoader.Load().ConfigureAwait(false);
        }

        public static async Task LoadFrequency()
        {
            if (!FreqDicts.ContainsKey(ConfigManager.FrequencyListName))
            {
                bool callGc = false;
                Task taskNewFreqlist = null;
                if (ConfigManager.FrequencyListName != "None")
                {
                    callGc = true;
                    FreqDicts.Clear();
                    FreqDicts.Add(ConfigManager.FrequencyListName, new Dictionary<string, List<FrequencyEntry>>());

                    taskNewFreqlist = Task.Run(async () =>
                    {
                        FrequencyLoader.BuildFreqDict(await FrequencyLoader
                            .LoadJson(Path.Join(Storage.ApplicationPath,
                                Storage.FrequencyLists[ConfigManager.FrequencyListName]))
                            .ConfigureAwait(false));
                    });
                }

                else if (FreqDicts.Any())
                {
                    callGc = true;
                    FreqDicts.Clear();
                }

                if (callGc)
                {
                    if (taskNewFreqlist != null)
                        await taskNewFreqlist.ConfigureAwait(false);

                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                }
            }
        }
    }
}
