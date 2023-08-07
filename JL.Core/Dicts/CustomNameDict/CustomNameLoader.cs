using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomNameDict;

public static class CustomNameLoader
{
    internal static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            string[] lines = await File.ReadAllLinesAsync(fullPath)
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (lParts.Length is 3)
                {
                    AddToDictionary(lParts[0], lParts[1], lParts[2], dict.Contents);
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
