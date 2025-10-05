using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using NAudio.Wave;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisUtils
{
    private static long s_lastAudioPlayTimestamp;
    public static string? InstalledVoiceWithHighestPriority { get; private set; }
    private static readonly SpeechSynthesizer s_synthesizer = new();
    private static readonly SpeechSynthesizer s_streamSynthesizer = new();

    public static ComboBoxItem[]? InstalledVoices { get; } = GetInstalledVoiceNames();

    private static ComboBoxItem[]? GetInstalledVoiceNames()
    {
        s_synthesizer.InjectOneCoreVoices();
        s_synthesizer.SetOutputToDefaultAudioDevice();

        s_streamSynthesizer.InjectOneCoreVoices();

#pragma warning disable CA1304 // Specify CultureInfo
        List<InstalledVoice> installedVoices = s_synthesizer.GetInstalledVoices().Where(static iv => iv.Enabled && iv.VoiceInfo.Name is not null && iv.VoiceInfo.Culture is not null).ToList();
#pragma warning restore CA1304 // Specify CultureInfo

        return installedVoices.Count is 0
            ? null
            : Application.Current?.Dispatcher.Invoke(() =>
            {
                ReadOnlySpan<InstalledVoice> installedVoicesSpan = installedVoices.AsReadOnlySpan();
                ComboBoxItem[] installedVoiceComboboxItems = new ComboBoxItem[installedVoicesSpan.Length];

                for (int i = 0; i < installedVoicesSpan.Length; i++)
                {
                    ref readonly InstalledVoice installedVoice = ref installedVoicesSpan[i];

                    ComboBoxItem comboBoxItem = new()
                    {
                        Content = installedVoice.VoiceInfo.Name
                    };

                    if (installedVoice.VoiceInfo.Culture.TwoLetterISOLanguageName is not "ja")
                    {
                        comboBoxItem.Foreground = Brushes.LightSlateGray;
                    }

                    installedVoiceComboboxItems[i] = comboBoxItem;
                }

                return installedVoiceComboboxItems
                    .OrderBy(static iv => iv.Foreground == Brushes.LightSlateGray)
                    .ThenBy(static iv => (string)iv.Content, StringComparer.InvariantCulture)
                    .ToArray();
            });
    }

    public static void StopTextToSpeech()
    {
        s_synthesizer.SpeakAsyncCancelAll();
    }

    public static async Task TextToSpeech(string voiceName, string text)
    {
        if (WindowsUtils.AudioPlayer?.PlaybackState is PlaybackState.Playing)
        {
            await WindowsUtils.AudioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                WindowsUtils.AudioPlayer.Stop();
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Error while stopping audio player");
            }
            finally
            {
                _ = WindowsUtils.AudioPlayerSemaphoreSlim.Release();
            }
        }

        if (s_synthesizer.State is SynthesizerState.Speaking && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < 300)
        {
            s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
            return;
        }

        s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();

        s_synthesizer.SpeakAsyncCancelAll();

        try
        {
            s_synthesizer.SelectVoice(voiceName);
        }
        catch (ArgumentException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to select voice {VoiceName}", voiceName);
            WindowsUtils.Alert(AlertLevel.Error, $"{voiceName} is not available on your system. Deactivating it.");
            AudioUtils.AudioSources[voiceName].Active = false;
            SetInstalledVoiceWithHighestPriority();
            return;
        }

        _ = s_synthesizer.SpeakAsync(text);
    }

    public static byte[]? GetAudioResponseFromTextToSpeech(string text)
    {
        if (InstalledVoiceWithHighestPriority is null)
        {
            return null;
        }

        s_streamSynthesizer.SpeakAsyncCancelAll();
        s_streamSynthesizer.SelectVoice(InstalledVoiceWithHighestPriority);
        using MemoryStream audioDataStream = new();
        s_streamSynthesizer.SetOutputToWaveStream(audioDataStream);
        s_streamSynthesizer.Speak(text);
        return audioDataStream.ToArray();
    }

    public static void SetInstalledVoiceWithHighestPriority()
    {
        List<KeyValuePair<string, AudioSource>> textToSpeechAudioSources = AudioUtils.AudioSources
            .Where(static a => a.Value is { Active: true, Type: AudioSourceType.TextToSpeech }).ToList();

        InstalledVoiceWithHighestPriority = textToSpeechAudioSources.Count > 0
            ? textToSpeechAudioSources.Aggregate(static (a1, a2) => a1.Value.Priority < a2.Value.Priority ? a1 : a2).Key
            : null;
    }
}
