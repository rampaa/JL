namespace JL.Core;

public class CoreConfig
{
    public Uri AnkiConnectUri { get; protected set; } = new("http://localhost:8765");
    public bool KanjiMode { get; set; } = false;
    public bool ForceSyncAnki { get; protected set; } = false;
    public bool AllowDuplicateCards { get; protected set; } = false;
    public int LookupRate { get; protected set; } = 0;
}
