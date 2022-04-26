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
        CustomNameEntry newNameEntry = new(spelling, reading, nameType);

        Dictionary<string, List<IResult>> customNameDictionary = Storage.Dicts[DictType.CustomNameDictionary].Contents;

        if (customNameDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(spelling), out List<IResult>? entry))
        {
            if (!entry.Contains(newNameEntry))
            {
                entry.Add(newNameEntry);
            }
        }

        else
        {
            customNameDictionary.Add(Kana.KatakanaToHiraganaConverter(spelling),
                new List<IResult> { newNameEntry });
        }
    }
}
