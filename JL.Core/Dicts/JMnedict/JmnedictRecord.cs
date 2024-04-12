using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.JMnedict;

internal sealed class JmnedictRecord : IDictRecord
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
        //RelatedTerms = new List<List<string>?>();
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        if (Definitions.Length is 1)
        {
            string[] nameTypes = NameTypes[0];
            return nameTypes.Length > 1 || !nameTypes.Contains("unclass")
                ? $"({string.Join(", ", nameTypes)}) {string.Join("; ", Definitions[0])}"
                : string.Join("; ", Definitions[0]);
        }

        bool newlines = options?.NewlineBetweenDefinitions?.Value ?? true;

        string separator = newlines
            ? "\n"
            : " ";

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

            // if (options?.RelatedTerm?.Value ?? false)
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
        if (obj is null)
        {
            return false;
        }

        JmnedictRecord jmnedictObj = (JmnedictRecord)obj;
        return Id == jmnedictObj.Id && PrimarySpelling == jmnedictObj.PrimarySpelling;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Id * PrimarySpelling.GetHashCode(StringComparison.Ordinal);
        }
    }
}
