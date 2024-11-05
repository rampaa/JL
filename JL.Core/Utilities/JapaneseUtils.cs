using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JL.Core.Utilities;

public static partial class JapaneseUtils
{
    // Matches the following Unicode ranges:
    // × (\u00D7)
    // General Punctuation (2000-206F): ‥, …, •, ※
    // Geometric Shapes (25A0-U+25FF): ◦, ◎, ○, △, ◉
    // CJK Radicals Supplement (2E80–2EFF)
    // Kangxi Radicals (2F00–2FDF)
    // Ideographic Description Characters (2FF0–2FFF)
    // CJK Symbols and Punctuation (3000–303F)
    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Kanbun (3190–319F)
    // CJK Strokes (31C0–31EF)
    // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included in the regex.
    // Enclosed CJK Letters and Months (3200–32FF)
    // CJK Compatibility (3300–33FF)
    // CJK Unified Ideographs Extension A (3400–4DBF)
    // CJK Unified Ideographs (4E00–9FFF)
    // CJK Compatibility Ideographs (F900–FAFF)
    // CJK Compatibility Forms (FE30–FE4F)
    // Halfwidth and Fullwidth Forms (FF00–FFEF)
    // Ideographic Symbols and Punctuation (16FE0-16FFF): It does not contain any Japanese characters, so it's not included in the regex.
    // Kana Extended-B (1AFF0-1AFFF): The range does not contain any Japanese characters; it only includes Taiwanese kana, so it's not included in the regex.
    // Kana Supplement (1B000-1B0FF)
    // Kana Extended-A (1B100-1B12F)
    // Small Kana Extension (1B130-1B16F)
    // Enclosed Ideographic Supplement (1F200-1F2FF)
    // CJK Unified Ideographs Extension B (20000–2A6DF)
    // CJK Unified Ideographs Extension C (2A700–2B73F)
    // CJK Unified Ideographs Extension D (2B740–2B81F)
    // CJK Unified Ideographs Extension E (2B820–2CEAF)
    // CJK Unified Ideographs Extension F (2CEB0–2EBEF)
    // CJK Unified Ideographs Extension I (2EBF0–2EE5F): It's a Chinese-only range, so it's not included in the regex.
    // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
    // CJK Unified Ideographs Extension G (30000–3134F)
    // CJK Unified Ideographs Extension H (31350–323AF)
    [GeneratedRegex(@"[\u00D7\u2000-\u206F\u25A0-\u25FF\u2E80-\u319F\u31C0-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF\uFE30-\uFE4F\uFF00-\uFFEF]|\uD82C[\uDC00-\uDD6F]|\uD83C[\uDE00-\uDEFF]|\uD840[\uDC00-\uDFFF]|[\uD841-\uD868][\uDC00-\uDFFF]|\uD869[\uDC00-\uDEDF]|\uD869[\uDF00-\uDFFF]|[\uD86A-\uD87A][\uDC00-\uDFFF]|\uD87B[\uDC00-\uDE5F]|\uD87E[\uDC00-\uDE1F]|\uD880[\uDC00-\uDFFF]|[\uD881-\uD887][\uDC00-\uDFFF]|\uD888[\uDC00-\uDFAF]", RegexOptions.CultureInvariant)]
    public static partial Regex JapaneseRegex();

    private static readonly FrozenDictionary<char, string> s_katakanaToHiraganaDict = new Dictionary<char, string>(87)
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        { 'ア', "あ" }, { 'イ', "い" }, { 'ウ', "う" }, { 'エ', "え" }, { 'オ', "お" },
        { 'カ', "か" }, { 'キ', "き" }, { 'ク', "く" }, { 'ケ', "け" }, { 'コ', "こ" },
        { 'サ', "さ" }, { 'シ', "し" }, { 'ス', "す" }, { 'セ', "せ" }, { 'ソ', "そ" },
        { 'タ', "た" }, { 'チ', "ち" }, { 'ツ', "つ" }, { 'テ', "て" }, { 'ト', "と" },
        { 'ナ', "な" }, { 'ニ', "に" }, { 'ヌ', "ぬ" }, { 'ネ', "ね" }, { 'ノ', "の" },
        { 'ハ', "は" }, { 'ヒ', "ひ" }, { 'フ', "ふ" }, { 'ヘ', "へ" }, { 'ホ', "ほ" },
        { 'マ', "ま" }, { 'ミ', "み" }, { 'ム', "む" }, { 'メ', "め" }, { 'モ', "も" },
        { 'ラ', "ら" }, { 'リ', "り" }, { 'ル', "る" }, { 'レ', "れ" }, { 'ロ', "ろ" },

