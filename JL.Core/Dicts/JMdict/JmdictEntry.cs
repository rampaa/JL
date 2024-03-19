namespace JL.Core.Dicts.JMdict;

internal ref struct JmdictEntry
{
    public int Id { get; set; }
    public List<KanjiElement> KanjiElements { get; }
    public List<ReadingElement> ReadingElements { get; }
    public List<Sense> SenseList { get; }

    public JmdictEntry()
    {
        Id = 0;
        KanjiElements = [];
        ReadingElements = [];
        SenseList = [];
    }
}
