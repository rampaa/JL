using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Utilities;

namespace JL.Windows.GUI;

internal sealed partial class AlertWindow
{
    public nint WindowHandle { get; private set; }

    public AlertWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHandle = new WindowInteropHelper(this).Handle;
        WinApi.BringToFront(WindowHandle);
    }

    public void SetAlert(AlertLevel alertLevel, string message)
    {
        AlertBorder.BorderBrush = alertLevel switch
        {
            AlertLevel.Error => Brushes.Red,
            AlertLevel.Warning => Brushes.Orange,
            AlertLevel.Information => Brushes.White,
            AlertLevel.Success => Brushes.Green,
            _ => Brushes.White
        };

        AlertWindowTextBlock.Text = message;
    }
}
