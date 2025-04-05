using System.Buffers;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    private static readonly SearchValues<char> s_invalidCharacters = SearchValues.Create('�', '〓', '\n');

    public static bool IsValidEpwingResultForDictType(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        return !MemoryExtensions.ContainsAny(primarySpelling, s_invalidCharacters)
            && FilterDuplicateEntries(primarySpelling, reading, definitions, dict);
    }

    private static bool FilterDuplicateEntries(string primarySpelling, string? reading, string[] definitions, Dict dict)
    {
        if (dict.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<IDictRecord>? previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.AsSpan().SequenceEqual(definitions))
                {
                    // If an entry has reading info while others don't, keep the one with the reading info.
                    if (previousResult.Reading is null && reading is not null)
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

        else if (reading is not null && dict.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = (IEpwingRecord)previousResults[i];

                if (previousResult.Definitions.AsSpan().SequenceEqual(definitions))
                {
                    if (previousResult.Reading is null)
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
