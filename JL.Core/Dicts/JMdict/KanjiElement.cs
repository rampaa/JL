namespace JL.Core.Dicts.JMdict;

internal sealed class KanjiElement
{
    public string Keb { get; set; } = ""; //e.g. 娘
    public List<string> KeInfList { get; } = []; //e.g. Ateji.
    // public List<string> KePriList { get; } = [] // e.g. gai1
}
