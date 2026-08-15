using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Japanese;

public static partial class JapaneseUtils
{
    internal const char NormalizedFuseji = '○';

    private const char VariationSelectorRangeStart = '\uFE00';
    private const char VariationSelectorRangeEnd = '\uFE0F';

    private const char VariationSelectorSupplementHighSurrogate = '\uDB40';
    private const char VariationSelectorSupplementLowSurrogateRangeStart = '\uDD00';
    private const char VariationSelectorSupplementLowSurrogateRangeEnd = '\uDDEF';

    // Matches the following Unicode ranges:
    // × (\u00D7)
    // General Punctuation (2000-206F): ‥, …, •, ※
    // Geometric Shapes (25A0-25FF): ◦, ◎, ○, △, ◉
    // CJK Radicals Supplement (2E80–2EFF)
    // Kangxi Radicals (2F00–2FDF)
    // Ideographic Description Characters (2FF0–2FFF)
    // CJK Symbols and Punctuation (3000–303F)
    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Kanbun (3190–319F)
    // CJK Strokes (31C0–31EF)
    // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included in the regex.
    // Enclosed CJK Letters and Months (3200–32FF) 3220-325F, 3280-32FF
    // CJK Compatibility (3300–33FF)
    // CJK Unified Ideographs Extension A (3400–4DBF)
    // CJK Unified Ideographs (4E00–9FFF)
    // CJK Compatibility Ideographs (F900–FAFF)
    // Vertical Forms (FE10–FE1F)
    // CJK Compatibility Forms (FE30–FE4F)
    // Halfwidth and Fullwidth Forms (FF00–FFEF) FF00-FF9F,FFE0-FFEF
    // Ideographic Symbols and Punctuation (16FE0-16FFF): It does not contain any Japanese characters, so it's not included in the regex.
    // Kana Extended-B (1AFF0-1AFFF): The range does not contain any Japanese characters; it only includes Taiwanese kana, so it's not included in the regex.
    // Kana Supplement (1B000-1B0FF)
    // Kana Extended-A (1B100-1B12F)
    // Small Kana Extension (1B130-1B16F)
    // Counting Rod Numerals (1D360-1D37F)
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
    // CJK Unified Ideographs Extension J (323B0-3347F)
    [GeneratedRegex(@"[\u00D7\u2000-\u206F\u25A0-\u25FF\u2E80-\u2FDF\u2FF0-\u30FF\u3190-\u319F\u31C0-\u31FF\u3220-\u325F\u3280-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF\uFE10-\uFE1F\uFE30-\uFE4F\uFF00-\uFF9F\uFFE0-\uFFEF]|\uD82C[\uDC00-\uDD6F]|\uD834[\uDF60-\uDF7F]|\uD83C[\uDE00-\uDEFF]|\uD840[\uDC00-\uDFFF]|[\uD841-\uD868][\uDC00-\uDFFF]|\uD869[\uDC00-\uDEDF]|\uD869[\uDF00-\uDFFF]|[\uD86A-\uD87A][\uDC00-\uDFFF]|\uD87B[\uDC00-\uDE5F]|\uD87E[\uDC00-\uDE1F]|\uD880[\uDC00-\uDFFF]|[\uD881-\uD88C][\uDC00-\uDFFF]|\uD88D[\uDC00-\uDC7F]", RegexOptions.CultureInvariant)]
    private static partial Regex JapaneseRegex { get; }

    private static readonly FrozenDictionary<char, char> s_normalizationDict = new KeyValuePair<char, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        // Katakana to Hiragana
        new('ア', 'あ'), new('イ', 'い'), new('ウ', 'う'), new('エ', 'え'), new('オ', 'お'),
        new('カ', 'か'), new('キ', 'き'), new('ク', 'く'), new('ケ', 'け'), new('コ', 'こ'),
        new('サ', 'さ'), new('シ', 'し'), new('ス', 'す'), new('セ', 'せ'), new('ソ', 'そ'),
        new('タ', 'た'), new('チ', 'ち'), new('ツ', 'つ'), new('テ', 'て'), new('ト', 'と'),
        new('ナ', 'な'), new('ニ', 'に'), new('ヌ', 'ぬ'), new('ネ', 'ね'), new('ノ', 'の'),
        new('ハ', 'は'), new('ヒ', 'ひ'), new('フ', 'ふ'), new('ヘ', 'へ'), new('ホ', 'ほ'),
        new('マ', 'ま'), new('ミ', 'み'), new('ム', 'む'), new('メ', 'め'), new('モ', 'も'),
        new('ラ', 'ら'), new('リ', 'り'), new('ル', 'る'), new('レ', 'れ'), new('ロ', 'ろ'),

        new('ガ', 'が'), new('ギ', 'ぎ'), new('グ', 'ぐ'), new('ゲ', 'げ'), new('ゴ', 'ご'),
        new('ザ', 'ざ'), new('ジ', 'じ'), new('ズ', 'ず'), new('ゼ', 'ぜ'), new('ゾ', 'ぞ'),
        new('ダ', 'だ'), new('ヂ', 'ぢ'), new('ヅ', 'づ'), new('デ', 'で'), new('ド', 'ど'),
        new('バ', 'ば'), new('ビ', 'び'), new('ブ', 'ぶ'), new('ベ', 'べ'), new('ボ', 'ぼ'),
        new('パ', 'ぱ'), new('ピ', 'ぴ'), new('プ', 'ぷ'), new('ペ', 'ぺ'), new('ポ', 'ぽ'),

        new('ワ', 'わ'), new('ヲ', 'を'),
        new('ヤ', 'や'), new('ユ', 'ゆ'), new('ヨ', 'よ'),
        new('ン', 'ん'),

        new('ァ', 'ぁ'), new('ィ', 'ぃ'), new('ゥ', 'ぅ'), new('ェ', 'ぇ'), new('ォ', 'ぉ'),
        new('ャ', 'ゃ'), new('ュ', 'ゅ'), new('ョ', 'ょ'),

        new('ヮ', 'ゎ'),

        new('ヰ', 'ゐ'), new('ヱ', 'ゑ'), new('ヵ', 'ゕ'), new('ヶ', 'ゖ'),

        new('ヴ', 'ゔ'), new('ヽ', 'ゝ'), new('ヾ', 'ゞ'), new('ッ', 'っ'),

