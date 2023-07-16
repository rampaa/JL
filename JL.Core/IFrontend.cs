using JL.Core.Utilities;

namespace JL.Core;

public interface IFrontend
{
    public void PlayAudio(byte[] audio, string audioFormat, float volume);

    public void Alert(AlertLevel alertLevel, string message);

    public bool ShowYesNoDialog(string text, string caption);

    public void ShowOkDialog(string text, string caption);

    public void CopyFromWebSocket(string text);

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease);

    public void InvalidateDisplayCache();

    public void ApplyDictOptions();

    public byte[]? GetImageFromClipboardAsByteArray();
}
