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
    internal static Timer Timer { get; } = new();
    public static Stopwatch StatsStopWatch { get; } = new();
    internal const string Jpod101NoAudioMd5Hash = "7E-2C-2F-95-4E-F6-05-13-73-BA-91-6F-00-01-68-DC";
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
    public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
    public static readonly HttpClient Client = new(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromMinutes(10) };
    public static readonly Version JLVersion = new(1, 16, 5);
    internal static readonly Uri s_gitHubApiUrlForLatestJLRelease = new("https://api.github.com/repos/rampaa/JL/releases/latest");
    public static readonly Uri JmdictUrl = new("https://www.edrdg.org/pub/Nihongo/JMdict_e.gz");
    public static readonly Uri JmnedictUrl = new("https://www.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
    public static readonly Uri KanjidicUrl = new("https://www.edrdg.org/kanjidic/kanjidic2.xml.gz");
    public static bool DictsReady { get; private set; } = false;
    public static bool UpdatingJMdict { get; set; } = false;
    public static bool UpdatingJMnedict { get; set; } = false;
    public static bool UpdatingKanjidic { get; set; } = false;
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, List<JmdictWordClass>> WordClassDictionary { get; internal set; } = new(65536); // 2022/10/29: 48909
    internal static readonly Dictionary<string, string> s_kanjiCompositionDict = new(86934);
    public static Dictionary<string, Freq> FreqDicts { get; internal set; } = new();

    public static readonly Dictionary<string, Dict> Dicts = new();

    public static readonly Dictionary<string, Dict> BuiltInDicts =
        new()
        {
            {
                "CustomWordDictionary",
                new Dict(DictType.CustomWordDictionary,
                    "Custom Word Dictionary",
                    $"{ResourcesPath}/custom_words.txt",
                    true, 1, 128,
                    new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }))
            },
            {
                "CustomNameDictionary",
                new Dict(DictType.CustomNameDictionary,
                    "Custom Name Dictionary",
                    $"{ResourcesPath}/custom_names.txt", true, 2, 128,
                    new DictOptions())
            },
            {
                "JMdict",
                new Dict(DictType.JMdict, "JMdict", $"{ResourcesPath}/JMdict.xml", true, 3, 500000,
                    new DictOptions(
                        new NewlineBetweenDefinitionsOption { Value = false },
                        wordClassInfo: new WordClassInfoOption { Value = true },
                        dialectInfo: new DialectInfoOption { Value = true },
                        pOrthographyInfo: new POrthographyInfoOption { Value = true },
                        pOrthographyInfoColor: new POrthographyInfoColorOption { Value = "#FFD2691E" },
                        pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption { Value = 15 },
                        aOrthographyInfo: new AOrthographyInfoOption { Value = true },
                        rOrthographyInfo: new ROrthographyInfoOption { Value = true },
                        wordTypeInfo: new WordTypeInfoOption { Value = true },
                        miscInfo: new MiscInfoOption { Value = true },
                        relatedTerm: new RelatedTermOption { Value = false },
                        antonym: new AntonymOption { Value = false },
                        loanwordEtymology: new LoanwordEtymologyOption { Value = true}
                        ))
            },
            {
                "Kanjidic",
                new Dict(DictType.Kanjidic, "Kanjidic", $"{ResourcesPath}/kanjidic2.xml", true, 4, 13108,
                    new DictOptions(noAll: new NoAllOption { Value = false }))
            },
            {
                "JMnedict",
                new Dict(DictType.JMnedict, "JMnedict", $"{ResourcesPath}/JMnedict.xml", true, 5, 700000,
                    new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }))
            }
        };

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new()
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)", $"{ResourcesPath}/freqlist_vns.json", true, 1, 57273)
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)", $"{ResourcesPath}/freqlist_narou.json", false, 2, 75588)
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)", $"{ResourcesPath}/freqlist_novels.json", false, 3, 114348)
        }
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
        DictType.NonspecificNazeka
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

    internal static readonly List<DictType> s_kanjiDictTypes = new()
    {
        DictType.Kanjidic,
        DictType.KanjigenYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiNazeka
    };

    internal static readonly List<DictType> s_nameDictTypes = new()
    {
        DictType.CustomNameDictionary,
        DictType.JMnedict,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificNameNazeka
    };

    internal static readonly List<DictType> s_wordDictTypes = new()
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
        JLField.LocalTime
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
        JLField.LocalTime
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
        JLField.LocalTime
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
    internal static readonly List<string> s_japanesePunctuation =
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

    public const int CacheSize = 1000;

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
                    if (dict is { Active: true, Contents.Count: 0 } && !UpdatingJMdict)
                    {
                        Task jMDictTask = Task.Run(async () =>
                        {
                            await JmdictLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        });

                        tasks.Add(jMDictTask);
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 } && !UpdatingJMdict)
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.JMnedict:
                    if (dict is { Active: true, Contents.Count: 0 } && !UpdatingJMnedict)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await JmnedictLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 } && !UpdatingJMnedict)
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }

                    break;
                case DictType.Kanjidic:
                    if (dict is { Active: true, Contents.Count: 0 } && !UpdatingKanjidic)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await KanjidicLoader.Load(dict).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 } && !UpdatingKanjidic)
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
                    if (dict is { Active: true, Contents.Count: 0 })
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
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.NonspecificKanjiYomichan:
                    if (dict is { Active: true, Contents.Count: 0 })
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
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.CustomWordDictionary:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await CustomWordLoader.Load(dict.Path).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.CustomNameDictionary:
                    if (dict is { Active: true, Contents.Count: 0 })
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await CustomNameLoader.Load(dict.Path).ConfigureAwait(false);
                            dict.Size = dict.Contents.Count;
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
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
                    if (dict is { Active: true, Contents.Count: 0 })
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
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                case DictType.PitchAccentYomichan:
                    if (dict is { Active: true, Contents.Count: 0 })
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
                                _ = Dicts.Remove(dict.Name);
                                dictRemoved = true;
                            }
                        }));
                    }

                    else if (dict is { Active: false, Contents.Count: > 0 })
                    {
                        dict.Contents.Clear();
                        dictRemoved = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid dict type");
            }
        }

        if (tasks.Count > 0 || dictRemoved)
        {
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            if (dictRemoved)
            {
                IOrderedEnumerable<Dict> orderedDicts = Dicts.Values.OrderBy(d => d.Priority);
                int priority = 1;

                foreach (Dict dict in orderedDicts)
                {
                    dict.Priority = priority;
                    ++priority;
                }
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
                    if (freq is { Active: true, Contents.Count: 0 })
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
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(nazekaFreqTask);
                    }

                    else if (freq is { Active: false, Contents.Count: > 0 })
                    {
                        freq.Contents.Clear();
                        freqRemoved = true;
                    }
                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    if (freq is { Active: true, Contents.Count: 0 })
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
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(yomichanFreqTask);
                    }

                    else if (freq is { Active: false, Contents.Count: > 0 })
                    {
                        freq.Contents.Clear();
                        freqRemoved = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid freq type");
            }
        }

        if (tasks.Count > 0 || freqRemoved)
        {
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            if (freqRemoved)
            {
                IOrderedEnumerable<Freq> orderedFreqs = FreqDicts.Values.OrderBy(f => f.Priority);
                int priority = 1;

                foreach(Freq freq in orderedFreqs)
                {
                    freq.Priority = priority;
                    ++priority;
                }
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

    internal static async Task InitializeWordClassDictionary()
    {
        Dict dict = Dicts.Values.First(static dict => dict.Type is DictType.JMdict);
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
                    _ = await ResourceUpdater.UpdateResource(dict.Path,
                        JmdictUrl,
                        dict.Type.ToString(), false, true).ConfigureAwait(false);
                }

                await Task.Run(async () =>
                    await JmdictLoader.Load(dict).ConfigureAwait(false)).ConfigureAwait(false);
                await JmdictWordClassUtils.SerializeJmdictWordClass().ConfigureAwait(false);
                dict.Contents.Clear();

                if (deleteJmdictFile)
                {
                    File.Delete(dict.Path);
                }
            }
        }

        await JmdictWordClassUtils.Load().ConfigureAwait(false);
    }

    internal static async Task InitializeKanjiCompositionDict()
    {
        if (File.Exists($"{ResourcesPath}/ids.txt"))
        {
            string[] lines = await File
                .ReadAllLinesAsync($"{ResourcesPath}/ids.txt")
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t");

                if (lParts.Length is 3)
                {
                    int endIndex = lParts[2].IndexOf("[", StringComparison.Ordinal);

                    s_kanjiCompositionDict.Add(lParts[1],
                        endIndex is -1 ? lParts[2] : lParts[2][..endIndex]);
                }

                else if (lParts.Length > 3)
                {
                    for (int j = 2; j < lParts.Length; j++)
                    {
                        if (lParts[j].Contains('J'))
                        {
                            int endIndex = lParts[j].IndexOf("[", StringComparison.Ordinal);
                            if (endIndex is not -1)
                            {
                                s_kanjiCompositionDict.Add(lParts[1], lParts[j][..endIndex]);
                                break;
                            }
                        }
                    }
                }
            }

            s_kanjiCompositionDict.TrimExcess();
        }
    }

}
