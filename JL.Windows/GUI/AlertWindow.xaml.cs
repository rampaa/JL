using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Utilities;

namespace JL.Windows.GUI;

internal sealed partial class AlertWindow : Window
{
    public AlertWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WinApi.BringToFront(new WindowInteropHelper(this).Handle);
    }

    public void SetAlert(AlertLevel alertLevel, string message)
    {
        AlertBorder.BorderBrush = alertLevel switch
        {
            AlertLevel.Error => Brushes.Red,
            AlertLevel.Warning => Brushes.Orange,
            AlertLevel.Information => Brushes.White,
            AlertLevel.Success => Brushes.Green,
            _ => throw new ArgumentOutOfRangeException(nameof(alertLevel), alertLevel, "Invalid AlertLevel")
        };
        AlertWindowTextBlock.Text = message;
    }
}
