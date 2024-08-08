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
    public void PlayAudio(byte[] audio, string audioFormat) => WindowsUtils.PlayAudio(audio, audioFormat);

    public void Alert(AlertLevel alertLevel, string message) => WindowsUtils.Alert(alertLevel, message);

    public bool ShowYesNoDialog(string text, string caption) => WindowsUtils.ShowYesNoDialog(text, caption);

    public void ShowOkDialog(string text, string caption)
    {
        _ = MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public void ApplyDictOptions() => DictOptionManager.ApplyDictOptions();

    public Task CopyFromWebSocket(string text) => MainWindow.Instance.CopyFromWebSocket(text);

    public byte[]? GetImageFromClipboardAsByteArray() => WindowsUtils.GetImageFromClipboardAsByteArray();

    public Task TextToSpeech(string voiceName, string text) => SpeechSynthesisUtils.TextToSpeech(voiceName, text);

    public Task StopTextToSpeech() => SpeechSynthesisUtils.StopTextToSpeech();

    public byte[]? GetAudioResponseFromTextToSpeech(string text) => SpeechSynthesisUtils.GetAudioResponseFromTextToSpeech(text);

    public void SetInstalledVoiceWithHighestPriority() => SpeechSynthesisUtils.SetInstalledVoiceWithHighestPriority();
}
