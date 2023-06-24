namespace JL.Core.Dicts.EDICT.JMnedict;

internal ref struct JmnedictEntry
{
    public int Id { get; set; }
    public List<string> KebList { get; }
    public List<string> RebList { get; }
    public List<Translation> TranslationList { get; }

    public JmnedictEntry()
    {
        Id = 0;
        KebList = new List<string>();
        RebList = new List<string>();
        TranslationList = new List<Translation>();
    }
}
