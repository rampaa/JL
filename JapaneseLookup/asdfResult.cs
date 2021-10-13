using System.Collections.Generic;

namespace JapaneseLookup
{
    // TODO: find a better name
    public class asdfResult
    {
        public List<IResult> ResultsList { get; }
        public List<string> ProcessList { get; }
        public string FoundForm { get; }
        public DictType DictType { get; }

        public asdfResult(List<IResult> resultsList, List<string> processList, string foundForm, DictType dictType)
        {
            ResultsList = resultsList;
            ProcessList = processList;
            FoundForm = foundForm;
            DictType = dictType;
        }
    }
}