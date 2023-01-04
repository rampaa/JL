namespace JL.Core.Dicts.EDICT.JMdict;

public class KanjiElement
{
    public string? Keb { get; set; } //e.g. 娘

    public List<string> KeInfList { get; } //e.g. Ateji.
    // public List<string> KePriList { get; set; } // e.g. gai1

    public KanjiElement()
    {
        KeInfList = new List<string>();
        // KePriList = new List<string>();
    }
}
