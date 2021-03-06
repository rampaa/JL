namespace JL.Core.Dicts.EDICT.JMdict;

public class JMdictResult : IResult
{
    public int Id { get; set; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<List<string>> Definitions { get; set; }
    public List<List<string>?>? RRestrictions { get; set; }
    public List<List<string>?>? KRestrictions { get; set; }
    public List<string>? Readings { get; set; }
    public List<string>? POrthographyInfoList { get; set; }
    public List<List<string>?>? AOrthographyInfoList { get; set; }
    public List<List<string>?>? ROrthographyInfoList { get; set; }
    public List<List<string>?>? WordClasses { get; set; } //e.g. noun +
    public List<List<string>?>? FieldList { get; set; } // e.g. "martial arts"
    public List<List<string>?>? MiscList { get; set; } // e.g. "abbr" +
    public List<string?>? DefinitionInfo { get; set; } // e.g. "often derog" +
    public List<List<string>?>? Dialects { get; set; } // e.g. ksb

    public string PrimarySpelling { get; set; }
    //public List<string> PriorityList { get; set; } // e.g. gai1
    public List<List<string>?>? RelatedTerms { get; set; }
    public List<List<string>?>? Antonyms { get; set; }
    public List<List<LSource>?>? LoanwordEtymology { get; set; }

    public JMdictResult()
    {
        PrimarySpelling = string.Empty;
        Definitions = new List<List<string>>();
        RRestrictions = new List<List<string>?>();
        KRestrictions = new List<List<string>?>();
        Readings = new List<string>();
        AlternativeSpellings = new List<string>();
        POrthographyInfoList = new List<string>();
        AOrthographyInfoList = new List<List<string>?>();
        ROrthographyInfoList = new List<List<string>?>();
        WordClasses = new List<List<string>?>();
        FieldList = new List<List<string>?>();
        MiscList = new List<List<string>?>();
        DefinitionInfo = new List<string?>();
        Dialects = new List<List<string>?>();
        //PriorityList = new List<string>();
        RelatedTerms = new List<List<string>?>();
        Antonyms = new List<List<string>?>();
        LoanwordEtymology = new List<List<LSource>?>();
    }
}
