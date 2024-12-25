using JL.Core.Utilities;

namespace JL.Core.Frontend;

internal sealed class DummyFrontend : IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat)
    {
    }

    public void Alert(AlertLevel alertLevel, string message)
    {
    }

    public bool ShowYesNoDialog(string text, string caption) => true;

    public void ShowOkDialog(string text, string caption)
    {
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => Task.CompletedTask;

    public void ApplyDictOptions()
    {
    }

    public Task CopyFromWebSocket(string text) => Task.CompletedTask;

    public Task<byte[]?> GetImageFromClipboardAsByteArray() => new(static () => null);

    public Task TextToSpeech(string voiceName, string text) => Task.CompletedTask;

    public Task StopTextToSpeech() => Task.CompletedTask;

    public Task<byte[]?> GetAudioResponseFromTextToSpeech(string text) => new(static () => null);

    public void SetInstalledVoiceWithHighestPriority()
    {
    }
}
