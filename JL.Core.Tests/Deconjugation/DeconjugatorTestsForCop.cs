using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForCop
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_Past_です()
    {
        const string termToDeconjugate = "でした";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "です" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Conditional_です()
    {
        const string termToDeconjugate = "でしたら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "です" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Conjectural_です()
    {
        const string termToDeconjugate = "でしょう";
        const string expected = "～conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "です" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Te_です()
    {
        const string termToDeconjugate = "でして";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "です" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Past_だ()
    {
        const string termToDeconjugate = "だった";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "だ" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Conditional_だ()
    {
        const string termToDeconjugate = "だったら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "だ" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Conjectural_だ()
    {
        const string termToDeconjugate = "だろう";
        const string expected = "～conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "だ" && form.Tags[^1] is "cop").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
