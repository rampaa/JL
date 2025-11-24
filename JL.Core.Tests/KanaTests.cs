using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class KanaTests
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [Test]
    public void KatakanaToHiraganaConverter_アToあ()
    {
        // Arrange
        const string expected = "あ";

        const string text = "ア";

        // Act
        string actual = JapaneseUtils.NormalizeText(text);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText1()
    {
        // Arrange
        const string expected = "か";
        const string text = "㋕";

        // Act
        string actual = JapaneseUtils.NormalizeText(text);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void KatakanaToHiraganaConverter_NormalizesText2()
    {
        // Arrange
        const string expected = "あぱーと";
        const string text = "㌀";

        // Act
        string actual = JapaneseUtils.NormalizeText(text);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
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
        string actual = JapaneseUtils.NormalizeText(text);

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void LongVowelMarkConverter_オーToオオAndオウ()
    {
        // Arrange
        List<string> expected = ["おお", "おう"];

        const string text = "オー";

        // Act
        List<string> actual = JapaneseUtils.NormalizeLongVowelMark(JapaneseUtils.NormalizeText(text));

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }
}
