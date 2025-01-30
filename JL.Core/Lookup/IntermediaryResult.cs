using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Lookup;

internal sealed class IntermediaryResult(
    string matchedText,
    Dict dict,
    List<IList<IDictRecord>> resultsList,
    string? deconjugatedMatchedText = null,
    List<List<List<string>>>? processListList = null)
{
    public string MatchedText { get; } = matchedText;
    public Dict Dict { get; } = dict;
    public List<IList<IDictRecord>> Results { get; } = resultsList;
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public List<List<List<string>>>? Processes { get; } = processListList;
}