        // CJK Radicals Supplement to Kanji
        new('⺁', '厂'), new('⺂', '乛'), new('⺃', '乚'), new('⺄', '乙'), new('⺅', '亻'),
        new('⺆', '冂'), new('⺇', '几'), new('⺈', '刀'), new('⺉', '刂'), new('⺊', '卜'),
        new('⺋', '㔾'), new('⺌', '小'), new('⺍', '小'), new('⺎', '兀'), new('⺏', '尣'),
        new('⺐', '尢'), new('⺑', '尣'), new('⺒', '巳'), new('⺓', '幺'), new('⺔', '彑'),
        new('⺕', '彐'), new('⺖', '忄'), new('⺗', '㣺'), new('⺘', '扌'), new('⺙', '攵'),
        new('⺛', '旡'), new('⺜', '日'), new('⺝', '月'), new('⺞', '歺'), new('⺠', '民'),
        new('⺡', '氵'), new('⺢', '氺'), new('⺣', '灬'), new('⺤', '爫'), new('⺥', '爫'),
        new('⺦', '丬'), new('⺧', '牛'), new('⺨', '犭'), new('⺩', '王'), new('⺪', '疋'),
        new('⺫', '罒'), new('⺬', '示'), new('⺭', '礻'), new('⺮', '竹'), new('⺯', '糹'),
        new('⺰', '纟'), new('⺱', '罓'), new('⺲', '罒'), new('⺳', '㓁'), new('⺴', '㓁'),
        new('⺵', '网'), new('⺶', '羊'), new('⺷', '羊'), new('⺸', '羋'), new('⺹', '耂'),
        new('⺺', '肀'), new('⺻', '聿'), new('⺼', '月'), new('⺽', '臼'), new('⺾', '艹'),
        new('⺿', '艹'), new('⻀', '艹'), new('⻁', '虎'), new('⻂', '衣'), new('⻃', '覀'),
        new('⻄', '西'), new('⻅', '见'), new('⻆', '角'), new('⻇', '角'), new('⻈', '讠'),
        new('⻉', '贝'), new('⻊', '足'), new('⻋', '车'), new('⻌', '辶'), new('⻍', '辶'),
        new('⻎', '辶'), new('⻏', '阝'), new('⻐', '钅'), new('⻑', '長'), new('⻒', '镸'),
        new('⻓', '长'), new('⻔', '门'), new('⻕', '阜'), new('⻖', '阝'), new('⻗', '雨'),
        new('⻘', '青'), new('⻙', '韦'), new('⻚', '页'), new('⻛', '风'), new('⻜', '飞'),
        new('⻝', '食'), new('⻞', '食'), new('⻟', '飠'), new('⻠', '饣'), new('⻡', '首'),
        new('⻢', '马'), new('⻣', '骨'), new('⻤', '鬼'), new('⻥', '鱼'), new('⻦', '鸟'),
        new('⻧', '卤'), new('⻨', '麦'), new('⻩', '黄'), new('⻪', '黾'), new('⻫', '斉'),
        new('⻬', '齐'), new('⻭', '歯'), new('⻮', '齿'), new('⻯', '竜'), new('⻰', '龙'),
        new('⻱', '龜'), new('⻲', '亀'), new('⻳', '亀'),

        // Kyuujitai to Shinjitai
        new('乘', '乗'), new('亂', '乱'), new('亞', '亜'), new('佛', '仏'), new('來', '来'),
        new('倂', '併'), new('假', '仮'), new('傳', '伝'), new('僞', '偽'), new('價', '価'),
        new('儉', '倹'), new('兒', '児'), new('內', '内'), new('兩', '両'), new('册', '冊'),
        new('剩', '剰'), new('劍', '剣'), new('劑', '剤'), new('勞', '労'), new('勳', '勲'),
        new('勵', '励'), new('勸', '勧'), new('區', '区'), new('卷', '巻'), new('卽', '即'),
        new('參', '参'), new('吳', '呉'), new('吿', '告'), new('單', '単'), new('嚴', '厳'),
        new('囑', '嘱'), new('圈', '圏'), new('國', '国'), new('圍', '囲'), new('圓', '円'),
        new('圖', '図'), new('團', '団'), new('堯', '尭'), new('增', '増'), new('墮', '堕'),
        new('壓', '圧'), new('壘', '塁'), new('壞', '壊'), new('壤', '壌'), new('壯', '壮'),
        new('壹', '壱'), new('壻', '婿'), new('壽', '寿'), new('奧', '奥'), new('奬', '奨'),
        new('姬', '姫'), new('娛', '娯'), new('孃', '嬢'), new('學', '学'), new('寢', '寝'),
        new('實', '実'), new('寫', '写'), new('寬', '寛'), new('寶', '宝'), new('將', '将'),
        new('專', '専'), new('對', '対'), new('尙', '尚'), new('屆', '届'), new('屬', '属'),
        new('峽', '峡'), new('嶽', '岳'), new('巖', '巌'), new('巢', '巣'), new('帶', '帯'),
        new('廚', '厨'), new('廢', '廃'), new('廣', '広'), new('廳', '庁'), new('彈', '弾'),
        new('彌', '弥'), new('徑', '径'), new('從', '従'), new('徵', '徴'), new('德', '徳'),
        new('恆', '恒'), new('悅', '悦'), new('惠', '恵'), new('惡', '悪'), new('惱', '悩'),
        new('愼', '慎'), new('慘', '惨'), new('應', '応'), new('懷', '懐'), new('戀', '恋'),
        new('戰', '戦'), new('戲', '戯'), new('戶', '戸'), new('戾', '戻'), new('拂', '払'),
        new('拔', '抜'), new('拜', '拝'), new('挾', '挟'), new('插', '挿'), new('揭', '掲'),
        new('搖', '揺'), new('搜', '捜'), new('擇', '択'), new('擊', '撃'), new('擔', '担'),
        new('據', '拠'), new('擧', '挙'), new('擴', '拡'), new('攝', '摂'), new('收', '収'),
        new('效', '効'), new('敍', '叙'), new('敎', '教'), new('敕', '勅'), new('數', '数'),
        new('斷', '断'), new('晉', '晋'), new('晚', '晩'), new('晝', '昼'), new('曆', '暦'),
        new('曉', '暁'), new('曾', '曽'), new('會', '会'), new('條', '条'), new('棧', '桟'),
        new('榮', '栄'), new('槇', '槙'), new('樂', '楽'), new('樓', '楼'), new('樞', '枢'),
        new('樣', '様'), new('橫', '横'), new('檢', '検'), new('櫻', '桜'), new('權', '権'),
        new('歐', '欧'), new('歡', '歓'), new('步', '歩'), new('歲', '歳'), new('歷', '歴'),
        new('歸', '帰'), new('殘', '残'), new('殼', '殻'), new('毆', '殴'), new('每', '毎'),
        new('氣', '気'), new('沒', '没'), new('涉', '渉'), new('淚', '涙'), new('淨', '浄'),
        new('淺', '浅'), new('渴', '渇'), new('溪', '渓'), new('溫', '温'), new('滯', '滞'),
        new('滿', '満'), new('潛', '潜'), new('澁', '渋'), new('澤', '沢'), new('濕', '湿'),
        new('濟', '済'), new('濱', '浜'), new('瀧', '滝'), new('瀨', '瀬'), new('灣', '湾'),
        new('燈', '灯'), new('燒', '焼'), new('營', '営'), new('爐', '炉'), new('爭', '争'),
        new('爲', '為'), new('犧', '犠'), new('狀', '状'), new('狹', '狭'), new('獨', '独'),
        new('獵', '猟'), new('獸', '獣'), new('獻', '献'), new('瑤', '瑶'), new('瓣', '弁'),
        new('甁', '瓶'), new('產', '産'), new('畫', '画'), new('當', '当'), new('疊', '畳'),
        new('瘦', '痩'), new('癡', '痴'), new('發', '発'), new('盜', '盗'), new('盡', '尽'),
        new('眞', '真'), new('硏', '研'), new('碎', '砕'), new('祕', '秘'), new('祿', '禄'),
        new('禪', '禅'), new('禮', '礼'), new('稅', '税'), new('稱', '称'), new('稻', '稲'),
        new('穗', '穂'), new('穩', '穏'), new('穰', '穣'), new('竊', '窃'), new('竝', '並'),
        new('粹', '粋'), new('絕', '絶'), new('絲', '糸'), new('經', '経'), new('綠', '緑'),
        new('緖', '緒'), new('緣', '縁'), new('縣', '県'), new('縱', '縦'), new('總', '総'),
        new('繩', '縄'), new('繪', '絵'), new('繼', '継'), new('續', '続'), new('纖', '繊'),
        new('缺', '欠'), new('罐', '缶'), new('聰', '聡'), new('聲', '声'), new('聽', '聴'),
        new('肅', '粛'), new('脫', '脱'), new('腦', '脳'), new('膽', '胆'), new('臟', '臓'),
        new('臺', '台'), new('與', '与'), new('舊', '旧'), new('舍', '舎'), new('舖', '舗'),
        new('艷', '艶'), new('莊', '荘'), new('莖', '茎'), new('萬', '万'), new('薰', '薫'),
        new('藏', '蔵'), new('藝', '芸'), new('藥', '薬'), new('處', '処'), new('虛', '虚'),
        new('號', '号'), new('螢', '蛍'), new('蟲', '虫'), new('蠶', '蚕'), new('蠻', '蛮'),
        new('衞', '衛'), new('裝', '装'), new('襃', '褒'), new('覺', '覚'), new('覽', '覧'),
        new('觀', '観'), new('觸', '触'), new('謠', '謡'), new('證', '証'), new('譯', '訳'),
        new('譽', '誉'), new('讀', '読'), new('變', '変'), new('讓', '譲'), new('豐', '豊'),
        new('豫', '予'), new('貳', '弐'), new('賣', '売'), new('賴', '頼'), new('贊', '賛'),
        new('踐', '践'), new('輕', '軽'), new('轉', '転'), new('辨', '弁'), new('辭', '辞'),
        new('辯', '弁'), new('遙', '遥'), new('遞', '逓'), new('遲', '遅'), new('邊', '辺'),
        new('郞', '郎'), new('鄕', '郷'), new('醉', '酔'), new('醫', '医'), new('釀', '醸'),
        new('釋', '釈'), new('銳', '鋭'), new('錄', '録'), new('錢', '銭'), new('鍊', '錬'),
        new('鎭', '鎮'), new('鐵', '鉄'), new('鑄', '鋳'), new('鑛', '鉱'), new('關', '関'),
        new('陷', '陥'), new('隨', '随'), new('險', '険'), new('隱', '隠'), new('隸', '隷'),
        new('雙', '双'), new('雜', '雑'), new('靈', '霊'), new('靑', '青'), new('靜', '静'),
        new('顏', '顔'), new('顯', '顕'), new('飮', '飲'), new('餘', '余'), new('餠', '餅'),
        new('騷', '騒'), new('驅', '駆'), new('驗', '験'), new('驛', '駅'), new('髓', '髄'),
        new('體', '体'), new('髮', '髪'), new('鬪', '闘'), new('鷄', '鶏'), new('鹽', '塩'),
        new('麥', '麦'), new('麵', '麺'), new('黃', '黄'), new('黑', '黒'), new('默', '黙'),
        new('點', '点'), new('黨', '党'), new('齊', '斉'), new('齋', '斎'), new('齒', '歯'),
        new('齡', '齢'), new('龍', '竜'), new('龜', '亀')
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<char, char> s_hiraganaToDakutenDict = new KeyValuePair<char, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        new('か', 'が'), new('き', 'ぎ'), new('く', 'ぐ'), new('け', 'げ'), new('こ', 'ご'),
        new('さ', 'ざ'), new('し', 'じ'), new('す', 'ず'), new('せ', 'ぜ'), new('そ', 'ぞ'),
        new('た', 'だ'), new('ち', 'ぢ'), new('つ', 'づ'), new('て', 'で'), new('と', 'ど'),
        new('は', 'ば'), new('ひ', 'び'), new('ふ', 'ぶ'), new('へ', 'べ'), new('ほ', 'ぼ'),
        new('う', 'ゔ')
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<char, char> s_kanaFinalVowelDict = new KeyValuePair<char, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        //Hiragana
        new('あ', 'あ'), new('か', 'あ'), new('さ', 'あ'), new('た', 'あ'), new('な', 'あ'), new('は', 'あ'),
        new('ま', 'あ'), new('ら', 'あ'), new('が', 'あ'), new('ざ', 'あ'), new('だ', 'あ'), new('ば', 'あ'),
        new('ぱ', 'あ'), new('わ', 'あ'), new('や', 'あ'), new('ぁ', 'あ'), new('ゃ', 'あ'), new('ゕ', 'あ'),
        new('ゎ', 'あ'),

