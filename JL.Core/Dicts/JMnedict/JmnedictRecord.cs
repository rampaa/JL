using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.JMnedict;

public sealed class JmnedictRecord : IDictRecordWithMultipleReadings, IEquatable<JmnedictRecord>
{
    public int Id { get; }
    public string PrimarySpelling { get; }
    public string[]? AlternativeSpellings { get; }
    public string[]? Readings { get; }
    public string[][] Definitions { get; }
    public string[][] NameTypes { get; }
    //public string[]?[]? RelatedTerms { get; }

    public JmnedictRecord(int id, string primarySpelling, string[]? alternativeSpellings, string[]? readings, string[][] definitions, string[][] nameTypes)
    {
        Id = id;
        PrimarySpelling = primarySpelling;
        AlternativeSpellings = alternativeSpellings;
        Readings = readings;
        Definitions = definitions;
        NameTypes = nameTypes;
        //RelatedTerms = relatedTerms;
    }

    public string BuildFormattedDefinition(DictOptions options)
    {
        if (Definitions.Length is 1)
        {
            string[] nameTypes = NameTypes[0];
            return nameTypes.Length > 1 || nameTypes[0] is not "unclass"
                ? $"[{string.Join(", ", nameTypes)}] {string.Join("; ", Definitions[0])}"
                : string.Join("; ", Definitions[0]);
        }

        Debug.Assert(options.NewlineBetweenDefinitions is not null);
        char separator = options.NewlineBetweenDefinitions.Value
            ? '\n'
            : '；';

        StringBuilder defResult = new();

        string[][] definitions = Definitions;
        for (int i = 0; i < definitions.Length; i++)
        {
            int sequence = i + 1;
            _ = defResult.Append(CultureInfo.InvariantCulture, $"{sequence}. ");

            string[] nameTypes = NameTypes[i];
            if (nameTypes.Length > 1 || nameTypes[0] is not "unclass")
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", nameTypes)}] ");
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", definitions[i])}");

            // if (showRelatedTerms)
            // {
            //     string[]? relatedTerms = RelatedTerms?[i];
            //     if (relatedTerms?.Length > 0)
            //     {
            //         _ = defResult.Append("(related terms: {string.Join(", ", relatedTerms)}) ");
            //     }
            // }

            if (sequence != definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, PrimarySpelling.GetHashCode(StringComparison.Ordinal));
    }

    public override bool Equals(object? obj)
    {
        return obj is JmnedictRecord jmnedictObj
               && Id == jmnedictObj.Id
               && PrimarySpelling == jmnedictObj.PrimarySpelling;
    }

    public bool Equals(JmnedictRecord? other)
    {
        return other is not null
               && Id == other.Id
               && PrimarySpelling == other.PrimarySpelling;
    }

    public static bool operator ==(JmnedictRecord? left, JmnedictRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(JmnedictRecord? left, JmnedictRecord? right) => !left?.Equals(right) ?? right is not null;
}
