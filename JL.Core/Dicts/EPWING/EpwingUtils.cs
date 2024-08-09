using System.Collections.Frozen;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    //'×', '‐', '。', '、', '⻖', '/', '-', '・', '+', ':', '！', '！', '●',
    //'？', '～', '〃', '､', '!', '−', '＆', '?', '&', '－', '／', '√', '$',
    //'＄', '°', '＋', ',', '®', '＼', '─', '─', '．', '■', '’', '⻌', '◎',
    //'Ⓒ', 'Ⓡ', '’', '＠', '〒', '@', '〜', '，', '㏄', '\'', '％', '#',
    //'△', '~', '%', '℃', '：', '※', '㊙', '©', '—', '‘', '△', '*', '≒',
    //'←', '↑', '↓', '☆', '.', '･'
    private static readonly FrozenSet<char> s_invalidCharacters = FrozenSet.ToFrozenSet(
    [
        '�', '〓', '㋝', '㋜',
        '（', '）', '(', ')',
        '【', '】', '「', '」',
        '［', '］', '[', ']',
        '{', '}', '〈', '〉',
        '＜', '＞', '〔', '〕',
        '《', '》', '<', '>',
        '○', '∘', '＝', '=',
        '…', '‥', ';', '；',
        '→', '━'
    ]);

    public static bool IsValidEpwingResultForDictType(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        foreach (char c in primarySpelling)
        {
            if (s_invalidCharacters.Contains(c) || char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return FilterDuplicateEntries(primarySpelling, reading, definitions, dict);
    }

    private static bool FilterDuplicateEntries(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        if (dict.Contents.TryGetValue(
                JapaneseUtils.KatakanaToHiragana(primarySpelling),
                out IList<IDictRecord>? previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.SequenceEqual(definitions))
                {
                    // If an entry has reading info while others don't, keep the one with the reading info.
                    if (string.IsNullOrEmpty(previousResult.Reading) &&
                        !string.IsNullOrEmpty(reading))
                    {
                        previousResults.RemoveAt(i);
                        break;
                    }

                    if (reading == previousResult.Reading)
                    {
                        return false;
                    }
                }
            }
        }

        else if (reading is not null && dict.Contents.TryGetValue(
                     JapaneseUtils.KatakanaToHiragana(reading),
                     out previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.SequenceEqual(definitions))
                {
                    if (string.IsNullOrEmpty(previousResult.Reading))
                    {
                        previousResults.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        return true;
    }
}