        new('い', 'い'), new('き', 'い'), new('し', 'い'), new('ち', 'い'), new('に', 'い'), new('ひ', 'い'),
        new('み', 'い'), new('り', 'い'), new('ぎ', 'い'), new('じ', 'い'), new('ぢ', 'い'), new('び', 'い'),
        new('ぴ', 'い'), new('ぃ', 'い'), new('ゐ', 'い'),

        new('う', 'う'), new('く', 'う'), new('す', 'う'), new('つ', 'う'), new('ぬ', 'う'), new('ふ', 'う'),
        new('む', 'う'), new('る', 'う'), new('ぐ', 'う'), new('ず', 'う'), new('づ', 'う'), new('ぶ', 'う'),
        new('ぷ', 'う'), new('ゆ', 'う'), new('ぅ', 'う'), new('ゅ', 'う'), new('ゔ', 'う'),

        new('え', 'え'), new('け', 'え'), new('せ', 'え'), new('て', 'え'), new('ね', 'え'), new('へ', 'え'),
        new('め', 'え'), new('れ', 'え'), new('げ', 'え'), new('ぜ', 'え'), new('で', 'え'), new('べ', 'え'),
        new('ぺ', 'え'), new('ぇ', 'え'), new('ゖ', 'え'), new('ゑ', 'え'),

