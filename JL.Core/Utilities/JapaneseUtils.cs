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

    //private static readonly Dictionary<string, string> s_hiraganaToKatakanaDict = new()
    //{
    //    #pragma warning disable format
    //    { "あ", "ア" }, { "い", "イ" }, { "う", "ウ" }, { "え", "エ" }, { "お", "オ" },
    //    { "か", "カ" }, { "き", "キ" }, { "く", "ク" }, { "け", "ケ" }, { "こ", "コ" },
    //    { "さ", "サ" }, { "し", "シ" }, { "す", "ス" }, { "せ", "セ" }, { "そ", "ソ" },
    //    { "た", "タ" }, { "ち", "チ" }, { "つ", "ツ" }, { "て", "テ" }, { "と", "ト" },
    //    { "な", "ナ" }, { "に", "ニ" }, { "ぬ", "ヌ" }, { "ね", "ネ" }, { "の", "ノ" },
    //    { "は", "ハ" }, { "ひ", "ヒ" }, { "ふ", "フ" }, { "へ", "ヘ" }, { "ほ", "ホ" },
    //    { "ま", "マ" }, { "み", "ミ" }, { "む", "ム" }, { "め", "メ" }, { "も", "モ" },
    //    { "ら", "ラ" }, { "り", "リ" }, { "る", "ル" }, { "れ", "レ" }, { "ろ", "ロ" },

    //    { "が", "ガ" }, { "ぎ", "ギ" }, { "ぐ", "グ" }, { "げ", "ゲ" }, { "ご", "ゴ" },
    //    { "ざ", "ザ" }, { "じ", "ジ" }, { "ず", "ズ" }, { "ぜ", "ゼ" }, { "ぞ", "ゾ" },
    //    { "だ", "ダ" }, { "ぢ", "ヂ" }, { "づ", "ヅ" }, { "で", "デ" }, { "ど", "ド" },
    //    { "ば", "バ" }, { "び", "ビ" }, { "ぶ", "ブ" }, { "べ", "ベ" }, { "ぼ", "ボ" },
    //    { "ぱ", "パ" }, { "ぴ", "ピ" }, { "ぷ", "プ" }, { "ぺ", "ペ" }, { "ぽ", "ポ" },

    //    { "わ", "ワ" }, { "を", "ヲ" },
    //    { "や", "ヤ" }, { "ゆ", "ユ" }, { "よ", "ヨ" },
    //    { "ん", "ン" },

    //    { "ぁ", "ァ" }, { "ぃ", "ィ" }, { "ぅ", "ゥ" }, { "ぇ", "ェ" }, { "ぉ", "ォ" },
    //    { "ゃ", "ャ" }, { "ゅ", "ュ" }, { "ょ", "ョ" },

    //    { "ゎ", "ヮ" },

    //    { "ゕ", "ヵ" }, { "ゖ", "ヶ" }, { "ゔ", "ヴ" },
    //    { "ゝ", "ヽ" }, { "ゞ", "ヾ" }, { "っ", "ッ" },
    //    { "ゐ゙", "ヸ" }, { "ゑ゙", "ヹ" }, { "を゙", "ヺ" }
    //    #pragma warning restore format
    //};

    private static readonly Dictionary<string, string> s_katakanaToHiraganaDict = new()
    {
        #pragma warning disable format
        { "ア", "あ" }, { "イ", "い" }, { "ウ", "う" }, { "エ", "え" }, { "オ", "お" },
        { "カ", "か" }, { "キ", "き" }, { "ク", "く" }, { "ケ", "け" }, { "コ", "こ" },
        { "サ", "さ" }, { "シ", "し" }, { "ス", "す" }, { "セ", "せ" }, { "ソ", "そ" },
        { "タ", "た" }, { "チ", "ち" }, { "ツ", "つ" }, { "テ", "て" }, { "ト", "と" },
        { "ナ", "な" }, { "ニ", "に" }, { "ヌ", "ぬ" }, { "ネ", "ね" }, { "ノ", "の" },
        { "ハ", "は" }, { "ヒ", "ひ" }, { "フ", "ふ" }, { "ヘ", "へ" }, { "ホ", "ほ" },
        { "マ", "ま" }, { "ミ", "み" }, { "ム", "む" }, { "メ", "め" }, { "モ", "も" },
        { "ラ", "ら" }, { "リ", "り" }, { "ル", "る" }, { "レ", "れ" }, { "ロ", "ろ" },

        { "ガ", "が" }, { "ギ", "ぎ" }, { "グ", "ぐ" }, { "ゲ", "げ" }, { "ゴ", "ご" },
        { "ザ", "ざ" }, { "ジ", "じ" }, { "ズ", "ず" }, { "ゼ", "ぜ" }, { "ゾ", "ぞ" },
        { "ダ", "だ" }, { "ヂ", "ぢ" }, { "ヅ", "づ" }, { "デ", "で" }, { "ド", "ど" },
        { "バ", "ば" }, { "ビ", "び" }, { "ブ", "ぶ" }, { "ベ", "べ" }, { "ボ", "ぼ" },
        { "パ", "ぱ" }, { "ピ", "ぴ" }, { "プ", "ぷ" }, { "ペ", "ぺ" }, { "ポ", "ぽ" },

        { "ワ", "わ" }, { "ヲ", "を" },
        { "ヤ", "や" }, { "ユ", "ゆ" }, { "ヨ", "よ" },
        { "ン", "ん" },

        { "ァ", "ぁ" }, { "ィ", "ぃ" }, { "ゥ", "ぅ" }, { "ェ", "ぇ" }, { "ォ", "ぉ" },
        { "ャ", "ゃ" }, { "ュ", "ゅ" }, { "ョ", "ょ" },

        { "ヮ", "ゎ" },

        { "ヴ", "ゔ" }, { "ヽ", "ゝ" }, { "ヾ", "ゞ" }, { "ッ", "っ" },

        { "ヸ", "ゐ゙" }, { "ヹ", "ゑ゙" }, { "ヺ", "を゙" }
        #pragma warning restore format
    };

    private static readonly Dictionary<string, char> s_kanaFinalVowelDict = new()
    {
        #pragma warning disable format
        // Katakana
        { "ア", 'ア' }, { "カ", 'ア' }, { "サ", 'ア' }, { "タ", 'ア' }, { "ナ", 'ア' }, { "ハ", 'ア' },
        { "マ", 'ア' }, { "ラ", 'ア' }, { "ガ", 'ア' }, { "ザ", 'ア' }, { "ダ", 'ア' }, { "バ", 'ア' },
        { "パ", 'ア' }, { "ワ", 'ア' }, { "ヤ", 'ア' }, { "ァ", 'ア' }, { "ャ", 'ア' }, { "ヵ", 'ア' },
        { "ヮ", 'ア' },

        { "イ", 'イ' }, { "キ", 'イ' }, { "シ", 'イ' }, { "チ", 'イ' }, { "ニ", 'イ' }, { "ヰ", 'イ' },
        { "ヒ", 'イ' }, { "ミ", 'イ' }, { "リ", 'イ' }, { "ギ", 'イ' }, { "ジ", 'イ' }, { "ヂ", 'イ' },
        { "ビ", 'イ' }, { "ピ", 'イ' }, { "ィ", 'イ' }, { "ヸ", 'イ' },

        { "ウ", 'ウ' }, { "ク", 'ウ' }, { "ス", 'ウ' }, { "ツ", 'ウ' }, { "ヌ", 'ウ' }, { "フ", 'ウ' },
        { "ム", 'ウ' }, { "ル", 'ウ' }, { "グ", 'ウ' }, { "ズ", 'ウ' }, { "ヅ", 'ウ' }, { "ブ", 'ウ' },
        { "プ", 'ウ' }, { "ユ", 'ウ' }, { "ゥ", 'ウ' }, { "ュ", 'ウ' }, { "ヴ", 'ウ' },

        { "エ", 'エ' }, { "ケ", 'エ' }, { "セ", 'エ' }, { "テ", 'エ' }, { "ネ", 'エ' }, { "ヘ", 'エ' },
        { "メ", 'エ' }, { "レ", 'エ' }, { "ゲ", 'エ' }, { "ゼ", 'エ' }, { "デ", 'エ' }, { "ベ", 'エ' },
        { "ペ", 'エ' }, { "ヱ", 'エ' }, { "ェ", 'エ' }, { "ヶ", 'エ' }, { "ヹ", 'エ' },

        { "オ", 'オ' }, { "コ", 'オ' }, { "ソ", 'オ' }, { "ト", 'オ' }, { "ノ", 'オ' }, { "ホ", 'オ' },
        { "モ", 'オ' }, { "ロ", 'オ' }, { "ゴ", 'オ' }, { "ゾ", 'オ' }, { "ド", 'オ' }, { "ボ", 'オ' },
        { "ポ", 'オ' }, { "ヲ", 'オ' }, { "ヨ", 'オ' }, { "ォ", 'オ' }, { "ョ", 'オ' }, { "ヺ", 'オ' },

        //Hiragana
        { "あ", 'あ' }, { "か", 'あ' }, { "さ", 'あ' }, { "た", 'あ' }, { "な", 'あ' }, { "は", 'あ' },
        { "ま", 'あ' }, { "ら", 'あ' }, { "が", 'あ' }, { "ざ", 'あ' }, { "だ", 'あ' }, { "ば", 'あ' },
        { "ぱ", 'あ' }, { "わ", 'あ' }, { "や", 'あ' }, { "ぁ", 'あ' }, { "ゃ", 'あ' }, { "ゕ", 'あ' },
        { "ゎ", 'あ' },

        { "い", 'い' }, { "き", 'い' }, { "し", 'い' }, { "ち", 'い' }, { "に", 'い' }, { "ひ", 'い' },
        { "み", 'い' }, { "り", 'い' }, { "ぎ", 'い' }, { "じ", 'い' }, { "ぢ", 'い' }, { "び", 'い' },
        { "ぴ", 'い' }, { "ぃ", 'い' }, { "ゐ", 'い' }, { "ゐ゙", 'イ' },

        { "う", 'う' }, { "く", 'う' }, { "す", 'う' }, { "つ", 'う' }, { "ぬ", 'う' }, { "ふ", 'う' },
        { "む", 'う' }, { "る", 'う' }, { "ぐ", 'う' }, { "ず", 'う' }, { "づ", 'う' }, { "ぶ", 'う' },
        { "ぷ", 'う' }, { "ゆ", 'う' }, { "ぅ", 'う' }, { "ゅ", 'う' }, { "ゔ", 'う' },

        { "え", 'え' }, { "け", 'え' }, { "せ", 'え' }, { "て", 'え' }, { "ね", 'え' }, { "へ", 'え' },
        { "め", 'え' }, { "れ", 'え' }, { "げ", 'え' }, { "ぜ", 'え' }, { "で", 'え' }, { "べ", 'え' },
        { "ぺ", 'え' }, { "ぇ", 'え' }, { "ゖ", 'え' }, { "ゑ", 'え' }, { "ゑ゙", 'エ' },

        { "お", 'お' }, { "こ", 'お' }, { "そ", 'お' }, { "と", 'お' }, { "の", 'お' }, { "ほ", 'お' },
        { "も", 'お' }, { "ろ", 'お' }, { "ご", 'お' }, { "ぞ", 'お' }, { "ど", 'お' }, { "ぼ", 'お' },
        { "ぽ", 'お' }, { "を", 'お' }, { "よ", 'お' }, { "ぉ", 'お' }, { "ょ", 'お' }, { "を゙", 'オ' }
        #pragma warning restore format
    };

    private static readonly Dictionary<string, string> s_halfWidthToFullWidthDict = new()
    {
        #pragma warning disable format
        // Half-width katakana
        { "ｱ", "あ" }, { "ｲ", "い" }, { "ｳ", "う" }, { "ｴ", "え" }, { "ｵ", "お" },
        { "ｶ", "か" }, { "ｷ", "き" }, { "ｸ", "く" }, { "ｹ", "け" }, { "ｺ", "こ" },
        { "ｻ", "さ" }, { "ｼ", "し" }, { "ｽ", "す" }, { "ｾ", "せ" }, { "ｿ", "そ" },
        { "ﾀ", "た" }, { "ﾁ", "ち" }, { "ﾂ", "つ" }, { "ﾃ", "て" }, { "ﾄ", "と" },
        { "ﾅ", "な" }, { "ﾆ", "に" }, { "ﾇ", "ぬ" }, { "ﾈ", "ね" }, { "ﾉ", "の" },
        { "ﾊ", "は" }, { "ﾋ", "ひ" }, { "ﾌ", "ふ" }, { "ﾍ", "へ" }, { "ﾎ", "ほ" },
        { "ﾏ", "ま" }, { "ﾐ", "み" }, { "ﾑ", "む" }, { "ﾒ", "め" }, { "ﾓ", "も" },
        { "ﾗ", "ら" }, { "ﾘ", "り" }, { "ﾙ", "る" }, { "ﾚ", "れ" }, { "ﾛ", "ろ" },

        { "ﾜ", "わ" }, { "ｦ", "を" },
        { "ﾔ", "や" }, { "ﾕ", "ゆ" }, { "ﾖ", "よ" },
        { "ﾝ", "ん" },

        { "ｧ", "ぁ" }, { "ｨ", "ぃ" }, { "ｩ", "ぅ" }, { "ｪ", "ぇ" }, { "ｫ", "ぉ" },
        { "ｬ", "ゃ" }, { "ｭ", "ゅ" }, { "ｮ", "ょ" },

        { "ヵ", "ゕ" }, { "ヶ", "ゖ" }, { "ｯ", "っ" },

        // Uppercase letters
        { "A", "Ａ" }, { "B", "Ｂ" }, { "C", "Ｃ" }, { "D", "Ｄ" }, { "E", "Ｅ" }, { "F", "Ｆ" },
        { "G", "Ｇ" }, { "H", "Ｈ" }, { "I", "Ｉ" }, { "J", "Ｊ" }, { "K", "Ｋ" }, { "L", "Ｌ" },
        { "M", "Ｍ" }, { "N", "Ｎ" }, { "O", "Ｏ" }, { "P", "Ｐ" }, { "Q", "Ｑ" }, { "R", "Ｒ" },
        { "S", "Ｓ" }, { "T", "Ｔ" }, { "U", "Ｕ" }, { "V", "Ｖ" }, { "W", "Ｗ" }, { "X", "Ｘ" },
        { "Y", "Ｙ" }, { "Z", "Ｚ" },

        // Lowercase letters
        { "a", "ａ" }, { "b", "ｂ" }, { "c", "ｃ" }, { "d", "ｄ" }, { "e", "ｅ" }, { "f", "ｆ" },
        { "g", "ｇ" }, { "h", "ｈ" }, { "i", "ｉ" }, { "j", "ｊ" }, { "k", "ｋ" }, { "l", "ｌ" },
        { "m", "ｍ" }, { "n", "ｎ" }, { "o", "ｏ" }, { "p", "ｐ" }, { "q", "ｑ" }, { "r", "ｒ" },
        { "s", "ｓ" }, { "t", "ｔ" }, { "u", "ｕ" }, { "v", "ｖ" }, { "w", "ｗ" }, { "x", "ｘ" },
        { "y", "ｙ" }, { "z", "ｚ" },

        // Numbers
        { "0", "０" }, { "1", "１" }, { "2", "２" }, { "3", "３" }, { "4", "４" },
        { "5", "５" }, { "6", "６" }, { "7", "７" }, { "8", "８" }, { "9", "９" },

        // Typographical symbols and punctuation marks
        { "!", "！" }, { "\"", "＂" }, { "#", "＃" }, { "$", "＄" }, { "%", "％" }, { "&", "＆" },
        { "'", "＇" }, { "(", "（" }, { ")", "）" }, { "*", "＊" }, { "+", "＋" }, { "/", "／" },
        { ":", "：" }, { ";", "；" }, { "<", "＜" }, { "=", "＝" }, { ">", "＞" }, { "?", "？" },
        { "@", "＠" }, { "[", "［" }, { "\\", "＼" }, { "]", "］" }, { "^", "＾" }, { "{", "｛" },
        { "|", "｜" }, { "}", "｝" }, { "~", "～" }, { "ｰ", "ー" }
        // ，－ ．＿｀｟｡｢｣､･￠￡
        #pragma warning restore format
    };

    private static readonly Dictionary<string, string> s_compositeHalfWidthKatakanaToFullWidthHiraganaDict = new()
    {
        #pragma warning disable format
        { "ｶﾞ", "が" }, { "ｷﾞ", "ぎ" }, { "ｸﾞ", "ぐ" }, { "ｹﾞ", "げ" }, { "ｺﾞ", "ご" },
        { "ｻﾞ", "ざ" }, { "ｼﾞ", "じ" }, { "ｽﾞ", "ず" }, { "ｾﾞ", "ぜ" }, { "ｿﾞ", "ぞ" },
        { "ﾀﾞ", "だ" }, { "ﾁﾞ", "ぢ" }, { "ﾂﾞ", "づ" }, { "ﾃﾞ", "で" }, { "ﾄﾞ", "ど" },
        { "ﾊﾞ", "ば" }, { "ﾋﾞ", "び" }, { "ﾌﾞ", "ぶ" }, { "ﾍﾞ", "べ" }, { "ﾎﾞ", "ぼ" },
        { "ﾊﾟ", "ぱ" }, { "ﾋﾟ", "ぴ" }, { "ﾌﾟ", "ぷ" }, { "ﾍﾟ", "ぺ" }, { "ﾎﾟ", "ぽ" },
        { "ｳﾞ", "ゔ" }
        #pragma warning restore format
    };

    private static readonly HashSet<string> s_smallCombiningKanaSet = new()
    {
        #pragma warning disable format
        "ァ", "ィ", "ゥ", "ェ", "ォ", "ヮ",
        "ャ", "ュ", "ョ",

        "ぁ", "ぃ", "ぅ", "ぇ", "ぉ", "ゎ",
        "ゃ", "ゅ", "ょ"
        #pragma warning restore format
    };

    private static readonly List<char> s_sentenceTerminatingCharacters = new()
    {
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
        List<string> unicodeCharacters = text.Normalize(NormalizationForm.FormKC).ListUnicodeCharacters();
        StringBuilder textInHiragana = new(text.Length);

        for (int i = 0; i < unicodeCharacters.Count; i++)
        {
            if (unicodeCharacters.Count > i + 1
                && s_compositeHalfWidthKatakanaToFullWidthHiraganaDict.TryGetValue(
                    unicodeCharacters[i] + unicodeCharacters[i + 1], out string? compositeStr))
            {
                _ = textInHiragana.Append(compositeStr);
                ++i;
            }
            else if (s_katakanaToHiraganaDict.TryGetValue(unicodeCharacters[i], out string? hiraganaStr))
            {
                _ = textInHiragana.Append(hiraganaStr);
            }
            else if (s_halfWidthToFullWidthDict.TryGetValue(unicodeCharacters[i], out string? fullWidthStr))
            {
                _ = textInHiragana.Append(fullWidthStr);
            }
            else
            {
                _ = textInHiragana.Append(unicodeCharacters[i]);
            }
        }

        return textInHiragana.ToString();
    }

    //public static string HiraganaToKatakana(string text)
    //{
    //    StringBuilder textInKatakana = new(text.Length);
    //    foreach (string str in text.EnumerateUnicodeCharacters())
    //    {
    //        textInKatakana.Append(s_hiraganaToKatakanaDict.TryGetValue(str, out string? hiraganaStr)
    //            ? hiraganaStr
    //            : str);
    //    }

    //    return textInKatakana.ToString();
    //}

    internal static List<string> LongVowelMarkToKana(string text)
    {
        List<string> unicodeTextList = text.ListUnicodeCharacters();

        List<StringBuilder> stringBuilders = new(4)
        {
            new StringBuilder(unicodeTextList[0], text.Length)
        };

        for (int i = 1; i < unicodeTextList.Count; i++)
        {
            if (text[i] is 'ー' && s_kanaFinalVowelDict.TryGetValue(unicodeTextList[i - 1], out char vowel))
            {
                if (vowel is not 'お' and not 'え' and not 'オ' and not 'エ')
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
                        'オ' => 'ウ',
                        'エ' => 'イ',
                        _ => ' '
                    };

                    int listSize = stringBuilders.Count;
                    for (int j = 0; j < listSize; j++)
                    {
                        stringBuilders.Add(new StringBuilder(stringBuilders[j].ToString(), text.Length));
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
                    _ = stringBuilders[j].Append(text[i]);
                }
            }
        }

        return stringBuilders.ConvertAll(static sb => sb.ToString());
    }

    public static List<string> CreateCombinedForm(string text)
    {
        List<string> unicodeCharacterList = text.ListUnicodeCharacters();
        List<string> combinedForm = new(unicodeCharacterList.Count);

        for (int i = 0; i < unicodeCharacterList.Count; i++)
        {
            if (i + 1 < unicodeCharacterList.Count
                && s_smallCombiningKanaSet.Contains(unicodeCharacterList[i + 1]))
            {
                combinedForm.Add(unicodeCharacterList[i] + unicodeCharacterList[i + 1]);
                ++i;
            }

            else
            {
                combinedForm.Add(unicodeCharacterList[i]);
            }
        }

        return combinedForm;
    }

    //public static bool IsHiragana(string text)
    //{
    //    return s_hiraganaToKatakanaDict.ContainsKey(text.EnumerateUnicodeCharacters().First());
    //}

    internal static bool IsKatakana(string text)
    {
        return s_katakanaToHiraganaDict.ContainsKey(text.ListUnicodeCharacters().First());
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

        for (int i = 0; i < s_sentenceTerminatingCharacters.Count; i++)
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
            if (s_bracketsDict.ContainsValue(sentence.First()))
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
                else if (!sentence.Contains(rightBracket))
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

                if (!sentence.Contains(leftBracket))
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
        StringBuilder stringBuilder = new(text.Length);
        foreach (char character in text)
        {
            if (char.IsLetterOrDigit(character) || char.IsSurrogate(character))
            {
                _ = stringBuilder.Append(character);
            }
        }

        return stringBuilder.ToString();
    }
}
