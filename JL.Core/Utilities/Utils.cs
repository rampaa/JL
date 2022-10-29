using System.Runtime;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using JL.Core.Dicts;
using JL.Core.Frequency;
using JL.Core.Network;
using Serilog;
using Serilog.Core;

namespace JL.Core.Utilities;

public static class Utils
{
    public static readonly Logger Logger = new LoggerConfiguration().WriteTo.File("Logs/log.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(90),
            shared: true)
        .CreateLogger();

    public static readonly Dictionary<string, string> Iso6392BTo2T = new()
    {
        #pragma warning disable format
        { "tib", "bod" }, { "cze", "ces" }, { "wel", "cym" }, { "ger", "deu" }, { "gre", "ell" },
        { "baq", "eus" }, { "per", "fas" }, { "fre", "fra" }, { "arm", "hye" }, { "ice", "isl" },
        { "geo", "kat" }, { "mac", "mkd" }, { "mao", "mri" }, { "may", "msa" }, { "bur", "mya" },
        { "dut", "nld" }, { "rum", "ron" }, { "slo", "slk" }, { "alb", "sqi" }, { "chi", "zho" }
        #pragma warning restore format
    };

    public static void CreateDefaultDictsConfig()
    {
        var jso = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), },
        };

        try
        {
            Directory.CreateDirectory(Storage.ConfigPath);
            File.WriteAllText(Path.Join(Storage.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(Storage.BuiltInDicts, jso));
        }
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write default Dicts config");
            Logger.Error(e, "Couldn't write default Dicts config");
        }
    }

    public static void CreateDefaultFreqsConfig()
    {
        var jso = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), },
        };

        try
        {
            Directory.CreateDirectory(Storage.ConfigPath);
            File.WriteAllText(Path.Join(Storage.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(Storage.BuiltInFreqs, jso));
        }
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write default Freqs config");
            Logger.Error(e, "Couldn't write default Freqs config");
        }
    }

    public static async Task SerializeDicts()
    {
        try
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(), },
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            await File.WriteAllTextAsync(Path.Join(Storage.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(Storage.Dicts, jso)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "SerializeDicts failed");
            throw;
        }
    }

    public static async Task SerializeFreqs()
    {
        try
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            await File.WriteAllTextAsync(Path.Join(Storage.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(Storage.FreqDicts, jso)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "SerializeFreqs failed");
            throw;
        }
    }

    private static async Task DeserializeDicts()
    {
        try
        {
            var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(), }, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            using Stream dictStream = new StreamReader(Path.Join(Storage.ConfigPath, "dicts.json")).BaseStream;

            Dictionary<string, Dict>? deserializedDicts = await JsonSerializer
                .DeserializeAsync<Dictionary<string, Dict>>(dictStream, jso).ConfigureAwait(false);

            if (deserializedDicts != null)
            {
                foreach (Dict dict in deserializedDicts.Values)
                {
                    if (!Storage.Dicts.ContainsKey(dict.Name))
                    {
                        dict.Contents = dict.Size != 0
                            ? new Dictionary<string, List<IResult>>(dict.Size)
                            : dict.Type switch
                            {
                                DictType.CustomNameDictionary => new Dictionary<string, List<IResult>>(1024),
                                DictType.CustomWordDictionary => new Dictionary<string, List<IResult>>(1024),
                                DictType.JMdict => new Dictionary<string, List<IResult>>(500000), //2022/05/11: 394949, 2022/08/15: 398303
                                DictType.JMnedict => new Dictionary<string, List<IResult>>(700000), //2022/05/11: 608833, 2022/08/15: 609117
                                DictType.Kanjidic => new Dictionary<string, List<IResult>>(13108), //2022/05/11: 13108, 2022/08/15: 13108
                                DictType.Daijirin => new Dictionary<string, List<IResult>>(420429),
                                DictType.DaijirinNazeka => new Dictionary<string, List<IResult>>(420429),
                                DictType.Daijisen => new Dictionary<string, List<IResult>>(679115),
                                DictType.Gakken => new Dictionary<string, List<IResult>>(254558),
                                DictType.GakkenYojijukugoYomichan => new Dictionary<string, List<IResult>>(7989),
                                DictType.IwanamiYomichan => new Dictionary<string, List<IResult>>(101929),
                                DictType.JitsuyouYomichan => new Dictionary<string, List<IResult>>(69746),
                                DictType.KanjigenYomichan => new Dictionary<string, List<IResult>>(64730),
                                DictType.Kenkyuusha => new Dictionary<string, List<IResult>>(303677),
                                DictType.KenkyuushaNazeka => new Dictionary<string, List<IResult>>(191804),
                                DictType.KireiCakeYomichan => new Dictionary<string, List<IResult>>(332628),
                                DictType.Kotowaza => new Dictionary<string, List<IResult>>(30846),
                                DictType.Koujien => new Dictionary<string, List<IResult>>(402571),
                                DictType.Meikyou => new Dictionary<string, List<IResult>>(107367),
                                DictType.NikkokuYomichan => new Dictionary<string, List<IResult>>(451455),
                                DictType.OubunshaYomichan => new Dictionary<string, List<IResult>>(138935),
                                DictType.PitchAccentYomichan => new Dictionary<string, List<IResult>>(434991),
                                DictType.ShinjirinYomichan => new Dictionary<string, List<IResult>>(229758),
                                DictType.ShinmeikaiYomichan => new Dictionary<string, List<IResult>>(126049),
                                DictType.ShinmeikaiNazeka => new Dictionary<string, List<IResult>>(126049),
                                DictType.ShinmeikaiYojijukugoYomichan => new Dictionary<string, List<IResult>>(6088),
                                DictType.WeblioKogoYomichan => new Dictionary<string, List<IResult>>(30838),
                                DictType.ZokugoYomichan => new Dictionary<string, List<IResult>>(2392),
                                DictType.NonspecificWordYomichan => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificKanjiYomichan => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificNameYomichan => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificYomichan => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificWordNazeka => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificKanjiNazeka => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificNameNazeka => new Dictionary<string, List<IResult>>(250000),
                                DictType.NonspecificNazeka => new Dictionary<string, List<IResult>>(250000),
                                _ => new Dictionary<string, List<IResult>>(250000),
                            };

                        Storage.Dicts.Add(dict.Name, dict);
                    }
                }
            }
            else
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                Utils.Logger.Error("Couldn't load Config/dicts.json");
            }
        }
        catch (Exception e)
        {
            Utils.Logger.Fatal(e, "DeserializeDicts failed");
            throw;
        }
    }

    private static async Task DeserializeFreqs()
    {
        try
        {
            var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(), }, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            using Stream freqStream = new StreamReader(Path.Join(Storage.ConfigPath, "freqs.json")).BaseStream;

            Dictionary<string, Freq>? deserializedFreqs = await JsonSerializer
                .DeserializeAsync<Dictionary<string, Freq>>(freqStream, jso).ConfigureAwait(false);

            if (deserializedFreqs != null)
            {
                foreach (Freq freq in deserializedFreqs.Values)
                {
                    if (!Storage.FreqDicts.ContainsKey(freq.Name))
                    {
                        freq.Contents = freq.Size != 0
                            ? new Dictionary<string, List<FrequencyRecord>>(freq.Size)
                            : freq.Type switch
                            {
                                FreqType.Yomichan => new Dictionary<string, List<FrequencyRecord>>(1504512),
                                FreqType.YomichanKanji => new Dictionary<string, List<FrequencyRecord>>(169623),
                                FreqType.Nazeka => new Dictionary<string, List<FrequencyRecord>>(114348),
                                _ => new Dictionary<string, List<FrequencyRecord>>(500000),
                            };
                        Storage.FreqDicts.Add(freq.Name, freq);
                    }
                }
            }
            else
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/freqs.json");
                Utils.Logger.Error("Couldn't load Config/freqs.json");
            }
        }
        catch (Exception e)
        {
            Utils.Logger.Fatal(e, "DeserializeFreqs failed");
            throw;
        }
    }

    public static string GetMd5String(byte[] bytes)
    {
        byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")!).ComputeHash(bytes);
        string encoded = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

        return encoded;
    }

    public static int FindWordBoundary(string text, int position)
    {
        int endPosition = -1;

        for (int i = 0; i < Storage.JapanesePunctuation.Count; i++)
        {
            int tempIndex = text.IndexOf(Storage.JapanesePunctuation[i], position, StringComparison.Ordinal);

            if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                endPosition = tempIndex;
        }

        if (endPosition == -1)
            endPosition = text.Length;

        return endPosition;
    }

    public static string FindSentence(string text, int position)
    {
        List<string> japanesePunctuationLite = new()
        {
            "。",
            "！",
            "？",
            "…",
            ".",
            "\n",
        };

        Dictionary<string, string> japaneseParentheses = new() { { "「", "」" }, { "『", "』" }, { "（", "）" }, };

        int startPosition = -1;
        int endPosition = -1;

        for (int i = 0; i < japanesePunctuationLite.Count; i++)
        {
            string punctuation = japanesePunctuationLite[i];

            int tempIndex = text.LastIndexOf(punctuation, position, StringComparison.Ordinal);

            if (tempIndex > startPosition)
                startPosition = tempIndex;

            tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

            if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                endPosition = tempIndex;
        }

        ++startPosition;

        if (endPosition == -1)
            endPosition = text.Length - 1;

        string sentence = startPosition < endPosition
            ? text[startPosition..(endPosition + 1)].Trim('\n', '\t', '\r', ' ', '　')
            : "";

        if (sentence.Length > 1)
        {
            if (japaneseParentheses.ContainsValue(sentence.First().ToString()))
            {
                sentence = sentence[1..];
            }

            if (japaneseParentheses.ContainsKey(sentence.LastOrDefault().ToString()))
            {
                sentence = sentence[..^1];
            }

            if (japaneseParentheses.TryGetValue(sentence.FirstOrDefault().ToString(), out string? rightParenthesis))
            {
                if (sentence.Last().ToString() == rightParenthesis)
                    sentence = sentence[1..^1];

                else if (!sentence.Contains(rightParenthesis))
                    sentence = sentence[1..];

                else if (sentence.Contains(rightParenthesis))
                {
                    int numberOfLeftParentheses = sentence.Count(p => p == sentence[0]);
                    int numberOfRightParentheses = sentence.Count(p => p == rightParenthesis[0]);

                    if (numberOfLeftParentheses == numberOfRightParentheses + 1)
                        sentence = sentence[1..];
                }
            }

            else if (japaneseParentheses.ContainsValue(sentence.LastOrDefault().ToString()))
            {
                string leftParenthesis = japaneseParentheses.First(p => p.Value == sentence.Last().ToString()).Key;

                if (!sentence.Contains(leftParenthesis))
                    sentence = sentence[..^1];

                else if (sentence.Contains(leftParenthesis))
                {
                    int numberOfLeftParentheses = sentence.Count(p => p == leftParenthesis[0]);
                    int numberOfRightParentheses = sentence.Count(p => p == sentence.Last());

                    if (numberOfRightParentheses == numberOfLeftParentheses + 1)
                        sentence = sentence[..^1];
                }
            }
        }

        return sentence;
    }

    public static async Task GetAndPlayAudioFromJpod101(string foundSpelling, string? reading, float volume)
    {
        Utils.Logger.Information("Attempting to play audio from jpod101: {FoundSpelling} {Reading}", foundSpelling, reading);

        if (string.IsNullOrEmpty(reading))
            reading = foundSpelling;

        byte[]? sound = await Networking.GetAudioFromJpod101(foundSpelling, reading).ConfigureAwait(false);
        if (sound != null)
        {
            if (Utils.GetMd5String(sound) == Storage.Jpod101NoAudioMd5Hash)
            {
                // TODO sound = shortErrorSound
                return;
            }

            Storage.Frontend.PlayAudio(sound, volume);
            Stats.IncrementStat(StatType.TimesPlayedAudio);
        }
    }

    public static async Task CoreInitialize()
    {
        SetTimer();

        Storage.StatsStopWatch.Start();

        if (!File.Exists($"{Storage.ConfigPath}/dicts.json"))
            CreateDefaultDictsConfig();

        if (!File.Exists($"{Storage.ConfigPath}/freqs.json"))
            CreateDefaultFreqsConfig();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_words.txt"))
            await File.Create($"{Storage.ResourcesPath}/custom_words.txt").DisposeAsync();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_names.txt"))
            await File.Create($"{Storage.ResourcesPath}/custom_names.txt").DisposeAsync();

        Task[] tasks = new Task[2];
        tasks[0] = Task.Run(async () =>
        {
            await DeserializeDicts().ConfigureAwait(false);
            await Storage.LoadDictionaries(false).ConfigureAwait(false);
            await SerializeDicts().ConfigureAwait(false);
            await Storage.InitializePoS().ConfigureAwait(false);
        });

        tasks[1] = Task.Run(async () =>
        {
            await DeserializeFreqs().ConfigureAwait(false);
            await Storage.LoadFrequencies(false).ConfigureAwait(false);
        });

        await Storage.InitializeKanjiCompositionDict().ConfigureAwait(false);
        await Task.WhenAll(tasks).ConfigureAwait(false);

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
    }

    private static void SetTimer()
    {
        Storage.Timer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
        Storage.Timer.Elapsed += OnTimedEvent;
        Storage.Timer.AutoReset = true;
        Storage.Timer.Enabled = true;
    }

    private static async void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        Stats.IncrementStat(StatType.Time, Storage.StatsStopWatch.ElapsedTicks);

        if (Storage.StatsStopWatch.IsRunning)
        {
            Storage.StatsStopWatch.Restart();
        }

        else
        {
            Storage.StatsStopWatch.Reset();
        }

        await Stats.UpdateLifetimeStats().ConfigureAwait(false);
    }

    public static List<string>? TrimStringList(List<string> list)
    {
        List<string>? listClone = list;

        if (!listClone.Any() || listClone.All(string.IsNullOrEmpty))
            listClone = null;
        else
            listClone.TrimExcess();

        return listClone;
    }
}
