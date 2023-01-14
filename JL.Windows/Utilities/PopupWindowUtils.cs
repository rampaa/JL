using JL.Core.Lookup;
using System.Globalization;
using System.Text;

namespace JL.Windows.Utilities;
internal static class PopupWindowUtils
{
    public static string GradeToText(int grade)
    {
        string gradeText = "";
        switch (grade)
        {
            case 0:
                gradeText = "Hyougai";
                break;
            case <= 6:
                gradeText = $"{grade} (Kyouiku)";
                break;
            case 8:
                gradeText = $"{grade} (Jouyou)";
                break;
            case <= 10:
                gradeText = $"{grade} (Jinmeiyou)";
                break;
        }

        return gradeText;
    }

    public static string FrequenciesToText(List<LookupFrequencyResult> frequencies)
    {
        string freqStr = "";

        if (frequencies.Count is 1 && frequencies[0].Freq is > 0 and not int.MaxValue)
        {
            freqStr = "#" + frequencies.First().Freq;
        }

        else if (frequencies.Count > 1)
        {
            int freqResultCount = 0;
            StringBuilder freqStrBuilder = new();
            foreach (LookupFrequencyResult lookupFreqResult in frequencies)
            {
                if (lookupFreqResult.Freq is int.MaxValue or <= 0)
                {
                    continue;
                }

                _ = freqStrBuilder.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: #{lookupFreqResult.Freq}, ");
                freqResultCount++;
            }

            if (freqResultCount > 0)
            {
                _ = freqStrBuilder.Remove(freqStrBuilder.Length - 2, 1);

                freqStr = freqStrBuilder.ToString();
            }
        }

        return freqStr;
    }
}
