using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaLoader
{
    public const int Size = 250000;
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameNazeka;

        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        bool generateMazegaki = false;
        if (nonKanjiDict && nonNameDict)
        {
            Debug.Assert(generateMazegakiOption is not null);
            generateMazegaki = generateMazegakiOption.Value;
        }

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        bool generateFusejiVariants = false;
        if (nonKanjiDict)
        {
            Debug.Assert(generateFusejiVariantsOption is not null);
            generateFusejiVariants = generateFusejiVariantsOption.Value;
        }

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
        }

        HashSet<string> searchKeys = [];
        List<(string SearchKey, EpwingNazekaRecord Record)> alternativeRecords = [];
        Dictionary<string, ImageInfo?> imageInfoCache = new(StringComparer.Ordinal);

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
                    string? reading = jsonObj.GetProperty("r").GetString();
                    Debug.Assert(reading is not null);

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

                    if (spellingList is not null)
                    {
                        if (spellingList[0].ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                        {
                            continue;
                        }
                    }
                    else if (reading.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
                    {
                        continue;
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

                    reading = reading.GetPooledString();
                    string[] definitions = definitionList.ToArray();

                    if (spellingList is not null)
                    {
                        string primarySpelling = spellingList[0];
                        string readingInHiragana = nonKanjiDict && nonNameDict
                            ? JapaneseUtils.NormalizeText(reading).GetPooledString()
                            : "";

                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
                            : primarySpelling;

                        ImageInfo? imageInfo = GetImageInfo(jsonObj, imageInfoCache);

                        EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions, imageInfo);
                        bool primarySpellingWasAdded = AddSearchKey(primarySpellingInHiragana, record, dict, searchKeys, out _);
                        bool readingWasAdded = primarySpellingWasAdded && nonKanjiDict && nonNameDict
                            && primarySpellingInHiragana != readingInHiragana
                            && AddSearchKey(readingInHiragana, record, dict, searchKeys, out _);

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
                                : alternativeSpelling;

                            if (nonKanjiDict && nonNameDict && alternativeSpellingInHiragana == readingInHiragana)
                            {
                                continue;
                            }

                            if (primarySpellingInHiragana == alternativeSpellingInHiragana
                                || !searchKeys.Add(alternativeSpellingInHiragana))
                            {
                                continue;
                            }

                            EpwingNazekaRecord alternativeRecord = new(alternativeSpelling, reading, spellingList.RemoveAtToArray(j), definitions, imageInfo);
                            if (DictUtils.AddRecordToDictionary(alternativeSpellingInHiragana, alternativeRecord, dict) && nonKanjiDict)
                            {
                                alternativeRecords.Add((alternativeSpellingInHiragana, alternativeRecord));
                            }
                        }

                        if (primarySpellingWasAdded && nonKanjiDict)
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = AddSearchKey(fusejiVariant, record, dict, searchKeys, out _);
                                }
                            }

                            if (readingWasAdded)
                            {
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = AddSearchKey(fusejiVariant, record, dict, searchKeys, out _);
                                    }
                                }

                                if (generateMazegaki)
                                {
                                    foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, readingInHiragana))
                                    {
                                        if (AddSearchKey(mazegaki, record, dict, searchKeys, out _) && generateFusejiVariants)
                                        {
                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                            {
                                                _ = AddSearchKey(fusejiVariant, record, dict, searchKeys, out _);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        foreach ((string alternativeSpellingInHiragana, EpwingNazekaRecord alternativeRecord) in alternativeRecords)
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(alternativeSpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = AddSearchKey(fusejiVariant, alternativeRecord, dict, searchKeys, out _);
                                }
                            }

                            if (nonNameDict && generateMazegaki)
                            {
                                foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(alternativeSpellingInHiragana, readingInHiragana))
                                {
                                    _ = AddSearchKey(mazegaki, alternativeRecord, dict, searchKeys, out bool mazegakiWasNew);
                                    if (mazegakiWasNew && generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            _ = AddSearchKey(fusejiVariant, alternativeRecord, dict, searchKeys, out _);
                                        }
                                    }
                                }
                            }
                        }

                        searchKeys.Clear();
                        alternativeRecords.Clear();
                    }

                    else
                    {
                        ImageInfo? imageInfo = GetImageInfo(jsonObj, imageInfoCache);

                        EpwingNazekaRecord record = new(reading, null, null, definitions, imageInfo);
                        _ = DictUtils.AddRecordToDictionary(nonKanjiDict ? JapaneseUtils.NormalizeText(reading).GetPooledString() : reading, record, dict);
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static ImageInfo? GetImageInfo(JsonElement jsonObj, Dictionary<string, ImageInfo?> imageInfoCache)
    {
        if (!jsonObj.TryGetProperty("i", out JsonElement imagePathProperty))
        {
            return null;
        }

        string? imagePath = imagePathProperty.GetString();
        if (imagePath is null)
        {
            return null;
        }

        if (!imageInfoCache.TryGetValue(imagePath, out ImageInfo? imageInfo))
        {
            imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
            imageInfoCache.Add(imagePath, imageInfo);
        }

        return imageInfo;
    }

    private static bool AddSearchKey(string searchKey, IDictRecord record, Dict dict, HashSet<string> searchKeys,
        out bool searchKeyWasNew)
    {
        searchKeyWasNew = searchKeys.Add(searchKey);
        return searchKeyWasNew && DictUtils.AddRecordToDictionary(searchKey, record, dict);
    }
}
