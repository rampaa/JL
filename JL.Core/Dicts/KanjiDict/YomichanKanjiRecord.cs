using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        string? onReadingsStr = jsonElement[1].GetString();
        Debug.Assert(onReadingsStr is not null);
        OnReadings = onReadingsStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (OnReadings.Length is 0)
        {
            OnReadings = null;
        }

        else
        {
            OnReadings.DeduplicateStringsInArray();
        }

        string? kunReadingsStr = jsonElement[2].GetString();
        Debug.Assert(kunReadingsStr is not null);
        KunReadings = kunReadingsStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

        ref readonly JsonElement definitionsArray = ref jsonElement[4];
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
        return obj is YomichanKanjiRecord other
               && (ReferenceEquals(this, other) || (((OnReadings is null && other.OnReadings is null)
                || (OnReadings is not null && other.OnReadings is not null && OnReadings.AsReadOnlySpan().SequenceEqual(other.OnReadings)))
               && ((KunReadings is null && other.KunReadings is null)
                || (KunReadings is not null && other.KunReadings is not null && KunReadings.AsReadOnlySpan().SequenceEqual(other.KunReadings)))
               && ((Definitions is null && other.Definitions is null)
                || (Definitions is not null && other.Definitions is not null && Definitions.AsReadOnlySpan().SequenceEqual(other.Definitions)))));
    }

    public bool Equals([NotNullWhen(true)] YomichanKanjiRecord? other)
    {
        return other is not null
               && (ReferenceEquals(this, other) || (((OnReadings is null && other.OnReadings is null)
                || (OnReadings is not null && other.OnReadings is not null && OnReadings.AsReadOnlySpan().SequenceEqual(other.OnReadings)))
               && ((KunReadings is null && other.KunReadings is null)
                || (KunReadings is not null && other.KunReadings is not null && KunReadings.AsReadOnlySpan().SequenceEqual(other.KunReadings)))
               && ((Definitions is null && other.Definitions is null)
                || (Definitions is not null && other.Definitions is not null && Definitions.AsReadOnlySpan().SequenceEqual(other.Definitions)))));
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

            return hash;
        }
    }

    public static bool operator ==(YomichanKanjiRecord left, YomichanKanjiRecord right) => left.Equals(right);
    public static bool operator !=(YomichanKanjiRecord left, YomichanKanjiRecord right) => !left.Equals(right);
}
