using System.Buffers;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING;

internal static class EpwingUtils
{
    private static readonly SearchValues<char> s_invalidCharacters = SearchValues.Create('�', '〓', '\n');

    public static bool IsValidEpwingResultForDictType<T>(string primarySpelling, string? reading, string[] definitions, Dict<T> dict) where T : IEpwingRecord
    {
        return !MemoryExtensions.ContainsAny(primarySpelling, s_invalidCharacters)
            && FilterDuplicateEntries(primarySpelling, reading, definitions, dict);
    }

    private static bool FilterDuplicateEntries<T>(string primarySpelling, string? reading, string[] definitions, Dict<T> dict) where T : IEpwingRecord
    {
        if (dict.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out IList<T>? previousResults))
        {
            int previousResultCount = previousResults.Count;
            for (int i = 0; i < previousResultCount; i++)
            {
                IEpwingRecord previousResult = previousResults[i];

                if (previousResult.Definitions.AsReadOnlySpan().SequenceEqual(definitions))
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
                IEpwingRecord previousResult = previousResults[i];

                if (previousResult.Definitions.AsReadOnlySpan().SequenceEqual(definitions))
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
