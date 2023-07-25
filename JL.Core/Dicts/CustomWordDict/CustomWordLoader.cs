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
                    string[] spellings = lParts[0].Split(';');

                    string[]? readings = lParts[1].Split(';');

                    if (readings.Length is 0)
                    {
                        readings = null;
                    }

                    string[] definitions = lParts[2].Split(';');
                    string partOfSpeech = lParts[3].Trim();

                    string[]? wordClasses = null;
                    if (lParts.Length is 5)
                    {
                        wordClasses = lParts[4].Split(';');
                    }

                    AddToDictionary(spellings, readings, definitions, partOfSpeech, wordClasses);
                }
            }
        }
    }

    public static void AddToDictionary(string[] spellings, string[]? readings, string[] definitions,
        string rawPartOfSpeech, string[]? wordClasses)
    {
        for (int i = 0; i < spellings.Length; i++)
        {
            string[]? alternativeSpellings = spellings.RemoveAt(i);
            if (alternativeSpellings.Length is 0)
            {
                alternativeSpellings = null;
            }

            string spelling = spellings[i];

            bool hasUserDefinedWordClasses = wordClasses?.Length > 0;
            string[] wordClassArray;

            switch (rawPartOfSpeech)
            {
                case "Verb":
                    if (hasUserDefinedWordClasses)
                    {
                        wordClassArray = wordClasses!;
                    }
                    else
                    {
                        wordClassArray = new[] {
                            "v1",
                            "v1-s",
                            "v4r",
                            "v5aru",
                            "v5b",
                            "v5g",
                            "v5k",
                            "v5k-s",
                            "v5m",
                            "v5n",
                            "v5r",
                            "v5r-i",
                            "v5s",
                            "v5t",
                            "v5u",
                            "v5u-s",
                            "vk",
                            "vs-c",
                            "vs-i",
                            "vs-s",
                            "vz"
                        };
                    }

                    break;
                case "Adjective":
                    wordClassArray = new[] {
                        "adj-i",
                        "adj-na"
                    };

                    break;
                case "Noun":
                    wordClassArray = new[] {
                        "noun"
                    };

                    break;
                default:
                    wordClassArray = new[] {
                        "other"
                    };

                    break;
            }

            CustomWordRecord newWordRecord = new(spelling, alternativeSpellings, readings, definitions, wordClassArray, hasUserDefinedWordClasses);

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
