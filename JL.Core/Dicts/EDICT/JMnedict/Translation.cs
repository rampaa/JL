namespace JL.Core.Dicts.EDICT.JMnedict;

public class Translation
{
    public List<string> NameTypeList { get; }

    public List<string> TransDetList { get; }
    // public List<string> XRefList { get; set; }

    public Translation()
    {
        NameTypeList = new List<string>();
        TransDetList = new List<string>();
        // XRefList = new List<string>();
    }
}
