using JapaneseLookup;
using NUnit.Framework;
using System.Text.Json;
using JapaneseLookup.EDICT;

namespace JapaneseLookupTests
{
    [TestFixture]
    public class MainWindowUtilitiesTests
    {
        private static readonly JsonSerializerOptions Jso = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [OneTimeSetUp]
        public void ClassInit()
        {
            JMdictLoader.Load();
        }

        [Test]
        public void FindSentence_FromTheStart()
        {
            // Arrange
            var expected = ("すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。", 58);

            string text =
                "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_FromInTheMiddleOfASentence()
        {
            // Arrange
            var expected = ("1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。", 128);

            string text =
                "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
            int position = 97;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithSentencesEndingInTrimmedCharacters()
        {
            // Arrange
            var expected = ("a（アーカイブ）", 7);

            string text =
                "a（アーカイブ）";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithUnterminatedSentences()
        {
            // Arrange
            var expected = ("あああああああああ", 8);

            string text =
                "あああああああああ";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithNestedQuotes()
        {
            // Arrange
            var expected = ("「......はぁ、『高校生活を振り返って』というテーマの作文でしたが」", 35);

            string text =
                "「......はぁ、『高校生活を振り返って』というテーマの作文でしたが」";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithMultiplePunctuationMarksInARow()
        {
            // Arrange
            var expected = ("『今日の晩ご飯はなんと......、カレーでしたっ！！』みたいな。", 32);

            string text =
                "『今日の晩ご飯はなんと......、カレーでしたっ！！』みたいな。";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsLeadingTabs()
        {
            // Arrange
            var expected = ("a（アーカイブ）", 9);

            string text =
                "\t\ta（アーカイブ）";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsTrailingNewline()
        {
            // Arrange
            var expected = ("a（アーカイブ）", 8);

            string text =
                "a（アーカイブ）\n";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsUnmatchedParentheses()
        {
            // Arrange
            var expected = ("なぁ、比企谷。", 7);

            string text =
                "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
            int position = 0;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk()
        {
            // Arrange
            var expected = ("私が授業で出した課題は何だったかな？", 25);

            string text =
                "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
            int position = 8;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk2()
        {
            // Arrange
            var expected = ("実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。", 92);

            string text =
                "……単なる身びいきというものかも知れないが。　実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。";
            int position = 72;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk3()
        {
            // Arrange
            var expected = ("申し訳ありません。", 11);

            string text =
                "「……申し訳ありません。稽古をしていたのですが。　少し、考える事があって……没頭しておりました」";
            int position = 10;

            // Act
            var result = MainWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }
        [Test]
        public void LookUp_始まる()
        {
            // Arrange
            var expected =
                "[{\"foundSpelling\":[\"始まる\"],\"kanaSpellings\":[],\"readings\":[\"はじまる\"],\"definitions\":[\"(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) \"],\"foundForm\":[\"始まる\"],\"jmdictID\":[\"1307500\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]}]";

            string text = "始まる";

            // Act
            var result = MainWindowUtilities.LookUp(
                text);
            var actual = JsonSerializer.Serialize(result, Jso);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void LookUp_ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス()
        {
            // Arrange
            var expected =
                "[{\"foundSpelling\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"kanaSpellings\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"readings\":[],\"definitions\":[\"(n) (1) pneumonoultramicroscopicsilicovolcanoconiosis; pneumonoultramicroscopicsilicovolcanokoniosis \"],\"foundForm\":[\"ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス\"],\"jmdictID\":[\"2443650\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"乳母\"],\"kanaSpellings\":[\"にゅうも\"],\"readings\":[\"うば\",\"めのと\",\"おんば\",\"にゅうぼ\",\"ちうば\",\"ちおも\",\"ちも\",\"にゅうも\",\"まま\"],\"definitions\":[\"(n) (1) wet nurse; nursing mother \"],\"foundForm\":[\"ニューモ\"],\"jmdictID\":[\"1582750\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"ニュー\"],\"kanaSpellings\":[\"ニュー\"],\"readings\":[],\"definitions\":[\"(adj-f) (1) new \"],\"foundForm\":[\"ニュー\"],\"jmdictID\":[\"1091340\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"Ν\"],\"kanaSpellings\":[\"ニュー\"],\"readings\":[\"ニュー\"],\"definitions\":[\"(n) (1) nu \"],\"foundForm\":[\"ニュー\"],\"jmdictID\":[\"2173750\"],\"alternativeSpellings\":[\"ν\"],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[\"\"]},{\"foundSpelling\":[\"荷\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) load; baggage; cargo; freight; goods (n) (2) burden; responsibility \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1195250\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"似\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(suf) (1) looking like (someone); taking after (either of one\'s parents) \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1314550\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"丹\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) red earth (i.e. containing cinnabar or minium); vermilion \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1416890\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"二\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\",\"ふた\",\"ふ\",\"ふう\"],\"definitions\":[\"(num) (1) (ふ and ふう used mainly when counting aloud; 弐, 貳 and 貮 are used in legal documents.) two \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1461140\"],\"alternativeSpellings\":[\"２\",\"弐\",\"弍\",\"貳\",\"貮\"],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\",\"\",\"\",\"\"],\"aOrthographyInfoList\":[\"\",\"\",\"\",\"oK\",\"oK\"]},{\"foundSpelling\":[\"に\"],\"kanaSpellings\":[\"に\"],\"readings\":[],\"definitions\":[\"(prt) (1) at (place, time); in; on; during (prt) (2) to (direction, state); toward; into (prt) (3) for (purpose) (prt) (4) because of (reason); for; with (prt) (5) by; from (prt) (6) as (i.e. in the role of) (prt) (7) per; in; for; a (e.g. \\\"once a month\\\") (prt) (8) and; in addition to (prt) (9) (arch) if; although \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"2028990\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"ニ\"],\"kanaSpellings\":[\"ニ\"],\"readings\":[],\"definitions\":[\"(n) (1) 4th in a sequence denoted by the iroha system; 4th note in the diatonic scale (used in key names, etc.) \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"2029750\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"尼\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n, n-suf) (1) (abbr) bhikkhuni (fully ordained Buddhist nun) (n) (2) (abbr) Indonesia \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"2233520\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"土\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(n) (1) (arch) soil (esp. reddish soil) \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"2423330\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"煮\"],\"kanaSpellings\":[\"に\"],\"readings\":[\"に\"],\"definitions\":[\"(suf) (1) simmered with; cooked with (n) (2) boiling; boiled dish \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"2773810\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"似る\"],\"kanaSpellings\":[\"にる\"],\"readings\":[\"にる\"],\"definitions\":[\"(v1, vi) (1) to resemble; to look like; to be like; to be alike; to be similar; to take after \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1314600\"],\"alternativeSpellings\":[],\"process\":[\"(infinitive)\"],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]},{\"foundSpelling\":[\"煮る\"],\"kanaSpellings\":[\"にる\"],\"readings\":[\"にる\"],\"definitions\":[\"(v1, vt) (1) to boil; to simmer; to stew; to seethe \"],\"foundForm\":[\"ニ\"],\"jmdictID\":[\"1322540\"],\"alternativeSpellings\":[],\"process\":[\"(infinitive)\"],\"frequency\":[\"1000000\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[]}]";

            string text = "ニューモノウルトラマイクロスコーピックシリコヴォルケーノコニオシス";

            // Act
            var result = MainWindowUtilities.LookUp(
                text);
            var actual = JsonSerializer.Serialize(result, Jso);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}