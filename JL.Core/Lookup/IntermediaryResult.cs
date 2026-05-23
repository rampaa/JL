using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Lookup;

internal class IntermediaryResult(
    string matchedText,
    Dict dict,
    IList<IDictRecord> resultsList,
    string? deconjugatedMatchedText = null,
    List<List<ProcessNode>>? processListList = null) : IEquatable<IntermediaryResult>
{
    public string MatchedText { get; } = matchedText;
    public Dict Dict { get; } = dict;
    public IList<IDictRecord> Results { get; } = resultsList;
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public List<List<ProcessNode>>? Processes { get; } = processListList;

    public override bool Equals(object? obj)
    {
        return obj is IntermediaryResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * MatchedText.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + (DeconjugatedMatchedText?.GetHashCode(StringComparison.Ordinal) ?? 0);
            hash = (hash * 37) + Dict.GetHashCode();
            foreach (IDictRecord result in Results)
            {
                hash = (hash * 37) + result.GetHashCode();
            }

            if (Processes is not null)
            {
                foreach (List<ProcessNode> processList in Processes)
                {
                    foreach (ProcessNode process in processList)
                    {
                        hash = (hash * 37) + process.GetHashCode();
                    }
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public static bool operator ==(IntermediaryResult left, IntermediaryResult right) => left.Equals(right);

    public static bool operator !=(IntermediaryResult left, IntermediaryResult right) => !(left == right);

    public bool Equals(IntermediaryResult? other)
    {
        return other is not null
            && MatchedText == other.MatchedText
            && DeconjugatedMatchedText == other.DeconjugatedMatchedText
            && Dict.Equals(other.Dict)
            && Results.SequenceEqual(other.Results)
            && (Processes is not null
                ? other.Processes is not null && Processes.SequenceEqual(other.Processes)
                : other.Processes is null);
    }
}
