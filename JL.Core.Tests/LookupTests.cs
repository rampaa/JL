using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Lookup;
using NUnit.Framework;

namespace JL.Core.Tests
{
    [TestFixture]
    public class LookupTests
    {
        [OneTimeSetUp]
        public void ClassInit()
        {
            Storage.Frontend = new DummyFrontend();

            string jmdictPath = Storage.BuiltInDicts["JMdict"].Path;

            Storage.Dicts.Add(DictType.JMdict, new Dict(DictType.JMdict, jmdictPath, true, 0));
            Storage.Dicts[DictType.JMdict].Contents = new Dictionary<string, List<IResult>>();

            if (!File.Exists(jmdictPath))
            {
                ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMdict].Path,
                    new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                    DictType.JMdict.ToString(), false, true).Wait();
            }

            JMdictLoader.Load(Storage.Dicts[DictType.JMdict].Path).Wait();
            Storage.LoadFrequency().Wait();
        }

        [Test]
        public void LookupText_始まる()
        {
            // Arrange
            string expected =
                "[{\"foundSpelling\":[\"始まる\"],\"readings\":[\"はじまる\"],\"definitions\":[\"(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) \"],\"foundForm\":[\"始まる\"],\"EdictID\":[\"1307500\"],\"alternativeSpellings\":[],\"process\":[],\"frequency\":[\"759\"],\"pOrthographyInfoList\":[],\"rOrthographyInfoList\":[\"\"],\"aOrthographyInfoList\":[],\"DictType\":[\"JMdict\"]}]";

            string text = "始まる";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual = JsonSerializer.Serialize(result, Storage.JsoUnsafeEscaping);

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_た_他()
        {
            // Arrange
            string expected = "294";

            string text = "た";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.FoundSpelling].Contains("他"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_た_多()
        {
            // Arrange
            string expected = "9844";

            string text = "た";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.FoundSpelling].Contains("多"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_た_田()
        {
            // Arrange
            string expected = "21431";

            string text = "た";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.FoundSpelling].Contains("田"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_日_ひ()
        {
            // Arrange
            string expected = "227";

            string text = "日";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("ひ"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_日_にち()
        {
            // Arrange
            string expected = "777";

            string text = "日";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("にち"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_日_か()
        {
            // Arrange
            string expected = "1105";

            string text = "日";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("か"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_あんまり_余り()
        {
            // Arrange
            string expected = "284";

            string text = "余り";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.FoundSpelling].Contains("余り"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_懐かしい_いだく()
        {
            // Arrange
            string expected = "903";

            string text = "懐かしい";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("いだく"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_廃虚_はいきょ()
        {
            // Arrange
            string expected = "8560";

            string text = "廃虚";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("はいきょ"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        [Test]
        public void Freq_廃墟_はいきょ()
        {
            // Arrange
            string expected = "8560";

            string text = "廃墟";

            // Act
            List<Dictionary<LookupResult, List<string>>> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x[LookupResult.Readings].Contains("はいきょ"))[LookupResult.Frequency][0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}
