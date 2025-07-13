using System.Globalization;
using System.Text;
using JL.Core.Utilities;

namespace JL.Core.Lookup;

public static class LookupResultUtils
{
    internal static string? DeconjugationProcessesToText(ReadOnlySpan<List<string>> processList)
    {
        StringBuilder deconjugation = new();
        for (int i = 0; i < processList.Length; i++)
        {
            ref readonly List<string> form = ref processList[i];

            StringBuilder formText = new();
            bool added = false;

            ReadOnlySpan<string> formSpan = form.AsReadOnlySpan();
            for (int j = formSpan.Length - 1; j >= 0; j--)
            {
                string info = formSpan[j];
                if (info.Length is 0)
                {
                    continue;
                }

                bool startsWithParentheses = info[0] is '(';
                if (startsWithParentheses)
                {
                    if (j is not 0)
                    {
                        continue;
                    }

                    if (added)
                    {
                        _ = formText.Append('→');
                    }

                    _ = formText.Append(info.AsSpan(1, info.Length - 2));
                }
                else
                {
                    if (added)
                    {
                        _ = formText.Append('→');
                    }

                    _ = formText.Append(info);
                }

                added = true;
            }

            if (formText.Length is not 0)
            {
                if (i is 0)
                {
                    _ = deconjugation.Append(CultureInfo.InvariantCulture, $"～{formText}");
                }
                else
                {
                    _ = deconjugation.Append(CultureInfo.InvariantCulture, $"; {formText}");
                }
            }
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

    public static string FrequenciesToText(ReadOnlySpan<LookupFrequencyResult> frequencies, bool forMining, bool singleDict)
    {
        if (!forMining && singleDict)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        StringBuilder sb = new();
        for (int i = 0; i < frequencies.Length; i++)
        {
            ref readonly LookupFrequencyResult lookupFreqResult = ref frequencies[i];
            _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: {lookupFreqResult.Freq}");
            if (i + 1 != frequencies.Length)
            {
                _ = sb.Append(", ");
            }
        }
        return sb.ToString();
    }

    public static string ElementWithOrthographyInfoToText(string[] elements, string[]?[] orthographyInfoList)
    {
        StringBuilder sb = new();
        return ElementWithOrthographyInfoToText(sb, elements, orthographyInfoList).ToString();
    }

    public static string ElementWithOrthographyInfoToTextWithParentheses(string[] alternativeSpellings, string[]?[] aOrthographyInfoList)
    {
        StringBuilder sb = new();
        return ElementWithOrthographyInfoToText(sb.Append('('), alternativeSpellings, aOrthographyInfoList)
            .Append(')')
            .ToString();
    }

    private static StringBuilder ElementWithOrthographyInfoToText(StringBuilder sb, string[] elements, string[]?[] orthographyInfoList)
    {
        for (int index = 0; index < elements.Length; index++)
        {
            _ = sb.Append(elements[index]);

            if (index < orthographyInfoList.Length)
            {
                string[]? orthographyInfo = orthographyInfoList[index];
                if (orthographyInfo is not null)
                {
                    _ = sb.Append(" [").AppendJoin(", ", orthographyInfo).Append(']');
                }
            }

            if (index + 1 != elements.Length)
            {
                _ = sb.Append('、');
            }
        }

        return sb;
    }
}
