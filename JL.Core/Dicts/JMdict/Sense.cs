namespace JL.Core.Dicts.JMdict;

internal sealed class Sense(
    string? sInf,
    List<string> stagKList,
    List<string> stagRList,
    List<string> posList,
    List<string> fieldList,
    List<string> miscList,
    List<string> dialList,
    List<string> glossList,
    List<string> xRefList,
    List<string> antList,
    List<LoanwordSource> lSourceList)
{
    public List<string> StagKList { get; } = stagKList; // Meaning only valid for these kebs.
    public List<string> StagRList { get; } = stagRList; // Meaning only valid for these rebs.
    public List<string> PosList { get; } = posList; // e.g. "noun"
    public List<string> FieldList { get; } = fieldList; // e.g. "martial arts"
    public List<string> MiscList { get; } = miscList; // e.g. "abbr"
    public string? SInf { get; } = sInf; // e.g. "often derog"
    public List<string> DialList { get; } = dialList; // e.g. ksb
    public List<string> GlossList { get; } = glossList; // English meaning
    public List<string> XRefList { get; } = xRefList; // Related terms
    public List<string> AntList { get; } = antList; // Antonyms
    public List<LoanwordSource> LSourceList { get; } = lSourceList;
}
