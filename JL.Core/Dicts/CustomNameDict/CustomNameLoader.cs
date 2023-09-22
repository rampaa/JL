using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomNameDict;

public static class CustomNameLoader
{
    internal static void Load(Dict dict, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            Dictionary<string, IList<IDictRecord>> customNameDictionary = dict.Contents;

            foreach (string line in File.ReadLines(fullPath))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    customNameDictionary.Clear();
                    break;
                }

                string[] lParts = line.Split("\t", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (lParts.Length is 3)
                {
                    AddToDictionary(lParts[0], lParts[1], lParts[2], customNameDictionary);
                }
            }
        }
    }

    public static void AddToDictionary(string spelling, string reading, string nameType, Dictionary<string, IList<IDictRecord>> customNameDictionary)
    {
        CustomNameRecord newNameRecord = new(spelling, reading, nameType);
        if (customNameDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(spelling), out IList<IDictRecord>? entry))
        {
            if (!entry.Contains(newNameRecord))
            {
                entry.Add(newNameRecord);
            }
        }

        else
        {
            customNameDictionary.Add(JapaneseUtils.KatakanaToHiragana(spelling),
                new List<IDictRecord> { newNameRecord });
        }
    }
}
