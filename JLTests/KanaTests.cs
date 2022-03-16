using System.Collections.Generic;
using JL.Windows;
using JL.Core;
using NUnit.Framework;

namespace JLTests
{
    [TestFixture]
    public class KanaTests
    {
        [Test]
        public void HiraganaToKatakanaConverter_あToア()
        {
            // Arrange
            string expected = "ア";

            string text = "あ";

            // Act
            string result = Kana.HiraganaToKatakanaConverter(
                text);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, result);
        }

        [Test]
        public void KatakanaToHiraganaConverter_アToあ()
        {
            // Arrange
            string expected = "あ";

            string text = "ア";

            // Act
            string result = Kana.KatakanaToHiraganaConverter(
                text);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, result);
        }

        [Test]
        public void KatakanaToHiraganaConverter_NormalizesText()
        {
            // Arrange
            string expected1 = "か";
            string text1 = "㋕";

            string expected2 = "あぱーと";
            string text2 = "㌀";

            string expected3 = "令和";
            string text3 = "㋿";

            // Act
            string result1 = Kana.KatakanaToHiraganaConverter(
                text1);
            string result2 = Kana.KatakanaToHiraganaConverter(
                text2);
            string result3 = Kana.KatakanaToHiraganaConverter(
                text3);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected1, result1);
            StringAssert.AreEqualIgnoringCase(expected2, result2);
            StringAssert.AreEqualIgnoringCase(expected3, result3);
        }

        [Test]
        public void LongVowelMarkConverter_オーToオオAndオウ()
        {
            // Arrange
            List<string> expected = new() { "オオ", "オウ" };

            string text = "オー";

            // Act
            List<string> result = Kana.LongVowelMarkConverter(
                text);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
