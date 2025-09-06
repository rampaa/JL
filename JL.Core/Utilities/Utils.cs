using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    public static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 1024 * 4);
    public static IFrontend Frontend { get; set; } = new DummyFrontend();

    internal static readonly LoggingLevelSwitch s_loggingLevelSwitch = new()
    {
        MinimumLevel = LogEventLevel.Error
    };

    public static readonly Logger Logger = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(s_loggingLevelSwitch)
        .WriteTo.File(Path.Join(AppInfo.ApplicationPath, "Logs", "log.txt"),
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

        if (!File.Exists(Path.Join(AppInfo.ConfigPath, "dicts.json")))
        {
            await DictUtils.CreateDefaultDictsConfig().ConfigureAwait(false);
        }

        if (!File.Exists(Path.Join(AppInfo.ConfigPath, "freqs.json")))
        {
            await FreqUtils.CreateDefaultFreqsConfig().ConfigureAwait(false);
        }

        if (!File.Exists(Path.Join(AppInfo.ConfigPath, "AudioSourceConfig.json")))
        {
            await AudioUtils.CreateDefaultAudioSourceConfig().ConfigureAwait(false);
        }

        string customWordsPath = Path.Join(AppInfo.ResourcesPath, "custom_words.txt");
        if (!File.Exists(customWordsPath))
        {
            await File.Create(customWordsPath).DisposeAsync().ConfigureAwait(false);
        }

        string customNamesPath = Path.Join(AppInfo.ResourcesPath, "custom_names.txt");
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

        StringPoolUtils.StringPoolInstance.Reset();
    }

    public static string GetPortablePath(string path)
    {
        string fullPath = Path.GetFullPath(path, AppInfo.ApplicationPath);
        return fullPath.StartsWith(AppInfo.ApplicationPath, StringComparison.Ordinal)
            ? Path.GetRelativePath(AppInfo.ApplicationPath, fullPath)
            : fullPath;
    }
}
