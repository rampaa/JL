using System.Windows;
using System.Windows.Interop;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
public partial class InfoWindow : Window
{
    private IntPtr _windowHandle;

    public InfoWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        WinApi.BringToFront(_windowHandle);
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
