using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;
using Serilog.Events;

namespace JL.Core.Config;

public static class CoreConfigManager
{
    public static Uri AnkiConnectUri { get; set; } = new("http://127.0.0.1:8765");
    public static bool KanjiMode { get; set; } // = false;
    public static bool AnkiIntegration { get; set; } // = false;
    public static bool ForceSyncAnki { get; private set; } // = false;
    public static bool AllowDuplicateCards { get; private set; } // = false;
    public static double LookupRate { get; private set; } // = 0;
    public static bool CaptureTextFromClipboard { get; set; } = true;
    public static bool CaptureTextFromWebSocket { get; set; } // = false;
    public static bool AutoReconnectToWebSocket { get; private set; } // = false;
    public static bool TextBoxTrimWhiteSpaceCharacters { get; private set; } = true;
    public static bool TextBoxRemoveNewlines { get; private set; } // = false;
    public static Uri WebSocketUri { get; private set; } = new("ws://127.0.0.1:6677");
    public static bool CheckForJLUpdatesOnStartUp { get; private set; } = true;

    public static void ApplyPreferences(SqliteConnection connection)
    {
        Utils.s_loggingLevelSwitch.MinimumLevel = ConfigDBManager.GetValueFromConfig(connection, LogEventLevel.Error, "MinimumLogLevel", Enum.TryParse);

        {
            string? ankiConnectUriStr = ConfigDBManager.GetSettingValue(connection, nameof(AnkiConnectUri));
            if (ankiConnectUriStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(AnkiConnectUri), AnkiConnectUri.OriginalString);
            }

            else
            {
                ankiConnectUriStr = ankiConnectUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.Ordinal);

                if (Uri.TryCreate(ankiConnectUriStr, UriKind.Absolute, out Uri? ankiConnectUri))
                {
                    AnkiConnectUri = ankiConnectUri;
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save AnkiConnect server address, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save AnkiConnect server address, invalid URL");
                }
            }
        }

        {
            CaptureTextFromWebSocket = ConfigDBManager.GetValueFromConfig(connection, CaptureTextFromWebSocket, nameof(CaptureTextFromWebSocket), bool.TryParse);
            AutoReconnectToWebSocket = ConfigDBManager.GetValueFromConfig(connection, AutoReconnectToWebSocket, nameof(AutoReconnectToWebSocket), bool.TryParse);

            string? webSocketUriStr = ConfigDBManager.GetSettingValue(connection, nameof(WebSocketUri));
            if (webSocketUriStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(WebSocketUri), WebSocketUri.OriginalString);
            }
            else
            {
                webSocketUriStr = webSocketUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.Ordinal);

                if (Uri.TryCreate(webSocketUriStr, UriKind.Absolute, out Uri? webSocketUri))
                {
                    WebSocketUri = webSocketUri;
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save WebSocket address, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save WebSocket address, invalid URL");
                }
            }

            WebSocketUtils.HandleWebSocket();
        }

        KanjiMode = ConfigDBManager.GetValueFromConfig(connection, KanjiMode, nameof(KanjiMode), bool.TryParse);
        AnkiIntegration = ConfigDBManager.GetValueFromConfig(connection, AnkiIntegration, nameof(AnkiIntegration), bool.TryParse);
        ForceSyncAnki = ConfigDBManager.GetValueFromConfig(connection, ForceSyncAnki, nameof(ForceSyncAnki), bool.TryParse);
        AllowDuplicateCards = ConfigDBManager.GetValueFromConfig(connection, AllowDuplicateCards, nameof(AllowDuplicateCards), bool.TryParse);
        LookupRate = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, LookupRate, nameof(LookupRate), double.TryParse);
        TextBoxTrimWhiteSpaceCharacters = ConfigDBManager.GetValueFromConfig(connection, TextBoxTrimWhiteSpaceCharacters, nameof(TextBoxTrimWhiteSpaceCharacters), bool.TryParse);
        TextBoxRemoveNewlines = ConfigDBManager.GetValueFromConfig(connection, TextBoxRemoveNewlines, nameof(TextBoxRemoveNewlines), bool.TryParse);
        CheckForJLUpdatesOnStartUp = ConfigDBManager.GetValueFromConfig(connection, CheckForJLUpdatesOnStartUp, nameof(CheckForJLUpdatesOnStartUp), bool.TryParse);
        CaptureTextFromClipboard = ConfigDBManager.GetValueFromConfig(connection, CaptureTextFromClipboard, nameof(CaptureTextFromClipboard), bool.TryParse);

        if (!CaptureTextFromWebSocket && !CaptureTextFromClipboard)
        {
            StatsUtils.StatsStopWatch.Stop();
            StatsUtils.StopStatsTimer();
        }
        else
        {
            StatsUtils.StatsStopWatch.Start();
            StatsUtils.StartStatsTimer();
        }
    }
}
