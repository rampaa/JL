using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
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
    internal const long WholeFileParsingThreshold = 32 * 1024 * 1024;
    private const int LoadEntryBatchSize = 64;
    internal static JsonReaderState InitialJsonReaderState { get; } = CreateJsonReaderState();

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
        Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;

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

        HashSet<string> searchKeys = new(StringComparer.Ordinal);
        List<(string SearchKey, EpwingNazekaRecord Record)> alternativeRecords = [];
        Dictionary<string, ImageInfo?> imageInfoCache = new(StringComparer.Ordinal);

        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await foreach (EpwingNazekaImportEntryBatch batch in ReadEntryBatches(fileStream).ConfigureAwait(false))
            {
                for (int i = 0; i < batch.Count; i++)
                {
                    ref readonly EpwingNazekaImportEntry entry = ref batch.Entries[i];
                    string reading = entry.Reading;
                    List<string>? spellingList = entry.Spellings;
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

                    List<string> definitionList = entry.Definitions;
                    if (definitionList.Count is 0)
                    {
                        continue;
                    }

                    if (spellingList is not null)
                    {
                        for (int j = 0; j < spellingList.Count; j++)
                        {
                            spellingList[j] = spellingList[j].GetPooledString();
                        }
                    }

                    for (int j = 0; j < definitionList.Count; j++)
                    {
                        definitionList[j] = definitionList[j].GetPooledString();
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

                        ImageInfo? imageInfo = GetImageInfo(entry.ImagePath, imageInfoCache);

                        EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions, imageInfo);
                        bool primarySpellingWasAdded = AddSearchKey(primarySpellingInHiragana, record, dict, contents, searchKeys, out _);
                        bool readingWasAdded = primarySpellingWasAdded && nonKanjiDict && nonNameDict
                            && primarySpellingInHiragana != readingInHiragana
                            && AddSearchKey(readingInHiragana, record, dict, contents, searchKeys, out _);

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
                            if (DictUtils.AddRecordToDictionary(alternativeSpellingInHiragana, alternativeRecord, contents, dict) && nonKanjiDict)
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
                                    _ = AddSearchKey(fusejiVariant, record, dict, contents, searchKeys, out _);
                                }
                            }

                            if (readingWasAdded)
                            {
                                if (generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = AddSearchKey(fusejiVariant, record, dict, contents, searchKeys, out _);
                                    }
                                }

                                if (generateMazegaki)
                                {
                                    foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, readingInHiragana))
                                    {
                                        if (AddSearchKey(mazegaki, record, dict, contents, searchKeys, out _) && generateFusejiVariants)
                                        {
                                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                            {
                                                _ = AddSearchKey(fusejiVariant, record, dict, contents, searchKeys, out _);
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
                                    _ = AddSearchKey(fusejiVariant, alternativeRecord, dict, contents, searchKeys, out _);
                                }
                            }

                            if (nonNameDict && generateMazegaki)
                            {
                                foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(alternativeSpellingInHiragana, readingInHiragana))
                                {
                                    _ = AddSearchKey(mazegaki, alternativeRecord, dict, contents, searchKeys, out bool mazegakiWasNew);
                                    if (mazegakiWasNew && generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            _ = AddSearchKey(fusejiVariant, alternativeRecord, dict, contents, searchKeys, out _);
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
                        ImageInfo? imageInfo = GetImageInfo(entry.ImagePath, imageInfoCache);

                        EpwingNazekaRecord record = new(reading, null, null, definitions, imageInfo);
                        _ = DictUtils.AddRecordToDictionary(nonKanjiDict ? JapaneseUtils.NormalizeText(reading).GetPooledString() : reading, record, contents, dict);
                    }
                }
            }
        }

        dict.Contents = contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static async IAsyncEnumerable<EpwingNazekaImportEntryBatch> ReadEntryBatches(FileStream fileStream)
    {
        long fileLength = fileStream.Length;
        if (fileLength > WholeFileParsingThreshold)
        {
            IAsyncEnumerator<JsonElement> enumerator = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, JsonOptions.DefaultJso).GetAsyncEnumerator();
            await using (enumerator.ConfigureAwait(false))
            {
                _ = await enumerator.MoveNextAsync().ConfigureAwait(false);
                EpwingNazekaImportEntry[] streamEntries = ArrayPool<EpwingNazekaImportEntry>.Shared.Rent(LoadEntryBatchSize);
                int entryCount = 0;
                try
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        JsonElement jsonObject = enumerator.Current;
                        string? reading = jsonObject.GetProperty("r").GetString();
                        Debug.Assert(reading is not null);
                        List<string> spellings = ReadStringArray(jsonObject.GetProperty("s"));
                        List<string> definitions = ReadStringArray(jsonObject.GetProperty("l"));
                        string? imagePath = jsonObject.TryGetProperty("i", out JsonElement image) ? image.GetString() : null;
                        streamEntries[entryCount] = new EpwingNazekaImportEntry(reading, spellings.Count > 0 ? spellings : null, definitions, imagePath);
                        ++entryCount;
                        if (entryCount == streamEntries.Length)
                        {
                            yield return new EpwingNazekaImportEntryBatch(streamEntries, entryCount);

                            streamEntries.AsSpan(0, entryCount).Clear();
                            entryCount = 0;
                        }
                    }

                    if (entryCount > 0)
                    {
                        yield return new EpwingNazekaImportEntryBatch(streamEntries, entryCount);
                    }
                }
                finally
                {
                    streamEntries.AsSpan(0, entryCount).Clear();
                    ArrayPool<EpwingNazekaImportEntry>.Shared.Return(streamEntries);
                }
            }

            yield break;
        }

        byte[] json = GC.AllocateUninitializedArray<byte>((int)fileLength);
        await fileStream.ReadExactlyAsync(json).ConfigureAwait(false);
        int offset = json.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
        JsonReaderState readerState = InitialJsonReaderState;
        bool started = false;
        bool completed = false;
        EpwingNazekaImportEntry[] entries = ArrayPool<EpwingNazekaImportEntry>.Shared.Rent(LoadEntryBatchSize);
        int entriesToClear = 0;
        try
        {
            while (!completed)
            {
                entriesToClear = entries.Length;
                int entryCount = ReadImportBatch(json, ref offset, ref readerState, ref started, entries, out completed);
                entriesToClear = entryCount;
                if (entryCount > 0)
                {
                    yield return new EpwingNazekaImportEntryBatch(entries, entryCount);

                    entries.AsSpan(0, entryCount).Clear();
                    entriesToClear = 0;
                }
            }
        }
        finally
        {
            entries.AsSpan(0, entriesToClear).Clear();
            ArrayPool<EpwingNazekaImportEntry>.Shared.Return(entries);
        }
    }

    private static List<string> ReadStringArray(JsonElement jsonArray)
    {
        List<string> values = new(jsonArray.GetArrayLength());
        foreach (JsonElement element in jsonArray.EnumerateArray())
        {
            string? value = element.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    internal static int ReadImportBatch(byte[] json, ref int offset, ref JsonReaderState readerState, ref bool started, EpwingNazekaImportEntry[] entries, out bool completed)
    {
        ReadOnlySpan<byte> jsonBytes = json;
        Utf8JsonReader reader = new(jsonBytes[offset..], true, readerState);
        if (!started)
        {
            if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray || !reader.Read())
            {
                throw new JsonException("The Nazeka dictionary JSON root must be an array with a header.");
            }

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                if (reader.Read())
                {
                    throw new JsonException("Unexpected JSON content after the Nazeka dictionary array.");
                }

                offset += (int)reader.BytesConsumed;
                readerState = reader.CurrentState;
                completed = true;
                return 0;
            }

            reader.Skip();
            started = true;
        }

        int entryCount = 0;
        completed = false;
        while (entryCount < entries.Length)
        {
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of Nazeka dictionary JSON.");
            }

            if (reader.TokenType is JsonTokenType.EndArray)
            {
                completed = true;
                if (reader.Read())
                {
                    throw new JsonException("Unexpected JSON content after the Nazeka dictionary array.");
                }

                break;
            }

            string? reading = null;
            List<string>? spellings = null;
            List<string>? definitions = null;
            string? imagePath = null;

            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                if (reader.TokenType is not JsonTokenType.PropertyName)
                {
                    reader.Skip();
                    continue;
                }

                if (reader.ValueTextEquals("r"u8))
                {
                    _ = reader.Read();
                    reading = reader.GetString();
                }
                else if (reader.ValueTextEquals("s"u8))
                {
                    _ = reader.Read();
                    spellings = ReadStringArray(ref reader);
                }
                else if (reader.ValueTextEquals("l"u8))
                {
                    _ = reader.Read();
                    definitions = ReadStringArray(ref reader);
                }
                else if (reader.ValueTextEquals("i"u8))
                {
                    _ = reader.Read();
                    imagePath = reader.GetString();
                }
                else
                {
                    _ = reader.Read();
                    reader.Skip();
                }
            }

            Debug.Assert(reading is not null);
            Debug.Assert(definitions is not null);
            entries[entryCount] = new EpwingNazekaImportEntry(reading, spellings is { Count: > 0 } ? spellings : null, definitions, imagePath);

            ++entryCount;
        }

        offset += (int)reader.BytesConsumed;
        readerState = reader.CurrentState;
        return entryCount;
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

    private static List<string> ReadStringArray(ref Utf8JsonReader reader)
    {
        List<string> values = [];
        while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
        {
            string? value = reader.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static ImageInfo? GetImageInfo(string? imagePath, Dictionary<string, ImageInfo?> imageInfoCache)
    {
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

    private static bool AddSearchKey(string searchKey, EpwingNazekaRecord record, Dict dict, Dictionary<string, IList<IDictRecord>> contents, HashSet<string> searchKeys, out bool searchKeyWasNew)
    {
        searchKeyWasNew = searchKeys.Add(searchKey);
        return searchKeyWasNew && DictUtils.AddRecordToDictionary(searchKey, record, contents, dict);
    }
}
