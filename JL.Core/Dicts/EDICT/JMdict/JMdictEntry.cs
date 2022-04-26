namespace JL.Core.Dicts.EDICT.JMdict;

public class JMdictEntry
{
    public string Id { get; set; }
    public List<KanjiElement> KanjiElements { get; set; }
    public List<ReadingElement> ReadingElements { get; set; }
    public List<Sense> SenseList { get; set; }

    public JMdictEntry()
    {
        KanjiElements = new List<KanjiElement>();
        ReadingElements = new List<ReadingElement>();
        SenseList = new List<Sense>();
        Id = string.Empty;
    }
}
