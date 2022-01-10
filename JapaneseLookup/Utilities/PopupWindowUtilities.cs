using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JapaneseLookup.Utilities
{
    public static class PopupWindowUtilities
    {
        public static string MakeUiElementReadingsText(List<string> readings, List<string> rOrthographyInfoList)
        {
            var sb = new StringBuilder();
            if (readings.Count == 0) return sb.ToString();

            for (var index = 0; index < readings.Count; index++)
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
            var sb = new StringBuilder();
            if (alternativeSpellings.Count == 0) return sb.ToString();

            sb.Append('(');

            for (var index = 0; index < alternativeSpellings.Count; index++)
            {
                sb.Append(alternativeSpellings[index]);

                if (aOrthographyInfoList != null)
                {
                    if (index < aOrthographyInfoList.Count)
                    {
                        var altOrtho = "(" + aOrthographyInfoList[index] + ")";
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

        public static string FindSentence(string text, int position)
        {
            if (text == null)
                return null;

            List<string> japanesePunctuation = new()
            {
                "。",
                "！",
                "？",
                "…",
                ".",
                "\n",
            };

            Dictionary<string, string> japaneseParentheses = new() { { "「", "」" }, { "『", "』" }, { "（", "）" }, };

            int startPosition = -1;
            int endPosition = -1;

            foreach (string punctuation in japanesePunctuation)
            {
                int tempIndex = text.LastIndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex > startPosition)
                    startPosition = tempIndex;

                tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                    endPosition = tempIndex;
            }

            ++startPosition;

            if (endPosition == -1)
                endPosition = text.Length - 1;

            string sentence;

            if (startPosition < endPosition)
            {
                sentence = text[startPosition..(endPosition + 1)].Trim('\n', '\t', '\r', ' ', '　');
            }

            else
            {
                sentence = "";
            }

            if (sentence.Length > 1)
            {
                if (japaneseParentheses.ContainsValue(sentence.First().ToString()))
                {
                    sentence = sentence[1..];
                }

                if (japaneseParentheses.ContainsKey(sentence.LastOrDefault().ToString()))
                {
                    sentence = sentence[..^1];
                }

                if (japaneseParentheses.TryGetValue(sentence.FirstOrDefault().ToString(), out string rightParenthesis))
                {
                    if (sentence.Last().ToString() == rightParenthesis)
                        sentence = sentence[1..^1];

                    else if (!sentence.Contains(rightParenthesis))
                        sentence = sentence[1..];

                    else if (sentence.Contains(rightParenthesis))
                    {
                        int numberOfLeftParentheses = sentence.Count(p => p == sentence[0]);
                        int numberOfRightParentheses = sentence.Count(p => p == rightParenthesis[0]);

                        if (numberOfLeftParentheses == numberOfRightParentheses + 1)
                            sentence = sentence[1..];
                    }
                }

                else if (japaneseParentheses.ContainsValue(sentence.LastOrDefault().ToString()))
                {
                    string leftParenthesis = japaneseParentheses.First(p => p.Value == sentence.Last().ToString()).Key;

                    if (!sentence.Contains(leftParenthesis))
                        sentence = sentence[..^1];

                    else if (sentence.Contains(leftParenthesis))
                    {
                        int numberOfLeftParentheses = sentence.Count(p => p == leftParenthesis[0]);
                        int numberOfRightParentheses = sentence.Count(p => p == sentence.Last());

                        if (numberOfRightParentheses == numberOfLeftParentheses + 1)
                            sentence = sentence[..^1];
                    }
                }
            }

            return sentence;
        }
    }
}
