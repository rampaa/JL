using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.GUI.Notification;
using JL.Windows.Utilities;
using Windows.Storage.Streams;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisUtils
{
    private const int ResourceDisposalTimeoutSeconds = 2;
    private const int SpeechThrottleMilliseconds = 300;
    private static readonly SemaphoreSlim s_playbackSynthesisSemaphoreSlim = new(1, 1);
    private static readonly Lock s_miningLock = new();
    private static NativeSpeechSynthesizer? s_playbackSynthesizer;
    private static NativeSpeechSynthesizer? s_miningSynthesizer;
    private static Mp3Encoder? s_miningMp3Encoder;
    private static SpeechVoiceInfo[]? s_installedVoices;
    private static long s_lastAudioPlayTimestamp;
    private static bool s_stopping;

    public static SpeechPlaybackChannels PlaybackChannels { get; } = SpeechPlaybackChannels.EnginePreferred;
    public static string? InstalledVoiceWithHighestPriority { get; private set; }

    public static ComboBoxItem[] GetInstalledVoiceItems()
    {
        s_installedVoices ??= NativeSpeechSynthesizer.GetVoices();

        // Only the audio-settings windows use this cache, on the UI thread.
        ReadOnlySpan<SpeechVoiceInfo> voices = s_installedVoices;
        ComboBoxItem[] items = new ComboBoxItem[voices.Length];
        for (int i = 0; i < voices.Length; ++i)
        {
            SpeechVoiceInfo voice = voices[i];
            ComboBoxItem item = new() { Content = voice.Name };
            if (!voice.IsJapanese)
            {
                item.Foreground = Brushes.LightSlateGray;
            }

            items[i] = item;
        }

        return items;
    }

    public static async Task TextToSpeech(string voiceName, string text, long requestId)
    {
        await WindowsAudioUtils.AudioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref s_stopping) || !AudioUtils.IsCurrentAudioRequest(requestId))
            {
                return;
            }

            if (WindowsAudioUtils.IsPlayingSpeech() && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < SpeechThrottleMilliseconds)
            {
                s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
                return;
            }

            s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
            WindowsAudioUtils.DisposeCurrentMedia();
        }
        finally
        {
            _ = WindowsAudioUtils.AudioPlayerSemaphoreSlim.Release();
        }

        await Task.Run(() => StartTextToSpeech(voiceName, text, requestId)).ConfigureAwait(false);
    }

    private static async Task StartTextToSpeech(string voiceName, string text, long requestId)
    {
        await s_playbackSynthesisSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        SpeechAudioSource? speechAudio = null;
        bool synthesisWorkerStarted = false;
        bool voiceUnavailable = false;
        try
        {
            if (Volatile.Read(ref s_stopping) || !AudioUtils.IsCurrentAudioRequest(requestId))
            {
                return;
            }

            NativeSpeechSynthesizer synthesizer = s_playbackSynthesizer ??= new NativeSpeechSynthesizer();
            if (!synthesizer.TrySelectVoice(voiceName))
            {
                voiceUnavailable = true;
                return;
            }

            if (!AudioUtils.IsCurrentAudioRequest(requestId))
            {
                return;
            }

            speechAudio = new SpeechAudioSource(synthesizer.PreferredAudioFormat, PlaybackChannels);
            if (!await WindowsAudioUtils.PlaySpeech(speechAudio, requestId).ConfigureAwait(false))
            {
                return;
            }

            SpeechAudioSource playbackAudio = speechAudio;
            _ = Task.Run(() => GenerateSpeechForPlayback(synthesizer, text, playbackAudio));
            synthesisWorkerStarted = true;
            speechAudio = null; // The player and synthesis worker now own the audio source.
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't start speech synthesis with {VoiceName}", voiceName);
            NotificationManager.Notify(NotificationLevel.Error, "Couldn't synthesize speech. Check the logs for more details.");
        }
        finally
        {
            speechAudio?.Dispose();
            if (!synthesisWorkerStarted)
            {
                speechAudio?.ReleaseSynthesisResources();
                _ = s_playbackSynthesisSemaphoreSlim.Release();
            }

            if (voiceUnavailable)
            {
                DeactivateUnavailableVoice(voiceName).SafeFireAndForget("Couldn't deactivate an unavailable speech voice");
            }
        }
    }

    private static void GenerateSpeechForPlayback(NativeSpeechSynthesizer synthesizer, string text, SpeechAudioSource speechAudio)
    {
        try
        {
            if (!speechAudio.CancellationToken.IsCancellationRequested)
            {
                synthesizer.GenerateSpeech(text, speechAudio);
            }
        }
        catch (OperationCanceledException)
        {
            LoggerManager.Logger.Debug("Speech synthesis was canceled");
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't synthesize speech");
            speechAudio.ReportSynthesisFailure();
        }
        finally
        {
            speechAudio.ReleaseSynthesisResources();
            _ = s_playbackSynthesisSemaphoreSlim.Release();
        }
    }

    private static Task DeactivateUnavailableVoice(string voiceName)
    {
        LoggerManager.Logger.Error("Failed to select voice {VoiceName}", voiceName);
        NotificationManager.Notify(NotificationLevel.Error, $"{voiceName} is not available on your system. Deactivating it.");

        // AudioSources is also edited by the settings windows, so update it on their UI thread.
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (AudioUtils.AudioSources.TryGetValue(voiceName, out AudioSource? source))
            {
                source.Active = false;
                AudioUtils.UpdateActiveAudioSources();
                SetInstalledVoiceWithHighestPriority();
            }
        }).Task;
    }

    public static Task<byte[]?> GetAudioResponseFromTextToSpeech(string text)
    {
        string? voiceName = InstalledVoiceWithHighestPriority;
        if (voiceName is null || text.Length is 0)
        {
            return Task.FromResult<byte[]?>(null);
        }

        bool canSynthesizeOnCurrentThread = Thread.CurrentThread.GetApartmentState() is ApartmentState.MTA;
        return canSynthesizeOnCurrentThread
            ? CreateMiningAudioResponse(voiceName, text)
            : CreateMiningAudioOnWorker(voiceName, text);
    }

    private static Task<byte[]?> CreateMiningAudioOnWorker(string voiceName, string text)
    {
        // If created on the UI thread, Windows can route later synthesis calls back to that thread.
        // Create the cached mining synthesizer on a worker so mining never depends on UI message processing.
        return Task.Run(() => CreateMiningAudioResponse(voiceName, text));
    }

    private static async Task<byte[]?> CreateMiningAudioResponse(string voiceName, string text)
    {
        using InMemoryRandomAccessStream? compressedAudio = CreateMiningAudio(voiceName, text);
        if (compressedAudio is null)
        {
            return null;
        }

        byte[] audio = new byte[checked((int)compressedAudio.Size)];
        using DataReader reader = new(compressedAudio.GetInputStreamAt(0));
        uint loaded = await reader.LoadAsync((uint)audio.Length).AsTask().ConfigureAwait(false);
        if (loaded != audio.Length)
        {
            LoggerManager.Logger.Error("Couldn't read all of the synthesized MP3 audio");
            return null;
        }

        reader.ReadBytes(audio);
        return audio;
    }

    private static InMemoryRandomAccessStream? CreateMiningAudio(string voiceName, string text)
    {
        lock (s_miningLock)
        {
            if (Volatile.Read(ref s_stopping))
            {
                return null;
            }

            NativeSpeechSynthesizer synthesizer = s_miningSynthesizer ??= new NativeSpeechSynthesizer();
            if (!synthesizer.TrySelectVoice(voiceName))
            {
                DeactivateUnavailableVoice(voiceName).SafeFireAndForget("Couldn't deactivate an unavailable speech voice");
                return null;
            }

            using IRandomAccessStream? wave = synthesizer.GenerateWaveAudio(text);
            if (wave is null)
            {
                return null;
            }

            Mp3Encoder encoder = s_miningMp3Encoder ??= new Mp3Encoder();
            return encoder.Encode(wave, synthesizer.PreferredAudioFormat.SamplesPerSecond, synthesizer.PreferredAudioFormat.ChannelCount);
        }
    }

    public static async Task StopSpeechAndDisposeResources()
    {
        Volatile.Write(ref s_stopping, true);
        await WindowsAudioUtils.StopAudio(AudioUtils.StartAudioRequest()).ConfigureAwait(false);

        // Third-party SAPI engines can ignore cancellation and remain inside a native call.
        // Limit the exit wait; leave any objects still in use for process teardown to release.
        if (await s_playbackSynthesisSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(ResourceDisposalTimeoutSeconds)).ConfigureAwait(false))
        {
            try
            {
                s_playbackSynthesizer?.Dispose();
                s_playbackSynthesizer = null;
            }
            finally
            {
                _ = s_playbackSynthesisSemaphoreSlim.Release();
            }
        }
        else
        {
            LoggerManager.Logger.Warning("The speech engine did not stop before shutdown");
        }

        if (s_miningLock.TryEnter(TimeSpan.FromSeconds(ResourceDisposalTimeoutSeconds)))
        {
            try
            {
                s_miningSynthesizer?.Dispose();
                s_miningSynthesizer = null;
                s_miningMp3Encoder?.Dispose();
                s_miningMp3Encoder = null;
            }
            finally
            {
                s_miningLock.Exit();
            }
        }
        else
        {
            LoggerManager.Logger.Warning("Speech export did not finish before shutdown");
        }
    }

    public static void SetInstalledVoiceWithHighestPriority()
    {
        string? installedVoiceWithHighestPriority = null;
        int highestPriority = int.MaxValue;
        foreach ((string voiceName, AudioSource audioSource) in AudioUtils.AudioSources)
        {
            if (audioSource is { Active: true, Type: AudioSourceType.TextToSpeech }
                && audioSource.Priority <= highestPriority)
            {
                installedVoiceWithHighestPriority = voiceName;
                highestPriority = audioSource.Priority;
            }
        }

        InstalledVoiceWithHighestPriority = installedVoiceWithHighestPriority;
    }
}
