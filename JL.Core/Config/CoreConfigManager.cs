using JL.Core.Lookup;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;
using Serilog.Events;

namespace JL.Core.Config;

public sealed class CoreConfigManager
{
    public static CoreConfigManager Instance { get; private set; } = new();

    public Uri AnkiConnectUri { get; set; } = new("http://127.0.0.1:8765");
    public bool AnkiIntegration { get; set; } // = false;
    public bool ForceSyncAnki { get; private set; } // = false;
    public bool NotifyWhenMiningSucceeds { get; private set; } = true;
    public bool AllowDuplicateCards { get; private set; } // = false;
    public bool CheckForDuplicateCards { get; private set; } // = false;
    public bool CaptureTextFromClipboard { get; set; } = true;
    public bool CaptureTextFromWebSocket { get; set; } // = false;
    public bool AutoReconnectToWebSocket { get; private set; } // = false;
    public bool TextBoxTrimWhiteSpaceCharacters { get; private set; } = true;
    public bool TextBoxRemoveNewlines { get; private set; } // = false;
    public Uri WebSocketUri { get; private set; } = new("ws://127.0.0.1:6677");
    public string MpvNamedPipePath { get; private set; } = "/tmp/mpv-socket";
    public bool CheckForJLUpdatesOnStartUp { get; private set; } = true;
    public bool TrackTermLookupCounts { get; private set; } // = false;
    public int MinCharactersPerMinuteBeforeStoppingTimeTracking { get; private set; } = 10;
    public LookupCategory LookupCategory { get; set; } = LookupCategory.All;

    private CoreConfigManager()
    {
    }

    public static void CreateNewCoreConfigManager()
    {
        Instance = new CoreConfigManager();
    }

    public void ApplyPreferences(SqliteConnection connection)
    {
        Utils.s_loggingLevelSwitch.MinimumLevel = ConfigDBManager.GetValueEnumValueFromConfig(connection, LogEventLevel.Error, "MinimumLogLevel");

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
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.OrdinalIgnoreCase);

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
            CaptureTextFromWebSocket = ConfigDBManager.GetValueFromConfig(connection, CaptureTextFromWebSocket, nameof(CaptureTextFromWebSocket));
            AutoReconnectToWebSocket = ConfigDBManager.GetValueFromConfig(connection, AutoReconnectToWebSocket, nameof(AutoReconnectToWebSocket));

            string? webSocketUriStr = ConfigDBManager.GetSettingValue(connection, nameof(WebSocketUri));
            bool webSocketUriChanged = true;
            if (webSocketUriStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(WebSocketUri), WebSocketUri.OriginalString);
                webSocketUriChanged = true;
            }
            else
            {
                webSocketUriStr = webSocketUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.OrdinalIgnoreCase);

                if (Uri.TryCreate(webSocketUriStr, UriKind.Absolute, out Uri? webSocketUri))
                {
                    if (WebSocketUri.OriginalString != webSocketUri.OriginalString)
                    {
                        WebSocketUri = webSocketUri;
                        webSocketUriChanged = true;
                    }
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save WebSocket address, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save WebSocket address, invalid URL");
                }
            }

            if (!WebSocketUtils.Connected || webSocketUriChanged)
            {
                _ = WebSocketUtils.HandleWebSocket();
            }
        }

        if (TrackTermLookupCounts)
        {
            if (StatsUtils.ProfileLifetimeStats.TermLookupCountDict.Count > 0)
            {
                StatsDBUtils.UpdateProfileLifetimeStats(connection);
            }

            if (StatsUtils.LifetimeStats.TermLookupCountDict.Count > 0)
            {
                StatsDBUtils.UpdateLifetimeStats(connection);
            }
        }

        TrackTermLookupCounts = ConfigDBManager.GetValueFromConfig(connection, TrackTermLookupCounts, nameof(TrackTermLookupCounts));
        if (!TrackTermLookupCounts)
        {
            StatsUtils.SessionStats.TermLookupCountDict.Clear();
        }

        AnkiIntegration = ConfigDBManager.GetValueFromConfig(connection, AnkiIntegration, nameof(AnkiIntegration));
        ForceSyncAnki = ConfigDBManager.GetValueFromConfig(connection, ForceSyncAnki, nameof(ForceSyncAnki));
        NotifyWhenMiningSucceeds = ConfigDBManager.GetValueFromConfig(connection, NotifyWhenMiningSucceeds, nameof(NotifyWhenMiningSucceeds));
        AllowDuplicateCards = ConfigDBManager.GetValueFromConfig(connection, AllowDuplicateCards, nameof(AllowDuplicateCards));
        CheckForDuplicateCards = ConfigDBManager.GetValueFromConfig(connection, CheckForDuplicateCards, nameof(CheckForDuplicateCards));
        TextBoxTrimWhiteSpaceCharacters = ConfigDBManager.GetValueFromConfig(connection, TextBoxTrimWhiteSpaceCharacters, nameof(TextBoxTrimWhiteSpaceCharacters));
        TextBoxRemoveNewlines = ConfigDBManager.GetValueFromConfig(connection, TextBoxRemoveNewlines, nameof(TextBoxRemoveNewlines));
        CheckForJLUpdatesOnStartUp = ConfigDBManager.GetValueFromConfig(connection, CheckForJLUpdatesOnStartUp, nameof(CheckForJLUpdatesOnStartUp));
        CaptureTextFromClipboard = ConfigDBManager.GetValueFromConfig(connection, CaptureTextFromClipboard, nameof(CaptureTextFromClipboard));
        MinCharactersPerMinuteBeforeStoppingTimeTracking = ConfigDBManager.GetValueFromConfig(connection, MinCharactersPerMinuteBeforeStoppingTimeTracking, nameof(MinCharactersPerMinuteBeforeStoppingTimeTracking));
        LookupCategory = ConfigDBManager.GetValueEnumValueFromConfig(connection, LookupCategory, nameof(LookupCategory));
        MpvNamedPipePath = ConfigDBManager.GetValueFromConfig(connection, MpvNamedPipePath, nameof(MpvNamedPipePath));
    }
}
