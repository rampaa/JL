using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForV5US
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_MasuStem_v5us()
    {
        const string termToDeconjugate = "問い";
        const string expected = "～masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegative_v5us()
    {
        const string termToDeconjugate = "問わない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastAffirmative_v5us()
    {
        const string termToDeconjugate = "問います";
        const string expected = "～polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastVolitional_v5us()
    {
        const string termToDeconjugate = "問いましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegative_v5us()
    {
        const string termToDeconjugate = "問いません";
        const string expected = "～polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastAffirmative_v5us()
    {
        const string termToDeconjugate = "問った";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastNegative_v5us()
    {
        const string termToDeconjugate = "問わなかった";
        const string expected = "～negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastAffirmative_v5us()
    {
        const string termToDeconjugate = "問いました";
        const string expected = "～polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastNegative_v5us()
    {
        const string termToDeconjugate = "問いませんでした";
        const string expected = "～polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormAffirmative_v5us()
    {
        const string termToDeconjugate = "問って";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative_v5us()
    {
        const string termToDeconjugate = "問わなくて";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative2_v5us()
    {
        const string termToDeconjugate = "問わないで";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteTeFormAffirmative_v5us()
    {
        const string termToDeconjugate = "問いまして";
        const string expected = "～polite te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialAffirmative_v5us()
    {
        const string termToDeconjugate = "問える";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveAffirmative_v5us()
    {
        const string termToDeconjugate = "問われる";
        const string expected = "～passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialNegative_v5us()
    {
        const string termToDeconjugate = "問えない";
        const string expected = "～potential→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveNegative_v5us()
    {
        const string termToDeconjugate = "問われない";
        const string expected = "～passive→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialAffirmative_v5us()
    {
        const string termToDeconjugate = "問えた";
        const string expected = "～potential→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveAffirmative_v5us()
    {
        const string termToDeconjugate = "問われた";
        const string expected = "～passive→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialAffirmative_v5us()
    {
        const string termToDeconjugate = "問えました";
        const string expected = "～potential→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveAffirmative_v5us()
    {
        const string termToDeconjugate = "問われました";
        const string expected = "～passive→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialNegative_v5us()
    {
        const string termToDeconjugate = "問えなかった";
        const string expected = "～potential→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveNegative_v5us()
    {
        const string termToDeconjugate = "問われなかった";
        const string expected = "～passive→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialNegative_v5us()
    {
        const string termToDeconjugate = "問えませんでした";
        const string expected = "～potential→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveNegative_v5us()
    {
        const string termToDeconjugate = "問われませんでした";
        const string expected = "～passive→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialAffirmative_v5us()
    {
        const string termToDeconjugate = "問えます";
        const string expected = "～potential→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveAffirmative_v5us()
    {
        const string termToDeconjugate = "問われます";
        const string expected = "～passive→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialNegative_v5us()
    {
        const string termToDeconjugate = "問えません";
        const string expected = "～potential→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveNegative_v5us()
    {
        const string termToDeconjugate = "問われません";
        const string expected = "～passive→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問え";
        const string expected = "～imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeNegative_v5us()
    {
        const string termToDeconjugate = "問うな";
        const string expected = "～imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteImperativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問いなさい";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってください";
        const string expected = "～polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestNegative_v5us()
    {
        const string termToDeconjugate = "問わないでください";
        const string expected = "～negative→polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問おう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainKansaibenVolitionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問お";
        const string expected = "～volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteVolitionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問いましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問えば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問わなければ";
        const string expected = "～negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問ったら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_FormalConditionalAffirmative_v5us()
    {
        const string termToDeconjugate = "問ったらば";
        const string expected = "～formal conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問わなかったら";
        const string expected = "～negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問わせる";
        const string expected = "～causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeNegative_v5us()
    {
        const string termToDeconjugate = "問わせない";
        const string expected = "～causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeSlurred_v5us()
    {
        const string termToDeconjugate = "問わせん";
        const string expected = "～causative→slurred; causative→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問わせます";
        const string expected = "～causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeNegative_v5us()
    {
        const string termToDeconjugate = "問わせません";
        const string expected = "～causative→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePast_v5us()
    {
        const string termToDeconjugate = "問わせた";
        const string expected = "～causative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePastNegative_v5us()
    {
        const string termToDeconjugate = "問わせなかった";
        const string expected = "～causative→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePast_v5us()
    {
        const string termToDeconjugate = "問わせました";
        const string expected = "～causative→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePastNegative_v5us()
    {
        const string termToDeconjugate = "問わせませんでした";
        const string expected = "～causative→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainAffirmative_v5us()
    {
        const string termToDeconjugate = "問わせられる";
        const string expected = "～causative→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainNegative_v5us()
    {
        const string termToDeconjugate = "問わせられない";
        const string expected = "～causative→passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteAffirmative_v5us()
    {
        const string termToDeconjugate = "問わせられます";
        const string expected = "～causative→passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteNegative_v5us()
    {
        const string termToDeconjugate = "問わせられません";
        const string expected = "～causative→passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderative_v5us()
    {
        const string termToDeconjugate = "問いたい";
        const string expected = "～want";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeFormalNegative_v5us()
    {
        const string termToDeconjugate = "問いたくありません";
        const string expected = "～want→formal negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeFormalNegative_v5us()
    {
        const string termToDeconjugate = "問いたくありませんでした";
        const string expected = "～want→formal negative past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeNegative_v5us()
    {
        const string termToDeconjugate = "問いたくない";
        const string expected = "～want→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderative_v5us()
    {
        const string termToDeconjugate = "問いたかった";
        const string expected = "～want→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeNegative_v5us()
    {
        const string termToDeconjugate = "問いたくなかった";
        const string expected = "～want→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiru_v5us()
    {
        const string termToDeconjugate = "問っている";
        const string expected = "～teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiruNegative_v5us()
    {
        const string termToDeconjugate = "問っていない";
        const string expected = "～teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruAffirmative_v5us()
    {
        const string termToDeconjugate = "問っていた";
        const string expected = "～teiru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruNegative_v5us()
    {
        const string termToDeconjugate = "問っていなかった";
        const string expected = "～teiru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiru_v5us()
    {
        const string termToDeconjugate = "問っています";
        const string expected = "～teiru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiruNegative_v5us()
    {
        const string termToDeconjugate = "問っていません";
        const string expected = "～teiru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiru_v5us()
    {
        const string termToDeconjugate = "問っていました";
        const string expected = "～teiru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiruNegative_v5us()
    {
        const string termToDeconjugate = "問っていませんでした";
        const string expected = "～teiru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeru_v5us()
    {
        const string termToDeconjugate = "問ってる";
        const string expected = "～teru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeruNegative_v5us()
    {
        const string termToDeconjugate = "問ってない";
        const string expected = "～teru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeru_v5us()
    {
        const string termToDeconjugate = "問ってた";
        const string expected = "～teru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeruNegative_v5us()
    {
        const string termToDeconjugate = "問ってなかった";
        const string expected = "～teru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeru_v5us()
    {
        const string termToDeconjugate = "問ってます";
        const string expected = "～teru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeruNegative_v5us()
    {
        const string termToDeconjugate = "問ってません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeru_v5us()
    {
        const string termToDeconjugate = "問ってました";
        const string expected = "～teru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative_v5us()
    {
        const string termToDeconjugate = "問ってません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative2_v5us()
    {
        const string termToDeconjugate = "問ってませんでした";
        const string expected = "～teru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってしまう";
        const string expected = "～finish/completely/end up";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauKansaibenAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってもう";
        const string expected = "～finish/completely/end up→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまわない";
        const string expected = "～finish/completely/end up→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってしまった";
        const string expected = "～finish/completely/end up→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまわなかった";
        const string expected = "～finish/completely/end up→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTeForm_v5us()
    {
        const string termToDeconjugate = "問ってしまって";
        const string expected = "～finish/completely/end up→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問ってしまえば";
        const string expected = "～finish/completely/end up→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまわなければ";
        const string expected = "～finish/completely/end up→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまわなかったら";
        const string expected = "～finish/completely/end up→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問ってしまったら";
        const string expected = "～finish/completely/end up→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauVolitional_v5us()
    {
        const string termToDeconjugate = "問ってしまおう";
        const string expected = "～finish/completely/end up→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってしまいます";
        const string expected = "～finish/completely/end up→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまいません";
        const string expected = "～finish/completely/end up→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってしまいました";
        const string expected = "～finish/completely/end up→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauNegative_v5us()
    {
        const string termToDeconjugate = "問ってしまいませんでした";
        const string expected = "～finish/completely/end up→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPotential_v5us()
    {
        const string termToDeconjugate = "問ってしまえる";
        const string expected = "～finish/completely/end up→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPassive_v5us()
    {
        const string termToDeconjugate = "問ってしまわれる";
        const string expected = "～finish/completely/end up→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauCausative_v5us()
    {
        const string termToDeconjugate = "問ってしまわせる";
        const string expected = "～finish/completely/end up→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問っちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauNegative_v5us()
    {
        const string termToDeconjugate = "問っちゃわない";
        const string expected = "～finish/completely/end up→contracted→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauAffirmative_v5us()
    {
        const string termToDeconjugate = "問っちゃった";
        const string expected = "～finish/completely/end up→contracted→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauNegative_v5us()
    {
        const string termToDeconjugate = "問っちゃわなかった";
        const string expected = "～finish/completely/end up→contracted→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTeForm_v5us()
    {
        const string termToDeconjugate = "問っちゃって";
        const string expected = "～finish/completely/end up→contracted→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問っちゃえば";
        const string expected = "～finish/completely/end up→contracted→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問っちゃわなければ";
        const string expected = "～finish/completely/end up→contracted→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTemporalConditionalNegative_v5us()
    {
        const string termToDeconjugate = "問っちゃわなかったら";
        const string expected = "～finish/completely/end up→contracted→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauVolitional_v5us()
    {
        const string termToDeconjugate = "問っちゃおう";
        const string expected = "～finish/completely/end up→contracted→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauPotential_v5us()
    {
        const string termToDeconjugate = "問っちゃえる";
        const string expected = "～finish/completely/end up→contracted→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastOkuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っておく";
        const string expected = "～for now";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastOkuNegative_v5us()
    {
        const string termToDeconjugate = "問っておかない";
        const string expected = "～for now→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っておいた";
        const string expected = "～for now→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuNegative_v5us()
    {
        const string termToDeconjugate = "問っておかなかった";
        const string expected = "～for now→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTeForm_v5us()
    {
        const string termToDeconjugate = "問っておいて";
        const string expected = "～for now→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問っておけば";
        const string expected = "～for now→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問っておいたら";
        const string expected = "～for now→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuVolitional_v5us()
    {
        const string termToDeconjugate = "問っておこう";
        const string expected = "～for now→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPotential_v5us()
    {
        const string termToDeconjugate = "問っておける";
        const string expected = "～for now→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPassive_v5us()
    {
        const string termToDeconjugate = "問っておかれる";
        const string expected = "～for now→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っとく";
        const string expected = "～toku (for now)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuNegative_v5us()
    {
        const string termToDeconjugate = "問っとかない";
        const string expected = "～toku (for now)→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っといた";
        const string expected = "～toku (for now)→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuNegative_v5us()
    {
        const string termToDeconjugate = "問っとかなかった";
        const string expected = "～toku (for now)→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTeForm_v5us()
    {
        const string termToDeconjugate = "問っといて";
        const string expected = "～toku (for now)→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問っとけば";
        const string expected = "～toku (for now)→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問っといたら";
        const string expected = "～toku (for now)→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuVolitional_v5us()
    {
        const string termToDeconjugate = "問っとこう";
        const string expected = "～toku (for now)→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPotential_v5us()
    {
        const string termToDeconjugate = "問っとける";
        const string expected = "～toku (for now)→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPassive_v5us()
    {
        const string termToDeconjugate = "問っとかれる";
        const string expected = "～toku (for now)→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTearuAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってある";
        const string expected = "～tearu";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTearuAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってあった";
        const string expected = "～tearu→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTeForm_v5us()
    {
        const string termToDeconjugate = "問ってあって";
        const string expected = "～tearu→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問ってあったら";
        const string expected = "～tearu→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問ってあれば";
        const string expected = "～tearu→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っていく";
        const string expected = "～teiku";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuNegative_v5us()
    {
        const string termToDeconjugate = "問っていかない";
        const string expected = "～teiku→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っていった";
        const string expected = "～teiku→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuNegative_v5us()
    {
        const string termToDeconjugate = "問っていかなかった";
        const string expected = "～teiku→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuTeForm_v5us()
    {
        const string termToDeconjugate = "問っていって";
        const string expected = "～teiku→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuVolitional_v5us()
    {
        const string termToDeconjugate = "問っていこう";
        const string expected = "～teiku→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPotential_v5us()
    {
        const string termToDeconjugate = "問っていける";
        const string expected = "～teiku→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPassive_v5us()
    {
        const string termToDeconjugate = "問っていかれる";
        const string expected = "～teiku→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuCausative_v5us()
    {
        const string termToDeconjugate = "問っていかせる";
        const string expected = "～teiku→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってくる";
        const string expected = "～tekuru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruNegative_v5us()
    {
        const string termToDeconjugate = "問ってこない";
        const string expected = "～tekuru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってきた";
        const string expected = "～tekuru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruNegative_v5us()
    {
        const string termToDeconjugate = "問ってこなかった";
        const string expected = "～tekuru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTeForm_v5us()
    {
        const string termToDeconjugate = "問ってきて";
        const string expected = "～tekuru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問ってくれば";
        const string expected = "～tekuru→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問ってきたら";
        const string expected = "～tekuru→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruPassivePotentialAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってこられる";
        const string expected = "～tekuru→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruCausativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってこさせる";
        const string expected = "～tekuru→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nagara_v5us()
    {
        const string termToDeconjugate = "問いながら";
        const string expected = "～while";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative_v5us()
    {
        const string termToDeconjugate = "問いすぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouAffirmative_v5us()
    {
        const string termToDeconjugate = "問いそう";
        const string expected = "～seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeFormNu_v5us()
    {
        const string termToDeconjugate = "問わぬ";
        const string expected = "～archaic negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu_v5us()
    {
        const string termToDeconjugate = "問わず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalAdverbialFormZuNi_v5us()
    {
        const string termToDeconjugate = "問わずに";
        const string expected = "～without doing so";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariAffirmative_v5us()
    {
        const string termToDeconjugate = "問ったり";
        const string expected = "～tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariNegative_v5us()
    {
        const string termToDeconjugate = "問わなかったり";
        const string expected = "～negative→tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSlurredAffirmative_v5us()
    {
        const string termToDeconjugate = "問わん";
        const string expected = "～slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastSlurredNegative_v5us()
    {
        const string termToDeconjugate = "問わんかった";
        const string expected = "～slurred negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Zaru_v5us()
    {
        const string termToDeconjugate = "問わざる";
        const string expected = "～archaic attributive negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialVolitional_v5us()
    {
        const string termToDeconjugate = "問えよう";
        const string expected = "～potential→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenPotentialVolitional_v5us()
    {
        const string termToDeconjugate = "問えよ";
        const string expected = "～potential→volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialImperative_v5us()
    {
        const string termToDeconjugate = "問えろ";
        const string expected = "～potential→imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTeForm_v5us()
    {
        const string termToDeconjugate = "問えて";
        const string expected = "～potential→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問えたら";
        const string expected = "～potential→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialProvisionalConditional_v5us()
    {
        const string termToDeconjugate = "問えれば";
        const string expected = "～potential→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialPassivePotential_v5us()
    {
        const string termToDeconjugate = "問えられる";
        const string expected = "～potential→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialCausative_v5us()
    {
        const string termToDeconjugate = "問えさせる";
        const string expected = "～potential→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってあげる";
        const string expected = "～do for someone";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruPassive_v5us()
    {
        const string termToDeconjugate = "問ってあげられる";
        const string expected = "～do for someone→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoru_v5us()
    {
        const string termToDeconjugate = "問っておる";
        const string expected = "～teoru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruNegative_v5us()
    {
        const string termToDeconjugate = "問っておらない";
        const string expected = "～teoru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruSlurredNegative_v5us()
    {
        const string termToDeconjugate = "問っておらん";
        const string expected = "～teoru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruAffirmative_v5us()
    {
        const string termToDeconjugate = "問っておった";
        const string expected = "～teoru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruNegative_v5us()
    {
        const string termToDeconjugate = "問っておらなかった";
        const string expected = "～teoru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoru_v5us()
    {
        const string termToDeconjugate = "問っております";
        const string expected = "～teoru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoruNegative_v5us()
    {
        const string termToDeconjugate = "問っておりません";
        const string expected = "～teoru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoru_v5us()
    {
        const string termToDeconjugate = "問っておりました";
        const string expected = "～teoru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruNegative_v5us()
    {
        const string termToDeconjugate = "問っておりませんでした";
        const string expected = "～teoru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruTeForm_v5us()
    {
        const string termToDeconjugate = "問っておって";
        const string expected = "～teoru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruVolitional_v5us()
    {
        const string termToDeconjugate = "問っておろう";
        const string expected = "～teoru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPotential_v5us()
    {
        const string termToDeconjugate = "問っておれる";
        const string expected = "～teoru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPassive_v5us()
    {
        const string termToDeconjugate = "問っておられる";
        const string expected = "～teoru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToru_v5us()
    {
        const string termToDeconjugate = "問っとる";
        const string expected = "～toru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruNegative_v5us()
    {
        const string termToDeconjugate = "問っとらない";
        const string expected = "～toru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruSlurredNegative_v5us()
    {
        const string termToDeconjugate = "問っとらん";
        const string expected = "～toru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruAffirmative_v5us()
    {
        const string termToDeconjugate = "問っとった";
        const string expected = "～toru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruNegative_v5us()
    {
        const string termToDeconjugate = "問っとらなかった";
        const string expected = "～toru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToru_v5us()
    {
        const string termToDeconjugate = "問っとります";
        const string expected = "～toru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToruNegative_v5us()
    {
        const string termToDeconjugate = "問っとりません";
        const string expected = "～toru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToru_v5us()
    {
        const string termToDeconjugate = "問っとりました";
        const string expected = "～toru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruNegative_v5us()
    {
        const string termToDeconjugate = "問っとりませんでした";
        const string expected = "～toru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruTeForm_v5us()
    {
        const string termToDeconjugate = "問っとって";
        const string expected = "～toru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruVolitional_v5us()
    {
        const string termToDeconjugate = "問っとろう";
        const string expected = "～toru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPotential_v5us()
    {
        const string termToDeconjugate = "問っとれる";
        const string expected = "～toru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPassive_v5us()
    {
        const string termToDeconjugate = "問っとられる";
        const string expected = "～toru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShortCausativeAffirmative_v5us()
    {
        const string termToDeconjugate = "問わす";
        const string expected = "～short causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TopicOrCondition_v5us()
    {
        const string termToDeconjugate = "問っては";
        const string expected = "～topic/condition";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedTopicOrConditionCha_v5us()
    {
        const string termToDeconjugate = "問っちゃ";
        const string expected = "～topic/condition→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedProvisionalConditionalNegativeKya_v5us()
    {
        const string termToDeconjugate = "問わなきゃ";
        const string expected = "～negative→provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChimau_v5us()
    {
        const string termToDeconjugate = "問っちまう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChau_v5us()
    {
        const string termToDeconjugate = "問っちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuAffirmative_v5us()
    {
        const string termToDeconjugate = "問っていらっしゃる";
        const string expected = "～honorific teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuNegative_v5us()
    {
        const string termToDeconjugate = "問っていらっしゃらない";
        const string expected = "～honorific teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Tsutsu_v5us()
    {
        const string termToDeconjugate = "問いつつ";
        const string expected = "～while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってくれる";
        const string expected = "～statement/request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestNegative_v5us()
    {
        const string termToDeconjugate = "問ってくれない";
        const string expected = "～statement/request→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestAffirmative_v5us()
    {
        const string termToDeconjugate = "問ってくれます";
        const string expected = "～statement/request→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestNegative_v5us()
    {
        const string termToDeconjugate = "問ってくれません";
        const string expected = "～statement/request→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementImperative_v5us()
    {
        const string termToDeconjugate = "問ってくれ";
        const string expected = "～statement/request→imperative; statement/request→masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenNegative_v5us()
    {
        const string termToDeconjugate = "問わへん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenNegative_v5us()
    {
        const string termToDeconjugate = "問わへんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenSubDialectNegative_v5us()
    {
        const string termToDeconjugate = "問わひん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenSubDialectNegative_v5us()
    {
        const string termToDeconjugate = "問わひんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialCausativeNegative_v5us()
    {
        const string termToDeconjugate = "問わささない";
        const string expected = "～colloquial causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTemporalConditional_v5us()
    {
        const string termToDeconjugate = "問いましたら";
        const string expected = "～polite conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNinaru_v5us()
    {
        const string termToDeconjugate = "問いになる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNasaru_v5us()
    {
        const string termToDeconjugate = "問いなさる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificHaruKsbAffirmative_v5us()
    {
        const string termToDeconjugate = "問いはる";
        const string expected = "～honorific (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastHonorificNegativeNasaruna_v5us()
    {
        const string termToDeconjugate = "問いなさるな";
        const string expected = "～honorific→imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural_v5us()
    {
        const string termToDeconjugate = "問うまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "問う" && form.Tags[^1] is "v5u-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
