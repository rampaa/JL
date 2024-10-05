using System.Windows.Interop;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
internal sealed partial class InfoWindow
{
    private nint _windowHandle;

    public InfoWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }
}
