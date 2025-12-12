using System.Diagnostics;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomWordDict;

public static class CustomWordLoader
{
    private static readonly string[] s_verbs =
    [
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
    ];

    private static readonly string[] s_adjectives =
    [
        "adj-i"
    ];

    private static readonly string[] s_noun =
    [
        "n"
    ];

    private static readonly string[] s_other =
    [
        "other"
    ];

    internal static void Load(Dict dict, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> customWordDictionary = dict.Contents;

        Span<Range> tabRanges = stackalloc Range[5];
        foreach (string line in File.ReadLines(fullPath))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                customWordDictionary.Clear();
                break;
            }

            ReadOnlySpan<char> lineSpan = line.AsSpan();
            int tabCount = lineSpan.Split(tabRanges, '\t', StringSplitOptions.TrimEntries);

            if (tabCount >= 4)
            {
                string[]? wordClasses = null;
                if (tabCount is 5)
                {
                    wordClasses = lineSpan[tabRanges[4]].ToString().Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                }

                string partOfSpeech = lineSpan[tabRanges[3]].ToString();
                string[] definitions = lineSpan[tabRanges[2]].ToString().Replace("\\n", "\n", StringComparison.Ordinal).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                string[]? readings = lineSpan[tabRanges[1]].ToString().Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                string[] spellings = lineSpan[tabRanges[0]].ToString().Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (readings.Length is 0
                    || (spellings.Length is 1 && readings.Length is 1 && spellings[0] == readings[0]))
                {
                    readings = null;
                }

                AddToDictionary(spellings, readings, definitions, partOfSpeech, wordClasses, customWordDictionary);
            }
        }
    }

    public static void AddToDictionary(string[] spellings, string[]? readings, string[] definitions,
        ReadOnlySpan<char> rawPartOfSpeech, string[]? wordClasses, IDictionary<string, IList<IDictRecord>> customWordDictionary)
    {
        bool hasUserDefinedWordClasses = wordClasses?.Length > 0;

        string[] wordClassArray;
        if (hasUserDefinedWordClasses)
        {
            Debug.Assert(wordClasses is not null);
            wordClassArray = wordClasses;
        }
        else
        {
            wordClassArray = rawPartOfSpeech switch
            {
                "Verb" => s_verbs,
                "Adjective" => s_adjectives,
                "Noun" => s_noun,
                _ => s_other
            };
        }

        for (int i = 0; i < spellings.Length; i++)
        {
            string[]? alternativeSpellings = spellings.RemoveAt(i);
            string spelling = spellings[i];

            CustomWordRecord newWordRecord = new(spelling, alternativeSpellings, readings, definitions, wordClassArray, hasUserDefinedWordClasses);

            if (!AddRecordToDictionary(spelling, newWordRecord, customWordDictionary))
            {
                return;
            }

            if (i is 0 && readings is not null)
            {
                foreach (string reading in readings)
                {
                    if (!AddRecordToDictionary(reading, newWordRecord, customWordDictionary))
                    {
                        return;
                    }
                }
            }
        }
    }

    private static bool AddRecordToDictionary(string spelling, IDictRecord record, IDictionary<string, IList<IDictRecord>> dictionary)
    {
        string spellingInHiragana = JapaneseUtils.NormalizeText(spelling);
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
            dictionary[spellingInHiragana] = [record];
        }

        return true;
    }
}
