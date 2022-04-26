using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
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

        string jmdictPath = Storage.BuiltInDicts["JMdict"].Path;

        Storage.Dicts.Add(DictType.JMdict,
            new Dict(DictType.JMdict, jmdictPath, true, 0,
                new DictOptions(new NewlineBetweenDefinitionsOption { Value = false }, null, null)));
        Storage.Dicts[DictType.JMdict].Contents = new Dictionary<string, List<IResult>>();

        if (!File.Exists(jmdictPath))
        {
            ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMdict].Path,
                Storage.JmdictUrl,
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
                    FoundForm = "始まる",
                    Frequency = 759,
                    DictType = "JMdict",
                    FoundSpelling = "始まる",
                    Readings = new List<string> { "はじまる" },
                    FormattedDefinitions =
                        "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in) ",
                    EdictID = "1307500",
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
            ? result.First(x => x.FoundSpelling == "他").Frequency
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    //todo
    [Test]
    public void Freq_た_多()
    {
        // Arrange
        int expected = 9844;

        string text = "た";

        // Act
        List<LookupResult>? result = Lookup.Lookup.LookupText(text);
        int actual = result != null
            ? result.First(x => x.FoundSpelling == "多").Frequency
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
            ? result.First(x => x.FoundSpelling == "田").Frequency
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
            ? result.First(x => x.Readings?.Contains("ひ") ?? false).Frequency
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
            ? result.First(x => x.Readings?.Contains("にち") ?? false).Frequency
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
            ? result.First(x => x.Readings?.Contains("か") ?? false).Frequency
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
            ? result.First(x => x.FoundSpelling == "余り").Frequency
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
            ? result.First(x => x.Readings?.Contains("いだく") ?? false).Frequency
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
            ? result.First(x => x.Readings?.Contains("はいきょ") ?? false).Frequency
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
            ? result.First(x => x.Readings?.Contains("はいきょ") ?? false).Frequency
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }
}
