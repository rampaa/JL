using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.GUI.Notification;
using JL.Windows.Utilities;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisUtils
{
    private static readonly Lazy<SpeechSynthesizer> s_synthesizer = new(CreateSynthesizer);
    private static readonly Lazy<SpeechSynthesizer> s_streamSynthesizer = new(static () => new SpeechSynthesizer());
    private static VoiceInfo[]? s_installedVoices;

    private static long s_lastAudioPlayTimestamp;

    public static string? InstalledVoiceWithHighestPriority { get; private set; }

    private static SpeechSynthesizer CreateSynthesizer()
    {
        SpeechSynthesizer synthesizer = new();
        synthesizer.SetOutputToDefaultAudioDevice();
        return synthesizer;
    }

    private static VoiceInfo[] GetInstalledVoices()
    {
#pragma warning disable CA1304 // Specify CultureInfo
        return s_synthesizer.Value.GetInstalledVoices()
            .Where(static iv => iv.Enabled && iv.VoiceInfo.Name is not null && iv.VoiceInfo.Culture is not null)
            .Select(static iv => iv.VoiceInfo)
            .OrderBy(static voice => voice.Culture.TwoLetterISOLanguageName is not "ja")
            .ThenBy(static voice => voice.Name, StringComparer.InvariantCulture)
            .ToArray();
#pragma warning restore CA1304 // Specify CultureInfo
    }

    public static ComboBoxItem[] GetInstalledVoiceItems()
    {
        // Only the audio-settings windows use this cache, on the UI thread.
        ReadOnlySpan<VoiceInfo> installedVoices = s_installedVoices ??= GetInstalledVoices();
        ComboBoxItem[] items = new ComboBoxItem[installedVoices.Length];

        for (int i = 0; i < installedVoices.Length; i++)
        {
            VoiceInfo voice = installedVoices[i];
            ComboBoxItem item = new()
            {
                Content = voice.Name
            };

            if (voice.Culture.TwoLetterISOLanguageName is not "ja")
            {
                item.Foreground = Brushes.LightSlateGray;
            }

            items[i] = item;
        }

        return items;
    }

    public static void StopTextToSpeech()
    {
        if (s_synthesizer.IsValueCreated)
        {
            s_synthesizer.Value.SpeakAsyncCancelAll();
        }
    }

    public static async Task TextToSpeech(string voiceName, string text, long requestId)
    {
        await WindowsAudioUtils.AudioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!AudioUtils.IsCurrentAudioRequest(requestId))
            {
                return;
            }

            WindowsAudioUtils.DisposeCurrentMedia();

            SpeechSynthesizer synthesizer = s_synthesizer.Value;
            if (synthesizer.State is SynthesizerState.Speaking && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < 300)
            {
                s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
                return;
            }

            s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
            synthesizer.SpeakAsyncCancelAll();

            try
            {
                synthesizer.SelectVoice(voiceName);
            }
            catch (ArgumentException ex)
            {
                LoggerManager.Logger.Error(ex, "Failed to select voice {VoiceName}", voiceName);
                NotificationManager.Notify(NotificationLevel.Error, $"{voiceName} is not available on your system. Deactivating it.");

                // Run on the UI thread so disabling this voice and reading AudioSources cannot overlap with edits in the settings window.
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (AudioUtils.AudioSources.TryGetValue(voiceName, out AudioSource? audioSource))
                    {
                        audioSource.Active = false;
                        AudioUtils.UpdateActiveAudioSources();
                        SetInstalledVoiceWithHighestPriority();
                    }
                }).Task.ConfigureAwait(false);

                return;
            }

            if (AudioUtils.IsCurrentAudioRequest(requestId))
            {
                _ = synthesizer.SpeakAsync(text);
            }
        }
        finally
        {
            _ = WindowsAudioUtils.AudioPlayerSemaphoreSlim.Release();
        }
    }

    public static byte[]? GetAudioResponseFromTextToSpeech(string text)
    {
        string? voiceName = InstalledVoiceWithHighestPriority;
        if (voiceName is null)
        {
            return null;
        }

        lock (s_streamSynthesizer)
        {
            SpeechSynthesizer streamSynthesizer = s_streamSynthesizer.Value;
            streamSynthesizer.SelectVoice(voiceName);
            using MemoryStream audioDataStream = new();
            streamSynthesizer.SetOutputToWaveStream(audioDataStream);
            streamSynthesizer.Speak(text);
            return audioDataStream.ToArray();
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
