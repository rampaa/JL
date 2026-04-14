using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Japanese;
using JL.Core.Utilities.Japanese.Okurigana;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly);

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameYomichan;

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
                        AddToDictionary(record, dict, nonKanjiDict, nonNameDict);
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord? GetEpwingYomichanRecord(JsonElement[] jsonElements, Dict dict)
    {
        string primarySpelling;
        try
        {
            primarySpelling = jsonElements[0].GetString()!.GetPooledString();
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

        List<string>? imagePaths = null;
        string[]? definitions;
        try
        {
            definitions = EpwingYomichanUtils.GetDefinitions(jsonElements[5], dict, ref imagePaths);
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get definitions for EPWING Yomichan record: {JsonElements}", jsonElements);
            return null;
        }

        definitions?.DeduplicateStringsInArray();
        if (definitions is null && imagePaths is null)
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

        //jsonElements[4].TryGetInt32(out int score);
        //jsonElements[6].TryGetInt32(out int sequence);
        //string[] termTags = jsonElements[7].ToString();

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags, imagePaths?.ToArray());
    }

    private static void AddToDictionary(EpwingYomichanRecord yomichanRecord, Dict dict, bool nonKanjiDict, bool nonNameDict)
    {
        string primarySpellingInHiragana = nonKanjiDict
            ? JapaneseUtils.NormalizeText(yomichanRecord.PrimarySpelling).GetPooledString()
            : yomichanRecord.PrimarySpelling.GetPooledString();

        if (dict.Contents.TryGetValue(primarySpellingInHiragana, out IList<IDictRecord>? records))
        {
            records.Add(yomichanRecord);
        }
        else
        {
            dict.Contents[primarySpellingInHiragana] = [yomichanRecord];
        }

        if (nonKanjiDict && nonNameDict && yomichanRecord.Reading is not null)
        {
            string readingInHiragana = JapaneseUtils.NormalizeText(yomichanRecord.Reading).GetPooledString();
            if (primarySpellingInHiragana != readingInHiragana)
            {
                if (dict.Contents.TryGetValue(readingInHiragana, out records))
                {
                    records.Add(yomichanRecord);
                }
                else
                {
                    dict.Contents[readingInHiragana] = [yomichanRecord];
                }

                foreach (string variant in OkuriganaVariantGenerator.GenerateMixedVariants(primarySpellingInHiragana, readingInHiragana))
                {
                    if (dict.Contents.TryGetValue(variant, out IList<IDictRecord>? tempRecordList))
                    {
                        if (!tempRecordList.Contains(yomichanRecord))
                        {
                            tempRecordList.Add(yomichanRecord);
                        }
                    }
                    else
                    {
                        dict.Contents[variant] = [yomichanRecord];
                    }
                }
            }
        }
    }
}
