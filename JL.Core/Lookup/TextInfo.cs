using System.Collections.Concurrent;
using JL.Core.Deconjugation;
using JL.Core.Dicts.Interfaces;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Lookup;
internal sealed class TextInfo(List<string> textList,
    List<string> textInHiraganaList,
    List<List<Form>> deconjugationResultsList,
    List<List<List<Form>>?>? deconjugatedTextWithoutLongVowelMarksList,
    List<List<string>?>? textWithoutLongVowelMarksList,
    int textWithoutLongVowelMarksCount,
    string[]? deconjugatedTexts,
    ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? frequencyDicts,
    IDictionary<string, IList<IDictRecord>>? pitchAccentDict) : IEquatable<TextInfo>
{
    public List<string> TextList { get; } = textList;
    public List<string> TextInHiraganaList { get; } = textInHiraganaList;
    public List<List<Form>> DeconjugationResultsList { get; } = deconjugationResultsList;
    public List<List<List<Form>>?>? DeconjugatedTextWithoutLongVowelMarksList { get; } = deconjugatedTextWithoutLongVowelMarksList;
    public List<List<string>?>? TextWithoutLongVowelMarksList { get; } = textWithoutLongVowelMarksList;
    public int TextWithoutLongVowelMarksCount { get; } = textWithoutLongVowelMarksCount;
    public string[]? DeconjugatedTexts { get; } = deconjugatedTexts;
    public ConcurrentDictionary<string, Dictionary<string, List<FrequencyRecord>>>? FrequencyDicts { get; } = frequencyDicts;
    public IDictionary<string, IList<IDictRecord>>? PitchAccentDict { get; } = pitchAccentDict;

    public bool Equals(TextInfo? other)
    {
        return other?.TextList.AsReadOnlySpan().SequenceEqual(TextList.AsReadOnlySpan()) ?? false;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (string text in TextList.AsReadOnlySpan())
            {
                hash = (hash * 37) + text.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(TextInfo? left, TextInfo? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(TextInfo? left, TextInfo? right) => !(left == right);
}
