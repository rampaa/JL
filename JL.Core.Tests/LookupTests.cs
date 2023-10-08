using System.Text.Json;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
public class LookupTests
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        Utils.Frontend = new DummyFrontend();

        string jmdictPath = Path.Join(Utils.ResourcesPath, "MockJMdict.xml");

        DictUtils.Dicts.Add("JMdict",
            new Dict(DictType.JMdict, "JMdict", jmdictPath, true, 0, 500000,
                new DictOptions(
                    new NewlineBetweenDefinitionsOption(false),
                    wordClassInfo: new WordClassInfoOption(true),
                    dialectInfo: new DialectInfoOption(true),
                    pOrthographyInfo: new POrthographyInfoOption(true),
                    pOrthographyInfoColor: new POrthographyInfoColorOption("#FFD2691E"),
                    pOrthographyInfoFontSize: new POrthographyInfoFontSizeOption(15),
                    aOrthographyInfo: new AOrthographyInfoOption(true),
                    rOrthographyInfo: new ROrthographyInfoOption(true),
                    wordTypeInfo: new WordTypeInfoOption(true),
                    miscInfo: new MiscInfoOption(true),
                    loanwordEtymology: new LoanwordEtymologyOption(true),
                    relatedTerm: new RelatedTermOption(false),
                    antonym: new AntonymOption(false)
                )));

        Dict dict = DictUtils.Dicts["JMdict"];
        DictUtils.SingleDictTypeDicts[DictType.JMdict] = dict;
        JmdictLoader.Load(dict).Wait();

        FreqUtils.FreqDicts = FreqUtils.s_builtInFreqs;
        FreqUtils.LoadFrequencies().Wait();
        DeconjugatorUtils.DeserializeRules().Wait();
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
                    dict: DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.JMdict),
                    frequencies: new List<LookupFrequencyResult> { new("VN (Nazeka)", 759) },
                    primarySpelling: "始まる",
                    deconjugatedMatchedText: "始まる",
                    readings: new[] { "はじまる" },
                    formattedDefinitions: "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in)",
                    edictId: 1307500,
                    alternativeSpellingsOrthographyInfoList: new List<string?>(),
                    readingsOrthographyInfoList: new List<string?>()
                )
            };

        const string text = "始まる";

        // Act
        List<LookupResult>? result = LookupUtils.LookupText(text);

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
        List<LookupResult>? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "他").Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "多").Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "田").Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("ひ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("にち") ?? false).Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("か") ?? false).Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "余り").Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("なつかしい") ?? false).Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
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
        List<LookupResult>? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.AreEqual(expected, actual);
    }
}
