namespace JL.Core.Dicts.EDICT.JMdict;

internal sealed class Sense
{
    public List<string> StagKList { get; } // Meaning only valid for these kebs.
    public List<string> StagRList { get; } // Meaning only valid for these rebs.
    public List<string> PosList { get; } // e.g. "noun"
    public List<string> FieldList { get; } // e.g. "martial arts"
    public List<string> MiscList { get; } // e.g. "abbr"
    public string? SInf { get; set; } // e.g. "often derog"
    public List<string> DialList { get; } // e.g. ksb
    public List<string> GlossList { get; } // English meaning
    public List<string> XRefList { get; } // Related terms
    public List<string> AntList { get; } // Antonyms
    public List<LoanwordSource> LSourceList { get; }

    public Sense()
    {
        StagKList = new List<string>();
        StagRList = new List<string>();
        PosList = new List<string>();
        FieldList = new List<string>();
        MiscList = new List<string>();
        DialList = new List<string>();
        GlossList = new List<string>();
        XRefList = new List<string>();
        AntList = new List<string>();
        LSourceList = new List<LoanwordSource>();
    }
}