        { 'ガ', "が" }, { 'ギ', "ぎ" }, { 'グ', "ぐ" }, { 'ゲ', "げ" }, { 'ゴ', "ご" },
        { 'ザ', "ざ" }, { 'ジ', "じ" }, { 'ズ', "ず" }, { 'ゼ', "ぜ" }, { 'ゾ', "ぞ" },
        { 'ダ', "だ" }, { 'ヂ', "ぢ" }, { 'ヅ', "づ" }, { 'デ', "で" }, { 'ド', "ど" },
        { 'バ', "ば" }, { 'ビ', "び" }, { 'ブ', "ぶ" }, { 'ベ', "べ" }, { 'ボ', "ぼ" },
        { 'パ', "ぱ" }, { 'ピ', "ぴ" }, { 'プ', "ぷ" }, { 'ペ', "ぺ" }, { 'ポ', "ぽ" },

        { 'ワ', "わ" }, { 'ヲ', "を" },
        { 'ヤ', "や" }, { 'ユ', "ゆ" }, { 'ヨ', "よ" },
        { 'ン', "ん" },

        { 'ァ', "ぁ" }, { 'ィ', "ぃ" }, { 'ゥ', "ぅ" }, { 'ェ', "ぇ" }, { 'ォ', "ぉ" },
        { 'ャ', "ゃ" }, { 'ュ', "ゅ" }, { 'ョ', "ょ" },

        { 'ヮ', "ゎ" },

        { 'ヴ', "ゔ" }, { 'ヽ', "ゝ" }, { 'ヾ', "ゞ" }, { 'ッ', "っ" },

        { 'ヸ', "ゐ゙" }, { 'ヹ', "ゑ゙" }, { 'ヺ', "を゙" }
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, char> s_kanaFinalVowelDict = new Dictionary<string, char>(87, StringComparer.Ordinal)
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        //Hiragana
        { "あ", 'あ' }, { "か", 'あ' }, { "さ", 'あ' }, { "た", 'あ' }, { "な", 'あ' }, { "は", 'あ' },
        { "ま", 'あ' }, { "ら", 'あ' }, { "が", 'あ' }, { "ざ", 'あ' }, { "だ", 'あ' }, { "ば", 'あ' },
        { "ぱ", 'あ' }, { "わ", 'あ' }, { "や", 'あ' }, { "ぁ", 'あ' }, { "ゃ", 'あ' }, { "ゕ", 'あ' },
        { "ゎ", 'あ' },

        { "い", 'い' }, { "き", 'い' }, { "し", 'い' }, { "ち", 'い' }, { "に", 'い' }, { "ひ", 'い' },
        { "み", 'い' }, { "り", 'い' }, { "ぎ", 'い' }, { "じ", 'い' }, { "ぢ", 'い' }, { "び", 'い' },
        { "ぴ", 'い' }, { "ぃ", 'い' }, { "ゐ", 'い' }, { "ゐ゙", 'い' },

        { "う", 'う' }, { "く", 'う' }, { "す", 'う' }, { "つ", 'う' }, { "ぬ", 'う' }, { "ふ", 'う' },
        { "む", 'う' }, { "る", 'う' }, { "ぐ", 'う' }, { "ず", 'う' }, { "づ", 'う' }, { "ぶ", 'う' },
        { "ぷ", 'う' }, { "ゆ", 'う' }, { "ぅ", 'う' }, { "ゅ", 'う' }, { "ゔ", 'う' },

        { "え", 'え' }, { "け", 'え' }, { "せ", 'え' }, { "て", 'え' }, { "ね", 'え' }, { "へ", 'え' },
        { "め", 'え' }, { "れ", 'え' }, { "げ", 'え' }, { "ぜ", 'え' }, { "で", 'え' }, { "べ", 'え' },
        { "ぺ", 'え' }, { "ぇ", 'え' }, { "ゖ", 'え' }, { "ゑ", 'え' }, { "ゑ゙", 'え' },

        { "お", 'お' }, { "こ", 'お' }, { "そ", 'お' }, { "と", 'お' }, { "の", 'お' }, { "ほ", 'お' },
        { "も", 'お' }, { "ろ", 'お' }, { "ご", 'お' }, { "ぞ", 'お' }, { "ど", 'お' }, { "ぼ", 'お' },
        { "ぽ", 'お' }, { "を", 'お' }, { "よ", 'お' }, { "ぉ", 'お' }, { "ょ", 'お' }, { "を゙", 'お' }
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenSet<char> s_smallCombiningKanaSet = FrozenSet.ToFrozenSet(
    [
        #pragma warning disable format
        'ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ヮ',
        'ャ', 'ュ', 'ョ',

        'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゎ',
        'ゃ', 'ゅ', 'ょ'
        #pragma warning restore format
    ]);

