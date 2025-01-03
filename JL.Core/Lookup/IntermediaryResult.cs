using JL.Core.Dicts;

namespace JL.Core.Lookup;

internal sealed class IntermediaryResult(
    List<IList<IDictRecord>> resultsList,
    List<List<List<string>>>? processListList,
    string matchedText,
    string? deconjugatedMatchedText,
    Dict dict)
{
    public List<IList<IDictRecord>> Results { get; } = resultsList;
    public List<List<List<string>>>? Processes { get; } = processListList;
    public string MatchedText { get; } = matchedText;
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public Dict Dict { get; } = dict;
}
