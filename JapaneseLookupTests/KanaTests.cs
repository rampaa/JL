using JapaneseLookup;
using NUnit.Framework;
using System;

namespace JapaneseLookupTests
{
    [TestFixture]
    public class KanaTests
    {
        [Test]
        public void HiraganaToKatakanaConverter_あToア()
        {
            // Arrange
            var expected = "ア";

            string text = "あ";

            // Act
            var result = Kana.HiraganaToKatakanaConverter(
                text);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, result);
        }

        [Test]
        public void KatakanaToHiraganaConverter_アToあ()
        {
            // Arrange
            var expected = "あ";

            string text = "ア";

            // Act
            var result = Kana.KatakanaToHiraganaConverter(
                text);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, result);
        }

        [Test]
        public void LongVowelMarkConverter_オーToオオ()
        {
            // Arrange
            var expected = "オオ";

            string text = "オー";

            // Act
            var result = Kana.LongVowelMarkConverter(
                text);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, result);
        }
    }
}