using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        ReadOnlyMemory<JsonElement> jsonObjects;

        FileStream fileStream = File.OpenRead(fullPath);
        await using (fileStream.ConfigureAwait(false))
        {
            jsonObjects = await JsonSerializer.DeserializeAsync<ReadOnlyMemory<JsonElement>>(fileStream, Utils.s_jso).ConfigureAwait(false);
        }

        IDictionary<string, IList<IDictRecord>> nazekaEpwingDict = dict.Contents;

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameNazeka;

        for (int i = 1; i < jsonObjects.Span.Length; i++)
        {
            ref readonly JsonElement jsonObj = ref jsonObjects.Span[i];
            string reading = jsonObj.GetProperty("r").GetString()!.GetPooledString();

            JsonElement spellingJsonArray = jsonObj.GetProperty("s");
            List<string>? spellingList = new(spellingJsonArray.GetArrayLength());
            foreach (JsonElement spellingJsonElement in spellingJsonArray.EnumerateArray())
            {
                string? spelling = spellingJsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(spelling))
                {
                    spellingList.Add(spelling.GetPooledString());
                }
            }

            if (spellingList.Count is 0)
            {
                spellingList = null;
            }

            JsonElement definitionJsonArray = jsonObj.GetProperty("l");
            List<string> definitionList = new(definitionJsonArray.GetArrayLength());
            foreach (JsonElement definitionJsonElement in definitionJsonArray.EnumerateArray())
            {
                string? definition = definitionJsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionList.Add(definition.GetPooledString());
                }
            }

            if (definitionList.Count is 0)
            {
                continue;
            }

            string[] definitions = definitionList.ToArray();
            definitions.DeduplicateStringsInArray();

            if (spellingList is not null)
            {
                string primarySpelling = spellingList[0];
                if (!EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
                {
                    continue;
                }

                string primarySpellingInHiragana = nonKanjiDict
                    ? JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString()
                    : primarySpelling.GetPooledString();

                EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions);
                AddRecordToDictionary(primarySpellingInHiragana, record, nazekaEpwingDict);
                if (nonKanjiDict && nonNameDict)
                {
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                    if (primarySpellingInHiragana != readingInHiragana)
                    {
                        AddRecordToDictionary(readingInHiragana, record, nazekaEpwingDict);
                    }
                }

                ReadOnlySpan<string> spellingListSpan = spellingList.AsReadOnlySpan();
                for (int j = 1; j < spellingListSpan.Length; j++)
                {
                    ref readonly string alternativeSpelling = ref spellingListSpan[j];
                    if (!EpwingUtils.IsValidEpwingResultForDictType(alternativeSpelling, reading, definitions, dict))
                    {
                        continue;
                    }

                    string alternativeSpellingInHiragana = nonKanjiDict
                        ? JapaneseUtils.KatakanaToHiragana(alternativeSpelling).GetPooledString()
                        : alternativeSpelling.GetPooledString();

                    if (primarySpellingInHiragana != alternativeSpellingInHiragana)
                    {
                        AddRecordToDictionary(alternativeSpellingInHiragana, new EpwingNazekaRecord(alternativeSpelling, reading, spellingList.RemoveAtToArray(j), definitions), nazekaEpwingDict);
                    }
                }
            }

            else if (EpwingUtils.IsValidEpwingResultForDictType(reading, null, definitions, dict))
            {
                EpwingNazekaRecord record = new(reading, null, null, definitions);
                AddRecordToDictionary(nonKanjiDict ? JapaneseUtils.KatakanaToHiragana(reading).GetPooledString() : reading, record, nazekaEpwingDict);
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(entry => entry.Key, entry => (IList<IDictRecord>)entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static void AddRecordToDictionary(string keyInHiragana, IDictRecord record, IDictionary<string, IList<IDictRecord>> dictionary)
    {
        if (dictionary.TryGetValue(keyInHiragana, out IList<IDictRecord>? result))
        {
            result.Add(record);
        }
        else
        {
            dictionary[keyInHiragana] = [record];
        }
    }
}
