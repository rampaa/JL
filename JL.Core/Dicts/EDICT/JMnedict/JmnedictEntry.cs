namespace JL.Core.Dicts.EDICT.JMnedict;

public class JmnedictEntry
{
    public int Id { get; set; }
    public List<string> KebList { get; }
    public List<string> RebList { get; }
    public List<Translation> TranslationList { get; }

    public JmnedictEntry()
    {
        KebList = new List<string>();
        RebList = new List<string>();
        TranslationList = new List<Translation>();
    }
}
