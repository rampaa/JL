using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;
using MessagePack;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanLoader
{
    public const int Size = 250000;

    internal static JsonReaderState InitialJsonReaderState { get; } = CreateJsonReaderState();

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly);
        ConcurrentDictionary<string, ImageInfo> imageInfoCache = new();

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameYomichan;

        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        Debug.Assert(!nonKanjiDict || !nonNameDict || generateMazegakiOption is not null);
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

        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement jsonElement in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    EpwingYomichanRecord? record = GetEpwingYomichanRecord(jsonElement, dict, imageInfoCache);
                    if (record is not null)
                    {
                        string primarySpellingInHiragana = nonKanjiDict
                            ? JapaneseUtils.NormalizeText(record.PrimarySpelling).GetPooledString()
                            : record.PrimarySpelling.GetPooledString();

                        if (DictUtils.AddRecordToDictionary(primarySpellingInHiragana, record, dict))
                        {
                            if (nonKanjiDict)
                            {
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
                                    }
                                }

                                if (nonNameDict && record.Reading is not null)
                                {
                                    string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                                    if (primarySpellingInHiragana != readingInHiragana)
                                    {
                                        if (DictUtils.AddRecordToDictionary(readingInHiragana, record, dict))
                                        {
                                            if (generateFusejiVariants)
                                            {
                                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                {
                                                    _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
                                                }
                                            }

                                            if (generateMazegaki)
                                            {
                                                foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, readingInHiragana))
                                                {
                                                    if (DictUtils.AddRecordToDictionary(mazegaki, record, dict))
                                                    {
                                                        if (generateFusejiVariants)
                                                        {
                                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                            {
                                                                _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
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

    private static EpwingYomichanRecord? GetEpwingYomichanRecord(JsonElement jsonElement, Dict dict, ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        if (!TryGetPrimarySpellingAndReading(jsonElement, out string primarySpelling, out string? reading)
            || !TryGetDefinitionTags(jsonElement, out string[]? definitionTags)
            || (definitionTags?.Length is 1 && definitionTags[0] is "子" or "句")
            || !TryGetDefinitions(jsonElement, dict, imageInfoCache, out string[]? definitions, out List<ImageInfo>? imageInfos)
            || (definitions is null && imageInfos is null)
            || primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings)
            || !TryGetWordClasses(jsonElement, out string[]? wordClasses))
        {
            return null;
        }

        _ = jsonElement[4].TryGetDouble(out double popularityScore);

        return new EpwingYomichanRecord(primarySpelling, reading, popularityScore, definitions, wordClasses, definitionTags, imageInfos?.ToArray());
    }

    private static bool TryGetPrimarySpellingAndReading(JsonElement jsonElement, out string primarySpelling, out string? reading)
    {
        try
        {
            string? primarySpellingStr = jsonElement[0].GetString();
            Debug.Assert(primarySpellingStr is not null);
            primarySpelling = primarySpellingStr.GetPooledString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get the primary spelling for EPWING Yomichan record: {JsonElement}", jsonElement);
            primarySpelling = "";
            reading = null;
            return false;
        }

        try
        {
            reading = jsonElement[1].GetString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get the reading for EPWING Yomichan record: {JsonElement}", jsonElement);
            reading = null;
            return false;
        }

        reading = string.IsNullOrWhiteSpace(reading) || reading == primarySpelling
            ? null
            : reading.GetPooledString();

        if (string.IsNullOrWhiteSpace(primarySpelling))
        {
            if (reading is null)
            {
                return false;
            }

            primarySpelling = reading;
            reading = null;
        }

        return true;
    }

    private static bool TryGetDefinitionTags(JsonElement jsonElement, out string[]? definitionTags)
    {
        definitionTags = null;

        JsonElement definitionTagsElement = jsonElement[2];
        if (definitionTagsElement.ValueKind is not JsonValueKind.String)
        {
            return true;
        }

        string? definitionTagsStr;
        try
        {
            definitionTagsStr = definitionTagsElement.GetString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get definition tags for EPWING Yomichan record: {JsonElement}", jsonElement);
            return false;
        }

        Debug.Assert(definitionTagsStr is not null);
        definitionTags = SplitSpaceSeparatedTags(definitionTagsStr);
        return true;
    }

    private static bool TryGetDefinitions(JsonElement jsonElement, Dict dict, ConcurrentDictionary<string, ImageInfo> imageInfoCache,
        out string[]? definitions, out List<ImageInfo>? imageInfos)
    {
        imageInfos = null;

        try
        {
            definitions = EpwingYomichanUtils.GetDefinitions(jsonElement[5], dict, ref imageInfos, imageInfoCache);
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get definitions for EPWING Yomichan record: {JsonElement}", jsonElement);
            definitions = null;
            return false;
        }

        return true;
    }

    private static bool TryGetWordClasses(JsonElement jsonElement, out string[]? wordClasses)
    {
        string? wordClassesStr;
        try
        {
            wordClassesStr = jsonElement[3].GetString();
        }
        catch (InvalidOperationException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to get word classes for EPWING Yomichan record: {JsonElement}", jsonElement);
            wordClasses = null;
            return false;
        }

        Debug.Assert(wordClassesStr is not null);
        wordClasses = SplitSpaceSeparatedTags(wordClassesStr);
        return true;
    }

    private static string[]? SplitSpaceSeparatedTags(string value)
    {
        if (value.Length is 0)
        {
            return null;
        }

        string[] tags = value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tags.Length is 0)
        {
            return null;
        }

        tags.DeduplicateStringsInArray();
        return tags;
    }

    private static JsonReaderState CreateJsonReaderState()
    {
        JsonSerializerOptions serializerOptions = JsonOptions.DefaultJso;

        return new JsonReaderState(new JsonReaderOptions
        {
            AllowTrailingCommas = serializerOptions.AllowTrailingCommas,
            CommentHandling = serializerOptions.ReadCommentHandling is JsonCommentHandling.Allow
                ? JsonCommentHandling.Skip
                : serializerOptions.ReadCommentHandling,
            MaxDepth = serializerOptions.MaxDepth
        });
    }

    internal static int ReadImportRecords(byte[] jsonBytes, ref int offset, ref JsonReaderState readerState, ref bool started,
        Dict dict, bool nonKanjiDict, bool nonNameDict, ConcurrentDictionary<string, ImageInfo> imageInfoCache,
        EpwingYomichanImportRecord[] records, int recordOffset, int maxRecordCount, out bool completed)
    {
        Debug.Assert(recordOffset >= 0);
        Debug.Assert(maxRecordCount > 0);
        Debug.Assert(recordOffset + maxRecordCount <= records.Length);

        ReadOnlySpan<byte> json = jsonBytes.AsSpan(offset);
        Utf8JsonReader reader = new(json, true, readerState);

        int recordCount = 0;
        completed = false;

        if (!started)
        {
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
            {
                throw new JsonException("The Yomichan term bank JSON root must be an array.");
            }

            started = true;
        }

        while (recordCount < maxRecordCount)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of Yomichan term bank JSON.");
            }

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                completed = true;

                if (reader.Read())
                {
                    throw new JsonException("Unexpected JSON content after the Yomichan term bank array.");
                }

                break;
            }

            if (reader.TokenType is not JsonTokenType.StartArray)
            {
                throw new JsonException("A Yomichan term-bank record must be an array.");
            }

            int recordDepth = reader.CurrentDepth;
            bool prepared;
            EpwingYomichanImportRecord record;
            try
            {
                prepared = TryGetImportRecord(ref reader, json, dict, nonKanjiDict, nonNameDict, imageInfoCache, out record);
            }
            catch (InvalidOperationException ex)
            {
                LoggerManager.Logger.Error(ex, "Failed to read EPWING Yomichan record near byte offset {ByteOffset}", offset + reader.TokenStartIndex);
                record = default;
                prepared = false;
            }

            if (!MoveReaderToArrayEnd(ref reader, recordDepth))
            {
                throw new JsonException("Unexpected end of Yomichan term-bank record.");
            }

            if (prepared)
            {
                records[recordOffset + recordCount] = record;
                ++recordCount;
            }
        }

        offset += checked((int)reader.BytesConsumed);
        readerState = reader.CurrentState;
        return recordCount;
    }

    internal static bool TryGetImportRecord(JsonElement jsonElement, Dict dict, bool nonKanjiDict, bool nonNameDict,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, out EpwingYomichanImportRecord record)
    {
        if (!TryGetPrimarySpellingAndReading(jsonElement, out string primarySpelling, out string? reading)
            || !TryGetDefinitionTags(jsonElement, out string[]? definitionTags)
            || (definitionTags?.Length is 1 && definitionTags[0] is "子" or "句")
            || !TryGetDefinitions(jsonElement, dict, imageInfoCache, out string[]? definitions, out List<ImageInfo>? imageInfos)
            || (definitions is null && imageInfos is null)
            || primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings)
            || !TryGetWordClasses(jsonElement, out string[]? wordClasses))
        {
            record = default;
            return false;
        }

        _ = jsonElement[4].TryGetDouble(out double popularityScore);

        string searchKey = nonKanjiDict
            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
            : primarySpelling.GetPooledString();

        string? additionalSearchKey = null;
        if (nonKanjiDict && nonNameDict && reading is not null)
        {
            string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
            if (searchKey != readingInHiragana)
            {
                additionalSearchKey = readingInHiragana;
            }
        }

        record = new EpwingYomichanImportRecord(
            primarySpelling,
            reading,
            popularityScore,
            MessagePackSerializer.Serialize(definitions),
            wordClasses is not null ? MessagePackSerializer.Serialize(wordClasses) : null,
            definitionTags is not null ? MessagePackSerializer.Serialize(definitionTags) : null,
            imageInfos is not null ? MessagePackSerializer.Serialize(imageInfos) : null,
            searchKey,
            additionalSearchKey);

        return true;
    }

    private static bool TryGetImportRecord(ref Utf8JsonReader reader, ReadOnlySpan<byte> json, Dict dict,
        bool nonKanjiDict, bool nonNameDict, ConcurrentDictionary<string, ImageInfo> imageInfoCache, out EpwingYomichanImportRecord record)
    {
        record = default;

        if (!reader.Read() || reader.TokenType is not JsonTokenType.String)
        {
            return false;
        }

        string? primarySpellingValue = reader.GetString();
        Debug.Assert(primarySpellingValue is not null);
        string primarySpelling = primarySpellingValue.GetPooledString();

        if (!reader.Read())
        {
            return false;
        }

        string? reading;
        if (reader.TokenType is JsonTokenType.String)
        {
            reading = reader.GetString();
            Debug.Assert(reading is not null);
        }
        else if (reader.TokenType is JsonTokenType.Null)
        {
            reading = null;
        }
        else
        {
            return false;
        }

        reading = string.IsNullOrWhiteSpace(reading) || reading == primarySpelling
            ? null
            : reading.GetPooledString();

        if (string.IsNullOrWhiteSpace(primarySpelling))
        {
            if (reading is null)
            {
                return false;
            }

            primarySpelling = reading;
            reading = null;
        }

        if (!reader.Read())
        {
            return false;
        }

        string[]? definitionTags = null;
        if (reader.TokenType is JsonTokenType.String)
        {
            string? definitionTagsStr = reader.GetString();
            Debug.Assert(definitionTagsStr is not null);

            definitionTags = SplitSpaceSeparatedTags(definitionTagsStr);
        }
        else if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
        {
            reader.Skip();
        }

        if (definitionTags?.Length is 1 && definitionTags[0] is "子" or "句")
        {
            return false;
        }

        if (!reader.Read())
        {
            return false;
        }

        if (reader.TokenType is not JsonTokenType.String)
        {
            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }

            return false;
        }

        string? wordClassesStr = reader.GetString();
        Debug.Assert(wordClassesStr is not null);

        if (!reader.Read())
        {
            return false;
        }

        double popularityScore = reader.TokenType is JsonTokenType.Number && reader.TryGetDouble(out double score)
            ? score
            : 0D;

        if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
        {
            reader.Skip();
        }

        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
        {
            return false;
        }

        if (!TryReadDefinitions(
                ref reader,
                json,
                dict,
                imageInfoCache,
                out string[]? definitions,
                out List<ImageInfo>? imageInfos)
            || (definitions is null && imageInfos is null)
            || primarySpelling.ContainsAny(DictUtils.s_invalidCharactersForPrimarySpellings))
        {
            return false;
        }

        string[]? wordClasses = SplitSpaceSeparatedTags(wordClassesStr);

        string searchKey = nonKanjiDict
            ? JapaneseUtils.NormalizeText(primarySpelling).GetPooledString()
            : primarySpelling.GetPooledString();

        string? additionalSearchKey = null;
        if (nonKanjiDict && nonNameDict && reading is not null)
        {
            string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
            if (searchKey != readingInHiragana)
            {
                additionalSearchKey = readingInHiragana;
            }
        }

        record = new EpwingYomichanImportRecord(
            primarySpelling,
            reading,
            popularityScore,
            MessagePackSerializer.Serialize(definitions),
            wordClasses is not null ? MessagePackSerializer.Serialize(wordClasses) : null,
            definitionTags is not null ? MessagePackSerializer.Serialize(definitionTags) : null,
            imageInfos is not null ? MessagePackSerializer.Serialize(imageInfos) : null,
            searchKey,
            additionalSearchKey);

        return true;
    }

    private static bool MoveReaderToArrayEnd(ref Utf8JsonReader reader, int arrayDepth)
    {
        if (reader.TokenType is JsonTokenType.EndArray && reader.CurrentDepth == arrayDepth)
        {
            return true;
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray && reader.CurrentDepth == arrayDepth)
            {
                return true;
            }
        }

        return false;
    }

    private static bool MoveReaderToObjectEnd(ref Utf8JsonReader reader, int objectDepth)
    {
        if (reader.TokenType is JsonTokenType.EndObject && reader.CurrentDepth == objectDepth)
        {
            return true;
        }

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject && reader.CurrentDepth == objectDepth)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadDefinitions(ref Utf8JsonReader reader, ReadOnlySpan<byte> json, Dict dict,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, out string[]? definitions, out List<ImageInfo>? imageInfos)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartArray);

        string? firstDefinition = null;
        List<string>? definitionList = null;
        imageInfos = null;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                if (definitionList is not null)
                {
                    definitions = definitionList.ToArray();
                }
                else if (firstDefinition is not null)
                {
                    definitions = [firstDefinition];
                }
                else
                {
                    definitions = null;
                }

                return true;
            }

            string? definition = null;

            if (reader.TokenType is JsonTokenType.String)
            {
                definition = reader.GetString();
            }
            else if (reader.TokenType is JsonTokenType.StartObject)
            {
                if (!TryGetDefinitionsFromJsonObject(
                        ref reader,
                        json,
                        dict,
                        ref imageInfos,
                        imageInfoCache,
                        null,
                        out YomichanContent<ContentTag> objectContent,
                        out _,
                        out _))
                {
                    definitions = null;
                    return false;
                }

                if (objectContent.Tag is ContentTag.Img)
                {
                    if (objectContent.Content is not null)
                    {
                        ImageInfo? imageInfo = EpwingYomichanUtils.GetImageInfo(objectContent.Content, imageInfoCache);
                        if (imageInfo is not null)
                        {
                            imageInfos ??= [];
                            imageInfos.Add(imageInfo);
                        }
                    }
                }
                else
                {
                    definition = objectContent.Content;
                }
            }
            else if (reader.TokenType is JsonTokenType.StartArray)
            {
                // Deconjugation information. The existing loader ignores it.
                reader.Skip();
            }

            if (definition is not null)
            {
                string trimmedDefinition = definition.Trim();
                if (trimmedDefinition.Length is not 0)
                {
                    string pooledDefinition = trimmedDefinition.GetPooledString();
                    if (firstDefinition is null)
                    {
                        firstDefinition = pooledDefinition;
                    }
                    else
                    {
                        definitionList ??= [firstDefinition];
                        definitionList.Add(pooledDefinition);
                    }
                }
            }
        }

        definitions = null;
        return false;
    }

    private static bool TryAppendDefinitionsFromJsonArray(ref Utf8JsonReader reader, ReadOnlySpan<byte> json,
        StringBuilder stringBuilder, Dict dict, ref List<ImageInfo>? imageInfos,
        bool isOrderedList, string? inheritedMarker, ref int orderedListIndex, ref ContentTag lastTag,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, List<int>? tableRowSpans, bool isTableRow,
        ref int tableColumnIndex)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartArray);

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                return true;
            }

            if (reader.TokenType is JsonTokenType.String)
            {
                _ = stringBuilder.Append(reader.GetString());
                lastTag = ContentTag.None;
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartArray)
            {
                if (!TryAppendDefinitionsFromJsonArray(
                        ref reader,
                        json,
                        stringBuilder,
                        dict,
                        ref imageInfos,
                        isOrderedList,
                        inheritedMarker,
                        ref orderedListIndex,
                        ref lastTag,
                        imageInfoCache,
                        tableRowSpans,
                        isTableRow,
                        ref tableColumnIndex))
                {
                    return false;
                }

                continue;
            }

            if (reader.TokenType is not JsonTokenType.StartObject)
            {
                continue;
            }

            if (!TryGetDefinitionsFromJsonObject(
                    ref reader,
                    json,
                    dict,
                    ref imageInfos,
                    imageInfoCache,
                    tableRowSpans,
                    out YomichanContent<ContentTag> contentResult,
                    out int colSpan,
                    out int rowSpan))
            {
                return false;
            }

            if (isTableRow && contentResult.Tag is ContentTag.TH or ContentTag.TD)
            {
                Debug.Assert(tableRowSpans is not null);
                EpwingYomichanUtils.AppendTableCell(stringBuilder, contentResult.Content, colSpan, rowSpan,
                    tableRowSpans, ref tableColumnIndex);
            }
            else
            {
                AppendDefinitionContent(stringBuilder, contentResult, ref imageInfos, isOrderedList, inheritedMarker,
                    ref orderedListIndex, ref lastTag, imageInfoCache);
            }
        }

        return false;
    }

    private static void AppendDefinitionContent(StringBuilder stringBuilder, YomichanContent<ContentTag> contentResult,
        ref List<ImageInfo>? imageInfos, bool isOrderedList, string? inheritedMarker, ref int orderedListIndex, ref ContentTag lastTag,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        string? content = contentResult.Content;
        if (content is not null)
        {
            switch (contentResult.Tag)
            {
                case ContentTag.Span:
                    _ = stringBuilder.Append(content);
                    if (contentResult.AppendWhitespace)
                    {
                        _ = stringBuilder.Append(' ');
                    }
                    break;

                case ContentTag.A:
                case ContentTag.Ruby:
                    _ = stringBuilder.Append(content);
                    break;

                case ContentTag.RP:
                    break;

                case ContentTag.RT:
                    _ = stringBuilder.Append('[').Append(content).Append(']');
                    break;

                case ContentTag.LI:
                {
                    content = content.TrimStart();
                    ++orderedListIndex;
                    string? marker = contentResult.Marker ?? inheritedMarker;
                    if (marker is "none")
                    {
                        _ = stringBuilder.Append('\n').Append(content);
                        break;
                    }

                    if (marker is not null)
                    {
                        marker = EpwingYomichanUtils.GetListMarker(marker, orderedListIndex);
                    }

                    marker ??= isOrderedList ? $"{orderedListIndex}." : "•";
                    if (marker.Length is 0)
                    {
                        _ = stringBuilder.Append('\n').Append(content);
                        break;
                    }

                    if (content.StartsWith('•') || content.StartsWith(marker, StringComparison.Ordinal))
                    {
                        _ = stringBuilder.Append('\n').Append(marker).Append('\n').Append(content);
                    }
                    else
                    {
                        _ = stringBuilder.Append('\n').Append(marker).Append(' ').Append(content);
                    }
                    break;
                }

                case ContentTag.UL:
                case ContentTag.OL:
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                    break;

                case ContentTag.TH:
                case ContentTag.TD:
                    _ = stringBuilder.Append(" | ").Append(content.AsSpan().TrimStart());
                    break;

                case ContentTag.TR:
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart()).Append(" |");
                    break;

                case ContentTag.Img:
                {
                    ImageInfo? imageInfo = EpwingYomichanUtils.GetImageInfo(content, imageInfoCache);
                    if (imageInfo is not null)
                    {
                        imageInfos ??= [];
                        imageInfos.Add(imageInfo);
                    }
                    break;
                }

                case ContentTag.Div:
                    if (lastTag is ContentTag.Div && stringBuilder.Length > 0 && stringBuilder[^1] is '\n')
                    {
                        _ = stringBuilder.Append(content.AsSpan().Trim()).Append('\n');
                    }
                    else
                    {
                        _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                    }
                    break;

                case ContentTag.None:
                case ContentTag.BR:
                case ContentTag.Table:
                case ContentTag.THead:
                case ContentTag.TBody:
                case ContentTag.TFoot:
                case ContentTag.Other:
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart());
                    break;

                default:
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(YomichanContent<>), nameof(EpwingYomichanLoader), nameof(AppendDefinitionContent), contentResult.Tag);
                    break;
            }

            lastTag = contentResult.Tag;
        }
        else if (contentResult.Tag is ContentTag.BR)
        {
            _ = stringBuilder.Append('\n');
            lastTag = contentResult.Tag;
        }
    }

    private static bool TryGetDefinitionsFromJsonObject(ref Utf8JsonReader reader, ReadOnlySpan<byte> json,
        Dict dict, ref List<ImageInfo>? imageInfos,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, List<int>? tableRowSpans,
        out YomichanContent<ContentTag> result, out int colSpan, out int rowSpan)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);

        colSpan = 1;
        rowSpan = 1;

        bool tagPresent = false;
        ContentTag tag = ContentTag.None;
        bool invalidTag = false;
        ContentType type = ContentType.None;
        bool invalidType = false;

        bool contentPresent = false;
        ContentValueKind contentKind = ContentValueKind.None;
        string? contentText = null;
        bool emptyContentArray = false;
        int contentOffset = 0;
        int contentLength = 0;
        YomichanContent<ContentTag> parsedNestedContent = default;
        int nestedColSpan = 1;
        int nestedRowSpan = 1;
        // Content may precede its tag, so nested content is reparsed only when the final tag is known.
        bool contentNeedsParsing = false;
        bool invalidContent = false;
        int imageInfoCountBeforeContent = 0;

        bool stylePresent = false;
        bool invalidStyle = false;
        bool styleMarginRight = false;
        string? listStyleType = null;

        bool dataClassPresent = false;
        bool invalidData = false;

        string? href = null;
        bool invalidHref = false;
        bool pathPresent = false;
        string? path = null;
        bool invalidPath = false;
        bool titlePresent = false;
        string? title = null;
        bool invalidTitle = false;
        bool textPresent = false;
        string? text = null;
        bool invalidText = false;

        double height = double.PositiveInfinity;
        double width = double.PositiveInfinity;
        bool heightSpecified = false;
        bool widthSpecified = false;
        bool sizeUnitsEm = false;
        bool invalidHeight = false;
        bool invalidWidth = false;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                result = default;
                return false;
            }

            if (reader.ValueTextEquals("tag"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                ContentTag previousTag = tag;
                tagPresent = true;
                if (reader.TokenType is JsonTokenType.String)
                {
                    tag = GetContentTag(ref reader);
                    invalidTag = false;
                }
                else if (reader.TokenType is JsonTokenType.Null)
                {
                    tag = ContentTag.None;
                    invalidTag = false;
                }
                else
                {
                    tag = ContentTag.None;
                    invalidTag = true;
                    if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                    {
                        reader.Skip();
                    }
                }

                if (tableRowSpans is { Count: > 0 } && tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot)
                {
                    // Row spans do not cross row groups.
                    tableRowSpans.Clear();
                }

                if (contentPresent && previousTag != tag && contentKind is ContentValueKind.Array)
                {
                    RemoveImagesFromReplacedContent(ref imageInfos, imageInfoCountBeforeContent);
                    contentNeedsParsing = true;
                }

                continue;
            }

            if (reader.ValueTextEquals("type"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                if (reader.TokenType is JsonTokenType.String)
                {
                    type = GetContentType(ref reader);
                    invalidType = false;
                }
                else if (reader.TokenType is JsonTokenType.Null)
                {
                    type = ContentType.None;
                    invalidType = false;
                }
                else
                {
                    invalidType = true;
                    if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                    {
                        reader.Skip();
                    }
                }

                continue;
            }

            if (reader.ValueTextEquals("content"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                if (contentPresent)
                {
                    RemoveImagesFromReplacedContent(ref imageInfos, imageInfoCountBeforeContent);
                }

                imageInfoCountBeforeContent = imageInfos?.Count ?? 0;
                contentPresent = true;
                contentNeedsParsing = false;
                invalidContent = false;
                contentText = null;
                emptyContentArray = false;
                parsedNestedContent = default;

                if (reader.TokenType is JsonTokenType.String)
                {
                    contentKind = ContentValueKind.StringValue;
                    contentText = reader.GetString();
                    Debug.Assert(contentText is not null);
                    continue;
                }

                if (reader.TokenType is JsonTokenType.StartArray)
                {
                    contentKind = ContentValueKind.Array;
                    if (tag is ContentTag.TH)
                    {
                        Utf8JsonReader nextReader = reader;
                        emptyContentArray = nextReader.Read() && nextReader.TokenType is JsonTokenType.EndArray;
                    }
                    int contentStart = checked((int)reader.TokenStartIndex);
                    int contentDepth = reader.CurrentDepth;

                    if (!tagPresent)
                    {
                        SkipNestedContentForLater(ref reader, out contentOffset, out contentLength);
                        contentNeedsParsing = true;
                        continue;
                    }

                    StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
                    int orderedListIndex = 0;
                    ContentTag lastTag = ContentTag.None;
                    int tableColumnIndex = 0;
                    List<int>? childTableRowSpans = null;
                    if (tag is ContentTag.Table)
                    {
                        childTableRowSpans = [];
                    }
                    else if (tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot or ContentTag.TR)
                    {
                        childTableRowSpans = tableRowSpans;
                    }
                    bool success = TryAppendDefinitionsFromJsonArray(
                        ref reader,
                        json,
                        stringBuilder,
                        dict,
                        ref imageInfos,
                        tag is ContentTag.OL,
                        NormalizeListMarker(listStyleType),
                        ref orderedListIndex,
                        ref lastTag,
                        imageInfoCache,
                        childTableRowSpans,
                        tag is ContentTag.TR && childTableRowSpans is not null,
                        ref tableColumnIndex);

                    if (!success)
                    {
                        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                        if (!MoveReaderToArrayEnd(ref reader, contentDepth))
                        {
                            result = default;
                            return false;
                        }

                        contentOffset = contentStart;
                        contentLength = checked((int)reader.BytesConsumed) - contentStart;
                        invalidContent = true;
                        continue;
                    }

                    contentOffset = contentStart;
                    contentLength = checked((int)reader.BytesConsumed) - contentStart;

                    if (tag is ContentTag.TR && childTableRowSpans is not null)
                    {
                        EpwingYomichanUtils.AdvanceTableRow(stringBuilder, childTableRowSpans, tableColumnIndex);
                    }

                    if (stringBuilder.Length > 0)
                    {
                        contentText = stringBuilder.ToString();
                    }

                    ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                    continue;
                }

                if (reader.TokenType is JsonTokenType.StartObject)
                {
                    contentKind = ContentValueKind.ObjectValue;
                    int contentStart = checked((int)reader.TokenStartIndex);
                    int contentDepth = reader.CurrentDepth;

                    if (!tagPresent)
                    {
                        SkipNestedContentForLater(ref reader, out contentOffset, out contentLength);
                        contentNeedsParsing = true;
                        continue;
                    }

                    List<int>? childTableRowSpans = null;
                    if (tag is ContentTag.Table)
                    {
                        childTableRowSpans = [];
                    }
                    else if (tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot or ContentTag.TR)
                    {
                        childTableRowSpans = tableRowSpans;
                    }

                    if (!TryGetDefinitionsFromJsonObject(
                            ref reader,
                            json,
                            dict,
                            ref imageInfos,
                            imageInfoCache,
                            childTableRowSpans,
                            out parsedNestedContent,
                            out nestedColSpan,
                            out nestedRowSpan))
                    {
                        if (!MoveReaderToObjectEnd(ref reader, contentDepth))
                        {
                            result = default;
                            return false;
                        }

                        contentOffset = contentStart;
                        contentLength = checked((int)reader.BytesConsumed) - contentStart;
                        invalidContent = true;
                        continue;
                    }

                    contentOffset = contentStart;
                    contentLength = checked((int)reader.BytesConsumed) - contentStart;

                    continue;
                }

                contentKind = ContentValueKind.Unsupported;
                continue;
            }

            if (reader.ValueTextEquals("style"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                string? previousListStyleType = listStyleType;
                stylePresent = true;
                invalidStyle = reader.TokenType is not JsonTokenType.StartObject;
                if (invalidStyle)
                {
                    if (reader.TokenType is JsonTokenType.StartArray)
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    invalidStyle = !TryReadStyle(ref reader, out listStyleType, out styleMarginRight);
                }

                if (!invalidStyle && contentPresent && contentKind is ContentValueKind.Array
                    && previousListStyleType != listStyleType)
                {
                    RemoveImagesFromReplacedContent(ref imageInfos, imageInfoCountBeforeContent);
                    contentNeedsParsing = true;
                }

                continue;
            }

            if (reader.ValueTextEquals("data"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                dataClassPresent = false;
                invalidData = reader.TokenType is not JsonTokenType.StartObject;
                if (reader.TokenType is JsonTokenType.StartObject)
                {
                    if (!TryReadData(ref reader, out dataClassPresent))
                    {
                        result = default;
                        return false;
                    }
                }
                else if (reader.TokenType is JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            if (reader.ValueTextEquals("href"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                invalidHref = !TryReadNullableString(ref reader, out href);
                continue;
            }

            if (reader.ValueTextEquals("path"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                invalidPath = !TryReadNullableString(ref reader, out path);
                pathPresent = true;
                continue;
            }

            if (reader.ValueTextEquals("sizeUnits"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                sizeUnitsEm = reader.TokenType is JsonTokenType.String && reader.ValueTextEquals("em"u8);
                if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }

                continue;
            }

            if (reader.ValueTextEquals("title"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                invalidTitle = !TryReadNullableString(ref reader, out title);
                titlePresent = true;
                continue;
            }

            if (reader.ValueTextEquals("text"u8))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                invalidText = !TryReadNullableString(ref reader, out text);
                textPresent = true;
                continue;
            }

            bool isColSpan = tableRowSpans is not null && reader.ValueTextEquals("colSpan"u8);
            if (isColSpan || (tableRowSpans is not null && reader.ValueTextEquals("rowSpan"u8)))
            {
                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                if (reader.TokenType is JsonTokenType.Number && reader.TryGetInt32(out int span) && span > 0)
                {
                    if (isColSpan)
                    {
                        colSpan = span;
                    }
                    else
                    {
                        rowSpan = span;
                    }
                }

                if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }

                continue;
            }

            bool isHeight = reader.ValueTextEquals("height"u8);
            if (isHeight || reader.ValueTextEquals("width"u8))
            {
                if (isHeight)
                {
                    heightSpecified = true;
                }
                else
                {
                    widthSpecified = true;
                }

                if (!reader.Read())
                {
                    result = default;
                    return false;
                }

                if (reader.TokenType is JsonTokenType.Number)
                {
                    if (!reader.TryGetDouble(out double dimension))
                    {
                        if (isHeight)
                        {
                            invalidHeight = true;
                        }
                        else
                        {
                            invalidWidth = true;
                        }
                    }
                    else
                    {
                        if (isHeight)
                        {
                            invalidHeight = false;
                            height = dimension;
                        }
                        else
                        {
                            invalidWidth = false;
                            width = dimension;
                        }
                    }
                }
                else
                {
                    if (isHeight)
                    {
                        invalidHeight = true;
                    }
                    else
                    {
                        invalidWidth = true;
                    }

                    if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                    {
                        reader.Skip();
                    }
                }

                continue;
            }

            if (!reader.Read())
            {
                result = default;
                return false;
            }

            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        if (reader.TokenType is not JsonTokenType.EndObject || invalidTag || invalidStyle)
        {
            result = default;
            return false;
        }

        if (contentPresent)
        {
            if (invalidContent && !contentNeedsParsing)
            {
                result = default;
                return false;
            }

            if (contentKind is ContentValueKind.ObjectValue)
            {
                if (contentNeedsParsing)
                {
                    ReadOnlySpan<byte> nestedJson = json.Slice(contentOffset, contentLength);
                    Utf8JsonReader nestedReader = new(nestedJson, true, InitialJsonReaderState);
                    List<int>? childTableRowSpans = null;
                    if (tag is ContentTag.Table)
                    {
                        childTableRowSpans = [];
                    }
                    else if (tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot or ContentTag.TR)
                    {
                        childTableRowSpans = tableRowSpans;
                    }

                    if (!nestedReader.Read()
                        || nestedReader.TokenType is not JsonTokenType.StartObject
                        || !TryGetDefinitionsFromJsonObject(
                            ref nestedReader,
                            nestedJson,
                            dict,
                            ref imageInfos,
                            imageInfoCache,
                            childTableRowSpans,
                            out parsedNestedContent,
                            out nestedColSpan,
                            out nestedRowSpan))
                    {
                        result = default;
                        return false;
                    }
                }

                string? childText = parsedNestedContent.Content;
                string? marker = NormalizeListMarker(listStyleType);
                string? objectText;
                if (tag is ContentTag.TR && tableRowSpans is not null
                    && parsedNestedContent.Tag is ContentTag.TH or ContentTag.TD)
                {
                    StringBuilder objectStringBuilder = ObjectPoolManager.StringBuilderPool.Get();
                    int tableColumnIndex = 0;
                    EpwingYomichanUtils.AppendTableCell(objectStringBuilder, childText, nestedColSpan, nestedRowSpan, tableRowSpans, ref tableColumnIndex);
                    EpwingYomichanUtils.AdvanceTableRow(objectStringBuilder, tableRowSpans, tableColumnIndex);
                    objectText = objectStringBuilder.Length > 0 ? objectStringBuilder.ToString() : null;
                    ObjectPoolManager.StringBuilderPool.Return(objectStringBuilder);
                }
                else if (parsedNestedContent.Tag is ContentTag.BR)
                {
                    objectText = "\n";
                }
                else if (childText is null || parsedNestedContent.Tag is ContentTag.RP)
                {
                    objectText = null;
                }
                else if (parsedNestedContent.Tag is ContentTag.Span)
                {
                    objectText = parsedNestedContent.AppendWhitespace ? childText + " " : childText;
                }
                else if (parsedNestedContent.Tag is ContentTag.A or ContentTag.Ruby)
                {
                    objectText = childText;
                }
                else
                {
                    StringBuilder objectStringBuilder = ObjectPoolManager.StringBuilderPool.Get();
                    int objectOrderedListIndex = 0;
                    ContentTag lastTag = ContentTag.None;
                    AppendDefinitionContent(objectStringBuilder, parsedNestedContent, ref imageInfos, tag is ContentTag.OL,
                        NormalizeListMarker(listStyleType), ref objectOrderedListIndex, ref lastTag, imageInfoCache);
                    objectText = objectStringBuilder.Length > 0 ? objectStringBuilder.ToString() : null;
                    ObjectPoolManager.StringBuilderPool.Return(objectStringBuilder);
                }

                bool appendWhitespace = tag is ContentTag.Span && objectText is not null
                    && (stylePresent ? styleMarginRight : dataClassPresent && objectText.Length > 0 && char.IsAscii(objectText[0]));
                result = new YomichanContent<ContentTag>(tag, objectText, appendWhitespace, marker);
                return true;
            }

            if (contentKind is ContentValueKind.Array)
            {
                if (contentNeedsParsing)
                {
                    ReadOnlySpan<byte> nestedJson = json.Slice(contentOffset, contentLength);
                    Utf8JsonReader nestedReader = new(nestedJson, true, InitialJsonReaderState);
                    if (!nestedReader.Read() || nestedReader.TokenType is not JsonTokenType.StartArray)
                    {
                        result = default;
                        return false;
                    }

                    if (tag is ContentTag.TH)
                    {
                        Utf8JsonReader nextReader = nestedReader;
                        emptyContentArray = nextReader.Read() && nextReader.TokenType is JsonTokenType.EndArray;
                    }
                    StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
                    int orderedListIndex = 0;
                    ContentTag lastTag = ContentTag.None;
                    int tableColumnIndex = 0;
                    List<int>? childTableRowSpans = null;
                    if (tag is ContentTag.Table)
                    {
                        childTableRowSpans = [];
                    }
                    else if (tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot or ContentTag.TR)
                    {
                        childTableRowSpans = tableRowSpans;
                    }
                    bool success = TryAppendDefinitionsFromJsonArray(
                        ref nestedReader,
                        nestedJson,
                        stringBuilder,
                        dict,
                        ref imageInfos,
                        tag is ContentTag.OL,
                        NormalizeListMarker(listStyleType),
                        ref orderedListIndex,
                        ref lastTag,
                        imageInfoCache,
                        childTableRowSpans,
                        tag is ContentTag.TR && childTableRowSpans is not null,
                        ref tableColumnIndex);

                    if (!success)
                    {
                        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                        result = default;
                        return false;
                    }

                    if (tag is ContentTag.TR && childTableRowSpans is not null)
                    {
                        EpwingYomichanUtils.AdvanceTableRow(stringBuilder, childTableRowSpans, tableColumnIndex);
                    }

                    contentText = stringBuilder.Length > 0
                        ? stringBuilder.ToString()
                        : null;

                    ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                }
            }

            if (contentKind is ContentValueKind.StringValue or ContentValueKind.Array)
            {
                if (contentKind is ContentValueKind.StringValue
                    && tag is ContentTag.A
                    && invalidHref)
                {
                    result = default;
                    return false;
                }

                if (contentKind is ContentValueKind.StringValue
                    && tag is ContentTag.A
                    && href is not null
                    && !href.AsSpan().StartsWith("?query=", StringComparison.Ordinal))
                {
                    contentText = $"{contentText}: {href}";
                }
                else if (tag is ContentTag.TH
                    && ((contentKind is ContentValueKind.StringValue && string.IsNullOrWhiteSpace(contentText))
                        || emptyContentArray))
                {
                    contentText = "×";
                }

                bool appendWhitespace = false;
                if (tag is ContentTag.Span)
                {
                    if (stylePresent)
                    {
                        appendWhitespace = styleMarginRight;
                    }
                    else if (invalidData)
                    {
                        result = default;
                        return false;
                    }
                    else if (dataClassPresent)
                    {
                        appendWhitespace = contentText is { Length: > 0 } && char.IsAscii(contentText[0]);
                    }
                }

                if (tableRowSpans is { Count: > 0 } && tag is ContentTag.THead or ContentTag.TBody or ContentTag.TFoot)
                {
                    tableRowSpans.Clear();
                }

                result = new YomichanContent<ContentTag>(
                    tag,
                    contentText,
                    appendWhitespace,
                    NormalizeListMarker(listStyleType));

                return true;
            }

            result = default;
            return true;
        }

        if (tagPresent)
        {
            if (tag is ContentTag.TR && tableRowSpans is { Count: > 0 })
            {
                StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
                EpwingYomichanUtils.AdvanceTableRow(stringBuilder, tableRowSpans, 0);
                string content = stringBuilder.ToString();
                ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                result = new YomichanContent<ContentTag>(ContentTag.TR, content, false, null);
                return true;
            }

            if (tag is ContentTag.TH)
            {
                result = new YomichanContent<ContentTag>(ContentTag.TH, "×", false, null);
                return true;
            }

            if (tag is ContentTag.TD && tableRowSpans is not null)
            {
                result = new YomichanContent<ContentTag>(ContentTag.TD, null, false, null);
                return true;
            }

            if (tag is ContentTag.BR)
            {
                result = new YomichanContent<ContentTag>(ContentTag.BR, null, false, null);
                return true;
            }

            if (tag is ContentTag.Img && pathPresent)
            {
                if (invalidHeight || invalidWidth)
                {
                    result = default;
                    return false;
                }

                double maximumSize = sizeUnitsEm ? 1D : 16D;
                if (height <= maximumSize && width <= maximumSize)
                {
                    result = default;
                    return true;
                }

                if (invalidPath)
                {
                    result = default;
                    return false;
                }

                Debug.Assert(path is not null);
                string imagePath = PathUtils.GetPortablePath(Path.Join(dict.Path, path));
                if (EpwingYomichanUtils.IsSmallImageWithMissingDimension(imagePath, height, width,
                    heightSpecified, widthSpecified, sizeUnitsEm, imageInfoCache))
                {
                    result = default;
                    return true;
                }

                result = new YomichanContent<ContentTag>(
                    ContentTag.Img,
                    imagePath,
                    false,
                    null);

                return true;
            }

            if (titlePresent)
            {
                if (invalidTitle)
                {
                    result = default;
                    return false;
                }

                result = new YomichanContent<ContentTag>(
                    tag,
                    title,
                    false,
                    null);

                return true;
            }

            result = default;
            return true;
        }

        if (invalidType)
        {
            result = default;
            return false;
        }

        if (type is ContentType.Text && textPresent)
        {
            if (invalidText)
            {
                result = default;
                return false;
            }

            result = new YomichanContent<ContentTag>(ContentTag.Span, text, false, null);
            return true;
        }

        if (type is ContentType.Image && pathPresent)
        {
            if (invalidHeight || invalidWidth)
            {
                result = default;
                return false;
            }

            double maximumSize = sizeUnitsEm ? 1D : 16D;
            if (height <= maximumSize && width <= maximumSize)
            {
                result = default;
                return true;
            }

            if (invalidPath)
            {
                result = default;
                return false;
            }

            Debug.Assert(path is not null);
            string imagePath = PathUtils.GetPortablePath(Path.Join(dict.Path, path));
            if (EpwingYomichanUtils.IsSmallImageWithMissingDimension(imagePath, height, width,
                heightSpecified, widthSpecified, sizeUnitsEm, imageInfoCache))
            {
                result = default;
                return true;
            }

            result = new YomichanContent<ContentTag>(
                ContentTag.Img,
                imagePath,
                false,
                null);

            return true;
        }

        result = default;
        return true;
    }

    private static void SkipNestedContentForLater(ref Utf8JsonReader reader, out int contentOffset, out int contentLength)
    {
        contentOffset = checked((int)reader.TokenStartIndex);
        reader.Skip();
        contentLength = checked((int)reader.BytesConsumed) - contentOffset;
    }

    private static void RemoveImagesFromReplacedContent(ref List<ImageInfo>? imageInfos, int previousCount)
    {
        if (imageInfos is null || imageInfos.Count == previousCount)
        {
            return;
        }

        imageInfos.RemoveRange(previousCount, imageInfos.Count - previousCount);
        if (imageInfos.Count is 0)
        {
            imageInfos = null;
        }
    }

    private static bool TryReadStyle(ref Utf8JsonReader reader, out string? listStyleType, out bool marginRight)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);

        listStyleType = null;
        marginRight = false;
        bool valid = true;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return valid;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                return false;
            }

            if (reader.ValueTextEquals("listStyleType"u8))
            {
                if (!reader.Read())
                {
                    return false;
                }

                valid = TryReadNullableString(ref reader, out listStyleType);
                continue;
            }

            if (reader.ValueTextEquals("marginRight"u8))
            {
                if (!reader.Read())
                {
                    return false;
                }

                marginRight = true;
                if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }

                continue;
            }

            if (!reader.Read())
            {
                return false;
            }

            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return false;
    }

    private static bool TryReadData(ref Utf8JsonReader reader, out bool classPresent)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartObject);

        classPresent = false;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                return true;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                return false;
            }

            if (reader.ValueTextEquals("class"u8))
            {
                classPresent = true;
            }

            if (!reader.Read())
            {
                return false;
            }

            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return false;
    }

    private static bool TryReadNullableString(ref Utf8JsonReader reader, out string? value)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            value = reader.GetString();
            Debug.Assert(value is not null);
            return true;
        }

        if (reader.TokenType is JsonTokenType.Null)
        {
            value = null;
            return true;
        }

        value = null;
        if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
        {
            reader.Skip();
        }

        return false;
    }

    private static string? NormalizeListMarker(string? marker)
    {
        if (marker is "disc")
        {
            marker = "•";
        }
        else if (marker is "circle")
        {
            marker = "◦";
        }
        else if (marker is "square")
        {
            marker = "▪";
        }

        return marker;
    }

    private static ContentTag GetContentTag(ref Utf8JsonReader reader)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.String);

        if (reader.ValueTextEquals("span"u8))
        {
            return ContentTag.Span;
        }

        if (reader.ValueTextEquals("a"u8))
        {
            return ContentTag.A;
        }

        if (reader.ValueTextEquals("ruby"u8))
        {
            return ContentTag.Ruby;
        }

        if (reader.ValueTextEquals("rp"u8))
        {
            return ContentTag.RP;
        }

        if (reader.ValueTextEquals("rt"u8))
        {
            return ContentTag.RT;
        }

        if (reader.ValueTextEquals("li"u8))
        {
            return ContentTag.LI;
        }

        if (reader.ValueTextEquals("ul"u8))
        {
            return ContentTag.UL;
        }

        if (reader.ValueTextEquals("ol"u8))
        {
            return ContentTag.OL;
        }

        if (reader.ValueTextEquals("th"u8))
        {
            return ContentTag.TH;
        }

        if (reader.ValueTextEquals("td"u8))
        {
            return ContentTag.TD;
        }

        if (reader.ValueTextEquals("tr"u8))
        {
            return ContentTag.TR;
        }

        ContentTag tag = ContentTag.Other;
        if (reader.ValueTextEquals("img"u8))
        {
            tag = ContentTag.Img;
        }
        else if (reader.ValueTextEquals("div"u8))
        {
            tag = ContentTag.Div;
        }
        else if (reader.ValueTextEquals("br"u8))
        {
            tag = ContentTag.BR;
        }
        else if (reader.ValueTextEquals("table"u8))
        {
            tag = ContentTag.Table;
        }
        else if (reader.ValueTextEquals("thead"u8))
        {
            tag = ContentTag.THead;
        }
        else if (reader.ValueTextEquals("tbody"u8))
        {
            tag = ContentTag.TBody;
        }
        else if (reader.ValueTextEquals("tfoot"u8))
        {
            tag = ContentTag.TFoot;
        }

        return tag;
    }

    private static ContentType GetContentType(ref Utf8JsonReader reader)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.String);

        return reader.ValueTextEquals("text"u8)
            ? ContentType.Text
            : reader.ValueTextEquals("image"u8)
                ? ContentType.Image
                : ContentType.Other;
    }

    private enum ContentTag
    {
        None,
        Span,
        A,
        Ruby,
        RP,
        RT,
        LI,
        UL,
        OL,
        TH,
        TD,
        TR,
        Table,
        THead,
        TBody,
        TFoot,
        Img,
        Div,
        BR,
        Other
    }

    private enum ContentType
    {
        None,
        Text,
        Image,
        Other
    }

    private enum ContentValueKind
    {
        None,
        StringValue,
        Array,
        ObjectValue,
        Unsupported
    }

}
