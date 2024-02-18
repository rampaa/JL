namespace JL.Core.Dicts.JMnedict;

internal sealed class Translation
{
    public List<string> NameTypeList { get; }
    public List<string> TransDetList { get; }
    //public List<string> XRefList { get; }

    public Translation()
    {
        NameTypeList = new List<string>();
        TransDetList = new List<string>();
        //XRefList = new List<string>();
    }
}
