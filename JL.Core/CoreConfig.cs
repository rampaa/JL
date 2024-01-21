namespace JL.Core;

public static class CoreConfig
{
    public static Uri AnkiConnectUri { get; set; } = new("http://127.0.0.1:8765");
    public static bool KanjiMode { get; set; } = false;
    public static bool AnkiIntegration { get; set; } = false;
    public static bool ForceSyncAnki { get; set; } = false;
    public static bool AllowDuplicateCards { get; set; } = false;
    public static int LookupRate { get; set; } = 0;
    public static bool CaptureTextFromClipboard { get; set; } = true;
    public static bool CaptureTextFromWebSocket { get; set; } = false;
    public static Uri WebSocketUri { get; set; } = new("ws://127.0.0.1:6677");
    public static int AudioVolume { get; set; } = 100;
}
