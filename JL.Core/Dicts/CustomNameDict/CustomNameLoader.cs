using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomNameDict;

public static class CustomNameLoader
{
    internal static void Load(Dict dict, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> customNameDictionary = dict.Contents;

        Span<Range> tabRanges = stackalloc Range[4];
        foreach (string line in File.ReadLines(fullPath))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                customNameDictionary.Clear();
                break;
            }

            ReadOnlySpan<char> lineSpan = line.AsSpan();
            int tabCount = lineSpan.Split(tabRanges, '\t', StringSplitOptions.TrimEntries);

            if (tabCount >= 3)
            {
                string? extraInfo = null;
                if (tabCount is 4)
                {
                    ReadOnlySpan<char> extraInfoSpan = lineSpan[tabRanges[3]];
                    extraInfo = extraInfoSpan.Length is 0
                        ? null
                        : extraInfoSpan.ToString().Replace("\\n", "\n", StringComparison.Ordinal);
                }

                string nameType = lineSpan[tabRanges[2]].ToString();
                string? reading = lineSpan[tabRanges[1]].ToString();
                string spelling = lineSpan[tabRanges[0]].ToString();

                if (reading.Length is 0 || reading == spelling)
                {
                    reading = null;
                }

                AddToDictionary(spelling, reading, nameType, extraInfo, customNameDictionary);
            }
        }
    }

    public static void AddToDictionary(string spelling, string? reading, string nameType, string? extraInfo, IDictionary<string, IList<IDictRecord>> customNameDictionary)
    {
        CustomNameRecord newNameRecord = new(spelling, reading, nameType, extraInfo);

        string spellingInHiragana = JapaneseUtils.NormalizeText(spelling);
        if (customNameDictionary.TryGetValue(spellingInHiragana, out IList<IDictRecord>? entry))
        {
            //int entryIndex = entry.IndexOf(newNameRecord);
            //if (entryIndex >= 0)
            //{
            //    entry.RemoveAt(entryIndex);
            //}

            //entry.Add(newNameRecord);

            if (!entry.Contains(newNameRecord))
            {
                entry.Add(newNameRecord);
            }
        }

        else
        {
            customNameDictionary[spellingInHiragana] = [newNameRecord];
        }
    }
}
