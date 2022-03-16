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
