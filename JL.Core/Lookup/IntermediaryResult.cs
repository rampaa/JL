using JL.Core.Dicts;

namespace JL.Core.Lookup;

internal sealed class IntermediaryResult
{
    public List<IList<IDictRecord>> Results { get; }
    public List<List<List<string>>>? Processes { get; }
    public string MatchedText { get; }
    public string DeconjugatedMatchedText { get; }
    public Dict Dict { get; }

    public IntermediaryResult(List<IList<IDictRecord>> resultsList, List<List<List<string>>>? processListList, string matchedText, string deconjugatedMatchedText,
        Dict dict)
    {
        Results = resultsList;
        Processes = processListList;
        MatchedText = matchedText;
        DeconjugatedMatchedText = deconjugatedMatchedText;
        Dict = dict;
    }
}
