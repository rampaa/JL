using System.Text;
using JL.Core.Anki;

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

        // todo move to core
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

            for (int i = 0; i < japanesePunctuation.Count; i++)
            {
                string punctuation = japanesePunctuation[i];

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

        public static async Task GetAndPlayAudioFromJpod101(string foundSpelling, string reading, float volume)
        {
            Utils.Logger.Information("Attempting to play audio from jpod101: " + foundSpelling + " " + reading);

            if (string.IsNullOrEmpty(reading))
                reading = foundSpelling;

            byte[] sound = await AnkiConnect.GetAudioFromJpod101(foundSpelling, reading).ConfigureAwait(false);
            if (sound != null)
            {
                if (Utils.GetMd5String(sound) == "7e2c2f954ef6051373ba916f000168dc") // jpod101 no audio clip
                {
                    // TODO sound = shortErrorSound
                    return;
                }

                Storage.Frontend.PlayAudio(sound, volume);
            }
        }
    }
}
