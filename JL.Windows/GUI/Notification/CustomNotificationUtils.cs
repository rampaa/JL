using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using JL.Core.Frontend;
using JL.Windows.Interop;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.Notification;

internal static class CustomNotificationUtils
{
    private const int MaxVisibleCustomNotificationWindows = 5;
    private static List<CustomNotificationWindow>? s_activeCustomNotificationWindows;

    public static void ShowLegacyNotification(NotificationLevel notificationLevel, string message)
    {
        Application? application = Application.Current;
        if (application is null)
        {
            return;
        }

        _ = application.Dispatcher.BeginInvoke(() =>
        {
            s_activeCustomNotificationWindows ??= new List<CustomNotificationWindow>(MaxVisibleCustomNotificationWindows);
            while (s_activeCustomNotificationWindows.Count >= MaxVisibleCustomNotificationWindows)
            {
                s_activeCustomNotificationWindows[0].Close();
            }

            CustomNotificationWindow customNotificationWindow = new();
            customNotificationWindow.Closed += OnWindowClosed;
            customNotificationWindow.SetNotification(notificationLevel, message);

            customNotificationWindow.Opacity = 0;
            customNotificationWindow.Show();

            s_activeCustomNotificationWindows.Add(customNotificationWindow);
            RepositionActiveCustomNotificationWindows();

            customNotificationWindow.Opacity = 1;
            customNotificationWindow.StartDismissTimer();

        }, DispatcherPriority.Render);
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is CustomNotificationWindow window)
        {
            Debug.Assert(s_activeCustomNotificationWindows is not null);
            _ = s_activeCustomNotificationWindows.Remove(window);
            RepositionActiveCustomNotificationWindows();
        }
    }

    private static void RepositionActiveCustomNotificationWindows()
    {
        double offset = 30 * WindowsUtils.Dpi.DpiScaleX;
        double offsetBetweenCustomNotificationWindows = 2 * WindowsUtils.Dpi.DpiScaleY;
        double accumulatedHeight = 0;
        Rectangle workingArea = WindowsUtils.ActiveScreen.WorkingArea;

        Debug.Assert(s_activeCustomNotificationWindows is not null);
        foreach (CustomNotificationWindow window in s_activeCustomNotificationWindows)
        {
            double x = workingArea.Right - offset - (window.ActualWidth * WindowsUtils.Dpi.DpiScaleX);
            double y = workingArea.Top + offset + accumulatedHeight;

            WinApi.MoveWindowToPosition(window.WindowHandle, x, y);
            accumulatedHeight += (window.ActualHeight * WindowsUtils.Dpi.DpiScaleY) + offsetBetweenCustomNotificationWindows;
        }
    }
}
