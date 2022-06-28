using System.Runtime;
using System.Text.RegularExpressions;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Dicts.EPWING.EpwingYomichan;
using JL.Core.Dicts.Options;
using JL.Core.Frequency;
using JL.Core.Frequency.FreqNazeka;
using JL.Core.Frequency.FreqYomichan;
using JL.Core.Pitch;
using JL.Core.PoS;
using JL.Core.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Core;

public static class Storage
{
    public static Timer Timer { get; } = new();
    public const string Jpod101NoAudioMd5Hash = "7e2c2f954ef6051373ba916f000168dc";
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
    public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
    public static readonly HttpClient Client = new(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromMinutes(10) };
    public static readonly Version Version = new(1, 12, 1);
    public static readonly string GitHubApiUrlForLatestJLRelease = "https://api.github.com/repos/rampaa/JL/releases/latest";
    public static readonly Uri JmdictUrl = new("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz");
    public static readonly Uri JmnedictUrl = new("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    public static readonly Uri KanjidicUrl = new("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz");
    public static bool DictsReady { get; private set; } = false;
    public static bool UpdatingJMdict { get; set; } = false;
    public static bool UpdatingJMnedict { get; set; } = false;
    public static bool UpdatingKanjidic { get; set; } = false;
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, List<JmdictWc>> WcDict { get; set; } = new();
    public static Dictionary<string, Freq> FreqDicts { get; set; } = new();

    public static readonly Dictionary<string, Dict> Dicts = new();

    public static readonly Dictionary<string, Dict> BuiltInDicts =
        new()
        {
            {
                "CustomWordDictionary",
                new Dict(DictType.CustomWordDictionary,
                    "Custom Word Dictionary",
                    $"{ResourcesPath}/custom_words.txt",
                    true, 0,
                    new DictOptions(newlineBetweenDefinitions: new() { Value = false }))
            },
            {
                "CustomNameDictionary",
                new Dict(DictType.CustomNameDictionary,
                    "Custom Name Dictionary",
                    $"{ResourcesPath}/custom_names.txt", true, 1,
                    new DictOptions())
            },
            {
                "JMdict",
                new Dict(DictType.JMdict, "JMdict", $"{ResourcesPath}/JMdict.xml", true, 2,
                    new DictOptions(
                        newlineBetweenDefinitions: new() { Value = false },
                        wordClassInfo: new() { Value = true },
                        dialectInfo: new() { Value = true },
                        pOrthographyInfo: new() { Value = true },
                        pOrthographyInfoColor: new() { Value = "#FFD2691E" },
                        pOrthographyInfoFontSize: new() { Value = 15 },
                        aOrthographyInfo: new() { Value = true },
                        rOrthographyInfo: new() { Value = true },
                        wordTypeInfo: new() { Value = true },
                        miscInfo: new() { Value = true },
                        relatedTerm: new() { Value = false },
                        antonym: new() { Value = false },
                        loanwordEtymology: new () { Value = true}
                        ))
            },
            {
                "JMnedict",
                new Dict(DictType.JMnedict, "JMnedict", $"{ResourcesPath}/JMnedict.xml", true, 3,
                    new DictOptions(newlineBetweenDefinitions: new() { Value = false }))
            },
            {
                "Kanjidic",
                new Dict(DictType.Kanjidic, "Kanjidic", $"{ResourcesPath}/kanjidic2.xml", true, 4,
                    new DictOptions(noAll: new() { Value = false }))
            }
        };

    public static readonly Dictionary<string, Freq> BuiltInFreqs = new()
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)", $"{ResourcesPath}/freqlist_vns.json", true, 0)
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)", $"{ResourcesPath}/freqlist_narou.json", false, 1)
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)", $"{ResourcesPath}/freqlist_novels.json", false, 2)
        },
    };

    public static readonly List<DictType> YomichanDictTypes = new()
    {
        DictType.Daijirin,
        DictType.Daijisen,
        DictType.Gakken,
        DictType.GakkenYojijukugoYomichan,
        DictType.IwanamiYomichan,
        DictType.JitsuyouYomichan,
        DictType.KanjigenYomichan,
        DictType.Kenkyuusha,
        DictType.KireiCakeYomichan,
        DictType.Kotowaza,
        DictType.Koujien,
        DictType.Meikyou,
        DictType.NikkokuYomichan,
        DictType.OubunshaYomichan,
        DictType.ShinjirinYomichan,
        DictType.ShinmeikaiYomichan,
        DictType.ShinmeikaiYojijukugoYomichan,
        DictType.WeblioKogoYomichan,
        DictType.ZokugoYomichan,
        DictType.PitchAccentYomichan,
        DictType.NonspecificYomichan,
    };

    public static readonly List<DictType> NazekaDictTypes = new()
    {
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificNazeka,
    };

    public static readonly List<DictType> NonspecificDictTypes = new()
    {
        DictType.NonspecificYomichan,
        DictType.NonspecificNazeka,
    };

    public static readonly List<DictType> ObsoleteDictTypes = new()
    {
        DictType.Kanjium,
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

    public static int CacheSize { get; set; } = 1000;

    public static async Task LoadDictionaries()
    {
        DictsReady = false;

        List<Task> tasks = new();
        bool dictRemoved = false;

        foreach (Dict dict in Dicts.Values.ToList())
        {
            switch (dict.Type)
            {
                case DictType.JMdict:
                    if (dict.Active && !dict.Contents.Any() && !UpdatingJMdict)
                    {
                        Task jMDictTask = Task.Run(async () =>
                            await JMdictLoader.Load(dict).ConfigureAwait(false));

                        tasks.Add(jMDictTask);
                    }

                    else if (!dict.Active && dict.Contents.Any() && !UpdatingJMdict)
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.JMnedict:
                    if (dict.Active && !dict.Contents.Any() && !UpdatingJMnedict)
                    {
                        tasks.Add(Task.Run(async () =>
                            await JMnedictLoader.Load(dict).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any() && !UpdatingJMnedict)
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }

                    break;
                case DictType.Kanjidic:
                    if (dict.Active && !dict.Contents.Any() && !UpdatingKanjidic)
                    {
                        tasks.Add(Task.Run(async () =>
                            await KanjiInfoLoader.Load(dict).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any() && !UpdatingKanjidic)
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.Kenkyuusha:
                case DictType.Daijirin:
                case DictType.Daijisen:
                case DictType.Koujien:
                case DictType.Meikyou:
                case DictType.Gakken:
                case DictType.Kotowaza:
                case DictType.IwanamiYomichan:
                case DictType.JitsuyouYomichan:
                case DictType.ShinmeikaiYomichan:
                case DictType.NikkokuYomichan:
                case DictType.ShinjirinYomichan:
                case DictType.OubunshaYomichan:
                case DictType.ZokugoYomichan:
                case DictType.WeblioKogoYomichan:
                case DictType.GakkenYojijukugoYomichan:
                case DictType.ShinmeikaiYojijukugoYomichan:
                case DictType.KanjigenYomichan:
                case DictType.KireiCakeYomichan:
                case DictType.NonspecificYomichan:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await EpwingYomichanLoader.Load(dict).ConfigureAwait(false);
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                                Utils.Logger.Error("Couldn't import {DictType}: {Exception}", dict.Type, ex.ToString());
                                Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (!dict.Active && dict.Contents.Any())
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.CustomWordDictionary:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                            await CustomWordLoader.Load(dict.Path).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any())
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.CustomNameDictionary:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                            await CustomNameLoader.Load(dict.Path).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any())
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.DaijirinNazeka:
                case DictType.KenkyuushaNazeka:
                case DictType.ShinmeikaiNazeka:
                case DictType.NonspecificNazeka:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                            await EpwingNazekaLoader.Load(dict).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any())
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.PitchAccentYomichan:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                            await PitchLoader.Load(dict).ConfigureAwait(false)));
                    }

                    else if (!dict.Active && dict.Contents.Any())
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

            Storage.Frontend.InvalidateDisplayCache();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
        }

        DictsReady = true;
    }

    public static async Task LoadFrequencies()
    {
        FreqsReady = false;

        List<Task> tasks = new();
        bool freqRemoved = false;

        foreach (Freq freq in FreqDicts.Values.ToList())
        {
            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    if (freq.Active && !freq.Contents.Any())
                    {
                        Task nazekaFreqTask = Task.Run(async () =>
                            await FrequencyNazekaLoader.Load(freq).ConfigureAwait(false));

                        tasks.Add(nazekaFreqTask);
                    }

                    else if (!freq.Active && freq.Contents.Any())
                    {
                        freq.Contents.Clear();
                        freqRemoved = true;
                    }
                    break;

                case FreqType.Yomichan:
                    if (freq.Active && !freq.Contents.Any())
                    {
                        Task yomichanFreqTask = Task.Run(async () =>
                        {
                            try
                            {
                                await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                Utils.Logger.Error("Couldn't import {FreqName}: {Exception}", freq.Name, ex.ToString());
                                FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(yomichanFreqTask);
                    }

                    else if (!freq.Active && freq.Contents.Any())
                    {
                        freq.Contents.Clear();
                        freqRemoved = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (tasks.Any() || freqRemoved)
        {
            if (tasks.Any())
            {
                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
            }

            Storage.Frontend.InvalidateDisplayCache();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
        }

        FreqsReady = true;
    }

    public static async Task InitializePoS()
    {
        Dict dict = Dicts.Values.First(dict => dict.Type == DictType.JMdict);
        if (!File.Exists($"{Storage.ResourcesPath}/PoS.json"))
        {
            if (dict.Active)
            {
                await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
            }

            else
            {
                bool deleteJmdictFile = false;
                if (!File.Exists(dict.Path))
                {
                    deleteJmdictFile = true;
                    await ResourceUpdater.UpdateResource(dict.Path,
                        JmdictUrl,
                        dict.Type.ToString(), false, true).ConfigureAwait(false);
                }

                await Task.Run((async () =>
                    await JMdictLoader.Load(dict).ConfigureAwait(false)));
                await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);
                dict.Contents.Clear();

                if (deleteJmdictFile)
                    File.Delete(dict.Path);
            }
        }

        await JmdictWcLoader.Load().ConfigureAwait(false);
    }
}
