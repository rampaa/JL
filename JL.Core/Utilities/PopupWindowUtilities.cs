using System.Text;

namespace JL.Core.Utilities
{
    public static class PopupWindowUtilities
    {
        public static string MakeUiElementReadingsText(List<string> readings, List<string> rOrthographyInfoList)
        {
            StringBuilder sb = new();
            if (readings.Count == 0) return sb.ToString();

            for (int index = 0; index < readings.Count; index++)
            {
                sb.Append(readings[index]);

                if (rOrthographyInfoList != null)
                {
                    if (index < rOrthographyInfoList.Count)
                    {
                        string readingOrtho = "(" + rOrthographyInfoList[index] + ")";
                        if (readingOrtho != "()")
                        {
                            sb.Append(' ');
                            sb.Append(readingOrtho);
                        }
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
            StringBuilder sb = new();
            if (alternativeSpellings.Count == 0) return sb.ToString();

            sb.Append('(');

            for (int index = 0; index < alternativeSpellings.Count; index++)
            {
                sb.Append(alternativeSpellings[index]);

                if (aOrthographyInfoList != null)
                {
                    if (index < aOrthographyInfoList.Count)
                    {
                        string altOrtho = "(" + aOrthographyInfoList[index] + ")";
                        if (altOrtho != "()")
                        {
                            sb.Append(' ');
                            sb.Append(altOrtho);
                        }
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
}
