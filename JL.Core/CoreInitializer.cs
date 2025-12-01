using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
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

        await DictUtils.CreateDefaultDictsConfig().ConfigureAwait(false);
        await FreqUtils.CreateDefaultFreqsConfig().ConfigureAwait(false);
        await AudioUtils.CreateDefaultAudioSourceConfig().ConfigureAwait(false);


        PathUtils.CreateFileIfNotExists(DictUtils.CustomWordDictPath);
        PathUtils.CreateFileIfNotExists(DictUtils.CustomNameDictPath);
        PathUtils.CreateFileIfNotExists(ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfileName));
        PathUtils.CreateFileIfNotExists(ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfileName));

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

        ObjectPoolManager.s_stringPoolInstance.Reset();
    }
}
