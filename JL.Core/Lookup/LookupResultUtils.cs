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
                _ = first
                    ? deconjugation.Append(CultureInfo.InvariantCulture, $"～{formText}")
                    : deconjugation.Append(CultureInfo.InvariantCulture, $"; {formText}");
            }

            first = false;
        }

        return deconjugation.Length is 0 ? null : deconjugation.ToString();
    }

    public static string GradeToText(int grade)
    {
        string gradeText = grade switch
        {
            >= 1 and <= 6 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Kyouiku)"),
            8 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jouyou)"),
            >= 9 and <= 10 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jinmeiyou)"),
            _ => "Hyougai"
        };

        return gradeText;
    }

    public static string? FrequenciesToText(List<LookupFrequencyResult> frequencies, bool forMining)
    {
        if (!forMining && frequencies.Count is 1 && frequencies[0].Freq is > 0 and < int.MaxValue)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        if (frequencies.Count > 0)
        {
            int freqResultCount = 0;
            StringBuilder sb = new();
            foreach (LookupFrequencyResult lookupFreqResult in frequencies)
            {
                if (lookupFreqResult.Freq is > 0 and < int.MaxValue)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: {lookupFreqResult.Freq}, ");
                    ++freqResultCount;
                }
            }

            if (freqResultCount > 0)
            {
                return sb.Remove(sb.Length - 2, 2).ToString();
            }
        }

        return null;
    }

    public static string ReadingsToText(string[] readings, string[]?[] rOrthographyInfoList)
    {
        StringBuilder sb = new();

        for (int index = 0; index < readings.Length; index++)
        {
            _ = sb.Append(readings[index]);

            if (index < rOrthographyInfoList.Length)
            {
                string[]? rOrthographyInfo = rOrthographyInfoList[index];
                if (rOrthographyInfo is not null)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({string.Join(", ", rOrthographyInfo)})");
                }
            }

            if (index != (readings.Length - 1))
            {
                _ = sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public static string AlternativeSpellingsToText(string[] alternativeSpellings, string[]?[] aOrthographyInfoList)
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
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({string.Join(", ", aOrthographyInfo)})");
                }
            }

            if (index != (alternativeSpellings.Length - 1))
            {
                _ = sb.Append(", ");
            }
        }

        _ = sb.Append(')');

        return sb.ToString();
    }
}
