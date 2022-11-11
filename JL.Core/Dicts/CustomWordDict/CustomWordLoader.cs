namespace JL.Core.Dicts.CustomWordDict;

public static class CustomWordLoader
{
    public static async Task Load(string customWordDictPath)
    {
        if (File.Exists(customWordDictPath))
        {
            string[] lines = await File.ReadAllLinesAsync(customWordDictPath)
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t");

                if (lParts.Length == 4)
                {
                    string[] spellings = lParts[0].Split(';').Select(s => s.Trim()).ToArray();

                    List<string>? readings = lParts[1].Split(';').Select(r => r.Trim()).ToList();

                    if (!readings.Any())
                        readings = null;

                    List<string> definitions = lParts[2].Split(';').Select(d => d.Trim()).ToList();
                    string wordClass = lParts[3].Trim();

                    AddToDictionary(spellings, readings, definitions, wordClass);
                }
            }
        }
    }

    public static void AddToDictionary(string[] spellings, List<string>? readings, List<string> definitions,
        string rawWordClass)
    {
        for (int i = 0; i < spellings.Length; i++)
        {
            List<string>? alternativeSpellings = spellings.ToList();
            alternativeSpellings.RemoveAt(i);

            if (!alternativeSpellings.Any())
                alternativeSpellings = null;

            string spelling = spellings[i];

            List<string> wordClass = new();

            if (rawWordClass == "Verb")
            {
                wordClass.Add("v1");
                wordClass.Add("v1-s");
                wordClass.Add("v4r");
                wordClass.Add("v5aru");
                wordClass.Add("v5b");
                wordClass.Add("v5g");
                wordClass.Add("v5k");
                wordClass.Add("v5k-s");
                wordClass.Add("v5m");
                wordClass.Add("v5n");
                wordClass.Add("v5r");
                wordClass.Add("v5r-i");
                wordClass.Add("v5s");
                wordClass.Add("v5t");
                wordClass.Add("v5u");
                wordClass.Add("v5u-s");
                wordClass.Add("vk");
                wordClass.Add("vs-c");
                wordClass.Add("vs-i");
                wordClass.Add("vs-s");
                wordClass.Add("vz");
            }
            else if (rawWordClass == "Adjective")
            {
                wordClass.Add("adj-i");
                wordClass.Add("adj-na");
            }
            else if (rawWordClass == "Noun")
            {
                wordClass.Add("noun");
            }
            else
            {
                wordClass.Add("other");
            }

            CustomWordRecord newWordRecord = new(spelling, alternativeSpellings, readings, definitions, wordClass);

            Dictionary<string, List<IDictRecord>> customWordDictionary = Storage.Dicts.Values.First(dict => dict.Type == DictType.CustomWordDictionary).Contents;

            if (customWordDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(spelling), out List<IDictRecord>? result))
            {
                if (result.Contains(newWordRecord))
                {
                    break;
                }
                else
                {
                    result.Add(newWordRecord);
                }
            }
            else
            {
                customWordDictionary.Add(Kana.KatakanaToHiraganaConverter(spelling),
                    new List<IDictRecord> { newWordRecord });
            }
        }
    }
}
