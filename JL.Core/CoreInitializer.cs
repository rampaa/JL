using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core;
public static class CoreInitializer
{
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
                FrontendManager.Frontend.ApplyDictOptions();
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
                FrontendManager.Frontend.SetInstalledVoiceWithHighestPriority();
            }),
            Task.Run(static () => DeconjugatorUtils.DeserializeRules()))
            .ConfigureAwait(false);

        ObjectPoolManager.StringPoolInstance.Reset();
    }
}