        new('お', 'お'), new('こ', 'お'), new('そ', 'お'), new('と', 'お'), new('の', 'お'), new('ほ', 'お'),
        new('も', 'お'), new('ろ', 'お'), new('ご', 'お'), new('ぞ', 'お'), new('ど', 'お'), new('ぼ', 'お'),
        new('ぽ', 'お'), new('を', 'お'), new('よ', 'お'), new('ぉ', 'お'), new('ょ', 'お'),
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, char> s_supplementaryNormalizationDict = new KeyValuePair<string, char>[]
    {
        // Kyuujitai to Shinjitai
        new("姬", '姫'),
        new("𦤶", '致'),

        // Hentaigana to Hiragana
        new("𛀂", 'あ'), new("𛀃", 'あ'), new("𛀄", 'あ'), new("𛀅", 'あ'), new("𛀆", 'い'), new("𛀇", 'い'), new("𛀈", 'い'),
        new("𛀉", 'い'), new("𛀊", 'う'),new("𛀋", 'う'), new("𛀌", 'う'), new("𛀍", 'う'), new("𛀎", 'う'), new("𛀁", 'え'),
        new("𛀏", 'え'), new("𛀐", 'え'), new("𛀑", 'え'), new("𛀒", 'え'), new("𛀓", 'え'), new("𛀔", 'お'), new("𛀕", 'お'),
        new("𛀖", 'お'), new("𛀗", 'か'), new("𛀘", 'か'), new("𛀙", 'か'), new("𛀚", 'か'), new("𛀛", 'か'), new("𛀜", 'か'),
        new("𛀝", 'か'), new("𛀞", 'か'), new("𛀟", 'か'), new("𛀠", 'か'), new("𛀡", 'か'), new("𛀢", 'か'), new("𛀣", 'き'),
        new("𛀤", 'き'), new("𛀥", 'き'), new("𛀦", 'き'), new("𛀧", 'き'), new("𛀨", 'き'), new("𛀩", 'き'), new("𛀪", 'き'),
        new("𛀫", 'く'), new("𛀬", 'く'), new("𛀭", 'く'), new("𛀮", 'く'), new("𛀯", 'く'), new("𛀰", 'く'), new("𛀱", 'く'),
        new("𛀲", 'け'), new("𛀳", 'け'), new("𛀴", 'け'), new("𛀵", 'け'), new("𛀶", 'け'), new("𛀷", 'け'), new("𛀸", 'こ'),
        new("𛀹", 'こ'), new("𛀺", 'こ'), new("𛀻", 'こ'), new("𛀼", 'さ'), new("𛀽", 'さ'), new("𛀾", 'さ'), new("𛀿", 'さ'),
        new("𛁀", 'さ'), new("𛁁", 'さ'), new("𛁂", 'さ'), new("𛁃", 'さ'), new("𛁄", 'し'), new("𛁅", 'し'), new("𛁆", 'し'),
        new("𛁇", 'し'), new("𛁈", 'し'), new("𛁉", 'し'), new("𛁊", 'す'), new("𛁋", 'す'), new("𛁌", 'す'), new("𛁍", 'す'),
        new("𛁎", 'す'), new("𛁏", 'す'), new("𛁐", 'す'), new("𛁑", 'す'), new("𛁒", 'せ'), new("𛁓", 'せ'), new("𛁔", 'せ'),
        new("𛁕", 'せ'), new("𛁖", 'せ'), new("𛁗", 'そ'), new("𛁘", 'そ'), new("𛁙", 'そ'), new("𛁚", 'そ'), new("𛁛", 'そ'),
        new("𛁜", 'そ'), new("𛁝", 'そ'), new("𛁞", 'た'), new("𛁟", 'た'), new("𛁠", 'た'), new("𛁡", 'た'), new("𛁢", 'ち'),
        new("𛁣", 'ち'), new("𛁤", 'ち'), new("𛁥", 'ち'), new("𛁦", 'ち'), new("𛁧", 'ち'), new("𛁨", 'ち'), new("𛁩", 'つ'),
        new("𛁪", 'つ'), new("𛁫", 'つ'), new("𛁬", 'つ'), new("𛁭", 'つ'), new("𛁮", 'て'), new("𛁯", 'て'), new("𛁰", 'て'),
        new("𛁱", 'て'), new("𛁲", 'て'), new("𛁳", 'て'), new("𛁴", 'て'), new("𛁵", 'て'), new("𛁶", 'て'), new("𛁷", 'と'),
        new("𛁸", 'と'), new("𛁹", 'と'), new("𛁺", 'と'), new("𛁻", 'と'), new("𛁼", 'と'), new("𛁽", 'と'), new("𛁾", 'な'),
        new("𛁿", 'な'), new("𛂀", 'な'), new("𛂁", 'な'), new("𛂂", 'な'), new("𛂃", 'な'), new("𛂄", 'な'), new("𛂅", 'な'),
        new("𛂆", 'な'), new("𛂇", 'に'), new("𛂈", 'に'), new("𛂉", 'に'), new("𛂊", 'に'), new("𛂋", 'に'), new("𛂌", 'に'),
        new("𛂍", 'に'), new("𛂎", 'に'), new("𛂏", 'ぬ'), new("𛂐", 'ぬ'), new("𛂑", 'ぬ'), new("𛂒", 'ね'), new("𛂓", 'ね'),
        new("𛂔", 'ね'), new("𛂕", 'ね'), new("𛂖", 'ね'), new("𛂗", 'ね'), new("𛂘", 'ね'), new("𛂙", 'の'), new("𛂚", 'の'),
        new("𛂛", 'の'), new("𛂜", 'の'), new("𛂝", 'の'), new("𛂞", 'の'), new("𛂟", 'は'), new("𛂠", 'は'), new("𛂡", 'は'),
        new("𛂢", 'は'), new("𛂣", 'は'), new("𛂤", 'は'), new("𛂥", 'は'), new("𛂦", 'は'), new("𛂧", 'は'), new("𛂨", 'は'),
        new("𛂩", 'ひ'), new("𛂪", 'ひ'), new("𛂫", 'ひ'), new("𛂬", 'ひ'), new("𛂭", 'ひ'), new("𛂮", 'ひ'), new("𛂯", 'ひ'),
        new("𛂰", 'ふ'), new("𛂱", 'ふ'), new("𛂲", 'ふ'), new("𛂳", 'へ'), new("𛂴", 'へ'), new("𛂵", 'へ'), new("𛂶", 'へ'),
        new("𛂷", 'へ'), new("𛂸", 'へ'), new("𛂹", 'へ'), new("𛂺", 'ほ'), new("𛂻", 'ほ'), new("𛂼", 'ほ'), new("𛂽", 'ほ'),
        new("𛂾", 'ほ'), new("𛂿", 'ほ'), new("𛃀", 'ほ'), new("𛃁", 'ほ'), new("𛃂", 'ま'), new("𛃃", 'ま'), new("𛃄", 'ま'),
        new("𛃅", 'ま'), new("𛃆", 'ま'), new("𛃇", 'ま'), new("𛃈", 'ま'), new("𛃉", 'み'), new("𛃊", 'み'), new("𛃋", 'み'),
        new("𛃌", 'み'), new("𛃍", 'み'), new("𛃎", 'み'), new("𛃏", 'み'), new("𛃐", 'む'), new("𛃑", 'む'), new("𛃒", 'む'),
        new("𛃓", 'む'), new("𛃔", 'め'), new("𛃕", 'め'), new("𛃖", 'め'), new("𛃗", 'も'), new("𛃘", 'も'), new("𛃙", 'も'),
        new("𛃚", 'も'), new("𛃛", 'も'), new("𛃜", 'も'), new("𛃝", 'や'), new("𛃞", 'や'), new("𛃟", 'や'), new("𛃠", 'や'),
        new("𛃡", 'や'), new("𛃢", 'や'), new("𛃣", 'ゆ'), new("𛃤", 'ゆ'), new("𛃥", 'ゆ'), new("𛃦", 'ゆ'), new("𛃧", 'よ'),
        new("𛃨", 'よ'), new("𛃩", 'よ'), new("𛃪", 'よ'), new("𛃫", 'よ'), new("𛃬", 'よ'), new("𛃭", 'ら'), new("𛃮", 'ら'),
        new("𛃯", 'ら'), new("𛃰", 'ら'), new("𛃱", 'り'), new("𛃲", 'り'), new("𛃳", 'り'), new("𛃴", 'り'), new("𛃵", 'り'),
        new("𛃶", 'り'), new("𛃷", 'り'), new("𛃸", 'る'), new("𛃹", 'る'), new("𛃺", 'る'), new("𛃻", 'る'), new("𛃼", 'る'),
        new("𛃽", 'る'), new("𛃾", 'れ'), new("𛃿", 'れ'), new("𛄀", 'れ'), new("𛄁", 'れ'), new("𛄂", 'ろ'), new("𛄃", 'ろ'),
        new("𛄄", 'ろ'), new("𛄅", 'ろ'), new("𛄆", 'ろ'), new("𛄇", 'ろ'), new("𛄈", 'わ'), new("𛄉", 'わ'), new("𛄊", 'わ'),
        new("𛄋", 'わ'), new("𛄌", 'わ'), new("𛄍", 'ゐ'), new("𛄎", 'ゐ'), new("𛄏", 'ゐ'), new("𛄐", 'ゐ'), new("𛄑", 'ゐ'),
        new("𛄒", 'ゑ'), new("𛄓", 'ゑ'), new("𛄔", 'ゑ'), new("𛄕", 'ゑ'), new("𛄖", 'を'), new("𛄗", 'を'), new("𛄘", 'を'),
        new("𛄙", 'を'), new("𛄚", 'を'), new("𛄛", 'を'), new("𛄜", 'を'), new("𛄝", 'ん'), new("𛄞", 'ん')
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, char>.AlternateLookup<ReadOnlySpan<char>> s_supplementaryNormalizationDictAlternativeLookup = s_supplementaryNormalizationDict.GetAlternateLookup<ReadOnlySpan<char>>();

    public static readonly FrozenDictionary<char, char> SmallVowelHiraganaToFinalVowelDict = new KeyValuePair<char, char>[]
    {
        new('ぁ', 'あ'),
        new('ぃ', 'い'),
        new('ぅ', 'う'),
        new('ぇ', 'え'),
        new('ぉ', 'お'),
    }.ToFrozenDictionary();

    public static readonly SearchValues<char> SmallCombiningKanaSet = SearchValues.Create('ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ヮ', 'ャ', 'ュ', 'ョ', 'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゎ', 'ゃ', 'ゅ', 'ょ');

    private static readonly char[] s_sentenceTerminatingCharacters =
    [
        '。',
        '！',
        '？',
        '…',
        '‥',
        '︒',
        '!',
        '?',
        '︙',
        '︰',
        '\u001E',
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

    internal static readonly SearchValues<char> s_fuseji = SearchValues.Create(NormalizedFuseji, '〇', '◯', '●', '⬤', '◎', '◉', '□', '■', '×', '◇', '◆', '△', '▲', '▽', '▼', '※', '*', '#');
    internal static readonly SearchValues<char> s_longVowelMarkChars = SearchValues.Create('ー', '〜', '~');
    private static readonly SearchValues<char> s_charsToStrip = SearchValues.Create(' ', '・', '.', '·', '=', '゠', '☆', '★', '†', '‡', '♥', '♡');
    private static readonly SearchValues<char> s_longVowelMarksAndSmallVowelHiragana = SearchValues.Create(
    [
        'ー', '〜', '~', .. SmallVowelHiraganaToFinalVowelDict.Keys
    ]);

    private static char[] BuildHighSurrogates(FrozenDictionary<string, char> supplementaryDict)
    {
        char[] highSurrogates = new char[supplementaryDict.Count];
        int i = 0;
        foreach (string key in supplementaryDict.Keys)
        {
            highSurrogates[i] = key[0];
            ++i;
        }

        return highSurrogates;
    }

    private static char[] BuildVariationSelectorRange()
    {
        char[] chars = new char[VariationSelectorRangeEnd - VariationSelectorRangeStart + 1];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)(VariationSelectorRangeStart + i);
        }

        return chars;
    }

