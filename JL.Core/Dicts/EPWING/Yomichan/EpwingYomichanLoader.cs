using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanLoader
{
    public const int Size = 250000;

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly);

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameYomichan;

        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        Debug.Assert(!nonNameDict || nonKanjiDict || generateMazegakiOption is not null);
        bool generateMazegaki = nonKanjiDict && nonNameDict
                                             // ReSharper disable once NullableWarningSuppressionIsUsed
                                             && generateMazegakiOption!.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        Debug.Assert(!nonKanjiDict || generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = nonKanjiDict
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                && generateFusejiVariantsOption!.Value;

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        int maxConsecutiveFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;

            Debug.Assert(dict.Options.MaxConsecutiveFusejiCount is not null);
            maxConsecutiveFuseji = dict.Options.MaxConsecutiveFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
            maxConsecutiveFuseji = 0;
        }

        IDictionary<string, IList<IDictRecord>> dictContents = dict.Contents;
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonElements in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonElements is not null);

                    EpwingYomichanRecord? record = GetEpwingYomichanRecord(jsonElements, dict);
                    if (record is not null)
                    {
                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(record.PrimarySpelling).GetPooledString()
                            : record.PrimarySpelling.GetPooledString();

                        if (DictUtils.AddRecordToDictionary(primarySpellingInHiragana, record, dictContents))
                        {
                            if (nonKanjiDict)
                            {
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dictContents);
                                    }
                                }

                                if (nonNameDict && record.Reading is not null)
                                {
                                    string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                                    if (primarySpellingInHiragana != readingInHiragana)
                                    {
                                        if (DictUtils.AddRecordToDictionary(readingInHiragana, record, dictContents))
                                        {
                                            if (generateFusejiVariants)
                                            {
                                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                {
                                                    _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dictContents);
                                                }
                                            }

                                            if (generateMazegaki)
                                            {
                                                foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, readingInHiragana))
                                                {
                                                    if (DictUtils.AddRecordToDictionary(mazegaki, record, dictContents))
                                                    {
                                                        if (generateFusejiVariants)
                                                        {
                                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                            {
                                                                _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dictContents);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    public static EpwingYomichanRecord? GetEpwingYomichanRecord(JsonElement[] jsonElements, Dict dict)
    {
        string primarySpelling;
        try
        {
            primarySpelling = jsonElements[0]
                // ReSharper disable once NullableWarningSuppressionIsUsed
                .GetString()!.GetPooledString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get the primary spelling for EPWING Yomichan record: {JsonElements}", jsonElements);
            return null;
        }

        string? reading;
        try
        {
            reading = jsonElements[1].GetString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get the reading for EPWING Yomichan record: {JsonElements}", jsonElements);
            return null;
        }

        reading = string.IsNullOrWhiteSpace(reading) || reading == primarySpelling
            ? null
            : reading.GetPooledString();

        if (string.IsNullOrWhiteSpace(primarySpelling))
        {
            if (reading is null)
            {
                return null;
            }

            primarySpelling = reading;
            reading = null;
        }

        string[]? definitionTags = null;
        ref readonly JsonElement definitionTagsElement = ref jsonElements[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            string? definitionTagsStr;
            try
            {
                definitionTagsStr = definitionTagsElement.GetString();
            }
            catch (InvalidOperationException ex)
            {
                LoggerManager.Logger.Error(ex, "Failed to get definition tags for EPWING Yomichan record: {JsonElements}", jsonElements);
                return null;
            }

            Debug.Assert(definitionTagsStr is not null);
            if (definitionTagsStr.Length > 0)
            {
                definitionTags = definitionTagsStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (definitionTags.Length is 0)
                {
                    definitionTags = null;
                }
                else
                {
                    definitionTags.DeduplicateStringsInArray();
                }
            }
            else
            {
                definitionTags = null;
            }
        }

        if (definitionTags?.Length is 1 && definitionTags[0] is "子" or "句")
        {
            return null;
        }

        List<ImageInfo>? imageInfos = null;
        string[]? definitions;
        try
        {
            definitions = EpwingYomichanUtils.GetDefinitions(jsonElements[5], dict, ref imageInfos);
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get definitions for EPWING Yomichan record: {JsonElements}", jsonElements);
            return null;
        }

        definitions?.DeduplicateStringsInArray();
        if (definitions is null && imageInfos is null)
        {
            return null;
        }

        if (primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
        {
            return null;
        }

        string? wordClassesStr;
        try
        {
            wordClassesStr = jsonElements[3].GetString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get word classes for EPWING Yomichan record: {JsonElements}", jsonElements);
            return null;
        }

        Debug.Assert(wordClassesStr is not null);
        string[]? wordClasses;
        if (wordClassesStr.Length > 0)
        {
            wordClasses = wordClassesStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (wordClasses.Length is 0)
            {
                wordClasses = null;
            }
            else
            {
                wordClasses.DeduplicateStringsInArray();
            }
        }
        else
        {
            wordClasses = null;
        }

        _ = jsonElements[4].TryGetDouble(out double popularityScore);
        //jsonElements[6].TryGetInt32(out int sequence);
        //string[] termTags = jsonElements[7].ToString();

        return new EpwingYomichanRecord(primarySpelling, reading, popularityScore, definitions, wordClasses, definitionTags, imageInfos?.ToArray());
    }
}
