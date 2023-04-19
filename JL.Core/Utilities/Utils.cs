using System.Globalization;
using System.Runtime;
using System.Security.Cryptography;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Statistics;
using Serilog;
using Serilog.Core;

namespace JL.Core.Utilities;

public static class Utils
{
    public static readonly Version JLVersion = new(1, 18, 0);
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
    public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
    public const int CacheSize = 1000;

    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new() { MinimumLevel = Serilog.Events.LogEventLevel.Error };

    public static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(LoggingLevelSwitch)
        .WriteTo.File("Logs/log.txt",
            formatProvider: CultureInfo.InvariantCulture,
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(90),
            shared: true)
        .CreateLogger();

    internal static readonly Dictionary<string, string> s_iso6392BTo2T = new()
    {
        #pragma warning disable format
        { "tib", "bod" }, { "cze", "ces" }, { "wel", "cym" }, { "ger", "deu" }, { "gre", "ell" },
        { "baq", "eus" }, { "per", "fas" }, { "fre", "fra" }, { "arm", "hye" }, { "ice", "isl" },
        { "geo", "kat" }, { "mac", "mkd" }, { "mao", "mri" }, { "may", "msa" }, { "bur", "mya" },
        { "dut", "nld" }, { "rum", "ron" }, { "slo", "slk" }, { "alb", "sqi" }, { "chi", "zho" }
        #pragma warning restore format
    };

#pragma warning disable CA5351
    internal static string GetMd5String(byte[] bytes)
    {

        byte[] hash = MD5.HashData(bytes);
        string encoded = BitConverter.ToString(hash);

        return encoded;
    }
#pragma warning restore CA5351

    public static async Task CoreInitialize()
    {
        StatsUtils.StartTimer();

        StatsUtils.StatsStopWatch.Start();

        if (!File.Exists($"{ConfigPath}/dicts.json"))
        {
            DictUtils.CreateDefaultDictsConfig();
        }

        if (!File.Exists($"{ConfigPath}/freqs.json"))
        {
            FreqUtils.CreateDefaultFreqsConfig();
        }

        if (!File.Exists($"{ConfigPath}/AudioSourceConfig.json"))
        {
            AudioUtils.CreateDefaultAudioSourceConfig();
        }

        if (!File.Exists($"{ResourcesPath}/custom_words.txt"))
        {
            await File.Create($"{ResourcesPath}/custom_words.txt").DisposeAsync().ConfigureAwait(false);
        }

        if (!File.Exists($"{ResourcesPath}/custom_names.txt"))
        {
            await File.Create($"{ResourcesPath}/custom_names.txt").DisposeAsync().ConfigureAwait(false);
        }

        List<Task> tasks = new()
        {
            Task.Run(static async () =>
            {
                await DictUtils.DeserializeDicts().ConfigureAwait(false);
                Frontend.ApplyDictOptions();
                await DictUtils.LoadDictionaries(false).ConfigureAwait(false);
                await DictUtils.SerializeDicts().ConfigureAwait(false);
                await DictUtils.InitializeWordClassDictionary().ConfigureAwait(false);
            }),

            Task.Run(static async () =>
            {
                await AudioUtils.DeserializeAudioSources().ConfigureAwait(false);
                await FreqUtils.DeserializeFreqs().ConfigureAwait(false);
                await FreqUtils.LoadFrequencies(false).ConfigureAwait(false);
            })
        };

        await DictUtils.InitializeKanjiCompositionDict().ConfigureAwait(false);
        await Task.WhenAll(tasks).ConfigureAwait(false);

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
    }

    internal static List<string>? TrimStringList(List<string> list)
    {
        List<string>? listClone = list;

        if (listClone.Count is 0 || listClone.All(string.IsNullOrEmpty))
        {
            listClone = null;
        }
        else
        {
            listClone.TrimExcess();
        }

        return listClone;
    }
}
