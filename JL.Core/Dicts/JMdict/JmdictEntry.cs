namespace JL.Core.Dicts.JMdict;

internal ref struct JmdictEntry()
{
    public int Id { get; set; } = 0;
    public List<KanjiElement> KanjiElements { get; } = [];
    public List<ReadingElement> ReadingElements { get; } = [];
    public List<Sense> SenseList { get; } = [];
}
