using System.Text.Json;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests;

[TestFixture]
#pragma warning disable CA1812
internal sealed class LookupTests
#pragma warning restore CA1812
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        Utils.Frontend = new DummyFrontend();

        Dict jmdict = DictUtils.BuiltInDicts[nameof(DictType.JMdict)];
        jmdict.Options.NewlineBetweenDefinitions = new NewlineBetweenDefinitionsOption(false);
        jmdict.Options.UseDB.Value = false;
        jmdict.Path = Path.Join(Utils.ResourcesPath, "MockJMdict.xml");
        jmdict.Active = true;
        jmdict.Ready = false;

        DictUtils.Dicts.Add(nameof(DictType.JMdict), jmdict);

        Dict dict = DictUtils.Dicts[nameof(DictType.JMdict)];
        dict.Contents = new Dictionary<string, IList<IDictRecord>>(StringComparer.Ordinal);
        DictUtils.SingleDictTypeDicts[DictType.JMdict] = dict;
        JmdictLoader.Load(dict).Wait();

        foreach ((string key, Freq freq) in FreqUtils.s_builtInFreqs)
        {
            freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(StringComparer.Ordinal);
            freq.Options = new Freqs.Options.FreqOptions(new Freqs.Options.UseDBOption(false), new Freqs.Options.HigherValueMeansHigherFrequencyOption(false));
            FreqUtils.FreqDicts[key] = freq;
        }

        FreqUtils.LoadFrequencies().Wait();
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void LookupText_始まる()
    {
        // Arrange
        List<LookupResult> expected =
        [
            new
            (
                matchedText: "始まる",
                dict: DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.JMdict),
                frequencies: [new LookupFrequencyResult("VN (Nazeka)", 759, false)],
                primarySpelling: "始まる",
                deconjugatedMatchedText: null,
                readings: ["はじまる"],
                formattedDefinitions: "(v5r, vi) (1) to begin; to start; to commence；(v5r, vi) (2) to happen (again); to begin (anew)；(v5r, vi) (3) to date (from); to originate (in)",
                entryId: 1307500
            )
        ];

        const string text = "始まる";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        // Assert
        Assert.That(JsonSerializer.Serialize(expected) == JsonSerializer.Serialize(result));
    }

    [Test]
    public void Freq_た_他()
    {
        // Arrange
        const int expected = 294;

        const string text = "た";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "他").Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_た_多()
    {
        // Arrange
        const int expected = 9844;

        const string text = "た";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "多").Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_た_田()
    {
        // Arrange
        const int expected = 21431;

        const string text = "た";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "田").Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_日_ひ()
    {
        // Arrange
        const int expected = 227;

        const string text = "日";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("ひ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_日_にち()
    {
        // Arrange
        const int expected = 777;

        const string text = "日";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);
        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("にち") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_日_か()
    {
        // Arrange
        const int expected = 1105;

        const string text = "日";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("か") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_あんまり_余り()
    {
        // Arrange
        const int expected = 284;

        const string text = "あんまり";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.PrimarySpelling is "余り").Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_懐かしい_なつかしい()
    {
        // Arrange
        const int expected = 1776;

        const string text = "懐かしい";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("なつかしい") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_廃虚_はいきょ()
    {
        // Arrange
        const int expected = 8560;

        const string text = "廃虚";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }

    [Test]
    public void Freq_廃墟_はいきょ()
    {
        // Arrange
        const int expected = 8560;

        const string text = "廃墟";

        // Act
        LookupResult[]? result = LookupUtils.LookupText(text);

        int actual = result is not null
            ? result.First(static x => x.Readings?.Contains("はいきょ") ?? false).Frequencies?[0].Freq ?? int.MaxValue
            : int.MaxValue;

        // Assert
        Assert.That(expected == actual);
    }
}
