namespace JL.Core.Dicts.CustomNameDict;

public static class CustomNameLoader
{
    public static async Task Load(string customNameDictPath)
    {
        if (File.Exists(customNameDictPath))
        {
            string[] lines = await File.ReadAllLinesAsync(customNameDictPath)
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t");

                if (lParts.Length == 3)
                {
                    AddToDictionary(lParts[0].Trim(), lParts[1].Trim(), lParts[2].Trim());
                }
            }
        }
    }

    public static void AddToDictionary(string spelling, string reading, string nameType)
    {
        CustomNameRecord newNameRecord = new(spelling, reading, nameType);

        Dictionary<string, List<IDictRecord>> customNameDictionary = Storage.Dicts.Values.First(dict => dict.Type == DictType.CustomNameDictionary).Contents;

        if (customNameDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(spelling), out List<IDictRecord>? entry))
        {
            if (!entry.Contains(newNameRecord))
            {
                entry.Add(newNameRecord);
            }
        }

        else
        {
            customNameDictionary.Add(Kana.KatakanaToHiraganaConverter(spelling),
                new List<IDictRecord> { newNameRecord });
        }
    }
}
