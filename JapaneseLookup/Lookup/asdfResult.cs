using System.Collections.Generic;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.Lookup
{
    // TODO: find a better name
    public class AsdfResult
    {
        public List<IResult> ResultsList { get; }
        public List<string> ProcessList { get; }
        public string FoundForm { get; }
        public DictType DictType { get; }

        public AsdfResult(List<IResult> resultsList, List<string> processList, string foundForm, DictType dictType)
        {
            ResultsList = resultsList;
            ProcessList = processList;
            FoundForm = foundForm;
            DictType = dictType;
        }
    }
}