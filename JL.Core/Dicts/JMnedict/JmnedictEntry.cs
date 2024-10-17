namespace JL.Core.Dicts.JMnedict;

internal readonly ref struct JmnedictEntry(int id, List<string> kebList, List<string> rebList, List<Translation> translationList)
{
    public int Id { get; } = id;
    public List<string> KebList { get; } = kebList;
    public List<string> RebList { get; } = rebList;
    public List<Translation> TranslationList { get; } = translationList;
}
