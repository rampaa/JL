using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMdict;

[method: JsonConstructor]
internal readonly ref struct JmdictEntry(int id, List<KanjiElement> kanjiElements, List<ReadingElement> readingElements, List<Sense> senseList)
{
    public int Id { get; } = id;
    public List<KanjiElement> KanjiElements { get; } = kanjiElements;
    public List<ReadingElement> ReadingElements { get; } = readingElements;
    public List<Sense> SenseList { get; } = senseList;
}
