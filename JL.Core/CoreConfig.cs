namespace JL.Core;

public class CoreConfig
{
    public Uri AnkiConnectUri { get; set; } = new("http://127.0.0.1:8765");
    public bool KanjiMode { get; set; } = false;
    public bool ForceSyncAnki { get; protected set; } = false;
    public bool AllowDuplicateCards { get; protected set; } = false;
    public int LookupRate { get; protected set; } = 0;
    public bool CaptureTextFromWebSocket { get; set; } = false;
    public Uri WebSocketUri { get; protected set; } = new("ws://127.0.0.1:6677");
    public int AudioVolume { get; protected set; } = 100;
}
