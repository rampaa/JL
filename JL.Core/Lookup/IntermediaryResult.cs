using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Lookup;

internal sealed class IntermediaryResult(
    string matchedText,
    Dict dict,
    IList<IDictRecord> resultsList,
    string? deconjugatedMatchedText = null,
    List<List<ProcessNode>>? processListList = null)
{
    public string MatchedText { get; } = matchedText;
    public Dict Dict { get; } = dict;
    public IList<IDictRecord> Results { get; } = resultsList;
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public List<List<ProcessNode>>? Processes { get; } = processListList;
}
