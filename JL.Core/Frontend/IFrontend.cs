using JL.Core.Dicts;
using Microsoft.Data.Sqlite;

namespace JL.Core.Frontend;

public interface IFrontend
{
    public Task<bool> PlayAudio(byte[] audio, string audioFormat, long requestId);

    public void Notify(NotificationLevel notificationLevel, string message);

    public Task<bool> ShowYesNoDialogAsync(string text, string caption);

    public Task CopyFromWebSocket(string text, bool tsukikage);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease);

    public Task ApplyDictOptions();

    public Task<byte[]?> GetImageFromClipboardAsByteArray();

    public byte[]? GetMonitorScreenshotAsByteArray();

    public Task TextToSpeech(string voiceName, string text, long requestId);

    public Task StopAudio(long requestId);

    public Task<byte[]?> GetAudioResponseFromTextToSpeech(string text);

    public void SetInstalledVoiceWithHighestPriority();

    public void InsertSettingsForMpvProfile(SqliteConnection connection, int mpvProfileId);

    public void InsertSettingsForTsukikageProfile(SqliteConnection connection, int tsukikageProfileId);

    public void PopupDictTypeButtonsNeedUpdating();

    public ImageInfo? GetImageInfo(string imagePath);

    public Version JLVersion { get; }
}
