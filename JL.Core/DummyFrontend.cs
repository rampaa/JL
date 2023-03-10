using JL.Core.Utilities;

namespace JL.Core;

internal sealed class DummyFrontend : IFrontend
{
    public CoreConfig CoreConfig { get; } = new();

    public void PlayAudio(byte[] sound, float volume)
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

    public Task CopyFromWebSocket(string text) => Task.CompletedTask;
}
