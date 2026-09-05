using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JL.Core.Config;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Utilities;

public static class TextUtils
{
    internal static readonly Encoding s_utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private const char HighSurrogateStart = '\uD800';
    private const char ReplacementCharacter = '\uFFFD';
    private const char Noncharacter = '\uFFFE';
    private static readonly SearchValues<char> s_digits = SearchValues.Create('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
    private static readonly SearchValues<char> s_digitsAndGroupSeparator = SearchValues.Create('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ',');

    // See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Globalization/Normalization.Icu.cs
    // Modified from private static bool HasInvalidUnicodeSequence(ReadOnlySpan<char> s)
    private static int FirstInvalidUnicodeSequenceIndex(ReadOnlySpan<char> text)
    {
        for (int i = text.IndexOfAnyInRange(HighSurrogateStart, Noncharacter); (uint)i < (uint)text.Length; i++)
        {
            ref readonly char c = ref text[i];
            if (c < HighSurrogateStart)
            {
                continue;
            }

            if (c is Noncharacter || char.IsLowSurrogate(c))
            {
                return i;
            }

            if (char.IsHighSurrogate(c))
            {
                if ((uint)(i + 1) >= (uint)text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    return i;
                }

                ++i;
            }
        }

        return -1;
    }

    private static string RemoveInvalidUnicodeSequences(ReadOnlySpan<char> text, int index, bool keepLength)
    {
        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get().Append(text[..index]);
        for (int i = index + 1; i < text.Length; i++)
        {
            char c = text[i];
            if (c < HighSurrogateStart)
            {
                _ = sb.Append(c);
            }
            else if (char.IsHighSurrogate(c))
            {
                if ((uint)(i + 1) < (uint)text.Length)
                {
                    char nextChar = text[i + 1];
                    if (char.IsLowSurrogate(nextChar))
                    {
                        _ = sb.Append(c).Append(nextChar);
                        ++i;
                    }
                    else if (keepLength)
                    {
                        _ = sb.Append(ReplacementCharacter);
                    }
                }
                else if (keepLength)
                {
                    _ = sb.Append(ReplacementCharacter);
                }
            }
            else if (c is Noncharacter || char.IsLowSurrogate(c))
            {
                if (keepLength)
                {
                    _ = sb.Append(ReplacementCharacter);
                }
            }
            else
            {
                _ = sb.Append(c);
            }
        }

        string validString = sb.ToString();
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return validString;
    }

    public static string SanitizeText(string text, bool keepLength)
    {
        int firstInvalidUnicodeCharIndex = FirstInvalidUnicodeSequenceIndex(text);
        if (firstInvalidUnicodeCharIndex >= 0)
        {
            text = RemoveInvalidUnicodeSequences(text, firstInvalidUnicodeCharIndex, keepLength);
        }

        if (!keepLength)
        {
            CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
            if (coreConfigManager.TextBoxTrimWhiteSpaceCharacters)
            {
                text = text.Trim();
            }

            if (coreConfigManager.TextBoxRemoveNewlines)
            {
                text = text.ReplaceLineEndings("");
            }
            else
            {
                text = text.ReplaceLineEndings("\n");
            }

            List<KeyValuePair<Regex, string>>? regexReplacements = RegexReplacerUtils.RegexReplacements;
            if (regexReplacements is not null)
            {
                foreach (ref readonly KeyValuePair<Regex, string> regexReplacementKeyValuePair in regexReplacements.AsReadOnlySpan())
                {
                    text = regexReplacementKeyValuePair.Key.Replace(text, regexReplacementKeyValuePair.Value);
                }
            }
        }

        return text;
    }

    public static bool StartsWithWhiteSpace(ReadOnlySpan<char> text)
    {
        char firstChar = text[0];
        return !char.IsHighSurrogate(firstChar)
            ? char.IsWhiteSpace(firstChar)
            : Rune.IsWhiteSpace(new Rune(firstChar, text[1]));
    }

    internal static int ExtractFirstInt(ReadOnlySpan<char> text)
    {
        int startIndex = text.IndexOfAny(s_digits);
        if (startIndex < 0)
        {
            return -1;
        }

        ReadOnlySpan<char> remainingSpan = text[startIndex..];
        int nonDigitIndex = remainingSpan.IndexOfAnyExcept(s_digitsAndGroupSeparator);

        ReadOnlySpan<char> numberSlice = nonDigitIndex < 0
            ? remainingSpan
            : remainingSpan[..nonDigitIndex];

        return int.TryParse(numberSlice, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int result)
            ? result
            : -1;
    }
}
