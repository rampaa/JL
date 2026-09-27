using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Dicts.KanjiDict;

internal sealed class YomichanKanjiRecord : IDictRecord, IEquatable<YomichanKanjiRecord>
{
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    //public string[]? Tags { get; }
    public string[]? Definitions { get; }
    public string[]? Stats { get; }

    public YomichanKanjiRecord(string[]? onReadings, string[]? kunReadings, string[]? definitions, string[]? stats)
    {
        OnReadings = onReadings;
        KunReadings = kunReadings;
        Definitions = definitions;
        Stats = stats;
    }

    public YomichanKanjiRecord(JsonElement[] jsonElement)
    {
        ReadFields(jsonElement, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats);
        OnReadings = onReadings;
        KunReadings = kunReadings;
        Definitions = definitions;
        Stats = stats;
    }

    internal static void ReadFields(JsonElement[] jsonElement, out string[]? onReadings, out string[]? kunReadings, out string[]? definitions, out string[]? stats)
    {
        bool isVersion3 = jsonElement.Length > 4 && jsonElement[4].ValueKind is JsonValueKind.Array;
        stats = null;
        if (isVersion3)
        {
            // Stats
            ref readonly JsonElement statsElement = ref jsonElement[5];
            int statsElementPropertyCount = statsElement.GetPropertyCount();
            if (statsElementPropertyCount > 0)
            {
                stats = new string[statsElementPropertyCount];
                int index = 0;
                foreach (JsonProperty stat in statsElement.EnumerateObject())
                {
                    stats[index] = string.Create(CultureInfo.InvariantCulture, $"{stat.Name}: {stat.Value}");
                    ++index;
                }
            }
        }

        // Definitions
        int definitionCapacity = isVersion3 ? jsonElement[4].GetArrayLength() : jsonElement.Length - 4;
        string[] definitionArray = new string[definitionCapacity];
        int definitionCount = 0;
        if (isVersion3)
        {
            foreach (JsonElement definitionElement in jsonElement[4].EnumerateArray())
            {
                string? definition = definitionElement.GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionArray[definitionCount] = definition;
                    ++definitionCount;
                }
            }
        }
        else
        {
            for (int i = 4; i < jsonElement.Length; i++)
            {
                string? definition = jsonElement[i].GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionArray[definitionCount] = definition;
                    ++definitionCount;
                }
            }
        }

        if (definitionCount is 0)
        {
            definitions = null;
        }
        else
        {
            if (definitionCount < definitionArray.Length)
            {
                Array.Resize(ref definitionArray, definitionCount);
            }

            definitions = definitionArray;
        }

        // Tags
        //string? tagsStr = jsonElement[3].GetString();
        //Debug.Assert(tagsStr is not null);
        //Tags = tagsStr!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //if (Tags.Length is 0)
        //{
        //    Tags = null;
        //}

        // Kun Readings
        string? kunReadingsStr = jsonElement[2].GetString();
        Debug.Assert(kunReadingsStr is not null);
        kunReadings = SplitReadings(kunReadingsStr);

        // On readings
        string? onReadingsStr = jsonElement[1].GetString();
        Debug.Assert(onReadingsStr is not null);
        onReadings = SplitReadings(onReadingsStr);
    }

    internal static string[]? SplitReadings(string readings)
    {
        if (readings.Length is 0)
        {
            return null;
        }

        string[] result = readings.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return result.Length > 0 ? result : null;
    }

    public string? BuildFormattedDefinition(DictOptions options)
    {
        Debug.Assert(options.NewlineBetweenDefinitions is not null);

        return Definitions is null
            ? null
            : string.Join(options.NewlineBetweenDefinitions.Value ? '\n' : '；', Definitions);
    }

    public string? BuildFormattedStats()
    {
        string[]? stats = Stats;

        if (stats is null)
        {
            return null;
        }

        if (stats.Length is 1)
        {
            return stats[0];
        }

        StringBuilder statBuilder = ObjectPoolManager.StringBuilderPool.Get();
        for (int i = 0; i < stats.Length; i++)
        {
            _ = statBuilder.Append(stats[i]);
            if (i + 1 != stats.Length)
            {
                _ = statBuilder.Append('\n');
            }
        }

        string stat = statBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(statBuilder);
        return stat;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is YomichanKanjiRecord other && Equals(other);
    }

    public bool Equals([NotNullWhen(true)] IDictRecord? other)
    {
        return other is YomichanKanjiRecord yomichanKanjiRecord && Equals(yomichanKanjiRecord);
    }

    public bool Equals([NotNullWhen(true)] YomichanKanjiRecord? other)
    {
        return other is not null
               && (ReferenceEquals(this, other) || ((OnReadings is not null
                    ? other.OnReadings is not null && OnReadings.SequenceEqual(other.OnReadings)
                    : other.OnReadings is null)
                && (KunReadings is not null
                    ? other.KunReadings is not null && KunReadings.SequenceEqual(other.KunReadings)
                    : other.KunReadings is null)
                && (Definitions is not null
                    ? other.Definitions is not null && Definitions.SequenceEqual(other.Definitions)
                    : other.Definitions is null)
                && (Stats is not null
                    ? other.Stats is not null && Stats.SequenceEqual(other.Stats)
                    : other.Stats is null)));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * 37;
            string[]? onReadings = OnReadings;
            if (onReadings is not null)
            {
                foreach (string onReading in onReadings)
                {
                    hash = (hash * 37) + onReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[]? kunReadings = KunReadings;
            if (kunReadings is not null)
            {
                foreach (string kunReading in kunReadings)
                {
                    hash = (hash * 37) + kunReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[]? definitions = Definitions;
            if (definitions is not null)
            {
                foreach (string definition in definitions)
                {
                    hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[]? stats = Stats;
            if (stats is not null)
            {
                foreach (string stat in stats)
                {
                    hash = (hash * 37) + stat.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public static bool operator ==(YomichanKanjiRecord left, YomichanKanjiRecord right) => left.Equals(right);
    public static bool operator !=(YomichanKanjiRecord left, YomichanKanjiRecord right) => !left.Equals(right);
}
