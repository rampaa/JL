using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EDICT.JMnedict;

internal sealed class JmnedictRecord : IDictRecord
{
    public int Id { get; set; }
    public string PrimarySpelling { get; }
    public string[]? AlternativeSpellings { get; set; }
    public string[]? Readings { get; set; }
    public string[][] NameTypes { get; set; }
    public string[][] Definitions { get; set; }
    //public List<List<string>?>? RelatedTerms { get; set; }

    public JmnedictRecord(string primarySpelling, string[]? readings, string[][] definitions, string[][] nameTypes)
    {
        PrimarySpelling = primarySpelling;
        Readings = readings;
        Definitions = definitions;
        NameTypes = nameTypes;
        Id = 0;
        //RelatedTerms = new List<List<string>?>();
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        bool multipleDefinitions = Definitions.Length > 1;

        StringBuilder defResult = new();

        for (int i = 0; i < Definitions.Length; i++)
        {
            string[] definitions = Definitions[i];

            if (multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (NameTypes?.Length >= i)
            {
                string[] nameTypes = NameTypes[i];

                if (nameTypes.Length > 1 || !nameTypes.Contains("unclass"))
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", nameTypes)}) ");
                }
            }

            //if (options?.RelatedTerm?.Value ?? false)
            //{
            //    List<string>? relatedTerms = RelatedTerms?[i];
            //    if (relatedTerms?.Count > 0)
            //    {
            //        _ = defResult.Append("(related terms: {string.Join(", ", relatedTerms)}) ");
            //    }
            //}

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", definitions)} {separator}");
        }

        return defResult.Remove(defResult.Length - separator.Length - 1, separator.Length + 1).ToString();
    }
}
