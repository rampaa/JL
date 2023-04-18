using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
public class UtilsTests
{
    [Test]
    public void FindSentence_FromTheStart()
    {
        // Arrange
        const string expected = "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。";

        const string text = "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_FromInTheMiddleOfASentence()
    {
        // Arrange
        const string expected = "1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。";

        const string text = "すき焼き（鋤焼、すきやき）は、薄くスライスした食肉や他の食材を浅い鉄鍋で焼いてたり煮たりして調理する日本の料理である。調味料は醤油、砂糖が多用される。1862年「牛鍋屋」から始まる大ブームから広まったもので、当時は牛鍋（ぎゅうなべ、うしなべ）と言った[1]。一般にすき焼きと呼ばれるようになったのは大正になってからである[2]。";
        const int position = 97;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_WorksWithSentencesEndingInTrimmedCharacters()
    {
        // Arrange
        const string expected = "a（アーカイブ）";

        const string text = "a（アーカイブ）";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_WorksWithUnterminatedSentences()
    {
        // Arrange
        const string expected = "あああああああああ";

        const string text = "あああああああああ";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_WorksWithNestedQuotes()
    {
        // Arrange
        const string expected = "はぁ、『高校生活を振り返って』というテーマの作文でしたが";

        const string text = "「......はぁ、『高校生活を振り返って』というテーマの作文でしたが」";
        const int position = 15;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_WorksWithMultiplePunctuationMarksInARow()
    {
        // Arrange
        const string expected = "今日の晩ご飯はなんと.";

        const string text = "『今日の晩ご飯はなんと......、カレーでしたっ！！』みたいな。";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_TrimsLeadingTabs()
    {
        // Arrange
        const string expected = "a（アーカイブ）";

        const string text = "\t\ta（アーカイブ）";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_TrimsTrailingNewline()
    {
        // Arrange
        const string expected = "a（アーカイブ）";

        const string text = "a（アーカイブ）\n";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_TrimsUnmatchedParentheses()
    {
        // Arrange
        const string expected = "なぁ、比企谷。";

        const string text = "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
        const int position = 0;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_idk()
    {
        // Arrange
        const string expected = "私が授業で出した課題は何だったかな？";

        const string text = "「なぁ、比企谷。私が授業で出した課題は何だったかな？」";
        const int position = 8;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_idk2()
    {
        // Arrange
        const string expected = "実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。";

        const string text = "……単なる身びいきというものかも知れないが。　実際のところ、武者ではない俺が武者刀法を修める意味は少なく、素肌剣術をやっていれば良かったのだろうが、そうと認めてしまうのはなかなかに辛い。";
        const int position = 72;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void FindSentence_idk3()
    {
        // Arrange
        const string expected = "申し訳ありません。";

        const string text = "「……申し訳ありません。稽古をしていたのですが。　少し、考える事があって……没頭しておりました」";
        const int position = 10;

        // Act
        string result = JapaneseUtils.FindSentence(
            text,
            position);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
