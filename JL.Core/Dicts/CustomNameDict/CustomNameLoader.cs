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

        foreach (string line in File.ReadLines(fullPath))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                customNameDictionary.Clear();
                break;
            }

            string[] lParts = line.Split('\t', StringSplitOptions.TrimEntries);

            if (lParts.Length >= 3)
            {
                string spelling = lParts[0];

                string? reading = lParts[1];
                if (reading.Length is 0 || reading == spelling)
                {
                    reading = null;
                }

                string nameType = lParts[2];

                string? extraInfo = null;
                if (lParts.Length is 4)
                {
                    extraInfo = lParts[3];
                    extraInfo = extraInfo.Length is 0
                        ? null
                        : extraInfo.Replace("\\n", "\n", StringComparison.Ordinal);
                }

                AddToDictionary(spelling, reading, nameType, extraInfo, customNameDictionary);
            }
        }
    }

    public static void AddToDictionary(string spelling, string? reading, string nameType, string? extraInfo, IDictionary<string, IList<IDictRecord>> customNameDictionary)
    {
        CustomNameRecord newNameRecord = new(spelling, reading, nameType, extraInfo);

        string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(spelling);
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
