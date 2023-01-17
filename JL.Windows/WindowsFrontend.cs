using System.Windows;
using JL.Core;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.Utilities;
using MessageBox = HandyControl.Controls.MessageBox;

namespace JL.Windows;
internal sealed class WindowsFrontend : IFrontend
{
    public CoreConfig CoreConfig { get; } = ConfigManager.Instance;

    public void PlayAudio(byte[] sound, float volume) => WindowsUtils.PlayAudio(sound, volume);

    public void Alert(AlertLevel alertLevel, string message) => WindowsUtils.Alert(alertLevel, message);

    public bool ShowYesNoDialog(string text, string caption) => MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) is MessageBoxResult.Yes;

    public void ShowOkDialog(string text, string caption)
    {
        _ = MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public void InvalidateDisplayCache() => PopupWindow.StackPanelCache.Clear();

    public void ApplyDictOptions() => DictOptionManager.ApplyDictOptions();
}
