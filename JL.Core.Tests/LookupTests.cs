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
            List<LookupResult> expected =
                new()
                {
                    new LookupResult
                    {
                        FoundForm = new List<string> { "始まる" },
                        Frequency = new List<string> { "759" },
                        DictType = new List<string> { "JMdict" },
                        FoundSpelling = new List<string> { "始まる" },
                        Readings = new List<string> { "はじまる" },
                        Definitions =
                            new List<string>
                            {
                                "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) "
                            },
                        EdictID = new List<string> { "1307500" },
                        AlternativeSpellings = new List<string> { },
                        Process = new List<string> { },
                        POrthographyInfoList = new List<string> { },
                        ROrthographyInfoList = new List<string> { "" },
                        AOrthographyInfoList = new List<string> { },
                        OnReadings = null,
                        KunReadings = null,
                        Nanori = null,
                        StrokeCount = null,
                        Composition = null,
                        Grade = null,
                    }
                };

            string text = "始まる";

            // Act
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            List<LookupResult> actual = result;

            // Assert
            StringAssert.AreEqualIgnoringCase(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        }

        [Test]
        public void Freq_た_他()
        {
            // Arrange
            string expected = "294";

            string text = "た";

            // Act
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.FoundSpelling.Contains("他")).Frequency[0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }

        //todo
        [Test]
        public void Freq_た_多()
        {
            // Arrange
            string expected = "9844";

            string text = "た";

            // Act
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.FoundSpelling.Contains("多")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.FoundSpelling.Contains("田")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("ひ")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("にち")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("か")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.FoundSpelling.Contains("余り")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("いだく")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("はいきょ")).Frequency[0];

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
            List<LookupResult> result = Lookup.Lookup.LookupText(
                text);
            string actual =
                result.First(x =>
                    x.Readings.Contains("はいきょ")).Frequency[0];

            // Assert
            StringAssert.AreEqualIgnoringCase(expected, actual);
        }
    }
}
