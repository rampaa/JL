namespace JL.Core.Dicts.JMnedict;

internal ref struct JmnedictEntry()
{
    public int Id { get; set; } = 0;
    public List<string> KebList { get; } = [];
    public List<string> RebList { get; } = [];
    public List<Translation> TranslationList { get; } = [];
}