    private static readonly char[] s_sentenceTerminatingCharacters =
    [
        '。',
        '！',
        '？',
        '…',
        '‥',
        '︒',
        '.',
        '!',
        '?',
        '︙',
        '︰',
        '\n'
    ];

    private static readonly FrozenDictionary<char, char> s_leftToRightBracketDict = new Dictionary<char, char>(28)
    {
        // ReSharper disable BadExpressionBracesLineBreaks
        { '「', '」' },
        { '『', '』' },
        { '【', '】' },
        { '《', '》' },
        { '〔', '〕' },
        { '（', '）' },
        { '［', '］' },
        { '〈', '〉' },
        { '｛', '｝' },
        { '＜', '＞' },
        { '〝', '〟' },
        { '＂', '＂' },
        { '＇', '＇' },
        { '｢', '｣' },
        { '⟨', '⟩' },
        { '(', ')' },
        { '[', ']' },
        { '{', '}' },
        { '︗', '︘' },
        { '﹁', '﹂' },
        { '﹃', '﹄' },
        { '︵', '︶' },
        { '﹇', '﹈' },
        { '︷', '︸' },
        { '︹', '︺' },
        { '︻', '︼' },
        { '︽', '︾' },
        { '︿', '﹀' }
        // ReSharper restore BadExpressionBracesLineBreaks
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<char, char> s_rightToLeftBracketDict = s_leftToRightBracketDict.ToFrozenDictionary(static kvp => kvp.Value, static kvp => kvp.Key);

    private static readonly FrozenSet<char> s_expressionTerminatingCharacters = s_leftToRightBracketDict.Keys.Union(s_leftToRightBracketDict.Values).Union(s_sentenceTerminatingCharacters).ToFrozenSet();

    private static int FirstKatakanaIndex(ReadOnlySpan<char> text)
    {
        int textLength = text.Length;
        for (int i = 0; i < textLength; i++)
        {
            if (s_katakanaToHiraganaDict.ContainsKey(text[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public static string KatakanaToHiragana(string text)
    {
        string normalizedText = text;
        if (!normalizedText.IsNormalized(NormalizationForm.FormKC))
        {
            // Normalizes ＯＬ to OL, ｶﾞ to が, ﾜ to わ, ㍿ to 株式会社 etc.
            normalizedText = normalizedText.Normalize(NormalizationForm.FormKC);
        }

        // Normalizes vs to VS, xxx to XXX, h to H etc.
        normalizedText = normalizedText.ToUpperInvariant();

        int firstKatakanaIndex = FirstKatakanaIndex(normalizedText);
        if (firstKatakanaIndex < 0)
        {
            return normalizedText;
        }

        int normalizedTextLength = normalizedText.Length;
        StringBuilder textInHiragana = new(normalizedText[..firstKatakanaIndex], normalizedTextLength);
        for (int i = firstKatakanaIndex; i < normalizedTextLength; i++)
        {
            char character = normalizedText[i];
            _ = textInHiragana.Append(s_katakanaToHiraganaDict.TryGetValue(character, out string? hiraganaStr)
                ? hiraganaStr
                : character);
        }

        return textInHiragana.ToString();
    }

    internal static List<string> LongVowelMarkToKana(ReadOnlySpan<char> text)
    {
        List<string> unicodeTextList = text.ListUnicodeCharacters();

        List<StringBuilder> stringBuilders = new(4)
        {
            new StringBuilder(unicodeTextList[0], unicodeTextList.Count)
        };

        int unicodeTextListCount = unicodeTextList.Count;
        for (int i = 1; i < unicodeTextListCount; i++)
        {
            if (unicodeTextList[i] is "ー" && s_kanaFinalVowelDict.TryGetValue(unicodeTextList[i - 1], out char vowel))
            {
                if (vowel is not 'お' and not 'え')
                {
                    int stringBuilderCount = stringBuilders.Count;
                    for (int j = 0; j < stringBuilderCount; j++)
                    {
                        _ = stringBuilders[j].Append(vowel);
                    }
                }

                else
                {
                    char alternativeVowel = vowel switch
                    {
                        'お' => 'う',
                        'え' => 'い',
                        _ => ' '
                    };

                    int listSize = stringBuilders.Count;
                    for (int j = 0; j < listSize; j++)
                    {
                        stringBuilders.Add(new StringBuilder(stringBuilders[j].ToString(), unicodeTextList.Count));
                    }

                    listSize = stringBuilders.Count;
                    for (int j = 0; j < listSize; j++)
                    {
                        _ = stringBuilders[j].Append(j < listSize / 2 ? vowel : alternativeVowel);
                    }
                }
            }

            else
            {
                int stringBuilderCount = stringBuilders.Count;
                for (int j = 0; j < stringBuilderCount; j++)
                {
                    _ = stringBuilders[j].Append(unicodeTextList[i]);
                }
            }
        }

        return stringBuilders.ConvertAll(static sb => sb.ToString());
    }

    public static List<string> CreateCombinedForm(ReadOnlySpan<char> text)
    {
        List<string> combinedForm = new(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length
                && s_smallCombiningKanaSet.Contains(text[i + 1]))
            {
                combinedForm.Add(string.Create(CultureInfo.InvariantCulture, $"{text[i]}{text[i + 1]}"));
                ++i;
            }

            else
            {
                combinedForm.Add(text[i].ToString());
            }
        }

        return combinedForm;
    }

    internal static bool IsKatakana(char character)
    {
        return s_katakanaToHiraganaDict.ContainsKey(character);
    }

    public static int FindExpressionBoundary(ReadOnlySpan<char> text, int position)
    {
        int endPosition = text.Length;
        for (int i = position; i < text.Length; i++)
        {
            char c = text[i];
            if (s_expressionTerminatingCharacters.Contains(c) || char.IsWhiteSpace(c))
            {
                endPosition = i + 1;
                break;
            }
        }

        return endPosition;
    }

    internal static string FindSentence(string text, int position)
    {
        int startPosition = -1;
        int endPosition = -1;

        for (int i = 0; i < s_sentenceTerminatingCharacters.Length; i++)
        {
            char terminatingCharacter = s_sentenceTerminatingCharacters[i];

            int tempIndex = text.LastIndexOf(terminatingCharacter, position);

            if (tempIndex > startPosition)
            {
                startPosition = tempIndex;
            }

            tempIndex = text.IndexOf(terminatingCharacter, position);

            if (tempIndex >= 0 && (endPosition < 0 || tempIndex < endPosition))
            {
                endPosition = tempIndex;
            }
        }

        ++startPosition;

        if (endPosition < 0)
        {
            endPosition = text.Length - 1;
        }

        string sentence = startPosition <= endPosition
            ? text[startPosition..(endPosition + 1)].Trim()
            : "";

        if (sentence.Length <= 1)
        {
            return sentence;
        }

        if (s_rightToLeftBracketDict.ContainsKey(sentence[0]))
        {
            sentence = sentence[1..];
        }

        if (s_leftToRightBracketDict.ContainsKey(sentence.LastOrDefault()))
        {
            sentence = sentence[..^1];
        }

        if (s_leftToRightBracketDict.TryGetValue(sentence.FirstOrDefault(), out char rightBracket))
        {
            if (sentence[^1] == rightBracket)
            {
                sentence = sentence[1..^1];
            }
            else if (!sentence.Contains(rightBracket, StringComparison.Ordinal))
            {
                sentence = sentence[1..];
            }
            else
            {
                int numberOfLeftBrackets = sentence.Count(p => p == sentence[0]);
                int numberOfRightBrackets = sentence.Count(p => p == rightBracket);

                if (numberOfLeftBrackets == numberOfRightBrackets + 1)
                {
                    sentence = sentence[1..];
                }
            }
        }

        else if (s_rightToLeftBracketDict.TryGetValue(sentence.LastOrDefault(), out char leftBracket))
        {
            if (!sentence.Contains(leftBracket, StringComparison.Ordinal))
            {
                sentence = sentence[..^1];
            }
            else
            {
                int numberOfLeftBrackets = sentence.Count(p => p == leftBracket);
                int numberOfRightBrackets = sentence.Count(p => p == sentence[^1]);

                if (numberOfRightBrackets == numberOfLeftBrackets + 1)
                {
                    sentence = sentence[..^1];
                }
            }
        }

        return sentence;
    }

    private static int FirstPunctuationIndex(ReadOnlySpan<char> text)
    {
        int charIndex = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetterOrDigit(rune))
            {
                return charIndex;
            }

            charIndex += rune.Utf16SequenceLength;
        }

        return -1;
    }

    public static string RemovePunctuation(string text)
    {
        int index = FirstPunctuationIndex(text);
        if (index < 0)
        {
            return text;
        }

        if (index == text.Length - 1)
        {
            return text[..^1];
        }

        StringBuilder sb = new(text[..index], text.Length - 1);
        foreach (Rune rune in text.AsSpan(index + 1).EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                _ = sb.Append(rune);
            }
        }

        return sb.ToString();
    }
}
