using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.Options;
using JL.Core.Lookup;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
public class LookupTests
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        Storage.Frontend = new DummyFrontend();

        string jmdictPath = Path.Join(AppContext.BaseDirectory, "Resources/MockJMdict.xml");

        Storage.Dicts.Add("JMdict",
            new Dict(DictType.JMdict, "JMdict", jmdictPath, true, 0, 500000,
                    new DictOptions(
                        new NewlineBetweenDefinitionsOption { Value = false },
                        wordClassInfo: new WordClassInfoOption { Value = true },
                        dialectInfo: new DialectInfoOption { Value = true },
                        pOrthographyInfo: new POrthographyInfoOption { Value = true },
                        pOrthographyInfoColor: new POrthographyInfoColorOption { Value = "#FFD2691E" },
                        pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption { Value = 15 },
                        aOrthographyInfo: new AOrthographyInfoOption { Value = true },
                        rOrthographyInfo: new ROrthographyInfoOption { Value = true },
                        wordTypeInfo: new WordTypeInfoOption { Value = true },
                        miscInfo: new MiscInfoOption { Value = true },
                        relatedTerm: new RelatedTermOption { Value = false },
                        antonym: new AntonymOption { Value = false },
                        loanwordEtymology: new LoanwordEtymologyOption { Value = true }
                        )));

        JmdictLoader.Load(Storage.Dicts.Values.First(static dict => dict.Type == DictType.JMdict)).Wait();
        Storage.FreqDicts = Storage.s_builtInFreqs;
        Storage.LoadFrequencies().Wait();
    }

    [Test]
    public void LookupText_始まる()
    {
        // Arrange
        List<LookupResult> expected =
            new()
            {
                new LookupResult
                (
                    matchedText: "始まる",
                    dict: Storage.Dicts.Values.First(static dict => dict.Type == DictType.JMdict),
                    frequencies: new List<LookupFrequencyResult> {new ("VN (Nazeka)" ,759 ) },
                    primarySpelling: "始まる",
                    deconjugatedMatchedText: "始まる",
                    readings: new List<string> { "はじまる" },
                    formattedDefinitions:
                        "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in)",
                    edictId: 1307500,
                    alternativeSpellingsOrthographyInfoList: new List<string>(),
                    readingsOrthographyInfoList: new List<string>()
                )
            };

        const string text = "始まる";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        // Assert
        StringAssert.AreEqualIgnoringCase(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result));
    }

    [Test]
    public void Freq_た_他()
    {
        // Arrange
        const int expected = 294;

        const string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling == "他").Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_た_多()
    {
        // Arrange
        const int expected = 9844;

        const string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling == "多").Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_た_田()
    {
        // Arrange
        const int expected = 21431;

        const string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling == "田").Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_ひ()
    {
        // Arrange
        const int expected = 227;

        const string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("ひ") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_にち()
    {
        // Arrange
        const int expected = 777;

        const string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("にち") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_か()
    {
        // Arrange
        const int expected = 1105;

        const string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("か") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_あんまり_余り()
    {
        // Arrange
        const int expected = 284;

        const string text = "あんまり";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling == "余り").Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_懐かしい_なつかしい()
    {
        // Arrange
        const int expected = 1776;

        const string text = "懐かしい";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("なつかしい") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_廃虚_はいきょ()
    {
        // Arrange
        const int expected = 8560;

        const string text = "廃虚";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_廃墟_はいきょ()
    {
        // Arrange
        const int expected = 8560;

        const string text = "廃墟";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?.First().Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }
}
