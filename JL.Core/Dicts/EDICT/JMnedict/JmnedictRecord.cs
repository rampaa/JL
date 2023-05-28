using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EDICT.JMnedict;

internal sealed class JmnedictRecord : IDictRecord
{
    public int Id { get; set; }
    public string PrimarySpelling { get; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<string>? Readings { get; set; }
    public List<List<string>?>? NameTypes { get; set; }
    public List<List<string>?>? Definitions { get; set; }
    //public List<List<string>?>? RelatedTerms { get; set; }

    public JmnedictRecord(string primarySpelling)
    {
        PrimarySpelling = primarySpelling;
        AlternativeSpellings = new List<string>();
        Readings = new List<string>();
        NameTypes = new List<List<string>?>();
        Definitions = new List<List<string>?>();
        //RelatedTerms = new List<List<string>?>();
    }

    public string? BuildFormattedDefinition(DictOptions? options)
    {
        if (Definitions is null)
        {
            return null;
        }

        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        StringBuilder defResult = new();

        for (int i = 0; i < Definitions.Count; i++)
        {
            List<string>? definitions = Definitions[i];
            if (definitions is null)
            {
                continue;
            }

            if (Definitions.Count > 1)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (NameTypes?.Count >= i)
            {
                List<string>? nameTypes = NameTypes[i];

                if (nameTypes is not null &&
                    (nameTypes.Count > 1 || !nameTypes.Contains("unclass")))
                {
                    _ = defResult.Append('(')
                    .Append(string.Join(", ", nameTypes))
                    .Append(") ");
                }
            }

            //if ((options?.RelatedTerm?.Value ?? false) && RelatedTerms?[i]?.Count > 0)
            //{
            //    _ = defResult.Append("(related terms: ")
            //        .Append(string.Join(", ", RelatedTerms[i]!))
            //        .Append(") ");
            //}

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", definitions)} ")
                .Append(separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }
}
