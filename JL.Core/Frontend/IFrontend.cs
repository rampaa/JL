namespace JL.Core.Frontend;

public interface IFrontend
{
    public Task PlayAudio(byte[] audio, string audioFormat);

    public void Alert(AlertLevel alertLevel, string message);

    public Task<bool> ShowYesNoDialogAsync(string text, string caption);

    public Task ShowOkDialogAsync(string text, string caption);

    public Task CopyFromWebSocket(string text, bool tsukikage);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease);

    public void ApplyDictOptions();

    public Task<byte[]?> GetImageFromClipboardAsByteArray();

    public Task TextToSpeech(string voiceName, string text);

    public void StopTextToSpeech();

    public byte[]? GetAudioResponseFromTextToSpeech(string text);

    public void SetInstalledVoiceWithHighestPriority();

    public Version JLVersion { get; }
}
