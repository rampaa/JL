namespace JL.Core.Frontend;

internal sealed class DummyFrontend : IFrontend
{
    public Task PlayAudio(byte[] audio, string audioFormat) => Task.CompletedTask;

    public void Alert(AlertLevel alertLevel, string message)
    {
    }

    public Task<bool> ShowYesNoDialogAsync(string text, string caption) => Task.FromResult(true);

    public Task ShowOkDialogAsync(string text, string caption) => Task.CompletedTask;

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => Task.CompletedTask;

    public void ApplyDictOptions()
    {
    }

    public Task CopyFromWebSocket(string text) => Task.CompletedTask;

    public Task<byte[]?> GetImageFromClipboardAsByteArray() => Task.FromResult<byte[]?>(null);

    public Task TextToSpeech(string voiceName, string text) => Task.CompletedTask;

    public void StopTextToSpeech()
    {
    }

    public byte[]? GetAudioResponseFromTextToSpeech(string text) => null;

    public void SetInstalledVoiceWithHighestPriority()
    {
    }
}
