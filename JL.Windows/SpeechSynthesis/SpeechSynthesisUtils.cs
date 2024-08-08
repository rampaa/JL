using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using JL.Core.Audio;
using JL.Windows.Utilities;
using NAudio.Wave;

namespace JL.Windows.SpeechSynthesis;

internal static class SpeechSynthesisUtils
{
    private static DateTime s_lastAudioPlayTime;
    public static string? InstalledVoiceWithHighestPriority { get; private set; }
    private static SpeechSynthesizer Synthesizer { get; } = new();

    public static string[] InstalledVoices { get; } = GetInstalledJapaneseVoiceNames();

    private static string[] GetInstalledJapaneseVoiceNames()
    {
        Synthesizer.InjectOneCoreVoices();

        return Synthesizer.GetInstalledVoices(CultureInfo.GetCultureInfo("ja-JP"))
            .Where(static iv => iv.Enabled)
            .Select(static iv => iv.VoiceInfo.Name)
            .OrderBy(static name => name, StringComparer.InvariantCulture)
            .ToArray();
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

    public static byte[]? GetAudioResponseFromTextToSpeech(string text)
    {
        if (InstalledVoiceWithHighestPriority is null)
        {
            return null;
        }

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
