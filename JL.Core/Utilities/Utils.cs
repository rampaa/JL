using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.HighPerformance.Buffers;
using JL.Core.Audio;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Profile;
using JL.Core.Statistics;
using JL.Core.WordClass;
using Serilog;
using Serilog.Core;

namespace JL.Core.Utilities;

public static class Utils
{
    public static readonly Version JLVersion = new(1, 22, 0);
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(AppContext.BaseDirectory, "Resources");
    public static readonly string ConfigPath = Path.Join(AppContext.BaseDirectory, "Config");
    internal static StringPool StringPoolInstance => StringPool.Shared;
    public static IFrontend Frontend { get; set; } = new DummyFrontend();
    public const int CacheSize = 1000;

    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new() { MinimumLevel = Serilog.Events.LogEventLevel.Error };

    public static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(LoggingLevelSwitch)
        .WriteTo.File(Path.Join(ApplicationPath, "Logs/log.txt"),
            formatProvider: CultureInfo.InvariantCulture,
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(90),
            shared: true)
        .CreateLogger();

    internal static readonly JsonSerializerOptions s_defaultJso = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static readonly JsonSerializerOptions s_jsoWithEnumConverter = new()
    {
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static readonly JsonSerializerOptions s_jsoWithIndentation = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    internal static readonly JsonSerializerOptions s_jsoWithEnumConverterAndIndentation = new()
    {
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    internal static readonly Dictionary<string, string> s_iso6392BTo2T = new(20)
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
        StatsUtils.StartStatsTimer();

        StatsUtils.StatsStopWatch.Start();

        if (!File.Exists(Path.Join(ConfigPath, "dicts.json")))
        {
            await DictUtils.CreateDefaultDictsConfig().ConfigureAwait(false);
        }

        if (!File.Exists(Path.Join(ConfigPath, "freqs.json")))
        {
            await FreqUtils.CreateDefaultFreqsConfig().ConfigureAwait(false);
        }

        if (!File.Exists(Path.Join(ConfigPath, "AudioSourceConfig.json")))
        {
            await AudioUtils.CreateDefaultAudioSourceConfig().ConfigureAwait(false);
        }

        string customWordsPath = Path.Join(ResourcesPath, "custom_words.txt");
        if (!File.Exists(customWordsPath))
        {
            await File.Create(customWordsPath).DisposeAsync().ConfigureAwait(false);
        }

        string customNamesPath = Path.Join(ResourcesPath, "custom_names.txt");
        if (!File.Exists(customNamesPath))
        {
            await File.Create(customNamesPath).DisposeAsync().ConfigureAwait(false);
        }

        string profileCustomWordsPath = ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfile);
        if (!File.Exists(profileCustomWordsPath))
        {
            await File.Create(profileCustomWordsPath).DisposeAsync().ConfigureAwait(false);
        }

        string profileCustomNamesPath = ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfile);
        if (!File.Exists(profileCustomNamesPath))
        {
            await File.Create(profileCustomNamesPath).DisposeAsync().ConfigureAwait(false);
        }

        List<Task> tasks = new()
        {
            Task.Run(static async () =>
            {
                await DictUtils.DeserializeDicts().ConfigureAwait(false);
                Frontend.ApplyDictOptions();
                await DictUtils.LoadDictionaries().ConfigureAwait(false);
                await DictUtils.SerializeDicts().ConfigureAwait(false);
                await JmdictWordClassUtils.Initialize().ConfigureAwait(false);
            }),

            Task.Run(static async () =>
            {
                await FreqUtils.DeserializeFreqs().ConfigureAwait(false);
                await FreqUtils.LoadFrequencies().ConfigureAwait(false);
            }),

            Task.Run(static async() =>
            {
                await AudioUtils.DeserializeAudioSources().ConfigureAwait(false);
                Frontend.SetInstalledVoiceWithHighestPriority();
            }),

            Task.Run(static async() => await DeconjugatorUtils.DeserializeRules().ConfigureAwait(false))
        };

        await DictUtils.InitializeKanjiCompositionDict().ConfigureAwait(false);
        await Task.WhenAll(tasks).ConfigureAwait(false);
        StringPoolInstance.Reset();
    }

    public static void ClearStringPoolIfDictsAreReady()
    {
        if (DictUtils.DictsReady
            && FreqUtils.FreqsReady
            && !DictUtils.UpdatingJmdict
            && !DictUtils.UpdatingJmnedict
            && !DictUtils.UpdatingKanjidic)
        {
            StringPoolInstance.Reset();
        }
    }

    public static string GetPath(string path)
    {
        string fullPath = Path.GetFullPath(path, ApplicationPath);
        string relativePath = Path.GetRelativePath(ApplicationPath, fullPath);
        return relativePath.StartsWith('.') ? fullPath : relativePath;
    }
}
