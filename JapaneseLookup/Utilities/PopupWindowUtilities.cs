using JapaneseLookup.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace JapaneseLookup.Utilities
{
    public static class PopupWindowUtilities
    {
        // super bad hack that improves performance by a lot when many dictionaries are loaded...
        public const int MaxNumberOfResultsWhenNotInMiningMode = 10;

        public static TextBlock MakeTextBlockReadings(List<string> readings, List<string> rOrthographyInfoList)
        {
            var textBlockReadings = new TextBlock
            {
                Name = LookupResult.Readings.ToString(),
                Text = "",
                Tag = string.Join(", ", readings), // for mining
                Foreground = ConfigManager.ReadingsColor,
                FontSize = ConfigManager.ReadingsFontSize,
                Margin = new Thickness(5, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            if (readings.Count == 0) return textBlockReadings;

            for (var index = 0; index < readings.Count; index++)
            {
                var runReading = new Run(readings[index])
                {
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                };
                textBlockReadings.Inlines.Add(runReading);

                if (rOrthographyInfoList != null)
                {
                    if (index < rOrthographyInfoList.Count)
                    {
                        var runReadingOrtho = new Run("(" + rOrthographyInfoList[index] + ")")
                        {
                            Foreground = ConfigManager.ROrthographyInfoColor,
                            FontSize = ConfigManager.ROrthographyInfoFontSize,
                        };
                        if (runReadingOrtho.Text != "()")
                        {
                            textBlockReadings.Inlines.Add(" ");
                            textBlockReadings.Inlines.Add(runReadingOrtho);
                        }
                    }
                }

                if (index != readings.Count - 1)
                {
                    textBlockReadings.Inlines.Add(", ");
                }
            }

            return textBlockReadings;
        }

        public static TextBlock MakeTextBlockAlternativeSpellings(List<string> alternativeSpellings,
            List<string> aOrthographyInfoList)
        {
            var textBlockAlternativeSpellings = new TextBlock
            {
                Name = LookupResult.AlternativeSpellings.ToString(),
                Text = "",
                Tag = string.Join(", ", alternativeSpellings), // for mining
                Foreground = ConfigManager.AlternativeSpellingsColor,
                FontSize = ConfigManager.AlternativeSpellingsFontSize,
                Margin = new Thickness(5, 0, 0, 0),
            };

            if (alternativeSpellings.Count == 0) return textBlockAlternativeSpellings;

            textBlockAlternativeSpellings.Inlines.Add("(");

            for (var index = 0; index < alternativeSpellings.Count; index++)
            {
                var runAlt = new Run(alternativeSpellings[index])
                {
                    Foreground = ConfigManager.AlternativeSpellingsColor,
                    FontSize = ConfigManager.AlternativeSpellingsFontSize,
                };
                textBlockAlternativeSpellings.Inlines.Add(runAlt);

                if (index < aOrthographyInfoList.Count)
                {
                    var runAltOrtho = new Run("(" + aOrthographyInfoList[index] + ")")
                    {
                        Foreground = ConfigManager.AOrthographyInfoColor,
                        FontSize = ConfigManager.AOrthographyInfoFontSize,
                    };
                    if (runAltOrtho.Text != "()")
                    {
                        textBlockAlternativeSpellings.Inlines.Add(" ");
                        textBlockAlternativeSpellings.Inlines.Add(runAltOrtho);
                    }
                }

                if (index != alternativeSpellings.Count - 1)
                {
                    textBlockAlternativeSpellings.Inlines.Add(", ");
                }
            }

            textBlockAlternativeSpellings.Inlines.Add(")");

            return textBlockAlternativeSpellings;
        }

        public static string FindSentence(string text, int position)
        {
            List<string> japanesePunctuation = new() { "。", "！", "？", "…", ".", "\n", };

            Dictionary<string, string> japaneseParentheses = new()
            {
                { "「", "」" },
                { "『", "』" },
                { "（", "）" },
            };

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