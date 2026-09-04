using System.Runtime.CompilerServices;
using JL.Core;
using JL.Core.Frontend;
using JL.Core.Utilities;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace JL.Windows.GUI.Notification;

internal static class NotificationManager
{
    private const string True = "true";
    private const string False = "false";
    private static ToastNotifier? s_toastNotifier;

    public static void Notify(NotificationLevel notificationLevel, string message)
    {
        if (AppInfo.Windows10OrLater)
        {
            try
            {
                ShowNativeNotification(notificationLevel, message);
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "ShowNativeToast method failed unexpectedly");
                CustomNotificationUtils.ShowLegacyNotification(notificationLevel, message);
            }
        }
        else
        {
            CustomNotificationUtils.ShowLegacyNotification(notificationLevel, message);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ShowNativeNotification(NotificationLevel notificationLevel, string message)
    {
        bool silent = notificationLevel is NotificationLevel.Information or NotificationLevel.Success;
        const bool suppressPopup = false;

        string xml = /*lang=xml*/ $"""
        <toast>
            <visual>
                <binding template="ToastText01">
                    <text id="1">{GetNotificationLevelSymbol(notificationLevel)} {message}</text>
                </binding>
            </visual>
            <audio silent="{(silent ? True : False)}"/> 
        </toast>
        """;

        XmlDocument xmlDoc = new();
        xmlDoc.LoadXml(xml);

        ToastNotification toast = new(xmlDoc)
        {
            SuppressPopup = suppressPopup
        };

        s_toastNotifier ??= ToastNotificationManager.CreateToastNotifier("JL");
        s_toastNotifier.Show(toast);
    }

    private static string GetNotificationLevelSymbol(NotificationLevel notificationLevel)
    {
        return notificationLevel switch
        {
            NotificationLevel.Error => "❌",
            NotificationLevel.Warning => "⚠️",
            NotificationLevel.Information => "ℹ️",
            NotificationLevel.Success => "✅",
            _ => ""
        };
    }
}
