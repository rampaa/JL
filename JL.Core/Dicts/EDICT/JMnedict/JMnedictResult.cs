using System.Text;

namespace JL.Core.Dicts.EDICT.JMnedict;

public class JMnedictResult : IResult
{
    public int Id { get; set; }
    public string PrimarySpelling { get; set; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<string>? Readings { get; set; }
    public List<string>? NameTypes { get; set; }
    public List<string>? Definitions { get; set; }

    public JMnedictResult()
    {
        PrimarySpelling = string.Empty;
        AlternativeSpellings = new List<string>();
        Readings = new List<string>();
        NameTypes = new List<string>();
        Definitions = new List<string>();
    }

    public string? BuildFormattedDefinition()
    {
        if (Definitions is null)
            return null;

        int count = 1;
        StringBuilder defResult = new();

        if (NameTypes != null &&
            (NameTypes.Count > 1 || !NameTypes.Contains("unclass")))
        {
            for (int i = 0; i < NameTypes.Count; i++)
            {
                defResult.Append('(');
                defResult.Append(NameTypes[i]);
                defResult.Append(") ");
            }
        }

        for (int i = 0; i < Definitions.Count; i++)
        {
            if (Definitions.Any())
            {
                if (Definitions.Count > 0)
                    defResult.Append($"({count}) ");

                defResult.Append($"{string.Join("; ", Definitions[i])} ");
                ++count;
            }
        }

        return defResult.ToString();
    }
}
