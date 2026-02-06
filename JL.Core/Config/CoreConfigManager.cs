using JL.Core.External.AnkiConnect;
using JL.Core.Frontend;
using JL.Core.Lookup;
using JL.Core.Network.WebSocket;
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
    public bool AllowDuplicateCards { get; private set; } // = false;
    public bool CheckForDuplicateCards { get; private set; } // = false;
    public bool CheckEntireCollectionForDuplicates { get; private set; } // = false;
    public bool CheckChildDecksForDuplicates { get; private set; } // = false;
    public bool CheckAllNoteTypesForDuplicates { get; private set; } // = false;
    public bool NotifyWhenMiningSucceeds { get; private set; } = true;
    public bool CaptureTextFromClipboard { get; set; } = true;
    public bool CaptureTextFromWebSocket { get; set; } // = false;
    public bool CaptureTextFromTsukikageWebsocket { get; set; } // = false;
    public bool AutoReconnectToWebSocket { get; private set; } // = false;
    public bool AutoReconnectToTsukikageWebSocket { get; private set; } = true;
    public bool TextBoxTrimWhiteSpaceCharacters { get; private set; } = true;
    public bool TextBoxRemoveNewlines { get; private set; } // = false;
    public List<Uri> WebSocketUris { get; private set; } = [new("ws://127.0.0.1:6677")];
    public Uri TsukikageWebSocketUri { get; private set; } = new("ws://127.0.0.1:8768");
    public string MpvNamedPipePath { get; private set; } = "/tmp/mpv-socket";
    public bool CheckForJLUpdatesOnStartUp { get; private set; } = true;
    public bool TrackTermLookupCounts { get; private set; } // = false;
    public int MinCharactersPerMinuteBeforeStoppingTimeTracking { get; private set; } = 10;
    public LookupCategory LookupCategory { get; set; } = LookupCategory.All;

    #region Implicit setting names
    public const string MinimumLogLevelSettingName = "MinimumLogLevel";
    #endregion

    private CoreConfigManager()
    {
    }

    public static void CreateNewCoreConfigManager()
    {
        Instance = new CoreConfigManager();
    }

    public void ApplyPreferences(SqliteConnection connection, Dictionary<string, string> configs)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        LoggerManager.s_loggingLevelSwitch.MinimumLevel = ConfigDBManager.GetValueEnumValueFromConfig(connection, configs, LogEventLevel.Error, MinimumLogLevelSettingName);

        {
            string? ankiConnectUriStr = configs.GetValueOrDefault(nameof(AnkiConnectUri));
            if (ankiConnectUriStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(AnkiConnectUri), AnkiConnectUri.OriginalString);
            }
            else
            {
                ankiConnectUriStr = ankiConnectUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.OrdinalIgnoreCase);

                if (Uri.TryCreate(ankiConnectUriStr, UriKind.Absolute, out Uri? ankiConnectUri)
                    && (ankiConnectUri.Scheme == Uri.UriSchemeHttp || ankiConnectUri.Scheme == Uri.UriSchemeHttps))
                {
                    AnkiConnectUri = ankiConnectUri;
                }
                else
                {
                    ConfigDBManager.UpdateSetting(connection, nameof(AnkiConnectUri), AnkiConnectUri.OriginalString);
                    LoggerManager.Logger.Warning("Couldn't save AnkiConnect server address, invalid URL");
                    FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't save AnkiConnect server address, invalid URL");
                }
            }
        }

        {
            CaptureTextFromTsukikageWebsocket = ConfigDBManager.GetValueFromConfig(connection, configs, CaptureTextFromTsukikageWebsocket, nameof(CaptureTextFromTsukikageWebsocket));
            AutoReconnectToTsukikageWebSocket = ConfigDBManager.GetValueFromConfig(connection, configs, AutoReconnectToTsukikageWebSocket, nameof(AutoReconnectToTsukikageWebSocket));

            string? tsukikageWebSocketUriStr = configs.GetValueOrDefault(nameof(TsukikageWebSocketUri));
            if (tsukikageWebSocketUriStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(TsukikageWebSocketUri), TsukikageWebSocketUri.OriginalString);
                if (CaptureTextFromTsukikageWebsocket)
                {
                    WebSocketUtils.TsukikageWebSocketConnection = new WebSocketConnection(TsukikageWebSocketUri);
                    WebSocketUtils.TsukikageWebSocketConnection.Connect(false);
                }
            }
            else
            {
                tsukikageWebSocketUriStr = tsukikageWebSocketUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.OrdinalIgnoreCase);

                if (Uri.TryCreate(tsukikageWebSocketUriStr, UriKind.Absolute, out Uri? webSocketUri) && (webSocketUri.Scheme == Uri.UriSchemeWs || webSocketUri.Scheme == Uri.UriSchemeWss))
                {
                    if (TsukikageWebSocketUri.OriginalString != webSocketUri.OriginalString)
                    {
                        WebSocketUtils.TsukikageWebSocketConnection?.Disconnect().SafeFireAndForget("Unexpected error while disconnecting from Tsukikage WebSocket");
                        WebSocketUtils.TsukikageWebSocketConnection = null;

                        TsukikageWebSocketUri = webSocketUri;
                        ConfigDBManager.UpdateSetting(connection, nameof(TsukikageWebSocketUri), TsukikageWebSocketUri.OriginalString);
                    }

                    if (CaptureTextFromTsukikageWebsocket)
                    {
                        WebSocketUtils.TsukikageWebSocketConnection ??= new WebSocketConnection(webSocketUri);
                        WebSocketUtils.TsukikageWebSocketConnection.Connect(true);
                    }
                    else
                    {
                        WebSocketUtils.TsukikageWebSocketConnection?.Disconnect().SafeFireAndForget("Unexpected error while disconnecting from Tsukikage WebSocket");
                        WebSocketUtils.TsukikageWebSocketConnection = null;
                    }
                }
                else
                {
                    LoggerManager.Logger.Warning("Couldn't save Tsukikage WebSocket address, invalid URL");
                    FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't save Tsukikage WebSocket address, invalid URL");
                }
            }
        }

        {
            CaptureTextFromWebSocket = ConfigDBManager.GetValueFromConfig(connection, configs, CaptureTextFromWebSocket, nameof(CaptureTextFromWebSocket));
            AutoReconnectToWebSocket = ConfigDBManager.GetValueFromConfig(connection, configs, AutoReconnectToWebSocket, nameof(AutoReconnectToWebSocket));

            string? webSocketUrisStr = configs.GetValueOrDefault(nameof(WebSocketUris));
            if (webSocketUrisStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(WebSocketUris), string.Join('\n', WebSocketUris.Select(static ws => ws.OriginalString)));

                if (CaptureTextFromWebSocket)
                {
                    foreach (Uri webSocketUri in WebSocketUris)
                    {
                        WebSocketUtils.ConnectToWebSocket(webSocketUri);
                    }
                }
            }
            else
            {
                string[] uriStrs = webSocketUrisStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.OrdinalIgnoreCase)
                    .ReplaceLineEndings("\n")
                    .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                List<Uri> newWebSocketUris = new(uriStrs.Length);
                foreach (string uriStr in uriStrs)
                {
                    if (Uri.TryCreate(uriStr, UriKind.Absolute, out Uri? webSocketUri)
                        && (webSocketUri.Scheme == Uri.UriSchemeWs || webSocketUri.Scheme == Uri.UriSchemeWss))
                    {
                        if (webSocketUri.OriginalString != TsukikageWebSocketUri.OriginalString)
                        {
                            newWebSocketUris.Add(webSocketUri);
                            if (CaptureTextFromWebSocket)
                            {
                                WebSocketUtils.ConnectToWebSocket(webSocketUri);
                            }
                            else
                            {
                                WebSocketUtils.DisconnectFromWebSocket(webSocketUri).SafeFireAndForget("Unexpected error while disconnecting from WebSocket");
                            }
                        }
                    }
                    else
                    {
                        LoggerManager.Logger.Warning("Couldn't save WebSocket address, invalid URL");
                        FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't save WebSocket address, invalid URL");
                    }
                }

                foreach (Uri uri in WebSocketUris)
                {
                    if (!newWebSocketUris.Contains(uri))
                    {
                        WebSocketUtils.DisconnectFromWebSocket(uri).SafeFireAndForget("Unexpected error while disconnecting from WebSocket");
                    }
                }

                if (!WebSocketUris.Select(static w => w.OriginalString).SequenceEqual(newWebSocketUris.Select(static w => w.OriginalString), StringComparer.Ordinal))
                {
                    WebSocketUris = newWebSocketUris;
                    ConfigDBManager.UpdateSetting(connection, nameof(WebSocketUris), string.Join('\n', WebSocketUris.Select(static ws => ws.OriginalString)));
                }
            }
        }

        AnkiIntegration = ConfigDBManager.GetValueFromConfig(connection, configs, AnkiIntegration, nameof(AnkiIntegration));
        ForceSyncAnki = ConfigDBManager.GetValueFromConfig(connection, configs, ForceSyncAnki, nameof(ForceSyncAnki));
        AllowDuplicateCards = ConfigDBManager.GetValueFromConfig(connection, configs, AllowDuplicateCards, nameof(AllowDuplicateCards));
        CheckEntireCollectionForDuplicates = ConfigDBManager.GetValueFromConfig(connection, configs, CheckEntireCollectionForDuplicates, nameof(CheckEntireCollectionForDuplicates));
        CheckChildDecksForDuplicates = ConfigDBManager.GetValueFromConfig(connection, configs, CheckChildDecksForDuplicates, nameof(CheckChildDecksForDuplicates));
        CheckAllNoteTypesForDuplicates = ConfigDBManager.GetValueFromConfig(connection, configs, CheckAllNoteTypesForDuplicates, nameof(CheckAllNoteTypesForDuplicates));
        CheckForDuplicateCards = ConfigDBManager.GetValueFromConfig(connection, configs, CheckForDuplicateCards, nameof(CheckForDuplicateCards));
        if (AnkiIntegration)
        {
            Dictionary<string, object> duplicateScopeOptions = new(2, StringComparer.Ordinal)
            {
                {
                    "checkChildren", CheckChildDecksForDuplicates
                },
                {
                    "checkAllModels", CheckAllNoteTypesForDuplicates
                }
            };

            string duplicateScope = CheckEntireCollectionForDuplicates ? "collection" : "deck";

            AnkiConnectUtils.AnkiOptions.Clear();
            AnkiConnectUtils.AnkiOptions.Add("allowDuplicate", AllowDuplicateCards);
            AnkiConnectUtils.AnkiOptions.Add("duplicateScope", duplicateScope);
            AnkiConnectUtils.AnkiOptions.Add("duplicateScopeOptions", duplicateScopeOptions);

            AnkiConnectUtils.CheckDuplicateOptions.Clear();
            AnkiConnectUtils.CheckDuplicateOptions.Add("allowDuplicate", false);
            AnkiConnectUtils.CheckDuplicateOptions.Add("duplicateScope", duplicateScope);
            AnkiConnectUtils.CheckDuplicateOptions.Add("duplicateScopeOptions", duplicateScopeOptions);
        }
        else
        {
            AnkiConnectUtils.AnkiOptions.Clear();
            AnkiConnectUtils.AnkiOptions.Clear();
        }

        NotifyWhenMiningSucceeds = ConfigDBManager.GetValueFromConfig(connection, configs, NotifyWhenMiningSucceeds, nameof(NotifyWhenMiningSucceeds));
        TextBoxTrimWhiteSpaceCharacters = ConfigDBManager.GetValueFromConfig(connection, configs, TextBoxTrimWhiteSpaceCharacters, nameof(TextBoxTrimWhiteSpaceCharacters));
        TextBoxRemoveNewlines = ConfigDBManager.GetValueFromConfig(connection, configs, TextBoxRemoveNewlines, nameof(TextBoxRemoveNewlines));
        CheckForJLUpdatesOnStartUp = ConfigDBManager.GetValueFromConfig(connection, configs, CheckForJLUpdatesOnStartUp, nameof(CheckForJLUpdatesOnStartUp));
        CaptureTextFromClipboard = ConfigDBManager.GetValueFromConfig(connection, configs, CaptureTextFromClipboard, nameof(CaptureTextFromClipboard));
        MinCharactersPerMinuteBeforeStoppingTimeTracking = ConfigDBManager.GetValueFromConfig(connection, configs, MinCharactersPerMinuteBeforeStoppingTimeTracking, nameof(MinCharactersPerMinuteBeforeStoppingTimeTracking));
        LookupCategory = ConfigDBManager.GetValueEnumValueFromConfig(connection, configs, LookupCategory, nameof(LookupCategory));
        MpvNamedPipePath = ConfigDBManager.GetValueFromConfig(connection, configs, MpvNamedPipePath, nameof(MpvNamedPipePath));

        transaction.Commit();

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

        TrackTermLookupCounts = ConfigDBManager.GetValueFromConfig(connection, configs, TrackTermLookupCounts, nameof(TrackTermLookupCounts));
        if (!TrackTermLookupCounts)
        {
            StatsUtils.SessionStats.TermLookupCountDict.Clear();
        }
    }
}
