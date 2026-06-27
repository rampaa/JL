using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Japanese;
using JL.Core.Utilities.Japanese.Okurigana;

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
                    string reading = jsonObj.GetProperty("r")
                        // ReSharper disable once NullableWarningSuppressionIsUsed
                        .GetString()!.GetPooledString();

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
                        if (primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                        {
                            continue;
                        }

                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
                            : primarySpelling.GetPooledString();

                        ImageInfo? imageInfo = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            string? imagePath = imagePathProperty.GetString();
                            if (imagePath is not null)
                            {
                                imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
                            }
                        }

                        EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions, imageInfo);
                        if (DictUtils.AddRecordToDictionary(primarySpellingInHiragana, record, nazekaEpwingDict))
                        {
                            if (nonKanjiDict && nonNameDict)
                            {
                                string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
                                if (primarySpellingInHiragana != readingInHiragana)
                                {
                                    _ = DictUtils.AddRecordToDictionary(readingInHiragana, record, nazekaEpwingDict);
                                }

                                foreach (string variant in OkuriganaVariantGenerator.GenerateMixedVariants(primarySpellingInHiragana, readingInHiragana))
                                {
                                    _ = DictUtils.AddRecordToDictionary(variant, record, nazekaEpwingDict);
                                }
                            }
                        }

                        ReadOnlySpan<string> spellingListSpan = spellingList.AsReadOnlySpan();
                        for (int j = 1; j < spellingListSpan.Length; j++)
                        {
                            ref readonly string alternativeSpelling = ref spellingListSpan[j];
                            if (alternativeSpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                            {
                                continue;
                            }

                            string alternativeSpellingInHiragana = nonKanjiDict
                                ? JapaneseUtils.NormalizeText(alternativeSpelling).GetPooledString()
                                : alternativeSpelling.GetPooledString();

                            if (primarySpellingInHiragana != alternativeSpellingInHiragana)
                            {
                                _ = DictUtils.AddRecordToDictionary(alternativeSpellingInHiragana, new EpwingNazekaRecord(alternativeSpelling, reading, spellingList.RemoveAtToArray(j), definitions, imageInfo), nazekaEpwingDict);
                            }
                        }
                    }

                    else if (!reading.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                    {
                        ImageInfo? imageInfo = null;
                        if (jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
                        {
                            string? imagePath = imagePathProperty.GetString();
                            if (imagePath is not null)
                            {
                                imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
                            }
                        }

                        EpwingNazekaRecord record = new(reading, null, null, definitions, imageInfo);
                        _ = DictUtils.AddRecordToDictionary(nonKanjiDict ? JapaneseUtils.NormalizeText(reading).GetPooledString() : reading, record, nazekaEpwingDict);
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
