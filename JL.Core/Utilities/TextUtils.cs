using System.Text;

namespace JL.Core.Utilities;
public static class TextUtils
{
    private static int FirstInvalidUnicodeSequenceIndex(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c >= '\uD800')
            {
                if (c is '\uFFFE' || char.IsLowSurrogate(c))
                {
                    return i;
                }

                if (char.IsHighSurrogate(c))
                {
                    if ((i + 1) >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                    {
                        return i;
                    }

                    ++i;
                }
            }
        }

        return -1;
    }

    private static string RemoveInvalidUnicodeSequences(string text, int index)
    {
        StringBuilder sb = new(text[..index], text.Length - 1);

        for (int i = index + 1; i < text.Length; i++)
        {
            char c = text[i];

            if (c < '\uD800')
            {
                _ = sb.Append(c);
            }

            else if (c is not '\uFFFE' && !char.IsLowSurrogate(c))
            {
                if (char.IsHighSurrogate(c))
                {
                    if ((i + 1) < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        _ = sb.Append(c).Append(text[i + 1]);
                        ++i;
                    }
                }

                else
                {
                    _ = sb.Append(c);
                }
            }
        }

        return sb.ToString();
    }

    public static string SanitizeText(string text)
    {
        int firstInvalidUnicodeCharIndex = FirstInvalidUnicodeSequenceIndex(text);
        if (firstInvalidUnicodeCharIndex is not -1)
        {
            text = RemoveInvalidUnicodeSequences(text, firstInvalidUnicodeCharIndex);
        }

        if (CoreConfig.TextBoxTrimWhiteSpaceCharacters)
        {
            text = text.Trim();
        }

        if (CoreConfig.TextBoxRemoveNewlines)
        {
            text = text.ReplaceLineEndings("");
        }

        return text;
    }
}
