using System.Buffers;
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
    public static partial Regex JapaneseRegex { get; }

    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included in the regex.
    [GeneratedRegex(@"[\u3040-\u31FF]", RegexOptions.CultureInvariant)]

    private static partial Regex KanaRegex { get; }

    private static readonly FrozenDictionary<char, string> s_katakanaToHiraganaDict = new KeyValuePair<char, string>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        new('ア', "あ"), new('イ', "い"), new('ウ', "う"), new('エ', "え"), new('オ', "お"),
        new('カ', "か"), new('キ', "き"), new('ク', "く"), new('ケ', "け"), new('コ', "こ"),
        new('サ', "さ"), new('シ', "し"), new('ス', "す"), new('セ', "せ"), new('ソ', "そ"),
        new('タ', "た"), new('チ', "ち"), new('ツ', "つ"), new('テ', "て"), new('ト', "と"),
        new('ナ', "な"), new('ニ', "に"), new('ヌ', "ぬ"), new('ネ', "ね"), new('ノ', "の"),
        new('ハ', "は"), new('ヒ', "ひ"), new('フ', "ふ"), new('ヘ', "へ"), new('ホ', "ほ"),
        new('マ', "ま"), new('ミ', "み"), new('ム', "む"), new('メ', "め"), new('モ', "も"),
        new('ラ', "ら"), new('リ', "り"), new('ル', "る"), new('レ', "れ"), new('ロ', "ろ"),

        new('ガ', "が"), new('ギ', "ぎ"), new('グ', "ぐ"), new('ゲ', "げ"), new('ゴ', "ご"),
        new('ザ', "ざ"), new('ジ', "じ"), new('ズ', "ず"), new('ゼ', "ぜ"), new('ゾ', "ぞ"),
        new('ダ', "だ"), new('ヂ', "ぢ"), new('ヅ', "づ"), new('デ', "で"), new('ド', "ど"),
        new('バ', "ば"), new('ビ', "び"), new('ブ', "ぶ"), new('ベ', "べ"), new('ボ', "ぼ"),
        new('パ', "ぱ"), new('ピ', "ぴ"), new('プ', "ぷ"), new('ペ', "ぺ"), new('ポ', "ぽ"),

        new('ワ', "わ"), new('ヲ', "を"),
        new('ヤ', "や"), new('ユ', "ゆ"), new('ヨ', "よ"),
        new('ン', "ん"),

        new('ァ', "ぁ"), new('ィ', "ぃ"), new('ゥ', "ぅ"), new('ェ', "ぇ"), new('ォ', "ぉ"),
        new('ャ', "ゃ"), new('ュ', "ゅ"), new('ョ', "ょ"),

        new('ヮ', "ゎ"),

        new('ヴ', "ゔ"), new('ヽ', "ゝ"), new('ヾ', "ゞ"), new('ッ', "っ"),

        new('ヸ', "ゐ゙"), new('ヹ', "ゑ゙"), new('ヺ', "を゙")
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, char> s_kanaFinalVowelDict = new KeyValuePair<string, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        //Hiragana
        new("あ", 'あ'), new("か", 'あ'), new("さ", 'あ'), new("た", 'あ'), new("な", 'あ'), new("は", 'あ'),
        new("ま", 'あ'), new("ら", 'あ'), new("が", 'あ'), new("ざ", 'あ'), new("だ", 'あ'), new("ば", 'あ'),
        new("ぱ", 'あ'), new("わ", 'あ'), new("や", 'あ'), new("ぁ", 'あ'), new("ゃ", 'あ'), new("ゕ", 'あ'),
        new("ゎ", 'あ'),

        new("い", 'い'), new("き", 'い'), new("し", 'い'), new("ち", 'い'), new("に", 'い'), new("ひ", 'い'),
        new("み", 'い'), new("り", 'い'), new("ぎ", 'い'), new("じ", 'い'), new("ぢ", 'い'), new("び", 'い'),
        new("ぴ", 'い'), new("ぃ", 'い'), new("ゐ", 'い'), new("ゐ゙", 'い'),

        new("う", 'う'), new("く", 'う'), new("す", 'う'), new("つ", 'う'), new("ぬ", 'う'), new("ふ", 'う'),
        new("む", 'う'), new("る", 'う'), new("ぐ", 'う'), new("ず", 'う'), new("づ", 'う'), new("ぶ", 'う'),
        new("ぷ", 'う'), new("ゆ", 'う'), new("ぅ", 'う'), new("ゅ", 'う'), new("ゔ", 'う'),

        new("え", 'え'), new("け", 'え'), new("せ", 'え'), new("て", 'え'), new("ね", 'え'), new("へ", 'え'),
        new("め", 'え'), new("れ", 'え'), new("げ", 'え'), new("ぜ", 'え'), new("で", 'え'), new("べ", 'え'),
        new("ぺ", 'え'), new("ぇ", 'え'), new("ゖ", 'え'), new("ゑ", 'え'), new("ゑ゙", 'え'),

        new("お", 'お'), new("こ", 'お'), new("そ", 'お'), new("と", 'お'), new("の", 'お'), new("ほ", 'お'),
        new("も", 'お'), new("ろ", 'お'), new("ご", 'お'), new("ぞ", 'お'), new("ど", 'お'), new("ぼ", 'お'),
        new("ぽ", 'お'), new("を", 'お'), new("よ", 'お'), new("ぉ", 'お'), new("ょ", 'お'), new("を゙", 'お')
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

    private static readonly FrozenDictionary<char, char> s_leftToRightBracketDict = new KeyValuePair<char, char>[]
    {
        // ReSharper disable BadExpressionBracesLineBreaks
        new('「', '」'),
        new('『', '』'),
        new('【', '】'),
        new('《', '》'),
        new('〔', '〕'),
        new('（', '）'),
        new('［', '］'),
        new('〈', '〉'),
        new('｛', '｝'),
        new('＜', '＞'),
        new('〝', '〟'),
        new('＂', '＂'),
        new('＇', '＇'),
        new('｢', '｣'),
        new('⟨', '⟩'),
        new('(', ')'),
        new('[', ']'),
        new('{', '}'),
        new('︗', '︘'),
        new('﹁', '﹂'),
        new('﹃', '﹄'),
        new('︵', '︶'),
        new('﹇', '﹈'),
        new('︷', '︸'),
        new('︹', '︺'),
        new('︻', '︼'),
        new('︽', '︾'),
        new('︿', '﹀')
        // ReSharper restore BadExpressionBracesLineBreaks
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<char, char> s_rightToLeftBracketDict = s_leftToRightBracketDict.ToFrozenDictionary(static kvp => kvp.Value, static kvp => kvp.Key);

    private static readonly SearchValues<char> s_expressionTerminatingCharacters = SearchValues.Create([.. s_leftToRightBracketDict.Keys.Union(s_leftToRightBracketDict.Values).Union(s_sentenceTerminatingCharacters)]);

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

    internal static string KatakanaToHiragana(string text)
    {
        string normalizedText = text;
        if (!normalizedText.IsNormalized(NormalizationForm.FormKC))
        {
            // Normalizes ＯＬ to OL, fullwidth space to halfwidth space, ｶﾞ to が, ﾜ to わ, ㍿ to 株式会社 etc.
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
                string unicodeText = unicodeTextList[i];
                for (int j = 0; j < stringBuilderCount; j++)
                {
                    _ = stringBuilders[j].Append(unicodeText);
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

    internal static int GetCombinedFormLength(ReadOnlySpan<char> text)
    {
        int length = 0;
        for (int i = 0; i < text.Length; i++)
        {
            ++length;
            if (i + 1 < text.Length
                && s_smallCombiningKanaSet.Contains(text[i + 1]))
            {
                ++i;
            }
        }

        return length;
    }

    internal static bool IsKatakana(char character)
    {
        return s_katakanaToHiraganaDict.ContainsKey(character);
    }

    public static int FindExpressionBoundary(ReadOnlySpan<char> text, int position)
    {
        int endPosition = text[position..].IndexOfAny(s_expressionTerminatingCharacters);
        return endPosition < 0 ? text.Length : endPosition + position + 1;
    }

    internal static string FindSentence(ReadOnlySpan<char> text, int position)
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

        ReadOnlySpan<char> sentence = startPosition <= endPosition
            ? text[startPosition..(endPosition + 1)].Trim()
            : "";

        if (sentence.Length <= 1)
        {
            return sentence.ToString();
        }

        if (s_rightToLeftBracketDict.ContainsKey(sentence[0]))
        {
            sentence = sentence[1..];
        }

        if (sentence.Length > 0 && s_leftToRightBracketDict.ContainsKey(sentence[^1]))
        {
            sentence = sentence[..^1];
        }

        if (sentence.Length > 0)
        {
            if (s_leftToRightBracketDict.TryGetValue(sentence[0], out char rightBracket))
            {
                if (sentence[^1] == rightBracket)
                {
                    sentence = sentence[1..^1];
                }
                else if (!sentence.Contains(rightBracket))
                {
                    sentence = sentence[1..];
                }
                else
                {
                    char sentenceFirstChar = sentence[0];
                    int numberOfLeftBrackets = sentence.Count(sentenceFirstChar);
                    int numberOfRightBrackets = sentence.Count(rightBracket);

                    if (numberOfLeftBrackets == numberOfRightBrackets + 1)
                    {
                        sentence = sentence[1..];
                    }
                }
            }

            else if (s_rightToLeftBracketDict.TryGetValue(sentence[^1], out char leftBracket))
            {
                if (!sentence.Contains(leftBracket))
                {
                    sentence = sentence[..^1];
                }
                else
                {
                    int numberOfLeftBrackets = sentence.Count(leftBracket);
                    int numberOfRightBrackets = sentence.Count(sentence[^1]);

                    if (numberOfRightBrackets == numberOfLeftBrackets + 1)
                    {
                        sentence = sentence[..^1];
                    }
                }
            }
        }

        return sentence.ToString();
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

    internal static string GetPrimarySpellingAndReadingMapping(string primarySpelling, string reading)
    {
        if (!KanaRegex.IsMatch(primarySpelling))
        {
            return $"{primarySpelling}[{reading}]";
        }

        List<string> primarySpellingSegments = new(primarySpelling.Length);
        bool wasKana = true;
        foreach (Rune rune in primarySpelling.EnumerateRunes())
        {
            string runeAsString = rune.ToString();
            bool isKana = KanaRegex.IsMatch(runeAsString);
            if (primarySpellingSegments.Count is 0 || wasKana != isKana)
            {
                wasKana = isKana;
                primarySpellingSegments.Add(runeAsString);
            }
            else
            {
                primarySpellingSegments[^1] += runeAsString;
            }
        }

        string? result = GetPrimarySpellingAndReadingMapping(primarySpellingSegments, reading);
        return result ?? $"{primarySpelling}[{reading}]";
    }

    private static string? GetPrimarySpellingAndReadingMapping(List<string> primarySpellingSegments, string reading)
    {
        StringBuilder stringBuilder = new();

        bool firstSegmentIsKana = KanaRegex.IsMatch(primarySpellingSegments[0]);
        int currentReadingPosition = firstSegmentIsKana ? 0 : 1;
        for (int i = currentReadingPosition; i < primarySpellingSegments.Count; i += 2)
        {
            string segment = primarySpellingSegments[i];
            int searchLength = reading.Length - currentReadingPosition - primarySpellingSegments.Count + i + 1;
            if (searchLength < 0)
            {
                searchLength = reading.Length - currentReadingPosition;
            }

            List<int> indexes = reading.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segment);
            bool hasKatakana = false;
            int index = -1;
            if (indexes.Count is 0)
            {
                string readingInHiragana = KatakanaToHiragana(reading);
                if (readingInHiragana.Length != reading.Length)
                {
                    return null;
                }
                string segmentInHiragana = KatakanaToHiragana(segment);
                if (segmentInHiragana.Length != segment.Length)
                {
                    return null;
                }

                indexes = readingInHiragana.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segmentInHiragana);
                hasKatakana = true;
            }

            if (indexes.Count is 0)
            {
                return null;
            }

            if (indexes.Count == 1)
            {
                index = indexes[0];
            }
            else
            {
                for (int j = 0; j < indexes.Count; j++)
                {
                    int currentIndex = indexes[j];
                    if (currentIndex is 0 && i is 0)
                    {
                        index = 0;
                        break;
                    }

                    if (i + 1 == primarySpellingSegments.Count)
                    {
                        if (currentIndex + segment.Length == reading.Length)
                        {
                            index = currentIndex;
                            break;
                        }
                    }
                    else if (i + 2 < primarySpellingSegments.Count)
                    {
                        bool unambiguous = IsPrimarySpellingAndReadingMappingUnambiguous(primarySpellingSegments[(i + 2)..], reading[(currentIndex + segment.Length + 1)..]);
                        if (unambiguous)
                        {
                            if (index >= 0)
                            {
                                return null;
                            }

                            index = currentIndex;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            if (index < 0)
            {
                return null;
            }

            if (i > 0)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{primarySpellingSegments[i - 1]}[{reading[(currentReadingPosition - 1)..index]}]");
            }

            if (hasKatakana)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{segment}[{reading[index..(index + segment.Length)]}]");
            }
            else
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{segment} ");
            }

            currentReadingPosition = index + segment.Length;

            if (i + 2 == primarySpellingSegments.Count)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{primarySpellingSegments[i + 1]}[{reading[currentReadingPosition..]}]");
            }

            ++currentReadingPosition;
        }

        return stringBuilder.ToString();
    }

    private static bool IsPrimarySpellingAndReadingMappingUnambiguous(List<string> primarySpellingSegments, string reading)
    {
        bool firstSegmentIsKana = KanaRegex.IsMatch(primarySpellingSegments[0]);
        int currentReadingPosition = firstSegmentIsKana ? 0 : 1;
        for (int i = currentReadingPosition; i < primarySpellingSegments.Count; i += 2)
        {
            string segment = primarySpellingSegments[i];
            int searchLength = reading.Length - currentReadingPosition - primarySpellingSegments.Count + i + 1;
            if (searchLength < 0)
            {
                searchLength = reading.Length - currentReadingPosition;
            }

            List<int> indexes = reading.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segment);
            int index = -1;
            if (indexes.Count is 0)
            {
                string readingInHiragana = KatakanaToHiragana(reading);
                if (readingInHiragana.Length != reading.Length)
                {
                    return false;
                }
                string segmentInHiragana = KatakanaToHiragana(segment);
                if (segmentInHiragana.Length != segment.Length)
                {
                    return false;
                }

                indexes = readingInHiragana.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segmentInHiragana);
            }

            if (indexes.Count is 0)
            {
                return false;
            }

            if (indexes.Count == 1)
            {
                index = indexes[0];
            }
            else
            {
                for (int j = 0; j < indexes.Count; j++)
                {
                    int currentIndex = indexes[j];
                    if (currentIndex is 0 && i is 0)
                    {
                        index = 0;
                        break;
                    }

                    if (i + 1 == primarySpellingSegments.Count)
                    {
                        if (currentIndex + segment.Length == reading.Length)
                        {
                            index = currentIndex;
                            break;
                        }
                    }
                    else if (i + 2 < primarySpellingSegments.Count)
                    {
                        bool unambiguous = IsPrimarySpellingAndReadingMappingUnambiguous(primarySpellingSegments[(i + 2)..], reading[(currentIndex + segment.Length + 1)..]);
                        if (unambiguous)
                        {
                            if (index >= 0)
                            {
                                return false;
                            }

                            index = currentIndex;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            currentReadingPosition = index + segment.Length + 1;
        }

        return true;
    }
}
