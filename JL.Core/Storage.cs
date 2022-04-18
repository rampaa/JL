using System.Runtime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomDict;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Dicts.Kanjium;
using JL.Core.Dicts.Options;
using JL.Core.Frequency;
using JL.Core.PoS;

namespace JL.Core
{
    public static class Storage
    {
        public const string Jpod101NoAudioMd5Hash = "7e2c2f954ef6051373ba916f000168dc";
        public static Stats SessionStats { get; set; } = new();
        public static IFrontend Frontend { get; set; } = new UnimplementedFrontend();
        public static readonly string ApplicationPath = AppContext.BaseDirectory;
        public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
        public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
        public static readonly HttpClient Client = new(new HttpClientHandler { UseProxy = false });
        public static readonly Version Version = new(1, 9);
        public static readonly string RepoUrl = "https://github.com/rampaa/JL/";
        public static bool Ready { get; set; } = false;
        public static bool UpdatingJMdict { get; set; } = false;
        public static bool UpdatingJMnedict { get; set; } = false;
        public static bool UpdatingKanjidic { get; set; } = false;
        public static Dictionary<string, List<JmdictWc>>? WcDict { get; set; } = new();
        public static Dictionary<string, Dictionary<string, List<FrequencyEntry>>> FreqDicts { get; set; } = new();

        public static readonly Dictionary<DictType, Dict> Dicts = new();

