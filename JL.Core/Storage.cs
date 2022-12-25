using System.Diagnostics;
using System.Runtime;
using System.Text.RegularExpressions;
using JL.Core.Anki;
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
using JL.Core.Dicts.YomichanKanji;
using JL.Core.Freqs;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.PitchAccent;
using JL.Core.Utilities;
using JL.Core.WordClass;
using Timer = System.Timers.Timer;

namespace JL.Core;

public static class Storage
{
    public static Timer Timer { get; } = new();
    public static Stopwatch StatsStopWatch { get; } = new();
    public const string Jpod101NoAudioMd5Hash = "7e2c2f954ef6051373ba916f000168dc";
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
    public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
    public static readonly HttpClient Client = new(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromMinutes(10) };
    public static readonly Version JLVersion = new(1, 16, 1);
    public static readonly Uri GitHubApiUrlForLatestJLRelease = new("https://api.github.com/repos/rampaa/JL/releases/latest");
    public static readonly Uri JmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e.gz");
    public static readonly Uri JmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    public static readonly Uri KanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");
    public static bool DictsReady { get; private set; } = false;
    public static bool UpdatingJMdict { get; set; } = false;
    public static bool UpdatingJMnedict { get; set; } = false;
    public static bool UpdatingKanjidic { get; set; } = false;
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, List<JmdictWordClass>> WordClassDictionary { get; set; } = new(65536); // 2022/10/29: 48909
    public static readonly Dictionary<string, string> KanjiCompositionDict = new(86934);
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
                    true, 0, 128,
                    new DictOptions(newlineBetweenDefinitions: new() { Value = false }))
            },
            {
                "CustomNameDictionary",
                new Dict(DictType.CustomNameDictionary,
                    "Custom Name Dictionary",
                    $"{ResourcesPath}/custom_names.txt", true, 1, 128,
                    new DictOptions())
            },
            {
                "JMdict",
                new Dict(DictType.JMdict, "JMdict", $"{ResourcesPath}/JMdict.xml", true, 2, 500000,
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
                new Dict(DictType.JMnedict, "JMnedict", $"{ResourcesPath}/JMnedict.xml", true, 3, 700000,
                    new DictOptions(newlineBetweenDefinitions: new() { Value = false }))
            },
            {
                "Kanjidic",
                new Dict(DictType.Kanjidic, "Kanjidic", $"{ResourcesPath}/kanjidic2.xml", true, 4, 13108,
                    new DictOptions(noAll: new() { Value = false }))
            }
        };

    public static readonly Dictionary<string, Freq> BuiltInFreqs = new()
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)", $"{ResourcesPath}/freqlist_vns.json", true, 0, 57273)
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)", $"{ResourcesPath}/freqlist_narou.json", false, 1, 75588)
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)", $"{ResourcesPath}/freqlist_novels.json", false, 2, 114348)
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
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan
    };

    public static readonly List<DictType> NazekaDictTypes = new()
    {
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka,
    };

    public static readonly List<DictType> NonspecificDictTypes = new()
    {
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    };

    public static readonly List<DictType> KanjiDictTypes = new()
    {
        DictType.Kanjidic,
        DictType.KanjigenYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiNazeka
    };

    public static readonly List<DictType> NameDictTypes = new()
    {
        DictType.CustomNameDictionary,
        DictType.JMnedict,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificNameNazeka,
    };

    public static readonly List<DictType> WordDictTypes = new()
    {
        DictType.CustomWordDictionary,
        DictType.JMdict,
        DictType.Daijirin,
        DictType.Daijisen,
        DictType.Gakken,
        DictType.GakkenYojijukugoYomichan,
        DictType.IwanamiYomichan,
        DictType.JitsuyouYomichan,
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
        DictType.NonspecificWordYomichan,
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificWordNazeka
    };

    public static readonly List<JLField> JLFieldsForWordDicts = new()
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.AlternativeSpellings,
        JLField.Readings,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.SourceText,
        JLField.Sentence,
        JLField.MatchedText,
        JLField.DeconjugatedMatchedText,
        JLField.DeconjugationProcess,
        JLField.Frequencies,
        JLField.EdictId,
        JLField.LocalTime,
    };

    public static readonly List<JLField> JLFieldsForKanjiDicts = new()
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.KunReadings,
        JLField.OnReadings,
        JLField.NanoriReadings,
        JLField.StrokeCount,
        JLField.KanjiGrade,
        JLField.KanjiComposition,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.SourceText,
        JLField.Sentence,
        JLField.Frequencies,
        JLField.EdictId,
        JLField.LocalTime,
    };

    public static readonly List<JLField> JLFieldsForNameDicts = new()
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.AlternativeSpellings,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.SourceText,
        JLField.Sentence,
        JLField.EdictId,
        JLField.LocalTime,
    };

    // Matches the following Unicode ranges:
    // CJK Radicals Supplement (2E80–2EFF)
    // Kangxi Radicals (2F00–2FDF)
    // Ideographic Description Characters (2FF0–2FFF)
    // CJK Symbols and Punctuation (3000–303F)
    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Kanbun (3190–319F)
    // CJK Strokes (31C0–31EF)
    // Katakana Phonetic Extensions (31F0–31FF)
    // Enclosed CJK Letters and Months (3200–32FF)
    // CJK Compatibility (3300–33FF)
    // CJK Unified Ideographs Extension A (3400–4DBF)
    // CJK Unified Ideographs (4E00–9FFF)
    // CJK Compatibility Ideographs (F900–FAFF)
    // CJK Compatibility Forms (FE30–FE4F)
    // CJK Unified Ideographs Extension B (20000–2A6DF)
    // CJK Unified Ideographs Extension C (2A700–2B73F)
    // CJK Unified Ideographs Extension D (2B740–2B81F)
    // CJK Unified Ideographs Extension E (2B820–2CEAF)
    // CJK Unified Ideographs Extension F (2CEB0–2EBEF)
    // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
    // CJK Unified Ideographs Extension G (30000–3134F)
    // CJK Unified Ideographs Extension H (31350–323AF)
    public static readonly Regex JapaneseRegex =
        new(
            @"[\u2e80-\u30ff\u3190–\u319f\u31c0-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|\ud82c[\udc00-\udcff]|\ud83c[\ude00-\udeff]|\ud840[\udc00-\udfff]|[\ud841-\ud868][\udc00-\udfff]|\ud869[\udc00-\udedf]|\ud869[\udf00-\udfff]|[\ud86a-\ud879][\udc00-\udfff]|\ud87a[\udc00-\udfef]|\ud87e[\udc00-\ude1f]|\ud880[\udc00-\udfff]|[\ud881-\ud883][\udc00-\udfff]|\ud884[\udc00-\udfff]|[\ud885-\ud887][\udc00-\udfff]|\ud888[\udc00-\udfaf]",
            RegexOptions.Compiled);

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

    public static readonly Dictionary<string, string> JmdictEntities = new();

    public static readonly Dictionary<string, string> JmnedictEntities = new()
    {
        #pragma warning disable format
        {"char", "character"}, {"company", "company name"}, {"creat", "creature"}, {"dei", "deity"},
        {"doc", "document"}, {"ev", "event"}, {"fem", "female given name or forename"}, {"fict", "fiction"},
        {"given", "given name or forename, gender not specified"},
        {"group", "group"}, {"leg", "legend"}, {"masc", "male given name or forename"}, {"myth", "mythology"},
        {"obj", "object"}, {"organization", "organization name"}, {"oth", "other"}, {"person", "full name of a particular person"},
        {"place", "place name"}, {"product", "product name"}, {"relig", "religion"}, {"serv", "service"},
        {"station", "railway station"}, {"surname", "family or surname"}, {"unclass", "unclassified name"}, {"work", "work of art, literature, music, etc. name"},
        #pragma warning restore format
    };

    public static int CacheSize { get; set; } = 1000;

    public static async Task LoadDictionaries(bool runGC = true)
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
                        {
                            await JmdictLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        });

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
                        {
                            await JmnedictLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
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
                        {
                            await KanjidicLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
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
                case DictType.NonspecificWordYomichan:
                case DictType.NonspecificNameYomichan:
                case DictType.NonspecificYomichan:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await EpwingYomichanLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
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

                case DictType.NonspecificKanjiYomichan:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await YomichanKanjiLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
                                Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }
                    break;

                case DictType.CustomWordDictionary:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await CustomWordLoader.Load(dict.Path).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
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
                        {
                            await CustomNameLoader.Load(dict.Path).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
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
                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificKanjiNazeka:
                case DictType.NonspecificNameNazeka:
                case DictType.NonspecificNazeka:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await EpwingNazekaLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
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

                case DictType.PitchAccentYomichan:
                    if (dict.Active && !dict.Contents.Any())
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await PitchAccentLoader.Load(dict).ConfigureAwait(false);
                                dict.Size = dict.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {DictType}", dict.Type);
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

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid dict type");
            }
        }

        if (tasks.Any() || dictRemoved)
        {
            if (tasks.Any())
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            Frontend.InvalidateDisplayCache();

            if (runGC)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }
        }

        DictsReady = true;
    }

    public static async Task LoadFrequencies(bool runGC = true)
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
                        {
                            try
                            {
                                await FrequencyNazekaLoader.Load(freq).ConfigureAwait(false);
                                freq.Size = freq.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(nazekaFreqTask);
                    }

                    else if (!freq.Active && freq.Contents.Any())
                    {
                        freq.Contents.Clear();
                        freqRemoved = true;
                    }
                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    if (freq.Active && !freq.Contents.Any())
                    {
                        Task yomichanFreqTask = Task.Run(async () =>
                        {
                            try
                            {
                                await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                                freq.Size = freq.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
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
                    throw new ArgumentOutOfRangeException(null, "Invalid freq type");
            }
        }

        if (tasks.Any() || freqRemoved)
        {
            if (tasks.Any())
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            Frontend.InvalidateDisplayCache();

            if (runGC)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }
        }

        FreqsReady = true;
    }

    public static async Task InitializeWordClassDictionary()
    {
        Dict dict = Dicts.Values.First(dict => dict.Type == DictType.JMdict);
        if (!File.Exists($"{ResourcesPath}/PoS.json"))
        {
            if (dict.Active)
            {
                await JmdictWordClassUtils.SerializeJmdictWordClass().ConfigureAwait(false);
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

                await Task.Run(async () =>
                    await JmdictLoader.Load(dict).ConfigureAwait(false));
                await JmdictWordClassUtils.SerializeJmdictWordClass().ConfigureAwait(false);
                dict.Contents.Clear();

                if (deleteJmdictFile)
                    File.Delete(dict.Path);
            }
        }

        await JmdictWordClassUtils.Load().ConfigureAwait(false);
    }

    public static async Task InitializeKanjiCompositionDict()
    {
        if (File.Exists($"{ResourcesPath}/ids.txt"))
        {
            string[] lines = await File
                .ReadAllLinesAsync($"{ResourcesPath}/ids.txt")
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t");

                if (lParts.Length == 3)
                {
                    int endIndex = lParts[2].IndexOf("[", StringComparison.Ordinal);

                    KanjiCompositionDict.Add(lParts[1],
                        endIndex == -1 ? lParts[2] : lParts[2][..endIndex]);
                }

                else if (lParts.Length > 3)
                {
                    for (int j = 2; j < lParts.Length; j++)
                    {
                        if (lParts[j].Contains('J'))
                        {
                            int endIndex = lParts[j].IndexOf("[", StringComparison.Ordinal);
                            if (endIndex != -1)
                            {
                                KanjiCompositionDict.Add(lParts[1], lParts[j][..endIndex]);
                                break;
                            }
                        }
                    }
                }
            }

            KanjiCompositionDict.TrimExcess();
        }
    }

}
