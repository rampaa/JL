namespace JL.Core.Dicts.EDICT.JMnedict;

public class JMnedictEntry
{
    public int Id { get; set; }
    public List<string> KebList { get; set; }
    public List<string> RebList { get; set; }
    public List<Trans> TransList { get; set; }

    public JMnedictEntry()
    {
        KebList = new List<string>();
        RebList = new List<string>();
        TransList = new List<Trans>();
    }
}
