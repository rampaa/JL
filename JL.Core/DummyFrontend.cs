using JL.Core.Utilities;

namespace JL.Core;

public class DummyFrontend : IFrontend
{
    public CoreConfig CoreConfig { get; set; } = new();

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

    public Task UpdateJL(Version latestVersion) => Task.CompletedTask;

    public void InvalidateDisplayCache()
    {
    }
}
