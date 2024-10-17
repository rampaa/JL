namespace JL.Core.Dicts.JMdict;

internal sealed class KanjiElement(string keb, List<string> keInfList)
{
    public string Keb { get; } = keb; // e.g. 娘
    public List<string> KeInfList { get; } = keInfList; // e.g. Ateji.
    // public List<string> KePriList { get; } // e.g. gai1
}
