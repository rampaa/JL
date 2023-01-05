using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
public class KanaTests
{
    //[Test]
    //public void HiraganaToKatakanaConverter_あToア()
    //{
    //    // Arrange
    //    string expected = "ア";

    //    string text = "あ";

    //    // Act
    //    string result = Kana.HiraganaToKatakana(text);

    //    // Assert
    //    StringAssert.AreEqualIgnoringCase(expected, result);
    //}

    [Test]
    public void KatakanaToHiraganaConverter_アToあ()
    {
        // Arrange
        const string expected = "あ";

        const string text = "ア";

        // Act
        string result = Kana.KatakanaToHiragana(
            text);

        // Assert
        StringAssert.AreEqualIgnoringCase(expected, result);
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText1()
    {
        // Arrange
        const string expected1 = "か";
        const string text1 = "㋕";

        // Act
        string result1 = Kana.KatakanaToHiragana(
            text1);

        // Assert
        StringAssert.AreEqualIgnoringCase(expected1, result1);
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText2()
    {
        // Arrange
        const string expected2 = "あぱーと";
        const string text2 = "㌀";

        // Act
        string result2 = Kana.KatakanaToHiragana(
            text2);

        // Assert
        StringAssert.AreEqualIgnoringCase(expected2, result2);
    }

    // this one seems to be inconsistent between platforms
    [Test]
    [Explicit]
    public void KatakanaToHiraganaConverter_NormalizesText3()
    {
        // Arrange
        const string expected3 = "令和";
        const string text3 = "㋿";

        // Act
        string result3 = Kana.KatakanaToHiragana(
            text3);

        // Assert
        StringAssert.AreEqualIgnoringCase(expected3, result3);
    }

    [Test]
    public void LongVowelMarkConverter_オーToオオAndオウ()
    {
        // Arrange
        List<string> expected = new() { "オオ", "オウ" };

        const string text = "オー";

        // Act
        List<string> result = Kana.LongVowelMarkToKana(
            text);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