    private static readonly SearchValues<char> s_charactersToNormalize = SearchValues.Create(
    [
        '々', '〻', 'ゝ', 'ゞ', // IsIterationMark
        NormalizedFuseji, '〇', '◯', '●', '⬤', '◎', '◉', '□', '■', '×', '◇', '◆', '△', '▲', '▽', '▼', '※', '*', '#', // s_fuseji
        ' ', '・', '.', '·', '=', '゠', '☆', '★', '†', '‡', '♥', '♡', // s_charsToStrip
        VariationSelectorSupplementHighSurrogate,
        .. SmallVowelHiraganaToFinalVowelDict.Keys,
        .. s_normalizationDict.Keys,
        .. BuildHighSurrogates(s_supplementaryNormalizationDict),
        .. BuildVariationSelectorRange()
    ]);

    public static string NormalizeText(string text)
    {
        string normalizedText = text;
        if (!normalizedText.IsNormalized(NormalizationForm.FormKC))
        {
            // Normalizes ＯＬ to OL, fullwidth space to halfwidth space, ｶﾞ to が, ﾜ to わ, ㍿ to 株式会社, ～ to ~, Kangxi radicals to their corresponding kanji etc.
            normalizedText = normalizedText.Normalize(NormalizationForm.FormKC);
        }

        // TODO: Benchmark SearchKey<char>.ContainsAny vs ContainsAnyInRange
        if (normalizedText.ContainsAnyInRange('a', 'z'))
        {
            // TODO: When migrating to .NET 11, use ToUpperOrdinal instead
            // Normalizes vs to VS, xxx to XXX, h to H etc.
            normalizedText = normalizedText.ToUpperInvariant();
        }

        int normalizationStartOffset = normalizedText.IndexOfAny(s_charactersToNormalize);
        if (normalizationStartOffset < 0)
        {
            return normalizedText;
        }

        // TODO: Benchmark char[] (both stackalloc + ArrayPool) vs StringBuilder
        StringBuilder normalizedTextBuilder = ObjectPoolManager.StringBuilderPool.Get().Append(normalizedText.AsSpan()[..normalizationStartOffset]);
        for (int i = normalizationStartOffset; i < normalizedText.Length; i++)
        {
            char character = normalizedText[i];
            if (character is >= VariationSelectorRangeStart and <= VariationSelectorRangeEnd)
            {
                continue;
            }

            bool nonLastChar = i + 1 < normalizedText.Length;
            if (nonLastChar)
            {
                if (char.IsHighSurrogate(character))
                {
                    if (character is VariationSelectorSupplementHighSurrogate
                        && normalizedText[i + 1] is >= VariationSelectorSupplementLowSurrogateRangeStart and <= VariationSelectorSupplementLowSurrogateRangeEnd)
                    {
                        ++i;
                        continue;
                    }

                    ReadOnlySpan<char> runeSpan = normalizedText.AsSpan(i, 2);
                    if (s_supplementaryNormalizationDictAlternativeLookup.TryGetValue(runeSpan, out char normalizedSupplementaryChar))
                    {
                        _ = normalizedTextBuilder.Append(normalizedSupplementaryChar);
                        ++i;
                        continue;
                    }
                    else
                    {
                        _ = normalizedTextBuilder.Append(runeSpan);
                        if (i + 2 < normalizedText.Length)
                        {
                            ReadOnlySpan<char> remainingSpan = normalizedText.AsSpan(i + 2);
                            normalizationStartOffset = remainingSpan.IndexOfAny(s_charactersToNormalize);
                            if (normalizationStartOffset < 0)
                            {
                                _ = normalizedTextBuilder.Append(remainingSpan);
                                break;
                            }

                            if (normalizationStartOffset > 0)
                            {
                                _ = normalizedTextBuilder.Append(normalizedText.AsSpan(i + 2, normalizationStartOffset));
                            }

                            i += 1 + normalizationStartOffset;
                        }
                        else
                        {
                            ++i;
                        }

                        continue;
                    }
                }

                if (i > 0 && s_charsToStrip.Contains(character))
                {
                    continue;
                }
            }

            if (s_normalizationDict.TryGetValue(character, out char normalizedChar))
            {
                if (IsIterationMark(normalizedChar))
                {
                    AppendIterationMark(normalizedTextBuilder, normalizedChar);
                }
                else if (!IsElongationRepeat(normalizedTextBuilder, normalizedChar))
                {
                    _ = normalizedTextBuilder.Append(normalizedChar);
                }
            }
            else
            {
                if (IsIterationMark(character))
                {
                    AppendIterationMark(normalizedTextBuilder, character);
                }
                else if (s_fuseji.Contains(character))
                {
                    _ = normalizedTextBuilder.Append(NormalizedFuseji);
                }
                else if (!IsElongationRepeat(normalizedTextBuilder, character))
                {
                    _ = normalizedTextBuilder.Append(character);

                    if (nonLastChar)
                    {
                        ReadOnlySpan<char> remainingSpan = normalizedText.AsSpan(i + 1);
                        normalizationStartOffset = remainingSpan.IndexOfAny(s_charactersToNormalize);
                        if (normalizationStartOffset < 0)
                        {
                            _ = normalizedTextBuilder.Append(remainingSpan);
                            break;
                        }

                        if (normalizationStartOffset > 0)
                        {
                            _ = normalizedTextBuilder.Append(normalizedText.AsSpan(i + 1, normalizationStartOffset));
                        }

                        i += normalizationStartOffset;
                    }
                }
            }
        }

        string textInHiragana = normalizedTextBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(normalizedTextBuilder);
        return textInHiragana;
    }

