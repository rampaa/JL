using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Lookup;

internal sealed class IntermediaryResult<T>(
    string matchedText,
    Dict<T> dict,
    List<IList<T>> resultsList,
    string? deconjugatedMatchedText = null,
    List<List<List<string>>>? processListList = null) where T : IDictRecord
{
    public string MatchedText { get; } = matchedText;
    public Dict<T> Dict { get; } = dict;
    public List<IList<T>> Results { get; } = resultsList;
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public List<List<List<string>>>? Processes { get; } = processListList;
}
