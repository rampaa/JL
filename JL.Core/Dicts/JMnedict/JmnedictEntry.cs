using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMnedict;

[method: JsonConstructor]
internal readonly ref struct JmnedictEntry(int id, List<string> kebList, string[] rebArray, List<Translation> translationList)
{
    public int Id { get; } = id;
    public List<string> KebList { get; } = kebList;
    public string[] RebArray { get; } = rebArray;
    public List<Translation> TranslationList { get; } = translationList;
}