    private static bool IsElongationRepeat(StringBuilder builder, char character)
    {
        return builder.Length > 0
            && SmallVowelHiraganaToFinalVowelDict.TryGetValue(character, out char vowel)
            && s_kanaFinalVowelDict.TryGetValue(builder[^1], out char previousVowel)
            && vowel == previousVowel;
    }

    private static bool IsIterationMark(char character)
    {
        return character is '々' or '〻' or 'ゝ' or 'ゞ';
    }

    private static void AppendIterationMark(StringBuilder builder, char iterationMark)
    {
        if (builder.Length is 0)
        {
            _ = builder.Append(iterationMark);
            return;
        }

        if (iterationMark is 'ゞ')
        {
            char previousChar = builder[^1];
            if (s_hiraganaToDakutenDict.TryGetValue(previousChar, out char dakuten))
            {
                _ = builder.Append(dakuten);
            }
            else
            {
                _ = builder.Append(iterationMark);
            }
        }
        else
        {
            char previousChar = builder[^1];
            if (char.IsLowSurrogate(previousChar) && builder.Length > 1)
            {
                _ = builder.Append(builder[^2]).Append(previousChar);
            }
            else if (previousChar is not '\u3099' and not '\u309A')
            {
                _ = builder.Append(previousChar);
            }
            else if (builder.Length > 1)
            {
                char twoPreviousChar = builder[^2];
                if (!char.IsLowSurrogate(twoPreviousChar))
                {
                    _ = builder.Append(twoPreviousChar).Append(previousChar);
                }
                else
                {
                    _ = builder.Append(iterationMark);
                }
            }
            else
            {
                _ = builder.Append(iterationMark);
            }
        }
    }

