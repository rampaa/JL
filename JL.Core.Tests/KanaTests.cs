using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
public class KanaTests
{
    [Test]
    public void KatakanaToHiraganaConverter_アToあ()
    {
        // Arrange
        const string expected = "あ";

        const string text = "ア";

        // Act
        string result = JapaneseUtils.KatakanaToHiragana(text);

        // Assert
        Assert.That(expected == result);
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText1()
    {
        // Arrange
        const string expected1 = "か";
        const string text1 = "㋕";

        // Act
        string result1 = JapaneseUtils.KatakanaToHiragana(text1);

        // Assert
        Assert.That(expected1 == result1);
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText2()
    {
        // Arrange
        const string expected = "あぱーと";
        const string text = "㌀";

        // Act
        string result = JapaneseUtils.KatakanaToHiragana(text);

        // Assert
        Assert.That(expected == result);
    }

    // this one seems to be inconsistent between platforms
    [Test]
    [Explicit]
    public void KatakanaToHiraganaConverter_NormalizesText3()
    {
        // Arrange
        const string expected = "令和";
        const string text = "㋿";

        // Act
        string result3 = JapaneseUtils.KatakanaToHiragana(text);

        // Assert
        Assert.That(expected == result3);
    }

    [Test]
    public void LongVowelMarkConverter_オーToオオAndオウ()
    {
        // Arrange
        List<string> expected = ["おお", "おう"];

        const string text = "オー";

        // Act
        List<string> result = JapaneseUtils.LongVowelMarkToKana(JapaneseUtils.KatakanaToHiragana(text));

        // Assert
        Assert.That(expected.SequenceEqual(result));
    }
}
