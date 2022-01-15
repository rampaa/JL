using JapaneseLookup;
using NUnit.Framework;
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
            var result = Kana.LongVowelMarkConverter(
                text);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