    internal static List<string> NormalizeLongVowelMark(ReadOnlySpan<char> text)
    {
        List<StringBuilder> stringBuilders = new(4)
        {
            ObjectPoolManager.StringBuilderPool.Get().Append(text[0])
        };

        for (int i = 1; i < text.Length; i++)
        {
            char currentCharacter = text[i];
            if (s_longVowelMarkChars.Contains(currentCharacter) && s_kanaFinalVowelDict.TryGetValue(text[i - 1], out char vowel))
            {
                while (i + 1 < text.Length)
                {
                    char nextCharacter = text[i + 1];
                    if (s_longVowelMarksAndSmallVowelHiragana.Contains(nextCharacter))
                    {
                        ++i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (vowel is not 'お' and not 'え')
                {
                    foreach (ref readonly StringBuilder stringBuilder in stringBuilders.AsReadOnlySpan())
                    {
                        _ = stringBuilder.Append(vowel);
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

                    int stringBuildersCount = stringBuilders.Count;
                    for (int j = 0; j < stringBuildersCount; j++)
                    {
                        stringBuilders.Add(ObjectPoolManager.StringBuilderPool.Get().Append(stringBuilders[j]));
                    }

                    stringBuildersCount = stringBuilders.Count;
                    ReadOnlySpan<StringBuilder> stringBuildersSpan = stringBuilders.AsReadOnlySpan();
                    for (int j = 0; j < stringBuildersSpan.Length; j++)
                    {
                        _ = stringBuildersSpan[j].Append(j < stringBuildersCount / 2 ? vowel : alternativeVowel);
                    }
                }
            }
            else
            {
                foreach (ref readonly StringBuilder stringBuilder in stringBuilders.AsReadOnlySpan())
                {
                    _ = stringBuilder.Append(currentCharacter);
                }
            }
        }

        List<string> longVowelMarkToKanaList = new(stringBuilders.Count);
        foreach (ref readonly StringBuilder stringBuilder in stringBuilders.AsReadOnlySpan())
        {
            longVowelMarkToKanaList.Add(stringBuilder.ToString());
            ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
        }

        return longVowelMarkToKanaList;
    }

    internal static ReadOnlySpan<string> CreateCombinedForm(ReadOnlySpan<char> text)
    {
        List<string> combinedForm = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && SmallCombiningKanaSet.Contains(text[i + 1]))
            {
                combinedForm.Add(text.Slice(i, 2).ToString());
                ++i;
            }
            else
            {
                combinedForm.Add(text[i].ToString());
            }
        }

        return combinedForm.AsReadOnlySpan();
    }

    internal static int GetCombinedFormLength(ReadOnlySpan<char> text)
    {
        int length = 0;
        for (int i = 0; i < text.Length; i++)
        {
            ++length;
            if (i < text.Length - 1 && SmallCombiningKanaSet.Contains(text[i + 1]))
            {
                ++i;
            }
        }

        return length;
    }

    // Katakana (30A0–30FF)
    // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included
    // Halfwidth Katakana (FF66-FF9D)
    internal static bool IsKatakana(char character)
    {
        int codePoint = character;
        return codePoint is (>= 0x30A0 and <= 0x31FF) or (>= 0xFF66 and <= 0xFF9D);
    }

    // Hiragana (3040–309F)
    // Katakana (30A0–30FF)
    // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included
    // Halfwidth Katakana (FF66-FF9D)
    private static bool ContainsKana(ReadOnlySpan<char> text)
    {
        int textLength = text.Length;
        for (int i = 0; i < textLength; i++)
        {
            int codePoint = text[i];
            if (codePoint is (>= 0x3040 and <= 0x31FF) or (>= 0xFF66 and <= 0xFF9D))
            {
                return true;
            }
        }

        return false;
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

        foreach (char terminatingCharacter in s_sentenceTerminatingCharacters)
        {
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

        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get().Append(text[..index]);
        foreach (Rune rune in text.AsSpan(index + 1).EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                _ = sb.Append(rune);
            }
        }

        string textWithoutPunctuation = sb.ToString();
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return textWithoutPunctuation;
    }

    internal static string GetPrimarySpellingAndReadingMapping(string primarySpelling, string reading)
    {
        if (!ContainsKana(primarySpelling))
        {
            return $"{primarySpelling}[{reading}]";
        }

        List<string> primarySpellingSegments = new(primarySpelling.Length);
        bool wasKana = true;
        foreach (ref readonly string rune in primarySpelling.AsSpan().ListUnicodeCharacters())
        {
            bool isKana = ContainsKana(rune);
            if (primarySpellingSegments.Count is 0 || wasKana != isKana)
            {
                wasKana = isKana;
                primarySpellingSegments.Add(rune);
            }
            else
            {
                primarySpellingSegments[^1] += rune;
            }
        }

        string? result = GetPrimarySpellingAndReadingMapping(primarySpellingSegments.AsReadOnlySpan(), reading);
        return result ?? $"{primarySpelling}[{reading}]";
    }

    private static string? GetPrimarySpellingAndReadingMapping(ReadOnlySpan<string> primarySpellingSegments, string reading)
    {
        StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();

        bool firstSegmentIsKana = ContainsKana(primarySpellingSegments[0]);
        int currentReadingPosition = firstSegmentIsKana ? 0 : 1;
        for (int i = currentReadingPosition; i < primarySpellingSegments.Length; i += 2)
        {
            ref readonly string segment = ref primarySpellingSegments[i];
            int searchLength = reading.Length - currentReadingPosition - primarySpellingSegments.Length + i + 1;
            if (searchLength < 0)
            {
                searchLength = reading.Length - currentReadingPosition;
            }

            ReadOnlySpan<int> indexes = reading.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segment);
            bool hasKatakana = false;
            int index = -1;
            if (indexes.Length is 0)
            {
                string readingInHiragana = NormalizeText(reading);
                if (readingInHiragana.Length != reading.Length)
                {
                    ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                    return null;
                }
                string segmentInHiragana = NormalizeText(segment);
                if (segmentInHiragana.Length != segment.Length)
                {
                    ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                    return null;
                }

                indexes = readingInHiragana.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segmentInHiragana);
                hasKatakana = true;
            }

            if (indexes.Length is 0)
            {
                ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                return null;
            }

            if (indexes.Length is 1)
            {
                index = indexes[0];
            }
            else
            {
                foreach (int currentIndex in indexes)
                {
                    if (currentIndex is 0 && i is 0)
                    {
                        index = 0;
                        break;
                    }

                    if (i + 1 == primarySpellingSegments.Length)
                    {
                        if (currentIndex + segment.Length == reading.Length)
                        {
                            index = currentIndex;
                            break;
                        }
                    }
                    else if (i + 2 < primarySpellingSegments.Length)
                    {
                        bool unambiguous = IsPrimarySpellingAndReadingMappingUnambiguous(primarySpellingSegments[(i + 2)..], reading[(currentIndex + segment.Length + 1)..]);
                        if (unambiguous)
                        {
                            if (index >= 0)
                            {
                                ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                                return null;
                            }

                            index = currentIndex;
                        }
                    }
                    else
                    {
                        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                        return null;
                    }
                }
            }

            if (index < 0)
            {
                ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
                return null;
            }

            if (i > 0)
            {
                _ = stringBuilder.Append(primarySpellingSegments[i - 1]).Append('[').Append(reading.AsSpan()[(currentReadingPosition - 1)..index]).Append(']');
            }

            if (hasKatakana)
            {
                _ = stringBuilder.Append(segment).Append('[').Append(reading.AsSpan()[index..(index + segment.Length)]).Append(']');
            }
            else
            {
                _ = stringBuilder.Append(segment);
                if (i + 2 <= primarySpellingSegments.Length)
                {
                    _ = stringBuilder.Append(' ');
                }
            }

            currentReadingPosition = index + segment.Length;

            if (i + 2 == primarySpellingSegments.Length)
            {
                _ = stringBuilder.Append(primarySpellingSegments[i + 1]).Append('[').Append(reading.AsSpan()[currentReadingPosition..]).Append(']');
            }

            ++currentReadingPosition;
        }

