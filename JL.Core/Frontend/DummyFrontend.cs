using JL.Core.Utilities;

namespace JL.Core.Frontend;

internal sealed class DummyFrontend : IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat, float volume)
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

    public void InvalidateDisplayCache()
    {
    }

    public void ApplyDictOptions()
    {
    }
    public void CopyFromWebSocket(string text)
    {
    }

    public byte[]? GetImageFromClipboardAsByteArray() => null;

    public Task TextToSpeech(string voiceName, string text, int volume) => Task.CompletedTask;

    public Task StopTextToSpeech() => Task.CompletedTask;

    public byte[] GetAudioResponseFromTextToSpeech(string voiceName, string text) => Array.Empty<byte>();

    public void SetInstalledVoiceWithHighestPriority()
    {
    }
}
