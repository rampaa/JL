using JL.Core.Utilities;

namespace JL.Core;

public class UnimplementedFrontend : IFrontend
{
    private CoreConfig? _coreConfig;

    public CoreConfig CoreConfig
    {
        get => _coreConfig ?? throw new NotImplementedException("Please set CoreConfig.");
        set => _coreConfig = value;
    }

    public void PlayAudio(byte[] sound, float volume) =>
        throw new NotImplementedException("Please set a frontend in order to use this method.");

    public void Alert(AlertLevel alertLevel, string message) =>
        throw new NotImplementedException("Please set a frontend in order to use this method.");

    public bool ShowYesNoDialog(string text, string caption) =>
        throw new NotImplementedException("Please set a frontend in order to use this method.");

    public void ShowOkDialog(string text, string caption) =>
        throw new NotImplementedException("Please set a frontend in order to use this method.");

    public Task UpdateJL(Version latestVersion) =>
        throw new NotImplementedException("Please set a frontend in order to use this method.");
}
