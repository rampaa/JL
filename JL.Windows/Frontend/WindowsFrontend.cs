using JL.Core.Frontend;
using JL.Windows.Config;
using JL.Windows.GUI;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;

namespace JL.Windows.Frontend;

internal sealed class WindowsFrontend : IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat) => WindowsUtils.PlayAudio(audio, audioFormat);

    public void Alert(AlertLevel alertLevel, string message) => WindowsUtils.Alert(alertLevel, message);

    public Task<bool> ShowYesNoDialogAsync(string text, string caption) => WindowsUtils.ShowYesNoDialogAsync(text, caption, MainWindow.Instance);

    public Task ShowOkDialogAsync(string text, string caption) => WindowsUtils.ShowOkDialogAsync(text, caption, MainWindow.Instance);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public void ApplyDictOptions() => DictOptionManager.ApplyDictOptions();

    public Task CopyFromWebSocket(string text) => MainWindow.Instance.CopyFromWebSocket(text);

    public Task<byte[]?> GetImageFromClipboardAsByteArray() => WindowsUtils.GetImageFromClipboardAsByteArray();

    public void TextToSpeech(string voiceName, string text) => SpeechSynthesisUtils.TextToSpeech(voiceName, text);

    public void StopTextToSpeech() => SpeechSynthesisUtils.StopTextToSpeech();

    public byte[]? GetAudioResponseFromTextToSpeech(string text) => SpeechSynthesisUtils.GetAudioResponseFromTextToSpeech(text);

    public void SetInstalledVoiceWithHighestPriority() => SpeechSynthesisUtils.SetInstalledVoiceWithHighestPriority();
}
