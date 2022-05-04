using JL.Core.Utilities;

namespace JL.Core;

public interface IFrontend
{
    public CoreConfig CoreConfig { get; set; }

    public void PlayAudio(byte[] sound, float volume);

    public void Alert(AlertLevel alertLevel, string message);

    public bool ShowYesNoDialog(string text, string caption);

    public void ShowOkDialog(string text, string caption);

    public Task UpdateJL(Version latestVersion);

    public void InvalidateDisplayCache();
}
