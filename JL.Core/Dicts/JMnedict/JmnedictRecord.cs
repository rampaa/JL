using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.JMnedict;

internal sealed class JmnedictRecord : IDictRecord, IEquatable<JmnedictRecord>
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
            return nameTypes.Length > 1 || !nameTypes.Contains("unclass")
                ? $"({string.Join(", ", nameTypes)}) {string.Join("; ", Definitions[0])}"
                : string.Join("; ", Definitions[0]);
        }

        bool newlines = options.NewlineBetweenDefinitions!.Value;

        char separator = newlines
            ? '\n'
            : '；';

        StringBuilder defResult = new();
        for (int i = 0; i < Definitions.Length; i++)
        {
            string[] definitions = Definitions[i];

            if (newlines)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (NameTypes.Length >= i)
            {
                string[] nameTypes = NameTypes[i];
                if (nameTypes.Length > 1 || !nameTypes.Contains("unclass"))
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", nameTypes)}) ");
                }
            }

            if (!newlines)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            // if (showRelatedTerms)
            // {
            //     string[]? relatedTerms = RelatedTerms?[i];
            //     if (relatedTerms?.Length > 0)
            //     {
            //         _ = defResult.Append("(related terms: {string.Join(", ", relatedTerms)}) ");
            //     }
            // }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", definitions)}");

            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
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

    public override int GetHashCode()
    {
        unchecked
        {
            return Id * PrimarySpelling.GetHashCode(StringComparison.Ordinal);
        }
    }
}
