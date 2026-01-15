using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> nazekaEpwingDict = dict.Contents;

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameNazeka;

        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            IAsyncEnumerator<JsonElement> enumerator = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).GetAsyncEnumerator();
            await using (enumerator.ConfigureAwait(false))
            {
                _ = await enumerator.MoveNextAsync().ConfigureAwait(false);
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    JsonElement jsonObj = enumerator.Current;
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
                            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
                            : primarySpelling.GetPooledString();

                        string? imagePath = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            imagePath = imagePathProperty.GetString();
                        }

                        EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions, imagePath);
                        AddRecordToDictionary(primarySpellingInHiragana, record, nazekaEpwingDict);
                        if (nonKanjiDict && nonNameDict)
                        {
                            string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
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
                                ? JapaneseUtils.NormalizeText(alternativeSpelling).GetPooledString()
                                : alternativeSpelling.GetPooledString();

                            if (primarySpellingInHiragana != alternativeSpellingInHiragana)
                            {
                                AddRecordToDictionary(alternativeSpellingInHiragana, new EpwingNazekaRecord(alternativeSpelling, reading, spellingList.RemoveAtToArray(j), definitions, imagePath), nazekaEpwingDict);
                            }
                        }
                    }

                    else if (EpwingUtils.IsValidEpwingResultForDictType(reading, null, definitions, dict))
                    {
                        string? imagePath = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            imagePath = imagePathProperty.GetString();
                        }

                        EpwingNazekaRecord record = new(reading, null, null, definitions, imagePath);
                        AddRecordToDictionary(nonKanjiDict ? JapaneseUtils.NormalizeText(reading).GetPooledString() : reading, record, nazekaEpwingDict);
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
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
