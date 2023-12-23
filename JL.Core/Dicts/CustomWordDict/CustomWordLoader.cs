using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomWordDict;

public static class CustomWordLoader
{
    private static readonly string[] s_verbs = {
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

    private static readonly string[] s_adjectives = {
        "adj-i",
        "adj-na"
    };

    private static readonly string[] s_noun = {
        "noun"
    };

    private static readonly string[] s_other = {
        "other"
    };

    internal static void Load(Dict dict, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            Dictionary<string, IList<IDictRecord>> customWordDictionary = dict.Contents;

            foreach (string line in File.ReadLines(fullPath))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    customWordDictionary.Clear();
                    break;
                }

                string[] lParts = line.Split("\t", StringSplitOptions.TrimEntries);
                if (lParts.Length >= 4)
                {
                    string[] spellings = lParts[0].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    string[]? readings = lParts[1].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (readings.Length is 0)
                    {
                        readings = null;
                    }

                    string[] definitions = lParts[2].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    string partOfSpeech = lParts[3];

                    string[]? wordClasses = null;
                    if (lParts.Length is 5)
                    {
                        wordClasses = lParts[4].Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    }

                    AddToDictionary(spellings, readings, definitions, partOfSpeech, wordClasses, customWordDictionary);
                }
            }
        }
    }

    public static void AddToDictionary(string[] spellings, string[]? readings, string[] definitions,
        string rawPartOfSpeech, string[]? wordClasses, Dictionary<string, IList<IDictRecord>> customWordDictionary)
    {
        bool hasUserDefinedWordClasses = wordClasses?.Length > 0;
        string[] wordClassArray = rawPartOfSpeech switch
        {
            "Verb" => hasUserDefinedWordClasses ? wordClasses! : s_verbs,
            "Adjective" => s_adjectives,
            "Noun" => s_noun,
            _ => s_other
        };

        for (int i = 0; i < spellings.Length; i++)
        {
            string[]? alternativeSpellings = spellings.RemoveAt(i);
            if (alternativeSpellings.Length is 0)
            {
                alternativeSpellings = null;
            }

            string spelling = spellings[i];

            CustomWordRecord newWordRecord = new(spelling, alternativeSpellings, readings, definitions, wordClassArray, hasUserDefinedWordClasses);

            if (!AddRecordToDictionary(spelling, newWordRecord, customWordDictionary))
            {
                return;
            }

            if (i is 0 && readings is not null)
            {
                for (int j = 0; j < readings.Length; j++)
                {
                    if (!AddRecordToDictionary(readings[j], newWordRecord, customWordDictionary))
                    {
                        return;
                    }
                }
            }
        }
    }

    private static bool AddRecordToDictionary(string spelling, CustomWordRecord record, Dictionary<string, IList<IDictRecord>> dictionary)
    {
        string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(spelling);
        if (dictionary.TryGetValue(spellingInHiragana, out IList<IDictRecord>? result))
        {
            if (result.Contains(record))
            {
                return false;
            }

            result.Add(record);
        }
        else
        {
            dictionary[spellingInHiragana] = new List<IDictRecord> { record };
        }

        return true;
    }
}
