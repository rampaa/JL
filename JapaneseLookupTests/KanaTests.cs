using JapaneseLookup;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
        public void LongVowelMarkConverter_オーToオオAndオウ()
        {
            // Arrange
            List<string> expected = new() { "オオ", "オウ" };

            string text = "オー";

            // Act
            var result = Kana.LongVowelMarkConverter(
                text);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}