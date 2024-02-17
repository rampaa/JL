using System.Windows;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using MessageBox = HandyControl.Controls.MessageBox;

namespace JL.Windows;

internal sealed class WindowsFrontend : IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat, float volume) => WindowsUtils.PlayAudio(audio, audioFormat, volume);

    public void Alert(AlertLevel alertLevel, string message) => WindowsUtils.Alert(alertLevel, message);

    public bool ShowYesNoDialog(string text, string caption) => MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) is MessageBoxResult.Yes;

    public void ShowOkDialog(string text, string caption)
    {
        _ = MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public void InvalidateDisplayCache() => PopupWindow.StackPanelCache.Clear();

    public void ApplyDictOptions() => DictOptionManager.ApplyDictOptions();

    public async Task CopyFromWebSocket(string text) => await MainWindow.Instance.CopyFromWebSocket(text).ConfigureAwait(false);

    public byte[]? GetImageFromClipboardAsByteArray() => WindowsUtils.GetImageFromClipboardAsByteArray();

    public Task TextToSpeech(string voiceName, string text, int volume) => SpeechSynthesisUtils.TextToSpeech(voiceName, text, volume);

    public Task StopTextToSpeech() => SpeechSynthesisUtils.StopTextToSpeech();

    public byte[] GetAudioResponseFromTextToSpeech(string voiceName, string text) => SpeechSynthesisUtils.GetAudioResponseFromTextToSpeech(voiceName, text);

    public void SetInstalledVoiceWithHighestPriority() => SpeechSynthesisUtils.SetInstalledVoiceWithHighestPriority();
}
