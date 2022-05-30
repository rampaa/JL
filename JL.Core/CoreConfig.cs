namespace JL.Core;

public class CoreConfig
{
    public string AnkiConnectUri { get; protected set; } = "http://localhost:8765";
    public string FrequencyListName { get; protected set; } = "VN";
    public bool KanjiMode { get; set; } = false;
    public bool ForceSyncAnki { get; protected set; } = false;
    public bool AllowDuplicateCards { get; protected set; } = false;
    public int LookupRate { get; protected set; } = 0;
}
