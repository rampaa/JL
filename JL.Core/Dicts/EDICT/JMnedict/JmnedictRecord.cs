using System.Globalization;
using System.Text;

namespace JL.Core.Dicts.EDICT.JMnedict;

public class JmnedictRecord : IDictRecord
{
    public int Id { get; set; }
    public string PrimarySpelling { get; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<string>? Readings { get; set; }
    public List<string>? NameTypes { get; set; }
    public List<string>? Definitions { get; set; }

    public JmnedictRecord(string primarySpelling)
    {
        PrimarySpelling = primarySpelling;
        AlternativeSpellings = new List<string>();
        Readings = new List<string>();
        NameTypes = new List<string>();
        Definitions = new List<string>();
    }

    public string? BuildFormattedDefinition()
    {
        if (Definitions is null)
        {
            return null;
        }

        int count = 1;
        StringBuilder defResult = new();

        if (NameTypes is not null &&
            (NameTypes.Count > 1 || !NameTypes.Contains("unclass")))
        {
            for (int i = 0; i < NameTypes.Count; i++)
            {
                _ = defResult.Append('(')
                    .Append(NameTypes[i])
                    .Append(") ");
            }
        }

        for (int i = 0; i < Definitions.Count; i++)
        {
            if (Definitions.Count > 0)
            {
                if (Definitions.Count > 1)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({count}) ");
                }

                _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", Definitions[i])} ");
                ++count;
            }
        }

        return defResult.ToString();
    }
}
