using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Windows.Utilities;
using NAudio.Wave;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisUtils
{
    private static DateTime s_lastAudioPlayTime;
    public static string? InstalledVoiceWithHighestPriority { get; private set; }
    private static SpeechSynthesizer Synthesizer { get; } = new();

    public static ComboBoxItem[]? InstalledVoices { get; } = GetInstalledVoiceNames();

    private static ComboBoxItem[]? GetInstalledVoiceNames()
    {
        Synthesizer.InjectOneCoreVoices();

#pragma warning disable CA1304 // Specify CultureInfo
        List<InstalledVoice> installedVoices = Synthesizer.GetInstalledVoices().Where(static iv => iv.Enabled).ToList();
#pragma warning restore CA1304 // Specify CultureInfo

        return installedVoices.Count is 0
            ? null
            : Application.Current.Dispatcher.Invoke(() =>
            {
                ComboBoxItem[] installedVoiceComboboxItems = new ComboBoxItem[installedVoices.Count];

                for (int i = 0; i < installedVoices.Count; i++)
                {
                    InstalledVoice installedVoice = installedVoices[i];

                    if (installedVoice.VoiceInfo.Name is null || installedVoice.VoiceInfo.Culture is null)
                    {
                        continue;
                    }

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

    public static async Task StopTextToSpeech()
    {
        if (Synthesizer.State is SynthesizerState.Speaking)
        {
            Synthesizer.SpeakAsyncCancelAll();

            while (Synthesizer.State is SynthesizerState.Speaking)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }

    public static async Task TextToSpeech(string voiceName, string text)
    {
        if (WindowsUtils.AudioPlayer?.PlaybackState is PlaybackState.Playing)
        {
            WindowsUtils.AudioPlayer.Dispose();
        }

        DateTime currentTime = DateTime.Now;
        if (Synthesizer.State is SynthesizerState.Speaking && (currentTime - s_lastAudioPlayTime).TotalMilliseconds < 300)
        {
            s_lastAudioPlayTime = currentTime;
            return;
        }

        s_lastAudioPlayTime = currentTime;

        await StopTextToSpeech().ConfigureAwait(false);

        Synthesizer.SelectVoice(voiceName);

        Synthesizer.SetOutputToDefaultAudioDevice();
        _ = Synthesizer.SpeakAsync(text);
    }

    public static async ValueTask<byte[]?> GetAudioResponseFromTextToSpeech(string text)
    {
        if (InstalledVoiceWithHighestPriority is null)
        {
            return null;
        }

        await StopTextToSpeech().ConfigureAwait(false);

        Synthesizer.SelectVoice(InstalledVoiceWithHighestPriority);
        using MemoryStream audioDataStream = new();
        Synthesizer.SetOutputToWaveStream(audioDataStream);
        Synthesizer.Speak(text);
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
