using JL.Core.Dicts;
using JL.Core.Frontend;
using JL.Windows.Config;
using JL.Windows.GUI;
using JL.Windows.GUI.Notification;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Frontend;

internal sealed class WindowsFrontend : IFrontend
{
    private readonly MainWindow _mainWindow;

    public WindowsFrontend(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public Task PlayAudio(byte[] audio, string audioFormat) => WindowsAudioUtils.PlayAudio(audio, audioFormat);

    public void Notify(NotificationLevel notificationLevel, string message) => NotificationManager.Notify(notificationLevel, message);

    public async Task<bool> ShowYesNoDialogAsync(string text, string caption) => await WindowsUtils.ShowYesNoDialogAsync(text, caption, await WindowsUtils.GetVisibleOwnedWindowOrOwner(_mainWindow).ConfigureAwait(true)).ConfigureAwait(false);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public Task ApplyDictOptions() => DictOptionManager.ApplyDictOptions();

    public Task CopyFromWebSocket(string text, bool tsukikage) => _mainWindow.CopyFromWebSocket(text, tsukikage);

    public Task<byte[]?> GetImageFromClipboardAsByteArray() => WindowsUtils.GetImageFromClipboardAsByteArray();

    public Task TextToSpeech(string voiceName, string text) => SpeechSynthesisUtils.TextToSpeech(voiceName, text);

    public void StopTextToSpeech() => SpeechSynthesisUtils.StopTextToSpeech();

    public byte[]? GetAudioResponseFromTextToSpeech(string text) => SpeechSynthesisUtils.GetAudioResponseFromTextToSpeech(text);

    public void SetInstalledVoiceWithHighestPriority() => SpeechSynthesisUtils.SetInstalledVoiceWithHighestPriority();

    public byte[]? GetMonitorScreenshotAsByteArray() => ScreenshotUtils.GetMonitorScreenshot();

    public void InsertSettingsForMpvProfile(SqliteConnection connection, int mpvProfileId) => ConfigManager.InsertSettingsForMpvProfile(connection, mpvProfileId);

    public void InsertSettingsForTsukikageProfile(SqliteConnection connection, int tsukikageProfileId) => ConfigManager.InsertSettingsForTsukikageProfile(connection, tsukikageProfileId);

    public void PopupDictTypeButtonsNeedUpdating() => PopupWindowUtils.PopupDictTypeButtonsNeedUpdating();

    public ImageInfo? GetImageInfo(string imagePath) => WindowsUtils.GetImageInfo(imagePath);

    public Version JLVersion => WindowsUtils.JLVersion;
}
