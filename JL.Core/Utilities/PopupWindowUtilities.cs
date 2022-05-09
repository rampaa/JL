using System.Text;

namespace JL.Core.Utilities;

public static class PopupWindowUtilities
{
    public static string MakeUiElementReadingsText(List<string> readings, List<string> rOrthographyInfoList)
    {
        if (readings.Count == 0) return "";

        StringBuilder sb = new();

        for (int index = 0; index < readings.Count; index++)
        {
            sb.Append(readings[index]);

            if (index < rOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(rOrthographyInfoList[index]))
                {
                    sb.Append(" (");
                    sb.Append(rOrthographyInfoList[index]);
                    sb.Append(')');
                }
            }

            if (index != readings.Count - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public static string MakeUiElementAlternativeSpellingsText(List<string> alternativeSpellings,
        List<string> aOrthographyInfoList)
    {
        if (alternativeSpellings.Count == 0) return "";

        StringBuilder sb = new();

        sb.Append('(');

        for (int index = 0; index < alternativeSpellings.Count; index++)
        {
            sb.Append(alternativeSpellings[index]);

            if (index < aOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(aOrthographyInfoList[index]))
                {
                    sb.Append(" (");
                    sb.Append(aOrthographyInfoList[index]);
                    sb.Append(')');
                }
            }

            if (index != alternativeSpellings.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.Append(')');

        return sb.ToString();
    }
}