        public static readonly Dictionary<string, Dict> BuiltInDicts =
            new()
            {
                {
                    "CustomWordDictionary", new Dict(DictType.CustomWordDictionary,
                        $"{ResourcesPath}\\custom_words.txt",
                        true, 0,
                        new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null))
                },
                {
                    "CustomNameDictionary", new Dict(DictType.CustomNameDictionary,
                        $"{ResourcesPath}\\custom_names.txt", true, 1,
                        new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null))
                },
                {
                    "JMdict", new Dict(DictType.JMdict, $"{ResourcesPath}\\JMdict.xml", true, 2,
                        new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null))
                },
                {
                    "JMnedict", new Dict(DictType.JMnedict, $"{ResourcesPath}\\JMnedict.xml", true, 3,
                        new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null))
                },
                {
                    "Kanjidic", new Dict(DictType.Kanjidic, $"{ResourcesPath}\\kanjidic2.xml", true, 4,
                        new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null))
                }
            };

        public static readonly Dictionary<string, string> FrequencyLists = new()
        {
            { "VN", $"{ResourcesPath}/freqlist_vns.json" },
            { "Novel", $"{ResourcesPath}/freqlist_novels.json" },
            { "Narou", $"{ResourcesPath}/freqlist_narou.json" },
            { "None", "" }
        };

        public static readonly List<DictType> YomichanDictTypes = new()
        {
            DictType.Kenkyuusha,
            DictType.Daijirin,
            DictType.Daijisen,
            DictType.Koujien,
            DictType.Meikyou,
            DictType.Gakken,
            DictType.Kotowaza,
            DictType.Kanjium,
        };

        public static readonly List<DictType> NazekaDictTypes = new()
        {
            DictType.KenkyuushaNazeka,
            DictType.DaijirinNazeka,
            DictType.ShinmeikaiNazeka,
        };

        public static readonly JsonSerializerOptions JsoUnsafeEscaping = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static readonly Regex JapaneseRegex =
            new(
                @"[\u2e80-\u30ff\u31c0-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|\ud82c[\udc00-\udcff]|\ud83c[\ude00-\udeff]|\ud840[\udc00-\udfff]|[\ud841-\ud868][\udc00-\udfff]|\ud869[\udc00-\udedf]|\ud869[\udf00-\udfff]|[\ud86a-\ud879][\udc00-\udfff]|\ud87a[\udc00-\udfef]|\ud87e[\udc00-\ude1f]|\ud880[\udc00-\udfff]|[\ud881-\ud883][\udc00-\udfff]|\ud884[\udc00-\udf4f]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        public static readonly List<string> JapanesePunctuation =
            new()
            {
                "。",
                "！",
                "？",
                "…",
                ".",
                "、",
                "「",
                "」",
                "『",
                "』",
                "（",
                "）",
                "\n"
            };

        public static async Task LoadDictionaries()
        {
            Ready = false;

            List<Task> tasks = new();
            bool dictRemoved = false;

            foreach (Dict dict in Dicts.Values.ToList())
            {
                switch (dict.Type)
                {
                    case DictType.JMdict:
                        if (dict.Active && !Dicts[DictType.JMdict].Contents.Any() && !UpdatingJMdict)
                        {
                            Task jMDictTask = Task.Run(async () =>
                                await JMdictLoader.Load(dict.Path).ConfigureAwait(false));

                            tasks.Add(jMDictTask);
                        }

                        else if (!dict.Active && Dicts[DictType.JMdict].Contents.Any() && !UpdatingJMdict)
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.JMnedict:
                        if (dict.Active && !Dicts[DictType.JMnedict].Contents.Any() && !UpdatingJMnedict)
                        {
                            tasks.Add(Task.Run(async () =>
                                await JMnedictLoader.Load(dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.JMnedict].Contents.Any() && !UpdatingJMnedict)
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Kanjidic:
                        if (dict.Active && !Dicts[DictType.Kanjidic].Contents.Any() && !UpdatingKanjidic)
                        {
                            tasks.Add(Task.Run(async () =>
                                await KanjiInfoLoader.Load(dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Kanjidic].Contents.Any() && !UpdatingKanjidic)
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

                    case DictType.DaijirinNazeka:
                        if (dict.Active && !Dicts[DictType.DaijirinNazeka].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingNazekaLoader
                                    .Load(DictType.DaijirinNazeka, Dicts[DictType.DaijirinNazeka].Path)
                                    .ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.DaijirinNazeka].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.KenkyuushaNazeka:
                        if (dict.Active && !Dicts[DictType.KenkyuushaNazeka].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingNazekaLoader.Load(DictType.KenkyuushaNazeka,
                                        Dicts[DictType.KenkyuushaNazeka].Path)
                                    .ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.KenkyuushaNazeka].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.ShinmeikaiNazeka:
                        if (dict.Active && !Dicts[DictType.ShinmeikaiNazeka].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await EpwingNazekaLoader.Load(DictType.ShinmeikaiNazeka,
                                        Dicts[DictType.ShinmeikaiNazeka].Path)
                                    .ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.ShinmeikaiNazeka].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    case DictType.Kanjium:
                        if (dict.Active && !Dicts[DictType.Kanjium].Contents.Any())
                        {
                            tasks.Add(Task.Run(async () =>
                                await KanjiumLoader.Load(dict.Type, dict.Path).ConfigureAwait(false)));
                        }

                        else if (!dict.Active && Dicts[DictType.Kanjium].Contents.Any())
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

            Ready = true;
        }

        public static async Task InitializePoS()
        {
            if (!File.Exists($"{Storage.ResourcesPath}/PoS.json"))
            {
                if (Dicts[DictType.JMdict].Active)
                {
                    await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
                }

                else
                {
                    bool deleteJmdictFile = false;
                    if (!File.Exists(Dicts[DictType.JMdict].Path))
                    {
                        deleteJmdictFile = true;
                        await ResourceUpdater.UpdateResource(Dicts[DictType.JMdict].Path,
                            new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                            DictType.JMdict.ToString(), false, true).ConfigureAwait(false);
                    }

                    await Task.Run((async () =>
                        await JMdictLoader.Load(Dicts[DictType.JMdict].Path).ConfigureAwait(false)));
                    await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
                    Dicts[DictType.JMdict].Contents.Clear();

                    if (deleteJmdictFile)
                        File.Delete(Dicts[DictType.JMdict].Path);
                }
            }

            await JmdictWcLoader.Load().ConfigureAwait(false);
        }

        public static async Task LoadFrequency()
        {
            if (!FreqDicts.ContainsKey(Storage.Frontend.CoreConfig.FrequencyListName))
            {
                bool callGc = false;
                Task? taskNewFreqlist = null;
                if (Storage.Frontend.CoreConfig.FrequencyListName != "None")
                {
                    callGc = true;
                    FreqDicts.Clear();
                    FreqDicts.Add(Storage.Frontend.CoreConfig.FrequencyListName,
                        new Dictionary<string, List<FrequencyEntry>>());

                    taskNewFreqlist = Task.Run(async () =>
                    {
                        FrequencyLoader.BuildFreqDict((await FrequencyLoader
                            .LoadJson(Storage.FrequencyLists[Storage.Frontend.CoreConfig.FrequencyListName])
                            .ConfigureAwait(false))!);
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
