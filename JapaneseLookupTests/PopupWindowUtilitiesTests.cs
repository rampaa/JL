using JapaneseLookup.Utilities;
using NUnit.Framework;

namespace JapaneseLookupTests
{
    [TestFixture]
    public class PopupWindowUtilitiesTests
    {
        [Test]
        public void FindSentence_FromTheStart()
        {
            // Arrange
            var expected = "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。";

            string text =
                "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_FromInTheMiddleOfASentence()
        {
            // Arrange
            var expected = "1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。";

            string text =
                "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
            int position = 97;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithSentencesEndingInTrimmedCharacters()
        {
            // Arrange
            var expected = "a（アーカイブ）";

            string text =
                "a（アーカイブ）";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithUnterminatedSentences()
        {
            // Arrange
            var expected = "あああああああああ";

            string text =
                "あああああああああ";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithNestedQuotes()
        {
            // Arrange
            var expected = "はぁ、『高校生活を振り返って』というテーマの作文でしたが";

            string text =
                "「......はぁ、『高校生活を振り返って』というテーマの作文でしたが」";
            int position = 15;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_WorksWithMultiplePunctuationMarksInARow()
        {
            // Arrange
            var expected = "今日の晩ご飯はなんと.";

            string text =
                "『今日の晩ご飯はなんと......、カレーでしたっ！！』みたいな。";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsLeadingTabs()
        {
            // Arrange
            var expected = "a（アーカイブ）";

            string text =
                "\t\ta（アーカイブ）";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsTrailingNewline()
        {
            // Arrange
            var expected = "a（アーカイブ）";

            string text =
                "a（アーカイブ）\n";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_TrimsUnmatchedParentheses()
        {
            // Arrange
            var expected = "なぁ、比企谷。";

            string text =
                "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
            int position = 0;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk()
        {
            // Arrange
            var expected = "私が授業で出した課題は何だったかな？";

            string text =
                "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
            int position = 8;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk2()
        {
            // Arrange
            var expected = "実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。";

            string text =
                "……単なる身びいきというものかも知れないが。　実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。";
            int position = 72;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FindSentence_idk3()
        {
            // Arrange
            var expected = "申し訳ありません。";

            string text =
                "「……申し訳ありません。稽古をしていたのですが。　少し、考える事があって……没頭しておりました」";
            int position = 10;

            // Act
            var result = PopupWindowUtilities.FindSentence(
                text,
                position);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
