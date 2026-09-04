using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using JL.Core.Frontend;
using JL.Windows.Interop;

namespace JL.Windows.GUI.Notification;

internal sealed partial class CustomNotificationWindow : Window
{
    public nint WindowHandle { get; private set; }

    public CustomNotificationWindow()
    {
        InitializeComponent();
        MouseDown += OnWindowMouseDown;
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHandle = new WindowInteropHelper(this).Handle;
        WinApi.SetNoRedirectionBitmapStyle(WindowHandle);
        WinApi.PreventActivation(WindowHandle);
        WinApi.BringToFront(WindowHandle);
    }

    public void SetNotification(NotificationLevel notificationLevel, string message)
    {
        CustomNotificationWindowBorder.BorderBrush = notificationLevel switch
        {
            NotificationLevel.Error => Brushes.Red,
            NotificationLevel.Warning => Brushes.Orange,
            NotificationLevel.Information => Brushes.White,
            NotificationLevel.Success => Brushes.Green,
            _ => Brushes.White
        };

        CustomNotificationWindowTextBlock.Text = message;
    }

    public void StartDismissTimer()
    {
        DoubleAnimation fadeOut = new()
        {
            From = 1.0,
            To = 0.0,
            BeginTime = TimeSpan.FromMilliseconds(4004),
            Duration = TimeSpan.FromMilliseconds(300)
        };
        fadeOut.Completed += OnFadeOutCompleted;
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void OnFadeOutCompleted(object? sender, EventArgs e)
    {
        Close();
    }
}
