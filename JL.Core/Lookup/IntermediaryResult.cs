using JL.Core.Dicts;

namespace JL.Core.Lookup;

public class IntermediaryResult
{
    public List<List<IResult>> Results { get; }
    public List<List<List<string>>>? Processes { get; }
    public string FoundForm { get; }
    public string DeconjugatedFoundForm { get; }
    public Dict Dict { get; }

    public IntermediaryResult(List<List<IResult>> resultsList, List<List<List<string>>>? processListList, string foundForm, string deconjugatedFoundForm,
        Dict dict)
    {
        Results = resultsList;
        Processes = processListList;
        FoundForm = foundForm;
        DeconjugatedFoundForm = deconjugatedFoundForm;
        Dict = dict;
    }
}