        string primarySpellingAndReadingMapping = stringBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
        return primarySpellingAndReadingMapping;
    }

    private static bool IsPrimarySpellingAndReadingMappingUnambiguous(ReadOnlySpan<string> primarySpellingSegments, string reading)
    {
        bool firstSegmentIsKana = ContainsKana(primarySpellingSegments[0]);
        int currentReadingPosition = firstSegmentIsKana ? 0 : 1;
        for (int i = currentReadingPosition; i < primarySpellingSegments.Length; i += 2)
        {
            ref readonly string segment = ref primarySpellingSegments[i];
            int searchLength = reading.Length - currentReadingPosition - primarySpellingSegments.Length + i + 1;
            if (searchLength < 0)
            {
                searchLength = reading.Length - currentReadingPosition;
            }

            ReadOnlySpan<int> indexes = reading.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segment);
            int index = -1;
            if (indexes.Length is 0)
            {
                string readingInHiragana = NormalizeText(reading);
                if (readingInHiragana.Length != reading.Length)
                {
                    return false;
                }
                string segmentInHiragana = NormalizeText(segment);
                if (segmentInHiragana.Length != segment.Length)
                {
                    return false;
                }

                indexes = readingInHiragana.AsSpan().FindAllIndexes(currentReadingPosition, searchLength, segmentInHiragana);
            }

            if (indexes.Length is 0)
            {
                return false;
            }

            if (indexes.Length is 1)
            {
                index = indexes[0];
            }
            else
            {
                foreach (int currentIndex in indexes)
                {
                    if (currentIndex is 0 && i is 0)
                    {
                        index = 0;
                        break;
                    }

                    if (i + 1 == primarySpellingSegments.Length)
                    {
                        if (currentIndex + segment.Length == reading.Length)
                        {
                            index = currentIndex;
                            break;
                        }
                    }
                    else if (i + 2 < primarySpellingSegments.Length)
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

    public static bool ContainsJapaneseCharacters(params ReadOnlySpan<char> text)
    {
        // The regex approach is faster if the text is longer than 15 characters and does not start with a Japanese character
        if (text.Length > 15)
        {
            bool isFirstCharacterJapanese;
            char firstChar = text[0];
            if (!char.IsHighSurrogate(firstChar))
            {
                isFirstCharacterJapanese = IsJapaneseCharacter(firstChar);
            }
            else
            {
                Debug.Assert(text.Length > 1);
                char secondChar = text[1];
                isFirstCharacterJapanese = IsJapaneseCharacter(firstChar, secondChar);
            }

            return isFirstCharacterJapanese || JapaneseRegex.IsMatch(text);
        }

        return ContainsJapaneseCharactersHelper(text);
    }

    private static bool ContainsJapaneseCharactersHelper(params ReadOnlySpan<char> text)
    {
        int textLength = text.Length;
        for (int i = 0; i < textLength; i++)
        {
            char currentChar = text[i];
            if (!char.IsHighSurrogate(currentChar))
            {
                if (IsJapaneseCharacter(currentChar))
                {
                    return true;
                }
            }
            else
            {
                Debug.Assert(textLength > i + 1);
                char nextChar = text[i + 1];

                if (IsJapaneseCharacter(currentChar, nextChar))
                {
                    return true;
                }

                i += 1;
            }
        }

        return false;
    }

    private static bool IsJapaneseCharacter(char codePoint)
    {
        Debug.Assert(!char.IsHighSurrogate(codePoint) && !char.IsLowSurrogate(codePoint));

        // Katakana Phonetic Extensions (31F0–31FF): The range is mainly for Ainu, but some characters like ㇲ and ト are occasionally used in Japanese, so it's included
        return (ushort)codePoint is (>= 0x2FF0 and <= 0x30FF) // Ideographic Description Characters (2FF0–2FFF), CJK Symbols and Punctuation (3000–303F), Hiragana (3040–309F), Katakana (30A0–30FF)
            or (>= 0x4E00 and <= 0x9FFF) // CJK Unified Ideographs (4E00–9FFF)
            or 0x00D7 // × (\u00D7)
            or (>= 0x2000 and <= 0x206F) // General Punctuation (2000-206F): ‥, …, •, ※
            or (>= 0x25A0 and <= 0x25FF) // Geometric Shapes (25A0-U+25FF): ◦, ◎, ○, △, ◉
            or (>= 0x2E80 and <= 0x2FDF) // CJK Radicals Supplement (2E80–2EFF), Kangxi Radicals (2F00–2FDF)
            or (>= 0x3190 and <= 0x319F) // Kanbun (3190–319F)
            or (>= 0x31C0 and <= 0x325F) // CJK Strokes (31C0–31EF), Katakana Phonetic Extensions (31F0–31FF), Enclosed CJK Letters and Months 3220-325F
            or (>= 0x3280 and <= 0x4DBF) // Enclosed CJK Letters and Months 3280-32FF, CJK Compatibility (3300–33FF), CJK Unified Ideographs Extension A (3400–4DBF)
            or (>= 0xF900 and <= 0xFAFF) // CJK Compatibility Ideographs (F900–FAFF)
            or (>= 0xFE10 and <= 0xFE1F) // Vertical Forms (FE10–FE1F)
            or (>= 0xFE30 and <= 0xFE4F) // CJK Compatibility Forms (FE30–FE4F)
            or (>= 0xFF00 and <= 0xFF9F) // Halfwidth and Fullwidth Forms FF00-FF9F
            or (>= 0xFFE0 and <= 0xFFEF); // Halfwidth and Fullwidth Forms FFE0-FFEF
    }

    private static bool IsJapaneseCharacter(char firstChar, char secondChar)
    {
        Debug.Assert(char.IsHighSurrogate(firstChar));
        Debug.Assert(char.IsLowSurrogate(secondChar));
        int codePoint = char.ConvertToUtf32(firstChar, secondChar);

        // Ideographic Symbols and Punctuation (16FE0-16FFF): It does not contain any Japanese characters, so it's not included
        // Kana Extended-B (1AFF0-1AFFF): The range does not contain any Japanese characters; it only includes Taiwanese kana, so it's not included
        // CJK Unified Ideographs Extension I (2EBF0–2EE5F): It's a Chinese-only range, so it's not included in the regex.
        return codePoint is (>= 0x1B000 and <= 0x1B16F) // Kana Supplement (1B000-1B0FF), Kana Extended-A (1B100-1B12F), Small Kana Extension (1B130-1B16F)
            or (>= 0x1F200 and <= 0x1F2FF) // Enclosed Ideographic Supplement (1F200-1F2FF)
            or (>= 0x20000 and <= 0x2A6DF) // CJK Unified Ideographs Extension B (20000–2A6DF)
            or (>= 0x2A700 and <= 0x2EBEF) // CJK Unified Ideographs Extension C (2A700–2B73F), CJK Unified Ideographs Extension D (2B740–2B81F), CJK Unified Ideographs Extension E (2B820–2CEAF), CJK Unified Ideographs Extension F (2CEB0–2EBEF)
            or (>= 0x2F800 and <= 0x2FA1F) // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
            or (>= 0x30000 and <= 0x3347F) // CJK Unified Ideographs Extension G (30000–3134F), CJK Unified Ideographs Extension H (31350–323AF), CJK Unified Ideographs Extension J (323B0-3347F)
            or (>= 0x1D360 and <= 0x1D37F); // Counting Rod Numerals (1D360-1D37F)
    }

    private static bool IsKanji(char codePoint)
    {
        Debug.Assert(!char.IsHighSurrogate(codePoint) && !char.IsLowSurrogate(codePoint));
        return (ushort)codePoint is (>= 0x4E00 and <= 0x9FFF) // CJK Unified Ideographs (4E00–9FFF)
            or (>= 0x2E80 and <= 0x2FDF) // CJK Radicals Supplement (2E80–2EFF), Kangxi Radicals (2F00–2FDF)
            or (>= 0x3190 and <= 0x319F) // Kanbun (3190–319F)
            or (>= 0x3220 and <= 0x325F) // Enclosed CJK Letters and Months 3220-325F
            or (>= 0x3280 and <= 0x4DBF) // Enclosed CJK Letters and Months 3280-32FF, CJK Compatibility (3300–33FF), CJK Unified Ideographs Extension A (3400–4DBF)
            or (>= 0xF900 and <= 0xFAFF) // CJK Compatibility Ideographs (F900–FAFF)
            or (>= 0xFE10 and <= 0xFE1F) // Vertical Forms (FE10–FE1F)
            or (>= 0xFE30 and <= 0xFE4F); // CJK Compatibility Forms (FE30–FE4F)
    }

    private static bool IsKanji(int codePoint)
    {
        return codePoint is (>= 0x1F200 and <= 0x1F2FF) // Enclosed Ideographic Supplement (1F200-1F2FF)
                or (>= 0x20000 and <= 0x2A6DF) // CJK Unified Ideographs Extension B (20000–2A6DF)
                or (>= 0x2A700 and <= 0x2EBEF) // CJK Unified Ideographs Extension C (2A700–2B73F), CJK Unified Ideographs Extension D (2B740–2B81F), CJK Unified Ideographs Extension E (2B820–2CEAF), CJK Unified Ideographs Extension F (2CEB0–2EBEF)
                or (>= 0x2F800 and <= 0x2FA1F) // CJK Compatibility Ideographs Supplement (2F800–2FA1F)or (>= 0x1D360 and <= 0x1D37F) // Counting Rod Numerals (1D360-1D37F)
                or (>= 0x30000 and <= 0x3347F) // CJK Unified Ideographs Extension G (30000–3134F), CJK Unified Ideographs Extension H (31350–323AF), CJK Unified Ideographs Extension J (323B0-3347F)
                or (>= 0x1D360 and <= 0x1D37F); // Counting Rod Numerals (1D360-1D37F)
    }

    internal static string? GetFirstCharacterIfKanji(ReadOnlySpan<char> text)
    {
        char firstChar = text[0];
        if (!char.IsHighSurrogate(firstChar))
        {
            return IsKanji(firstChar)
                ? firstChar.ToString()
                : null;
        }

        Debug.Assert(text.Length > 1);
        char secondChar = text[1];

        Debug.Assert(char.IsHighSurrogate(firstChar));
        Debug.Assert(char.IsLowSurrogate(secondChar));
        int codePoint = char.ConvertToUtf32(firstChar, secondChar);

        return IsKanji(codePoint)
            ? char.ConvertFromUtf32(codePoint)
            : null;
    }
}
