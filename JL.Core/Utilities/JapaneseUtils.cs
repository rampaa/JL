using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JL.Core.Utilities;

public static class JapaneseUtils
{
    // Matches the following Unicode ranges:
    // CJK Radicals Supplement (2E80–2EFF)
    // Kangxi Radicals (2F00–2FDF)
    // Ideographic Description Characters (2FF0–2FFF)
    // CJK Symbols and Punctuation (3000–303F)
    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Kanbun (3190–319F)
    // CJK Strokes (31C0–31EF)
    // Katakana Phonetic Extensions (31F0–31FF)
    // Enclosed CJK Letters and Months (3200–32FF)
    // CJK Compatibility (3300–33FF)
    // CJK Unified Ideographs Extension A (3400–4DBF)
    // CJK Unified Ideographs (4E00–9FFF)
    // CJK Compatibility Ideographs (F900–FAFF)
    // CJK Compatibility Forms (FE30–FE4F)
    // CJK Unified Ideographs Extension B (20000–2A6DF)
    // CJK Unified Ideographs Extension C (2A700–2B73F)
    // CJK Unified Ideographs Extension D (2B740–2B81F)
    // CJK Unified Ideographs Extension E (2B820–2CEAF)
    // CJK Unified Ideographs Extension F (2CEB0–2EBEF)
    // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
    // CJK Unified Ideographs Extension G (30000–3134F)
    // CJK Unified Ideographs Extension H (31350–323AF)
    public static readonly Regex JapaneseRegex = new(
        @"[\u2e80-\u30ff\u3190–\u319f\u31c0-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|\ud82c[\udc00-\udcff]|\ud83c[\ude00-\udeff]|\ud840[\udc00-\udfff]|[\ud841-\ud868][\udc00-\udfff]|\ud869[\udc00-\udedf]|\ud869[\udf00-\udfff]|[\ud86a-\ud879][\udc00-\udfff]|\ud87a[\udc00-\udfef]|\ud87e[\udc00-\ude1f]|\ud880[\udc00-\udfff]|[\ud881-\ud883][\udc00-\udfff]|\ud884[\udc00-\udfff]|[\ud885-\ud887][\udc00-\udfff]|\ud888[\udc00-\udfaf]",
        RegexOptions.Compiled);

    private static readonly Dictionary<char, string> s_katakanaToHiraganaDict = new()
    {
        #pragma warning disable format
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
        #pragma warning restore format
    };

    private static readonly Dictionary<string, char> s_kanaFinalVowelDict = new()
    {
        #pragma warning disable format
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
        #pragma warning restore format
    };

    private static readonly HashSet<char> s_smallCombiningKanaSet = new()
    {
        #pragma warning disable format
        'ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ヮ',
        'ャ', 'ュ', 'ョ',

        'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゎ',
        'ゃ', 'ゅ', 'ょ'
        #pragma warning restore format
    };

    private static readonly char[] s_sentenceTerminatingCharacters = {
        '。',
        '！',
        '？',
        '…',
        '.',
        '!',
        '?',
        '\n'
    };

    private static readonly Dictionary<char, char> s_bracketsDict = new()
    {
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
        { '{', '}' }
    };

    private static readonly HashSet<char> s_brackets = new()
    {
        #pragma warning disable format
        '「', '」' , '『', '』' , '【', '】',
        '《', '》', '〔', '〕', '（', '）',
        '［', '］', '〈', '〉', '｛', '｝',
        '＜', '＞', '〝', '〟', '＂', '＂',
        '＇', '＇', '｢', '｣', '⟨', '⟩',
        '(', ')', '[', ']', '{', '}'
        #pragma warning restore format
    };

    public static string KatakanaToHiragana(string text)
    {
        // Normalizes ＯＬ to OL, ｶﾞ to が, ﾜ to わ, ㍿ to 株式会社 etc.
        string normalizedText = text.Normalize(NormalizationForm.FormKC);

        StringBuilder textInHiragana = new(normalizedText.Length);
        for (int i = 0; i < normalizedText.Length; i++)
        {
            char character = normalizedText[i];
            _ = s_katakanaToHiraganaDict.TryGetValue(character, out string? hiraganaStr)
                ? textInHiragana.Append(hiraganaStr)
                : textInHiragana.Append(character);
        }

        return textInHiragana.ToString();
    }

    internal static List<string> LongVowelMarkToKana(string text)
    {
        List<string> unicodeTextList = text.ListUnicodeCharacters();

        List<StringBuilder> stringBuilders = new(4)
        {
            new StringBuilder(unicodeTextList[0], unicodeTextList.Count)
        };

        for (int i = 1; i < unicodeTextList.Count; i++)
        {
            if (unicodeTextList[i] is "ー" && s_kanaFinalVowelDict.TryGetValue(unicodeTextList[i - 1], out char vowel))
            {
                if (vowel is not 'お' and not 'え')
                {
                    for (int j = 0; j < stringBuilders.Count; j++)
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
                for (int j = 0; j < stringBuilders.Count; j++)
                {
                    _ = stringBuilders[j].Append(unicodeTextList[i]);
                }
            }
        }

        return stringBuilders.ConvertAll(static sb => sb.ToString());
    }

    public static List<string> CreateCombinedForm(string text)
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

    internal static bool IsKatakana(string text)
    {
        return s_katakanaToHiraganaDict.ContainsKey(text[0]);
    }

    public static int FindExpressionBoundary(string text, int position)
    {
        int endPosition = text.Length;
        for (int i = position; i < text.Length; i++)
        {
            char c = text[i];
            if (s_brackets.Contains(c) || char.IsWhiteSpace(c))
            {
                endPosition = i;
                break;
            }
        }

        return endPosition;
    }

    public static string FindSentence(string text, int position)
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

            if (tempIndex is not -1 && (endPosition is -1 || tempIndex < endPosition))
            {
                endPosition = tempIndex;
            }
        }

        ++startPosition;

        if (endPosition is -1)
        {
            endPosition = text.Length - 1;
        }

        string sentence = startPosition < endPosition
            ? text[startPosition..(endPosition + 1)].Trim('\n', '\t', '\r', ' ', '　')
            : "";

        if (sentence.Length > 1)
        {
            if (s_bracketsDict.ContainsValue(sentence[0]))
            {
                sentence = sentence[1..];
            }

            if (s_bracketsDict.ContainsKey(sentence.LastOrDefault()))
            {
                sentence = sentence[..^1];
            }

            if (s_bracketsDict.TryGetValue(sentence.FirstOrDefault(), out char rightBracket))
            {
                if (sentence.Last() == rightBracket)
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

            else if (s_bracketsDict.ContainsValue(sentence.LastOrDefault()))
            {
                char leftBracket = s_bracketsDict.First(p => p.Value == sentence.Last()).Key;

                if (!sentence.Contains(leftBracket, StringComparison.Ordinal))
                {
                    sentence = sentence[..^1];
                }
                else
                {
                    int numberOfLeftBrackets = sentence.Count(p => p == leftBracket);
                    int numberOfRightBrackets = sentence.Count(p => p == sentence.Last());

                    if (numberOfRightBrackets == numberOfLeftBrackets + 1)
                    {
                        sentence = sentence[..^1];
                    }
                }
            }
        }

        return sentence;
    }

    public static string RemovePunctuation(string text)
    {
        StringBuilder sb = new(text.Length);
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                _ = sb.Append(rune);
            }
        }

        return sb.ToString();
    }
}
