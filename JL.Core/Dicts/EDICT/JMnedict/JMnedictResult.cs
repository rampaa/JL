namespace JL.Core.Dicts.EDICT.JMnedict;

public class JMnedictResult : IResult
{
    public int Id { get; set; }
    public string PrimarySpelling { get; set; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<string>? Readings { get; set; }
    public List<string>? NameTypes { get; set; }
    public List<string>? Definitions { get; set; }

    public JMnedictResult()
    {
        PrimarySpelling = string.Empty;
        AlternativeSpellings = new List<string>();
        Readings = new List<string>();
        NameTypes = new List<string>();
        Definitions = new List<string>();
    }
}
