using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForVSI
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_MasuStem_VSI()
    {
        const string termToDeconjugate = "し";
        const string expected = "～masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_MasuStem2_VSI()
    {
        const string termToDeconjugate = "さ";
        const string expected = "～masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastNegative_VSI()
    {
        const string termToDeconjugate = "しない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegative2_VSI()
    {
        const string termToDeconjugate = "さない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastAffirmative_VSI()
    {
        const string termToDeconjugate = "します";
        const string expected = "～polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastVolitional_VSI()
    {
        const string termToDeconjugate = "しましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegative_VSI()
    {
        const string termToDeconjugate = "しません";
        const string expected = "～polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastAffirmative_VSI()
    {
        const string termToDeconjugate = "した";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastNegative_VSI()
    {
        const string termToDeconjugate = "しなかった";
        const string expected = "～negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastAffirmative_VSI()
    {
        const string termToDeconjugate = "しました";
        const string expected = "～polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastNegative_VSI()
    {
        const string termToDeconjugate = "しませんでした";
        const string expected = "～polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormAffirmative_VSI()
    {
        const string termToDeconjugate = "して";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative_VSI()
    {
        const string termToDeconjugate = "しなくて";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative2_VSI()
    {
        const string termToDeconjugate = "しないで";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteTeFormAffirmative_VSI()
    {
        const string termToDeconjugate = "しまして";
        const string expected = "～polite te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveAffirmative_VSI()
    {
        const string termToDeconjugate = "される";
        const string expected = "～passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialAffirmative_VSI()
    {
        const string termToDeconjugate = "できる";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialAffirmative2_VSI()
    {
        const string termToDeconjugate = "せる";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveNegative_VSI()
    {
        const string termToDeconjugate = "されない";
        const string expected = "～passive→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialNegative_VSI()
    {
        const string termToDeconjugate = "できない";
        const string expected = "～potential→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveAffirmative_VSI()
    {
        const string termToDeconjugate = "された";
        const string expected = "～passive→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialAffirmative_VSI()
    {
        const string termToDeconjugate = "できた";
        const string expected = "～potential→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveAffirmative_VSI()
    {
        const string termToDeconjugate = "されました";
        const string expected = "～passive→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialAffirmative2_VSI()
    {
        const string termToDeconjugate = "できました";
        const string expected = "～potential→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveNegative_VSI()
    {
        const string termToDeconjugate = "されなかった";
        const string expected = "～passive→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialNegative_VSI()
    {
        const string termToDeconjugate = "できなかった";
        const string expected = "～potential→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveNegative_VSI()
    {
        const string termToDeconjugate = "されませんでした";
        const string expected = "～passive→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialNegative2_VSI()
    {
        const string termToDeconjugate = "できませんでした";
        const string expected = "～potential→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveAffirmative_VSI()
    {
        const string termToDeconjugate = "されます";
        const string expected = "～passive→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialAffirmative_VSI()
    {
        const string termToDeconjugate = "できます";
        const string expected = "～potential→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveNegative_VSI()
    {
        const string termToDeconjugate = "されません";
        const string expected = "～passive→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialNegative_VSI()
    {
        const string termToDeconjugate = "できません";
        const string expected = "～potential→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative_VSI()
    {
        const string termToDeconjugate = "しろ";
        const string expected = "～imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative2_VSI()
    {
        const string termToDeconjugate = "せよ";
        const string expected = "～imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative3_VSI()
    {
        const string termToDeconjugate = "せ";
        const string expected = "～imperative; masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeNegative_VSI()
    {
        const string termToDeconjugate = "するな";
        const string expected = "～imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteImperativeAffirmative_VSI()
    {
        const string termToDeconjugate = "しなさい";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestAffirmative_VSI()
    {
        const string termToDeconjugate = "してください";
        const string expected = "～polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestNegative_VSI()
    {
        const string termToDeconjugate = "しないでください";
        const string expected = "～negative→polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative_VSI()
    {
        const string termToDeconjugate = "しよう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative2_VSI()
    {
        const string termToDeconjugate = "そう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainKansaibenVolitionalAffirmative_VSI()
    {
        const string termToDeconjugate = "しよ";
        const string expected = "～volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteVolitionalAffirmative_VSI()
    {
        const string termToDeconjugate = "しましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative_VSI()
    {
        const string termToDeconjugate = "すれば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative2_VSI()
    {
        const string termToDeconjugate = "せば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "しなければ";
        const string expected = "～negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalAffirmative_VSI()
    {
        const string termToDeconjugate = "したら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_FormalConditionalAffirmative_VSI()
    {
        const string termToDeconjugate = "したらば";
        const string expected = "～formal conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "しなかったら";
        const string expected = "～negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeAffirmative_VSI()
    {
        const string termToDeconjugate = "させる";
        const string expected = "～causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeNegative_VSI()
    {
        const string termToDeconjugate = "させない";
        const string expected = "～causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeSlurred_VSI()
    {
        const string termToDeconjugate = "させん";
        const string expected = "～causative→slurred; causative→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeAffirmative_VSI()
    {
        const string termToDeconjugate = "させます";
        const string expected = "～causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteShortCausativeAffirmative_VSI()
    {
        const string termToDeconjugate = "さします";
        const string expected = "～short causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeNegative_VSI()
    {
        const string termToDeconjugate = "させません";
        const string expected = "～causative→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePast_VSI()
    {
        const string termToDeconjugate = "させた";
        const string expected = "～causative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePastNegative_VSI()
    {
        const string termToDeconjugate = "させなかった";
        const string expected = "～causative→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePast_VSI()
    {
        const string termToDeconjugate = "させました";
        const string expected = "～causative→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePastNegative_VSI()
    {
        const string termToDeconjugate = "させませんでした";
        const string expected = "～causative→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainAffirmative_VSI()
    {
        const string termToDeconjugate = "させられる";
        const string expected = "～causative→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainNegative_VSI()
    {
        const string termToDeconjugate = "させられない";
        const string expected = "～causative→passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteAffirmative_VSI()
    {
        const string termToDeconjugate = "させられます";
        const string expected = "～causative→passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteNegative_VSI()
    {
        const string termToDeconjugate = "させられません";
        const string expected = "～causative→passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderative_VSI()
    {
        const string termToDeconjugate = "したい";
        const string expected = "～want";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeFormalNegative_VSI()
    {
        const string termToDeconjugate = "したくありません";
        const string expected = "～want→formal negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeFormalNegative_VSI()
    {
        const string termToDeconjugate = "したくありませんでした";
        const string expected = "～want→formal negative past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeNegative_VSI()
    {
        const string termToDeconjugate = "したくない";
        const string expected = "～want→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderative_VSI()
    {
        const string termToDeconjugate = "したかった";
        const string expected = "～want→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeNegative_VSI()
    {
        const string termToDeconjugate = "したくなかった";
        const string expected = "～want→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiru_VSI()
    {
        const string termToDeconjugate = "している";
        const string expected = "～teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiruNegative_VSI()
    {
        const string termToDeconjugate = "していない";
        const string expected = "～teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruAffirmative_VSI()
    {
        const string termToDeconjugate = "していた";
        const string expected = "～teiru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruNegative_VSI()
    {
        const string termToDeconjugate = "していなかった";
        const string expected = "～teiru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiru_VSI()
    {
        const string termToDeconjugate = "しています";
        const string expected = "～teiru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiruNegative_VSI()
    {
        const string termToDeconjugate = "していません";
        const string expected = "～teiru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiru_VSI()
    {
        const string termToDeconjugate = "していました";
        const string expected = "～teiru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiruNegative_VSI()
    {
        const string termToDeconjugate = "していませんでした";
        const string expected = "～teiru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeru_VSI()
    {
        const string termToDeconjugate = "してる";
        const string expected = "～teru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeruNegative_VSI()
    {
        const string termToDeconjugate = "してない";
        const string expected = "～teru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeru_VSI()
    {
        const string termToDeconjugate = "してた";
        const string expected = "～teru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeruNegative_VSI()
    {
        const string termToDeconjugate = "してなかった";
        const string expected = "～teru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeru_VSI()
    {
        const string termToDeconjugate = "してます";
        const string expected = "～teru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeruNegative_VSI()
    {
        const string termToDeconjugate = "してません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeru_VSI()
    {
        const string termToDeconjugate = "してました";
        const string expected = "～teru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative_VSI()
    {
        const string termToDeconjugate = "してません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative2_VSI()
    {
        const string termToDeconjugate = "してませんでした";
        const string expected = "～teru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "してしまう";
        const string expected = "～finish/completely/end up";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauKansaibenAffirmative_VSI()
    {
        const string termToDeconjugate = "してもう";
        const string expected = "～finish/completely/end up→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauNegative_VSI()
    {
        const string termToDeconjugate = "してしまわない";
        const string expected = "～finish/completely/end up→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "してしまった";
        const string expected = "～finish/completely/end up→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauNegative_VSI()
    {
        const string termToDeconjugate = "してしまわなかった";
        const string expected = "～finish/completely/end up→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTeForm_VSI()
    {
        const string termToDeconjugate = "してしまって";
        const string expected = "～finish/completely/end up→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "してしまえば";
        const string expected = "～finish/completely/end up→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "してしまわなければ";
        const string expected = "～finish/completely/end up→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "してしまわなかったら";
        const string expected = "～finish/completely/end up→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditional_VSI()
    {
        const string termToDeconjugate = "してしまったら";
        const string expected = "～finish/completely/end up→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauVolitional_VSI()
    {
        const string termToDeconjugate = "してしまおう";
        const string expected = "～finish/completely/end up→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "してしまいます";
        const string expected = "～finish/completely/end up→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauNegative_VSI()
    {
        const string termToDeconjugate = "してしまいません";
        const string expected = "～finish/completely/end up→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "してしまいました";
        const string expected = "～finish/completely/end up→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauNegative_VSI()
    {
        const string termToDeconjugate = "してしまいませんでした";
        const string expected = "～finish/completely/end up→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPotential_VSI()
    {
        const string termToDeconjugate = "してしまえる";
        const string expected = "～finish/completely/end up→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPassive_VSI()
    {
        const string termToDeconjugate = "してしまわれる";
        const string expected = "～finish/completely/end up→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauCausative_VSI()
    {
        const string termToDeconjugate = "してしまわせる";
        const string expected = "～finish/completely/end up→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "しちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauNegative_VSI()
    {
        const string termToDeconjugate = "しちゃわない";
        const string expected = "～finish/completely/end up→contracted→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauAffirmative_VSI()
    {
        const string termToDeconjugate = "しちゃった";
        const string expected = "～finish/completely/end up→contracted→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauNegative_VSI()
    {
        const string termToDeconjugate = "しちゃわなかった";
        const string expected = "～finish/completely/end up→contracted→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTeForm_VSI()
    {
        const string termToDeconjugate = "しちゃって";
        const string expected = "～finish/completely/end up→contracted→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "しちゃえば";
        const string expected = "～finish/completely/end up→contracted→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "しちゃわなければ";
        const string expected = "～finish/completely/end up→contracted→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTemporalConditionalNegative_VSI()
    {
        const string termToDeconjugate = "しちゃわなかったら";
        const string expected = "～finish/completely/end up→contracted→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauVolitional_VSI()
    {
        const string termToDeconjugate = "しちゃおう";
        const string expected = "～finish/completely/end up→contracted→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauPotential_VSI()
    {
        const string termToDeconjugate = "しちゃえる";
        const string expected = "～finish/completely/end up→contracted→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastOkuAffirmative_VSI()
    {
        const string termToDeconjugate = "しておく";
        const string expected = "～for now";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastOkuNegative_VSI()
    {
        const string termToDeconjugate = "しておかない";
        const string expected = "～for now→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuAffirmative_VSI()
    {
        const string termToDeconjugate = "しておいた";
        const string expected = "～for now→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuNegative_VSI()
    {
        const string termToDeconjugate = "しておかなかった";
        const string expected = "～for now→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTeForm_VSI()
    {
        const string termToDeconjugate = "しておいて";
        const string expected = "～for now→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "しておけば";
        const string expected = "～for now→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTemporalConditional_VSI()
    {
        const string termToDeconjugate = "しておいたら";
        const string expected = "～for now→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuVolitional_VSI()
    {
        const string termToDeconjugate = "しておこう";
        const string expected = "～for now→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPotential_VSI()
    {
        const string termToDeconjugate = "しておける";
        const string expected = "～for now→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPassive_VSI()
    {
        const string termToDeconjugate = "しておかれる";
        const string expected = "～for now→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuAffirmative_VSI()
    {
        const string termToDeconjugate = "しとく";
        const string expected = "～toku (for now)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuNegative_VSI()
    {
        const string termToDeconjugate = "しとかない";
        const string expected = "～toku (for now)→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuAffirmative_VSI()
    {
        const string termToDeconjugate = "しといた";
        const string expected = "～toku (for now)→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuNegative_VSI()
    {
        const string termToDeconjugate = "しとかなかった";
        const string expected = "～toku (for now)→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTeForm_VSI()
    {
        const string termToDeconjugate = "しといて";
        const string expected = "～toku (for now)→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "しとけば";
        const string expected = "～toku (for now)→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTemporalConditional_VSI()
    {
        const string termToDeconjugate = "しといたら";
        const string expected = "～toku (for now)→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuVolitional_VSI()
    {
        const string termToDeconjugate = "しとこう";
        const string expected = "～toku (for now)→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPotential_VSI()
    {
        const string termToDeconjugate = "しとける";
        const string expected = "～toku (for now)→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPassive_VSI()
    {
        const string termToDeconjugate = "しとかれる";
        const string expected = "～toku (for now)→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTearuAffirmative_VSI()
    {
        const string termToDeconjugate = "してある";
        const string expected = "～tearu";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTearuAffirmative_VSI()
    {
        const string termToDeconjugate = "してあった";
        const string expected = "～tearu→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTeForm_VSI()
    {
        const string termToDeconjugate = "してあって";
        const string expected = "～tearu→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTemporalConditional_VSI()
    {
        const string termToDeconjugate = "してあったら";
        const string expected = "～tearu→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "してあれば";
        const string expected = "～tearu→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuAffirmative_VSI()
    {
        const string termToDeconjugate = "していく";
        const string expected = "～teiku";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuNegative_VSI()
    {
        const string termToDeconjugate = "していかない";
        const string expected = "～teiku→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuAffirmative_VSI()
    {
        const string termToDeconjugate = "していった";
        const string expected = "～teiku→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuNegative_VSI()
    {
        const string termToDeconjugate = "していかなかった";
        const string expected = "～teiku→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuTeForm_VSI()
    {
        const string termToDeconjugate = "していって";
        const string expected = "～teiku→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuVolitional_VSI()
    {
        const string termToDeconjugate = "していこう";
        const string expected = "～teiku→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPotential_VSI()
    {
        const string termToDeconjugate = "していける";
        const string expected = "～teiku→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPassive_VSI()
    {
        const string termToDeconjugate = "していかれる";
        const string expected = "～teiku→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuCausative_VSI()
    {
        const string termToDeconjugate = "していかせる";
        const string expected = "～teiku→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruAffirmative_VSI()
    {
        const string termToDeconjugate = "してくる";
        const string expected = "～tekuru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruNegative_VSI()
    {
        const string termToDeconjugate = "してこない";
        const string expected = "～tekuru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruAffirmative_VSI()
    {
        const string termToDeconjugate = "してきた";
        const string expected = "～tekuru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruNegative_VSI()
    {
        const string termToDeconjugate = "してこなかった";
        const string expected = "～tekuru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTeForm_VSI()
    {
        const string termToDeconjugate = "してきて";
        const string expected = "～tekuru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "してくれば";
        const string expected = "～tekuru→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTemporalConditional_VSI()
    {
        const string termToDeconjugate = "してきたら";
        const string expected = "～tekuru→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruPassivePotentialAffirmative_VSI()
    {
        const string termToDeconjugate = "してこられる";
        const string expected = "～tekuru→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruCausativeAffirmative_VSI()
    {
        const string termToDeconjugate = "してこさせる";
        const string expected = "～tekuru→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nagara_VSI()
    {
        const string termToDeconjugate = "しながら";
        const string expected = "～while";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative_VSI()
    {
        const string termToDeconjugate = "しすぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative2_VSI()
    {
        const string termToDeconjugate = "し過ぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouAffirmative_VSI()
    {
        const string termToDeconjugate = "しそう";
        const string expected = "～seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeFormNu_VSI()
    {
        const string termToDeconjugate = "せぬ";
        const string expected = "～archaic negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu_VSI()
    {
        const string termToDeconjugate = "せず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu2_VSI()
    {
        const string termToDeconjugate = "さず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalAdverbialFormZuNi_VSI()
    {
        const string termToDeconjugate = "せずに";
        const string expected = "～without doing so";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariAffirmative_VSI()
    {
        const string termToDeconjugate = "したり";
        const string expected = "～tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariNegative_VSI()
    {
        const string termToDeconjugate = "しなかったり";
        const string expected = "～negative→tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSlurredAffirmative_VSI()
    {
        const string termToDeconjugate = "せん";
        const string expected = "～slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastSlurredNegative_VSI()
    {
        const string termToDeconjugate = "せんかった";
        const string expected = "～slurred negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Zaru_VSI()
    {
        const string termToDeconjugate = "せざる";
        const string expected = "～archaic attributive negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialVolitional_VSI()
    {
        const string termToDeconjugate = "できよう";
        const string expected = "～potential→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenPotentialVolitional_VSI()
    {
        const string termToDeconjugate = "できよ";
        const string expected = "～potential→volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialImperative_VSI()
    {
        const string termToDeconjugate = "できろ";
        const string expected = "～potential→imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTeForm_VSI()
    {
        const string termToDeconjugate = "できて";
        const string expected = "～potential→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTemporalConditional_VSI()
    {
        const string termToDeconjugate = "できたら";
        const string expected = "～potential→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "できれば";
        const string expected = "～potential→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialPassivePotential_VSI()
    {
        const string termToDeconjugate = "できられる";
        const string expected = "～potential→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialCausative_VSI()
    {
        const string termToDeconjugate = "できさせる";
        const string expected = "～potential→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruAffirmative_VSI()
    {
        const string termToDeconjugate = "してあげる";
        const string expected = "～do for someone";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruPassive_VSI()
    {
        const string termToDeconjugate = "してあげられる";
        const string expected = "～do for someone→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoru_VSI()
    {
        const string termToDeconjugate = "しておる";
        const string expected = "～teoru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruNegative_VSI()
    {
        const string termToDeconjugate = "しておらない";
        const string expected = "～teoru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruSlurredNegative_VSI()
    {
        const string termToDeconjugate = "しておらん";
        const string expected = "～teoru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruAffirmative_VSI()
    {
        const string termToDeconjugate = "しておった";
        const string expected = "～teoru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruNegative_VSI()
    {
        const string termToDeconjugate = "しておらなかった";
        const string expected = "～teoru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoru_VSI()
    {
        const string termToDeconjugate = "しております";
        const string expected = "～teoru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoruNegative_VSI()
    {
        const string termToDeconjugate = "しておりません";
        const string expected = "～teoru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoru_VSI()
    {
        const string termToDeconjugate = "しておりました";
        const string expected = "～teoru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruNegative_VSI()
    {
        const string termToDeconjugate = "しておりませんでした";
        const string expected = "～teoru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruTeForm_VSI()
    {
        const string termToDeconjugate = "しておって";
        const string expected = "～teoru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruVolitional_VSI()
    {
        const string termToDeconjugate = "しておろう";
        const string expected = "～teoru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPotential_VSI()
    {
        const string termToDeconjugate = "しておれる";
        const string expected = "～teoru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPassive_VSI()
    {
        const string termToDeconjugate = "しておられる";
        const string expected = "～teoru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToru_VSI()
    {
        const string termToDeconjugate = "しとる";
        const string expected = "～toru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruNegative_VSI()
    {
        const string termToDeconjugate = "しとらない";
        const string expected = "～toru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruSlurredNegative_VSI()
    {
        const string termToDeconjugate = "しとらん";
        const string expected = "～toru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruAffirmative_VSI()
    {
        const string termToDeconjugate = "しとった";
        const string expected = "～toru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruNegative_VSI()
    {
        const string termToDeconjugate = "しとらなかった";
        const string expected = "～toru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToru_VSI()
    {
        const string termToDeconjugate = "しとります";
        const string expected = "～toru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToruNegative_VSI()
    {
        const string termToDeconjugate = "しとりません";
        const string expected = "～toru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToru_VSI()
    {
        const string termToDeconjugate = "しとりました";
        const string expected = "～toru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruNegative_VSI()
    {
        const string termToDeconjugate = "しとりませんでした";
        const string expected = "～toru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruTeForm_VSI()
    {
        const string termToDeconjugate = "しとって";
        const string expected = "～toru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruVolitional_VSI()
    {
        const string termToDeconjugate = "しとろう";
        const string expected = "～toru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPotential_VSI()
    {
        const string termToDeconjugate = "しとれる";
        const string expected = "～toru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPassive_VSI()
    {
        const string termToDeconjugate = "しとられる";
        const string expected = "～toru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShortCausativeAffirmative_VSI()
    {
        const string termToDeconjugate = "さす";
        const string expected = "～short causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNa_VSI()
    {
        const string termToDeconjugate = "しな";
        const string expected = "～casual polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TopicOrCondition_VSI()
    {
        const string termToDeconjugate = "しては";
        const string expected = "～topic/condition";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedTopicOrConditionCha_VSI()
    {
        const string termToDeconjugate = "しちゃ";
        const string expected = "～topic/condition→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedProvisionalConditionalNegativeKya_VSI()
    {
        const string termToDeconjugate = "しなきゃ";
        const string expected = "～negative→provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChimau_VSI()
    {
        const string termToDeconjugate = "しちまう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChau_VSI()
    {
        const string termToDeconjugate = "しちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuAffirmative_VSI()
    {
        const string termToDeconjugate = "していらっしゃる";
        const string expected = "～honorific teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuNegative_VSI()
    {
        const string termToDeconjugate = "していらっしゃらない";
        const string expected = "～honorific teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Tsutsu_VSI()
    {
        const string termToDeconjugate = "しつつ";
        const string expected = "～while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestAffirmative_VSI()
    {
        const string termToDeconjugate = "してくれる";
        const string expected = "～statement/request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestNegative_VSI()
    {
        const string termToDeconjugate = "してくれない";
        const string expected = "～statement/request→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestAffirmative_VSI()
    {
        const string termToDeconjugate = "してくれます";
        const string expected = "～statement/request→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestNegative_VSI()
    {
        const string termToDeconjugate = "してくれません";
        const string expected = "～statement/request→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementImperative_VSI()
    {
        const string termToDeconjugate = "してくれ";
        const string expected = "～statement/request→imperative; statement/request→masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenNegative_VSI()
    {
        const string termToDeconjugate = "せえへん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenNegative_VSI()
    {
        const string termToDeconjugate = "せえへんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenSubDialectNegative_VSI()
    {
        const string termToDeconjugate = "しひん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenSubDialectNegative_VSI()
    {
        const string termToDeconjugate = "しひんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedProvisionalConditional_VSI()
    {
        const string termToDeconjugate = "すりゃ";
        const string expected = "～provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShortCausativeNegative_VSI()
    {
        const string termToDeconjugate = "ささない";
        const string expected = "～short causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTemporalConditional_VSI()
    {
        const string termToDeconjugate = "しましたら";
        const string expected = "～polite conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNinaru_VSI()
    {
        const string termToDeconjugate = "しになる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNasaru_VSI()
    {
        const string termToDeconjugate = "なさる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificHaruKsbAffirmative_VSI()
    {
        const string termToDeconjugate = "しはる";
        const string expected = "～honorific (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastHonorificNegativeNasaruna_VSI()
    {
        const string termToDeconjugate = "なさるな";
        const string expected = "～honorific→imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural_VSI()
    {
        const string termToDeconjugate = "するまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural2_VSI()
    {
        const string termToDeconjugate = "すまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural3_VSI()
    {
        const string termToDeconjugate = "しまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegativeConjectural_VSI()
    {
        const string termToDeconjugate = "しますまい";
        const string expected = "～polite negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConditional_VSI()
    {
        const string termToDeconjugate = "せねば";
        const string expected = "～negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialNegativeConditional_VSI()
    {
        const string termToDeconjugate = "せにゃ";
        const string expected = "～colloquial negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Ntosuru_VSI()
    {
        const string termToDeconjugate = "せんとする";
        const string expected = "～about to";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nbakari_VSI()
    {
        const string termToDeconjugate = "せんばかり";
        const string expected = "～as if to";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Eru_VSI()
    {
        const string termToDeconjugate = "し得る";
        const string expected = "～be able to";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Eru2_VSI()
    {
        const string termToDeconjugate = "しえる";
        const string expected = "～be able to";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Uru_VSI()
    {
        const string termToDeconjugate = "しうる";
        const string expected = "～be able to";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Masende_VSI()
    {
        const string termToDeconjugate = "しませんで";
        const string expected = "～polite negative te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Masendeshite_VSI()
    {
        const string termToDeconjugate = "しませんでして";
        const string expected = "～polite negative te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Masereba_VSI()
    {
        const string termToDeconjugate = "しますれば";
        const string expected = "～polite provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Mase_VSI()
    {
        const string termToDeconjugate = "しませ";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Masendeshitara_VSI()
    {
        const string termToDeconjugate = "しませんでしたら";
        const string expected = "～polite past negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Itasu_VSI()
    {
        const string termToDeconjugate = "し致す";
        const string expected = "～humble";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Itashimasen_VSI()
    {
        const string termToDeconjugate = "し致しません";
        const string expected = "～humble→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Itasu2_VSI()
    {
        const string termToDeconjugate = "しいたす";
        const string expected = "～humble";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Itashimasen2_VSI()
    {
        const string termToDeconjugate = "しいたしません";
        const string expected = "～humble→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Temiru_VSI()
    {
        const string termToDeconjugate = "してみる";
        const string expected = "～try doing";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Yasui_VSI()
    {
        const string termToDeconjugate = "しやすい";
        const string expected = "～easy";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nikui_VSI()
    {
        const string termToDeconjugate = "しにくい";
        const string expected = "～difficult";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "する" && form.Tags[^1] is "vs-i").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
