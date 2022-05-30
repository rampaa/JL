using JL.Core.Dicts;

namespace JL.Core.Lookup;

public class IntermediaryResult
{
    public List<IResult> ResultsList { get; }
    public List<List<string>>? ProcessListList { get; }
    public string FoundForm { get; }
    public Dict Dict { get; }

    public IntermediaryResult(List<IResult> resultsList, List<List<string>>? processListList, string foundForm,
        Dict dict)
    {
        ResultsList = resultsList;
        ProcessListList = processListList;
        FoundForm = foundForm;
        Dict = dict;
    }
}
