namespace JL.Core;

public class CoreConfig
{
    public bool AnkiIntegration { get; set; } = false;
    public string AnkiConnectUri { get; set; } = "http://localhost:8765";
    public string FrequencyListName { get; set; } = "VN";
    public bool KanjiMode { get; set; } = false;
    public bool ForceSyncAnki { get; set; } = false;
    public bool AllowDuplicateCards { get; set; } = false;
    public int LookupRate { get; set; } = 0;
}
