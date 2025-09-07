namespace JL.Core.Frontend;

internal sealed class DummyFrontend : IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat)
    {
    }

    public void Alert(AlertLevel alertLevel, string message)
    {
    }

    public Task<bool> ShowYesNoDialog(string text, string caption) => Task.FromResult(true);

    public void ShowOkDialog(string text, string caption)
    {
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => Task.CompletedTask;

    public void ApplyDictOptions()
    {
    }

    public Task CopyFromWebSocket(string text) => Task.CompletedTask;

    public Task<byte[]?> GetImageFromClipboardAsByteArray() => Task.FromResult<byte[]?>(null);

    public Task TextToSpeech(string voiceName, string text) => Task.CompletedTask;

    public Task StopTextToSpeech() => Task.CompletedTask;

    public ValueTask<byte[]?> GetAudioResponseFromTextToSpeech(string text) => ValueTask.FromResult<byte[]?>(null);

    public void SetInstalledVoiceWithHighestPriority()
    {
    }
}
