namespace JL.Core.Frontend;

public interface IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat);

    public void Alert(AlertLevel alertLevel, string message);

    public Task<bool> ShowYesNoDialogAsync(string text, string caption);

    public Task ShowOkDialogAsync(string text, string caption);

    public Task CopyFromWebSocket(string text);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease);

    public void ApplyDictOptions();

    public Task<byte[]?> GetImageFromClipboardAsByteArray();

    public Task TextToSpeech(string voiceName, string text);

    public Task StopTextToSpeech();

    public ValueTask<byte[]?> GetAudioResponseFromTextToSpeech(string text);

    public void SetInstalledVoiceWithHighestPriority();
}
