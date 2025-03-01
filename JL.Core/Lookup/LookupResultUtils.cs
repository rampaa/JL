using System.Globalization;
using System.Text;

namespace JL.Core.Lookup;

public static class LookupResultUtils
{
    internal static string? DeconjugationProcessesToText(List<List<string>>? processList)
    {
        if (processList is null)
        {
            return null;
        }

        StringBuilder deconjugation = new();
        bool first = true;

        int processListCount = processList.Count;
        for (int i = 0; i < processListCount; i++)
        {
            List<string> form = processList[i];

            StringBuilder formText = new();
            int added = 0;

            for (int j = form.Count - 1; j >= 0; j--)
            {
                string info = form[j];

                if (info.Length is 0)
                {
                    continue;
                }

                if (info.StartsWith('(') && info.EndsWith(')') && j is not 0)
                {
                    continue;
                }

                if (added > 0)
                {
                    _ = formText.Append('→');
                }

                ++added;
                _ = formText.Append(info);
            }

            if (formText.Length is not 0)
            {
                if (first)
                {
                    _ = deconjugation.Append(CultureInfo.InvariantCulture, $"～{formText}");
                }
                else
                {
                    _ = deconjugation.Append(CultureInfo.InvariantCulture, $"; {formText}");
                }
            }

            first = false;
        }

        return deconjugation.Length is 0
            ? null
            : deconjugation.ToString();
    }

    public static string GradeToText(int grade)
    {
        string gradeText = grade switch
        {
            >= 1 and <= 6 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Kyouiku)"),
            8 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jouyou)"),
            9 or 10 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jinmeiyou)"),
            _ => "Hyougai"
        };

        return gradeText;
    }

    public static string FrequenciesToText(List<LookupFrequencyResult> frequencies, bool forMining, bool singleDict)
    {
        if (!forMining && singleDict)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        StringBuilder sb = new();
        for (int i = 0; i < frequencies.Count; i++)
        {
            LookupFrequencyResult lookupFreqResult = frequencies[i];
            _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: {lookupFreqResult.Freq}");
            if (i + 1 != frequencies.Count)
            {
                _ = sb.Append(", ");
            }
        }
        return sb.ToString();
    }

    public static string ElementWithOrthographyInfoToText(string[] elements, string[]?[] orthographyInfoList)
    {
        StringBuilder sb = new();

        for (int index = 0; index < elements.Length; index++)
        {
            _ = sb.Append(elements[index]);

            if (index < orthographyInfoList.Length)
            {
                string[]? orthographyInfo = orthographyInfoList[index];
                if (orthographyInfo is not null)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" [{string.Join(", ", orthographyInfo)}]");
                }
            }

            if (index + 1 != elements.Length)
            {
                _ = sb.Append('、');
            }
        }

        return sb.ToString();
    }

    public static string ElementWithOrthographyInfoToTextWithParentheses(string[] alternativeSpellings, string[]?[] aOrthographyInfoList)
    {
        StringBuilder sb = new();

        _ = sb.Append('(');

        for (int index = 0; index < alternativeSpellings.Length; index++)
        {
            _ = sb.Append(alternativeSpellings[index]);

            if (index < aOrthographyInfoList.Length)
            {
                string[]? aOrthographyInfo = aOrthographyInfoList[index];
                if (aOrthographyInfo is not null)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" [{string.Join(", ", aOrthographyInfo)}]");
                }
            }

            if (index + 1 != alternativeSpellings.Length)
            {
                _ = sb.Append('、');
            }
        }

        _ = sb.Append(')');

        return sb.ToString();
    }
}
