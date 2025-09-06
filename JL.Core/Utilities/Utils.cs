using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.WordClass;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace JL.Core.Utilities;

public static class Utils
{
    public static readonly Version JLVersion = new(3, 8, 3);
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(ApplicationPath, "Resources");
    public static readonly string ConfigPath = Path.Join(ApplicationPath, "Config");
    public static readonly bool Is64BitProcess = Environment.Is64BitProcess;
    internal static StringPool StringPoolInstance => StringPool.Shared;
    public static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 1024 * 4);
    public static IFrontend Frontend { get; set; } = new DummyFrontend();

    internal static readonly LoggingLevelSwitch s_loggingLevelSwitch = new()
    {
        MinimumLevel = LogEventLevel.Error
    };

    public static readonly Logger Logger = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(s_loggingLevelSwitch)
        .WriteTo.File(Path.Join(ApplicationPath, "Logs", "log.txt"),
            formatProvider: CultureInfo.InvariantCulture,
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(30),
            shared: true)
        .CreateLogger();

#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
    internal static string GetMd5String(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(MD5.HashData(bytes).AsReadOnlySpan());
    }
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

    public static async Task CoreInitialize()
    {
        NetworkUtils.InitializeUpdaterTimer();
        StatsUtils.InitializeStatsTimer();

        _ = Directory.CreateDirectory(ProfileUtils.ProfileFolderPath);
        _ = Directory.CreateDirectory(DBUtils.s_dictDBFolderPath);
        _ = Directory.CreateDirectory(DBUtils.s_freqDBFolderPath);

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

        string profileCustomWordsPath = ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfileName);
        if (!File.Exists(profileCustomWordsPath))
        {
            await File.Create(profileCustomWordsPath).DisposeAsync().ConfigureAwait(false);
        }

        string profileCustomNamesPath = ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfileName);
        if (!File.Exists(profileCustomNamesPath))
        {
            await File.Create(profileCustomNamesPath).DisposeAsync().ConfigureAwait(false);
        }

        await Task.WhenAll(
            Task.Run(static async () =>
            {
                await DictUtils.DeserializeDicts().ConfigureAwait(false);
                Frontend.ApplyDictOptions();
                await DictUtils.LoadDictionaries().ConfigureAwait(false);
                await DictUtils.SerializeDicts().ConfigureAwait(false);
                await JmdictWordClassUtils.Initialize().ConfigureAwait(false);
                await ResourceUpdater.AutoUpdateDicts().ConfigureAwait(false);
            }),
            Task.Run(static async () =>
            {
                await FreqUtils.DeserializeFreqs().ConfigureAwait(false);
                await FreqUtils.LoadFrequencies().ConfigureAwait(false);
                await FreqUtils.SerializeFreqs().ConfigureAwait(false);
                await ResourceUpdater.AutoUpdateFreqDicts().ConfigureAwait(false);
            }),
            Task.Run(static async () =>
            {
                await AudioUtils.DeserializeAudioSources().ConfigureAwait(false);
                Frontend.SetInstalledVoiceWithHighestPriority();
            }),
            Task.Run(static () => DeconjugatorUtils.DeserializeRules()))
            .ConfigureAwait(false);

        StringPoolInstance.Reset();
    }

    public static void ClearStringPoolIfDictsAreReady()
    {
        if (DictUtils.DictsReady
            && FreqUtils.FreqsReady
            && DictUtils.Dicts.Values.ToArray().All(static dict => !dict.Updating)
            && FreqUtils.FreqDicts.Values.ToArray().All(static freq => !freq.Updating))
        {
            StringPoolInstance.Reset();
        }
    }

    public static string GetPortablePath(string path)
    {
        string fullPath = Path.GetFullPath(path, ApplicationPath);
        return fullPath.StartsWith(ApplicationPath, StringComparison.Ordinal)
            ? Path.GetRelativePath(ApplicationPath, fullPath)
            : fullPath;
    }

    internal static T[]? ConcatNullableArrays<T>(params ReadOnlySpan<T[]?> arrays)
    {
        int position = 0;
        int length = 0;

        foreach (ref readonly T[]? array in arrays)
        {
            if (array is not null)
            {
                length += array.Length;
            }
        }

        if (length is 0)
        {
            return null;
        }

        T[] concatArray = new T[length];
        foreach (ref readonly T[]? array in arrays)
        {
            if (array is not null)
            {
                array.AsReadOnlySpan().CopyTo(concatArray.AsSpan(position, array.Length));
                position += array.Length;
            }
        }

        return concatArray;
    }
}
