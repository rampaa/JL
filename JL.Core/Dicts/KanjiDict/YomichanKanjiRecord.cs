using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;

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

    public YomichanKanjiRecord(ReadOnlySpan<JsonElement> jsonElement)
    {
        OnReadings = jsonElement[1].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (OnReadings.Length is 0)
        {
            OnReadings = null;
        }

        else
        {
            OnReadings.DeduplicateStringsInArray();
        }

        KunReadings = jsonElement[2].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (KunReadings.Length is 0)
        {
            KunReadings = null;
        }

        else
        {
            KunReadings.DeduplicateStringsInArray();
        }

        //Tags = jsonElement[3].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //if (Tags.Length is 0)
        //{
        //    Tags = null;
        //}

        JsonElement definitionsArray = jsonElement[4];
        List<string> definitionList = new(definitionsArray.GetArrayLength());
        foreach (JsonElement definitionElement in definitionsArray.EnumerateArray())
        {
            string? definition = definitionElement.GetString();
            if (!string.IsNullOrWhiteSpace(definition))
            {
                definitionList.Add(definition);
            }
        }

        Definitions = definitionList.TrimToArray();

        JsonElement statsElement = jsonElement[5];
        int statsElementPropertyCount = statsElement.GetPropertyCount();
        if (statsElementPropertyCount > 0)
        {
            Stats = new string[statsElementPropertyCount];
            int index = 0;
            foreach (JsonProperty stat in statsElement.EnumerateObject())
            {
                Stats[index] = string.Create(CultureInfo.InvariantCulture, $"{stat.Name}: {stat.Value}");
                ++index;
            }
        }
    }

    public string? BuildFormattedDefinition(DictOptions options)
    {
        return Definitions is null
            ? null
            : string.Join(options.NewlineBetweenDefinitions!.Value ? '\n' : '；', Definitions);
    }

    public string? BuildFormattedStats()
    {
        if (Stats is null)
        {
            return null;
        }

        if (Stats.Length is 1)
        {
            return Stats[0];
        }

        StringBuilder statResult = new();
        for (int i = 0; i < Stats.Length; i++)
        {
            _ = statResult.Append(Stats[i]);
            if (i + 1 != Stats.Length)
            {
                _ = statResult.Append('\n');
            }
        }

        return statResult.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is YomichanKanjiRecord yomichanKanjiRecord
               && ((OnReadings is null && yomichanKanjiRecord.OnReadings is null)
                || (OnReadings is not null && yomichanKanjiRecord.OnReadings is not null && OnReadings.SequenceEqual(yomichanKanjiRecord.OnReadings)))
               && ((KunReadings is null && yomichanKanjiRecord.KunReadings is null)
                || (KunReadings is not null && yomichanKanjiRecord.KunReadings is not null && KunReadings.SequenceEqual(yomichanKanjiRecord.KunReadings)))
               && ((Definitions is null && yomichanKanjiRecord.Definitions is null)
                || (Definitions is not null && yomichanKanjiRecord.Definitions is not null && Definitions.SequenceEqual(yomichanKanjiRecord.Definitions)));
    }

    public bool Equals(YomichanKanjiRecord? other)
    {
        return other is not null
               && ((OnReadings is null && other.OnReadings is null)
                || (OnReadings is not null && other.OnReadings is not null && OnReadings.SequenceEqual(other.OnReadings)))
               && ((KunReadings is null && other.KunReadings is null)
                || (KunReadings is not null && other.KunReadings is not null && KunReadings.SequenceEqual(other.KunReadings)))
               && ((Definitions is null && other.Definitions is null)
                || (Definitions is not null && other.Definitions is not null && Definitions.SequenceEqual(other.Definitions)));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * 37;
            if (OnReadings is not null)
            {
                foreach (string onReading in OnReadings)
                {
                    hash = (hash * 37) + onReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (KunReadings is not null)
            {
                foreach (string kunReading in KunReadings)
                {
                    hash = (hash * 37) + kunReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (Definitions is not null)
            {
                foreach (string definition in Definitions)
                {
                    hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
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
