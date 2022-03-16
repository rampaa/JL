using JL.Core.Dicts;

namespace JL.Core.Lookup
{
    public class IntermediaryResult
    {
        public List<IResult> ResultsList { get; }
        public List<List<string>> ProcessListList { get; }
        public string FoundForm { get; }
        public DictType DictType { get; }

        public IntermediaryResult(List<IResult> resultsList, List<List<string>> processListList, string foundForm,
            DictType dictType)
        {
            ResultsList = resultsList;
            ProcessListList = processListList;
            FoundForm = foundForm;
            DictType = dictType;
        }
    }
}
