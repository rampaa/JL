namespace JL.Core.Dicts.EDICT.JMnedict;

public class JmnedictEntry
{
    public int Id { get; set; }
    public List<string> KebList { get; }
    public List<string> RebList { get; }
    public List<Translation> TransList { get; }

    public JmnedictEntry()
    {
        KebList = new List<string>();
        RebList = new List<string>();
        TransList = new List<Translation>();
    }
}
