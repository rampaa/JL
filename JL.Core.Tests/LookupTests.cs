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
            new Dict(DictType.JMdict, "JMdict", jmdictPath, true, 0,
                    new DictOptions(
                        newlineBetweenDefinitions: new() { Value = false },
                        wordClassInfo: new() { Value = true },
                        dialectInfo: new() { Value = true },
                        pOrthographyInfo: new() { Value = true },
                        pOrthographyInfoColor: new() { Value = "#FFD2691E" },
                        pOrthographyInfoFontSize: new() { Value = 15 },
                        aOrthographyInfo: new() { Value = true },
                        rOrthographyInfo: new() { Value = true },
                        wordTypeInfo: new() { Value = true },
                        miscInfo: new() { Value = true },
                        relatedTerm: new() { Value = false },
                        antonym: new() { Value = false },
                        loanwordEtymology: new() { Value = true }
                        )));

        JMdictLoader.Load(Storage.Dicts.Values.First(dict => dict.Type == DictType.JMdict)).Wait();
        Storage.FreqDicts = Storage.BuiltInFreqs;
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
                {
                    FoundForm = "始まる",
                    Dict = Storage.Dicts.Values.First(dict => dict.Type == DictType.JMdict),
                    Frequencies = new () {new ("VN (Nazeka)" ,759 ) },
                    FoundSpelling = "始まる",
                    Readings = new List<string> { "はじまる" },
                    FormattedDefinitions =
                        "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in)",
                    EdictId = "1307500",
                    AlternativeSpellings = new List<string>(),
                    Process = null,
                    POrthographyInfoList = new List<string>(),
                    ROrthographyInfoList = new List<string>(),
                    AOrthographyInfoList = new List<string>(),
                    OnReadings = null,
                    KunReadings = null,
                    Nanori = null,
                    StrokeCount = 0,
                    Composition = null,
                    Grade = 0,
                }
            };

        string text = "始まる";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        List<LookupResult>? actual = result;

        // Assert
        StringAssert.AreEqualIgnoringCase(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }

    [Test]
    public void Freq_た_他()
    {
        // Arrange
        int expected = 294;

        string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.FoundSpelling == "他").Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_た_多()
    {
        // Arrange
        int expected = 9844;

        string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.FoundSpelling == "多").Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_た_田()
    {
        // Arrange
        int expected = 21431;

        string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.FoundSpelling == "田").Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_ひ()
    {
        // Arrange
        int expected = 227;

        string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.Readings?.Contains("ひ") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_にち()
    {
        // Arrange
        int expected = 777;

        string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.Readings?.Contains("にち") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_日_か()
    {
        // Arrange
        int expected = 1105;

        string text = "日";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result != null
            ? result.First(x => x.Readings?.Contains("か") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_あんまり_余り()
    {
        // Arrange
        int expected = 284;

        string text = "あんまり";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result != null
            ? result.First(x => x.FoundSpelling == "余り").Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_懐かしい_いだく()
    {
        // Arrange
        int expected = 903;

        string text = "懐かしい";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result != null
            ? result.First(x => x.Readings?.Contains("いだく") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_廃虚_はいきょ()
    {
        // Arrange
        int expected = 8560;

        string text = "廃虚";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result != null
            ? result.First(x => x.Readings?.Contains("はいきょ") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Freq_廃墟_はいきょ()
    {
        // Arrange
        int expected = 8560;

        string text = "廃墟";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);

        int actual = result != null
            ? result.First(x => x.Readings?.Contains("はいきょ") ?? false).Frequencies.First().Freq
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }
}
