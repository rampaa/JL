namespace JL.Core.Dicts.JMnedict;

internal sealed class Translation(List<string> nameTypeList, List<string> transDetList)
{
    public List<string> NameTypeList { get; } = nameTypeList;
    public List<string> TransDetList { get; } = transDetList;
    //public List<string> XRefList { get; }
}
