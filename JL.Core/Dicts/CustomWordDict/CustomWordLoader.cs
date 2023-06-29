using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomWordDict;

public static class CustomWordLoader
{
    internal static async Task Load(string customWordDictPath)
    {
        if (File.Exists(customWordDictPath))
        {
            string[] lines = await File.ReadAllLinesAsync(customWordDictPath)
                .ConfigureAwait(false);

            for (int i = 0; i < lines.Length; i++)
            {
                string[] lParts = lines[i].Split("\t");

                if (lParts.Length > 3)
                {
                    string[] spellings = lParts[0].Split(';').Select(static s => s.Trim()).ToArray();

                    List<string>? readings = lParts[1].Split(';').Select(static r => r.Trim()).ToList();

                    if (readings.Count is 0)
                    {
                        readings = null;
                    }

                    List<string> definitions = lParts[2].Split(';').Select(static d => d.Trim()).ToList();
                    string partOfSpeech = lParts[3].Trim();

                    List<string>? wordClasses = null;
                    if (lParts.Length is 5)
                    {
                        wordClasses = lParts[4].Split(';').Select(static wc => wc.Trim()).ToList();
                    }

                    AddToDictionary(spellings, readings, definitions, partOfSpeech, wordClasses);
                }
            }
        }
    }

    public static void AddToDictionary(string[] spellings, List<string>? readings, List<string> definitions,
        string rawPartOfSpeech, List<string>? wordClasses)
    {
        for (int i = 0; i < spellings.Length; i++)
        {
            List<string>? alternativeSpellings = spellings.ToList();
            alternativeSpellings.RemoveAt(i);

            if (alternativeSpellings.Count is 0)
            {
                alternativeSpellings = null;
            }

            string spelling = spellings[i];

            bool hasUserDefinedWordClasses = wordClasses?.Count > 0;
            List<string> wordClassList = new();

            switch (rawPartOfSpeech)
            {
                case "Verb":
                    if (hasUserDefinedWordClasses)
                    {
                        wordClassList.AddRange(wordClasses!);
                    }
                    else
                    {
                        wordClassList.Add("v1");
                        wordClassList.Add("v1-s");
                        wordClassList.Add("v4r");
                        wordClassList.Add("v5aru");
                        wordClassList.Add("v5b");
                        wordClassList.Add("v5g");
                        wordClassList.Add("v5k");
                        wordClassList.Add("v5k-s");
                        wordClassList.Add("v5m");
                        wordClassList.Add("v5n");
                        wordClassList.Add("v5r");
                        wordClassList.Add("v5r-i");
                        wordClassList.Add("v5s");
                        wordClassList.Add("v5t");
                        wordClassList.Add("v5u");
                        wordClassList.Add("v5u-s");
                        wordClassList.Add("vk");
                        wordClassList.Add("vs-c");
                        wordClassList.Add("vs-i");
                        wordClassList.Add("vs-s");
                        wordClassList.Add("vz");
                    }

                    break;
                case "Adjective":
                    wordClassList.Add("adj-i");
                    wordClassList.Add("adj-na");
                    break;
                case "Noun":
                    wordClassList.Add("noun");
                    break;
                default:
                    wordClassList.Add("other");
                    break;
            }

            CustomWordRecord newWordRecord = new(spelling, alternativeSpellings, readings, definitions, wordClassList, hasUserDefinedWordClasses);

            Dictionary<string, List<IDictRecord>> customWordDictionary = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.CustomWordDictionary).Contents;

            if (customWordDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(spelling), out List<IDictRecord>? result))
            {
                if (result.Contains(newWordRecord))
                {
                    break;
                }

                result.Add(newWordRecord);
            }
            else
            {
                customWordDictionary.Add(JapaneseUtils.KatakanaToHiragana(spelling),
                    new List<IDictRecord> { newWordRecord });
            }
        }
    }
}
