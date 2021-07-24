﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    public static class Kana
    {
        private static readonly Dictionary<char, char> HiraganaToKatakanaDict = new()
        {
            { 'あ', 'ア' }, { 'い', 'イ' }, { 'う', 'ウ' }, { 'え', 'エ' }, { 'お', 'オ' },
            { 'か', 'カ' }, { 'き', 'キ' }, { 'く', 'ク' }, { 'け', 'ケ' }, { 'こ', 'コ' },
            { 'さ', 'サ' }, { 'し', 'シ' }, { 'す', 'ス' }, { 'せ', 'セ' }, { 'そ', 'ソ' },
            { 'た', 'タ' }, { 'ち', 'チ' }, { 'つ', 'ツ' }, { 'て', 'テ' }, { 'と', 'ト' },
            { 'な', 'ナ' }, { 'に', 'ニ' }, { 'ぬ', 'ヌ' }, { 'ね', 'ネ' }, { 'の', 'ノ' },
            { 'は', 'ハ' }, { 'ひ', 'ヒ' }, { 'へ', 'ヘ' }, { 'ふ', 'フ' }, { 'ほ', 'ホ' },
            { 'ま', 'マ' }, { 'み', 'ミ' }, { 'む', 'ム' }, { 'め', 'メ' }, { 'も', 'モ' },
            { 'ら', 'ラ' }, { 'り', 'リ' }, { 'る', 'ル' }, { 'れ', 'レ' }, { 'ろ', 'ロ' },

            { 'が', 'ガ' }, { 'ぎ', 'ギ' }, { 'ぐ', 'グ' }, { 'げ', 'ゲ' }, { 'ご', 'ゴ' },
            { 'ざ', 'ザ' }, { 'じ', 'ジ' }, { 'ず', 'ズ' }, { 'ぜ', 'ゼ' }, { 'ぞ', 'ゾ' },
            { 'だ', 'ダ' }, { 'ぢ', 'ヂ' }, { 'づ', 'ヅ' }, { 'で', 'デ' }, { 'ど', 'ド' },
            { 'ば', 'バ' }, { 'び', 'ビ' }, { 'ぶ', 'ブ' }, { 'べ', 'ベ' }, { 'ぼ', 'ボ' },
            { 'ぱ', 'パ' }, { 'ぴ', 'ピ' }, { 'ぷ', 'プ' }, { 'ぺ', 'ペ' }, { 'ぽ', 'ポ' },

            { 'わ', 'ワ' }, { 'を', 'ヲ' },
            { 'や', 'ヤ' }, { 'ゆ', 'ユ' }, { 'よ', 'ヨ' },
            { 'ん', 'ン' },

            { 'ぁ', 'ァ' }, { 'ぃ', 'ィ' }, { 'ぅ', 'ゥ' }, { 'ぇ', 'ェ' }, { 'ぉ', 'ォ' },
            { 'ゃ', 'ャ' }, { 'ゅ', 'ュ' }, { 'ょ', 'ョ' },

            { 'ゕ', 'ヵ' }, { 'ゖ', 'ヶ' }, { 'ゔ', 'ヴ' },
            { 'ゝ', 'ヽ' }, { 'ゞ', 'ヾ' }, { 'っ', 'ッ' }
        };

        private static readonly Dictionary<char, char> KatakanaToHiraganaDict = new()
        {
            { 'ア', 'あ' }, { 'イ', 'い' }, { 'ウ', 'う' }, { 'エ', 'え' }, { 'オ', 'お' },
            { 'カ', 'か' }, { 'キ', 'き' }, { 'ク', 'く' }, { 'ケ', 'け' }, { 'コ', 'こ' },
            { 'サ', 'さ' }, { 'シ', 'し' }, { 'ス', 'す' }, { 'セ', 'せ' }, { 'ソ', 'そ' },
            { 'タ', 'た' }, { 'チ', 'ち' }, { 'ツ', 'つ' }, { 'テ', 'て' }, { 'ト', 'と' },
            { 'ナ', 'な' }, { 'ニ', 'に' }, { 'ヌ', 'ぬ' }, { 'ネ', 'ね' }, { 'ノ', 'の' },
            { 'ハ', 'は' }, { 'ヒ', 'ひ' }, { 'ヘ', 'へ' }, { 'フ', 'ふ' }, { 'ホ', 'ほ' },
            { 'マ', 'ま' }, { 'ミ', 'み' }, { 'ム', 'む' }, { 'メ', 'め' }, { 'モ', 'も' },
            { 'ラ', 'ら' }, { 'リ', 'り' }, { 'ル', 'る' }, { 'レ', 'れ' }, { 'ロ', 'ろ' },

            { 'ガ', 'が' }, { 'ギ', 'ぎ' }, { 'グ', 'ぐ' }, { 'ゲ', 'げ' }, { 'ゴ', 'ご' },
            { 'ザ', 'ざ' }, { 'ジ', 'じ' }, { 'ズ', 'ず' }, { 'ゼ', 'ぜ' }, { 'ゾ', 'ぞ' },
            { 'ダ', 'だ' }, { 'ヂ', 'ぢ' }, { 'ヅ', 'づ' }, { 'デ', 'で' }, { 'ド', 'ど' },
            { 'バ', 'ば' }, { 'ビ', 'び' }, { 'ブ', 'ぶ' }, { 'ベ', 'べ' }, { 'ボ', 'ぼ' },
            { 'パ', 'ぱ' }, { 'ピ', 'ぴ' }, { 'プ', 'ぷ' }, { 'ペ', 'ぺ' }, { 'ポ', 'ぽ' },

            { 'ワ', 'わ' }, { 'ヲ', 'を' },
            { 'ヤ', 'や' }, { 'ユ', 'ゆ' }, { 'ヨ', 'よ' },
            { 'ン', 'ん' },

            { 'ァ', 'ぁ' }, { 'ィ', 'ぃ' }, { 'ゥ', 'ぅ' }, { 'ェ', 'ぇ' }, { 'ォ', 'ぉ' },
            { 'ャ', 'ゃ' }, { 'ュ', 'ゅ' }, { 'ョ', 'ょ' },

            { 'ヴ', 'ゔ' }, { 'ヵ', 'ゕ' }, { 'ヶ', 'ゖ' },
            { 'ヽ', 'ゝ' }, { 'ヾ', 'ゞ' }, { 'ッ', 'っ' }

            // {'ヸ','ゐ゙'}, { 'ヹ', 'ゑ゙' } { 'ヺ', 'を゙' }
            // Apparently ゐ゙, ゑ゙ and を゙ don't count as single characters.
        };

        private static readonly Dictionary<char, char> KanaFinalVowelDict = new()
        {
            //Katakana
            { 'ア', 'ア' }, { 'カ', 'ア' }, { 'サ', 'ア' }, { 'タ', 'ア' }, { 'ナ', 'ア' }, { 'ハ', 'ア' },
            { 'マ', 'ア' }, { 'ラ', 'ア' }, { 'ガ', 'ア' }, { 'ザ', 'ア' }, { 'ダ', 'ア' }, { 'バ', 'ア' },
            { 'パ', 'ア' }, { 'ワ', 'ア' }, { 'ヤ', 'ア' }, { 'ァ', 'ア' }, { 'ャ', 'ア' }, { 'ヵ', 'ア' },

            { 'イ', 'イ' }, { 'キ', 'イ' }, { 'シ', 'イ' }, { 'チ', 'イ' }, { 'ニ', 'イ' },
            { 'ヰ', 'イ' }, { 'ヒ', 'イ' }, { 'ミ', 'イ' }, { 'リ', 'イ' }, { 'ギ', 'イ' },
            { 'ジ', 'イ' }, { 'ヂ', 'イ' }, { 'ビ', 'イ' }, { 'ピ', 'イ' }, { 'ィ', 'イ' },

            { 'ウ', 'ウ' }, { 'ク', 'ウ' }, { 'ス', 'ウ' }, { 'ツ', 'ウ' }, { 'ヌ', 'ウ' }, { 'ヘ', 'ウ' },
            { 'ム', 'ウ' }, { 'ル', 'ウ' }, { 'グ', 'ウ' }, { 'ズ', 'ウ' }, { 'ヅ', 'ウ' }, { 'ブ', 'ウ' },
            { 'プ', 'ウ' }, { 'ユ', 'ウ' }, { 'ゥ', 'ウ' }, { 'ュ', 'ウ' }, { 'ヴ', 'ウ' },

            { 'エ', 'エ' }, { 'ケ', 'エ' }, { 'セ', 'エ' }, { 'テ', 'エ' }, { 'ネ', 'エ' }, { 'フ', 'エ' },
            { 'メ', 'エ' }, { 'レ', 'エ' }, { 'ゲ', 'エ' }, { 'ゼ', 'エ' }, { 'デ', 'エ' }, { 'ベ', 'エ' },
            { 'ペ', 'エ' }, { 'ヱ', 'エ' }, { 'ェ', 'エ' }, { 'ヶ', 'エ' },

            { 'オ', 'オ' }, { 'コ', 'オ' }, { 'ソ', 'オ' }, { 'ト', 'オ' }, { 'ノ', 'オ' }, { 'ホ', 'オ' },
            { 'モ', 'オ' }, { 'ロ', 'オ' }, { 'ゴ', 'オ' }, { 'ゾ', 'オ' }, { 'ド', 'オ' }, { 'ボ', 'オ' },
            { 'ポ', 'オ' }, { 'ヲ', 'オ' }, { 'ヨ', 'オ' }, { 'ォ', 'オ' }, { 'ョ', 'オ' },


            //Hiragana
            { 'あ', 'あ' }, { 'か', 'あ' }, { 'さ', 'あ' }, { 'た', 'あ' }, { 'な', 'あ' }, { 'は', 'あ' },
            { 'ま', 'あ' }, { 'ら', 'あ' }, { 'が', 'あ' }, { 'ざ', 'あ' }, { 'だ', 'あ' }, { 'ば', 'あ' },
            { 'ぱ', 'あ' }, { 'わ', 'あ' }, { 'や', 'あ' }, { 'ぁ', 'あ' }, { 'ゃ', 'あ' }, { 'ゕ', 'あ' },

            { 'い', 'い' }, { 'き', 'い' }, { 'し', 'い' }, { 'ち', 'い' }, { 'に', 'い' },
            { 'ひ', 'い' }, { 'み', 'い' }, { 'り', 'い' }, { 'ぎ', 'い' }, { 'じ', 'い' },
            { 'ぢ', 'い' }, { 'び', 'い' }, { 'ぴ', 'い' }, { 'ぃ', 'い' }, { 'ゐ', 'い' },

            { 'う', 'う' }, { 'く', 'う' }, { 'す', 'う' }, { 'つ', 'う' }, { 'ぬ', 'う' }, { 'へ', 'う' },
            { 'む', 'う' }, { 'る', 'う' }, { 'ぐ', 'う' }, { 'ず', 'う' }, { 'づ', 'う' }, { 'ぶ', 'う' },
            { 'ぷ', 'う' }, { 'ゆ', 'う' }, { 'ぅ', 'う' }, { 'ゅ', 'う' }, { 'ゔ', 'う' },

            { 'え', 'え' }, { 'け', 'え' }, { 'せ', 'え' }, { 'て', 'え' }, { 'ね', 'え' }, { 'ふ', 'え' },
            { 'め', 'え' }, { 'れ', 'え' }, { 'げ', 'え' }, { 'ぜ', 'え' }, { 'で', 'え' }, { 'べ', 'え' },
            { 'ぺ', 'え' }, { 'ぇ', 'え' }, { 'ゖ', 'え' }, { 'ゑ', 'え' },

            { 'お', 'お' }, { 'こ', 'お' }, { 'そ', 'お' }, { 'と', 'お' }, { 'の', 'お' }, { 'ほ', 'お' },
            { 'も', 'お' }, { 'ろ', 'お' }, { 'ご', 'お' }, { 'ぞ', 'お' }, { 'ど', 'お' }, { 'ぼ', 'お' },
            { 'ぽ', 'お' }, { 'を', 'お' }, { 'よ', 'お' }, { 'ぉ', 'お' }, { 'ょ', 'お' }
        };

        public static string HiraganaToKatakanaConverter(string text)
        {
            string textInHiragana = "";
            foreach (var ch in text)
            {
                if (HiraganaToKatakanaDict.TryGetValue(ch, out char hiraganaChar))
                    textInHiragana += hiraganaChar;
                else
                    textInHiragana += ch;
            }

            return textInHiragana;
        }

        public static string KatakanaToHiraganaConverter(string text)
        {
            string textInHiragana = "";
            foreach (var ch in text)
            {
                if (KatakanaToHiraganaDict.TryGetValue(ch, out char hiraganaChar))
                    textInHiragana += hiraganaChar;
                else
                    textInHiragana += ch;
            }

            return textInHiragana;
        }

        public static string LongVowelMarkConverter(string text)
        {
            string textWithoutLongVowelMark = text[0].ToString();
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == 'ー' && KanaFinalVowelDict.TryGetValue(text[i - 1], out char vowel))
                    textWithoutLongVowelMark += vowel;
                else
                    textWithoutLongVowelMark += text[i];
            }

            return textWithoutLongVowelMark;
        }

        public static bool IsHiragana(string text)
        {
            return HiraganaToKatakanaDict.ContainsKey(text[0]);
        }

        public static bool IsKatakana(string text)
        {
            return KatakanaToHiraganaDict.ContainsKey(text[0]);
        }
    }
}