using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiLoader
{
    public const int Size = 50000;
    internal const long WholeFileParsingThreshold = 32 * 1024 * 1024;
    private const int InitialDefinitionCapacity = 4;
    private const int InitialStatCapacity = 5;

    internal static Utf8JsonReader CreateJsonReader(byte[] jsonBytes)
    {
        ReadOnlySpan<byte> json = jsonBytes;
        ReadOnlySpan<byte> utf8Preamble = Encoding.UTF8.Preamble;
        if (json.StartsWith(utf8Preamble))
        {
            json = json[utf8Preamble.Length..];
        }

        return new Utf8JsonReader(json, new JsonReaderOptions
        {
            AllowTrailingCommas = JsonOptions.DefaultJso.AllowTrailingCommas,
            CommentHandling = JsonOptions.DefaultJso.ReadCommentHandling is JsonCommentHandling.Allow
                ? JsonCommentHandling.Skip
                : JsonOptions.DefaultJso.ReadCommentHandling,
            MaxDepth = JsonOptions.DefaultJso.MaxDepth
        });
    }

    internal static void ReadRecord(ref Utf8JsonReader reader, out string kanji, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats)
    {
        Debug.Assert(reader.TokenType is JsonTokenType.StartArray);

        _ = reader.Read();
        string? kanjiValue = reader.GetString();
        Debug.Assert(kanjiValue is not null);
        kanji = kanjiValue;

        _ = reader.Read();
        string? onReadingsString = reader.GetString();
        Debug.Assert(onReadingsString is not null);
        onReadings = YomichanKanjiRecord.SplitReadings(onReadingsString);

        _ = reader.Read();
        string? kunReadingsString = reader.GetString();
        Debug.Assert(kunReadingsString is not null);
        kunReadings = YomichanKanjiRecord.SplitReadings(kunReadingsString);

        _ = reader.Read(); // Tags
        _ = reader.Read();

        stats = null;
        if (reader.TokenType is JsonTokenType.StartArray)
        {
            string[]? definitionArray = null;
            int definitionCount = 0;
            if (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
            {
                definitionArray = new string[InitialDefinitionCapacity];
                do
                {
                    string? definition = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(definition))
                    {
                        if (definitionCount == definitionArray.Length)
                        {
                            Array.Resize(ref definitionArray, definitionArray.Length * 2);
                        }

                        definitionArray[definitionCount] = definition;
                        ++definitionCount;
                    }
                }
                while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray);
            }

            if (definitionCount is 0)
            {
                definitions = null;
            }
            else
            {
                Debug.Assert(definitionArray is not null);
                if (definitionCount < definitionArray.Length)
                {
                    Array.Resize(ref definitionArray, definitionCount);
                }

                definitions = definitionArray;
            }

            _ = reader.Read();
            Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
            string[]? statArray = null;
            int statCount = 0;
            if (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                statArray = new string[InitialStatCapacity];
                do
                {
                    string? statName = reader.GetString();
                    _ = reader.Read();
                    string? statValue = reader.GetString();
                    Debug.Assert(statName is not null && statValue is not null);
                    if (statCount == statArray.Length)
                    {
                        Array.Resize(ref statArray, statArray.Length * 2);
                    }

                    statArray[statCount] = string.Create(CultureInfo.InvariantCulture, $"{statName}: {statValue}");
                    ++statCount;
                }
                while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject);
            }

            if (statCount > 0)
            {
                Debug.Assert(statArray is not null);
                if (statCount < statArray.Length)
                {
                    Array.Resize(ref statArray, statCount);
                }

                stats = statArray;
            }

            _ = reader.Read();
        }
        else
        {
            string[]? definitionArray = null;
            int definitionCount = 0;
            while (reader.TokenType is JsonTokenType.String)
            {
                string? definition = reader.GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionArray ??= new string[InitialDefinitionCapacity];
                    if (definitionCount == definitionArray.Length)
                    {
                        Array.Resize(ref definitionArray, definitionArray.Length * 2);
                    }

                    definitionArray[definitionCount] = definition;
                    ++definitionCount;
                }

                _ = reader.Read();
            }

            if (definitionArray is not null && definitionCount < definitionArray.Length)
            {
                Array.Resize(ref definitionArray, definitionCount);
            }

            definitions = definitionArray;
        }

        Debug.Assert(reader.TokenType is JsonTokenType.EndArray);
    }

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "kanji_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            if (new FileInfo(jsonFile).Length <= WholeFileParsingThreshold)
            {
                byte[] jsonBytes = await File.ReadAllBytesAsync(jsonFile).ConfigureAwait(false);
                LoadWholeFile(jsonBytes, dict);
            }
            else
            {
                FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
                await using (fileStream.ConfigureAwait(false))
                {
                    await foreach (JsonElement[]? jsonObj in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                    {
                        Debug.Assert(jsonObj is not null);
                        string? kanji = jsonObj[0].GetString();
                        Debug.Assert(kanji is not null);
                        if (string.IsNullOrWhiteSpace(kanji))
                        {
                            continue;
                        }

                        YomichanKanjiRecord record = new(jsonObj);
                        AddRecord(dict, kanji, record);
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static void LoadWholeFile(byte[] jsonBytes, Dict dict)
    {
        Utf8JsonReader reader = CreateJsonReader(jsonBytes);
        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
        {
            throw new JsonException("The Yomichan kanji bank root must be an array.");
        }

        while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
        {
            ReadRecord(ref reader, out string kanji, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats);
            if (!string.IsNullOrWhiteSpace(kanji))
            {
                YomichanKanjiRecord record = new(onReadings, kunReadings, definitions, stats);
                AddRecord(dict, kanji, record);
            }
        }

        if (reader.TokenType is not JsonTokenType.EndArray || reader.Read())
        {
            throw new JsonException("Unexpected content after the Yomichan kanji bank array.");
        }
    }

    private static void AddRecord(Dict dict, string kanji, YomichanKanjiRecord record)
    {
        kanji = kanji.GetPooledString();
        record.OnReadings?.DeduplicateStringsInArray();
        record.KunReadings?.DeduplicateStringsInArray();
        //if (record is { Definitions: null, KunReadings: null, OnReadings: null, Stats: null })
        //{
        //    return;
        //}

        _ = DictUtils.AddRecordToDictionary(kanji, record, dict);
    }
}
