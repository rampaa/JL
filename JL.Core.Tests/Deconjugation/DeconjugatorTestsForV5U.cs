using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForV5U
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_MasuStem_v5u()
    {
        const string termToDeconjugate = "言い";
        const string expected = "～masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegative_v5u()
    {
        const string termToDeconjugate = "言わない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastAffirmative_v5u()
    {
        const string termToDeconjugate = "言います";
        const string expected = "～polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastVolitional_v5u()
    {
        const string termToDeconjugate = "言いましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegative_v5u()
    {
        const string termToDeconjugate = "言いません";
        const string expected = "～polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastAffirmative_v5u()
    {
        const string termToDeconjugate = "言った";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastNegative_v5u()
    {
        const string termToDeconjugate = "言わなかった";
        const string expected = "～negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastAffirmative_v5u()
    {
        const string termToDeconjugate = "言いました";
        const string expected = "～polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastNegative_v5u()
    {
        const string termToDeconjugate = "言いませんでした";
        const string expected = "～polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormAffirmative_v5u()
    {
        const string termToDeconjugate = "言って";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative_v5u()
    {
        const string termToDeconjugate = "言わなくて";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative2_v5u()
    {
        const string termToDeconjugate = "言わないで";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteTeFormAffirmative_v5u()
    {
        const string termToDeconjugate = "言いまして";
        const string expected = "～polite te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialAffirmative_v5u()
    {
        const string termToDeconjugate = "言える";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveAffirmative_v5u()
    {
        const string termToDeconjugate = "言われる";
        const string expected = "～passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialNegative_v5u()
    {
        const string termToDeconjugate = "言えない";
        const string expected = "～potential→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveNegative_v5u()
    {
        const string termToDeconjugate = "言われない";
        const string expected = "～passive→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialAffirmative_v5u()
    {
        const string termToDeconjugate = "言えた";
        const string expected = "～potential→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveAffirmative_v5u()
    {
        const string termToDeconjugate = "言われた";
        const string expected = "～passive→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialAffirmative_v5u()
    {
        const string termToDeconjugate = "言えました";
        const string expected = "～potential→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveAffirmative_v5u()
    {
        const string termToDeconjugate = "言われました";
        const string expected = "～passive→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialNegative_v5u()
    {
        const string termToDeconjugate = "言えなかった";
        const string expected = "～potential→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveNegative_v5u()
    {
        const string termToDeconjugate = "言われなかった";
        const string expected = "～passive→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialNegative_v5u()
    {
        const string termToDeconjugate = "言えませんでした";
        const string expected = "～potential→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveNegative_v5u()
    {
        const string termToDeconjugate = "言われませんでした";
        const string expected = "～passive→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialAffirmative_v5u()
    {
        const string termToDeconjugate = "言えます";
        const string expected = "～potential→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveAffirmative_v5u()
    {
        const string termToDeconjugate = "言われます";
        const string expected = "～passive→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialNegative_v5u()
    {
        const string termToDeconjugate = "言えません";
        const string expected = "～potential→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveNegative_v5u()
    {
        const string termToDeconjugate = "言われません";
        const string expected = "～passive→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言え";
        const string expected = "～imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeNegative_v5u()
    {
        const string termToDeconjugate = "言うな";
        const string expected = "～imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteImperativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言いなさい";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってください";
        const string expected = "～polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestNegative_v5u()
    {
        const string termToDeconjugate = "言わないでください";
        const string expected = "～negative→polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言おう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainKansaibenVolitionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言お";
        const string expected = "～volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteVolitionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言いましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言えば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言わなければ";
        const string expected = "～negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言ったら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_FormalConditionalAffirmative_v5u()
    {
        const string termToDeconjugate = "言ったらば";
        const string expected = "～formal conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言わなかったら";
        const string expected = "～negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言わせる";
        const string expected = "～causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeNegative_v5u()
    {
        const string termToDeconjugate = "言わせない";
        const string expected = "～causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeSlurred_v5u()
    {
        const string termToDeconjugate = "言わせん";
        const string expected = "～causative→slurred; causative→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言わせます";
        const string expected = "～causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeNegative_v5u()
    {
        const string termToDeconjugate = "言わせません";
        const string expected = "～causative→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePast_v5u()
    {
        const string termToDeconjugate = "言わせた";
        const string expected = "～causative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePastNegative_v5u()
    {
        const string termToDeconjugate = "言わせなかった";
        const string expected = "～causative→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePast_v5u()
    {
        const string termToDeconjugate = "言わせました";
        const string expected = "～causative→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePastNegative_v5u()
    {
        const string termToDeconjugate = "言わせませんでした";
        const string expected = "～causative→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainAffirmative_v5u()
    {
        const string termToDeconjugate = "言わせられる";
        const string expected = "～causative→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainNegative_v5u()
    {
        const string termToDeconjugate = "言わせられない";
        const string expected = "～causative→passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteAffirmative_v5u()
    {
        const string termToDeconjugate = "言わせられます";
        const string expected = "～causative→passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteNegative_v5u()
    {
        const string termToDeconjugate = "言わせられません";
        const string expected = "～causative→passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderative_v5u()
    {
        const string termToDeconjugate = "言いたい";
        const string expected = "～want";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeFormalNegative_v5u()
    {
        const string termToDeconjugate = "言いたくありません";
        const string expected = "～want→formal negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeFormalNegative_v5u()
    {
        const string termToDeconjugate = "言いたくありませんでした";
        const string expected = "～want→formal negative past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeNegative_v5u()
    {
        const string termToDeconjugate = "言いたくない";
        const string expected = "～want→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderative_v5u()
    {
        const string termToDeconjugate = "言いたかった";
        const string expected = "～want→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeNegative_v5u()
    {
        const string termToDeconjugate = "言いたくなかった";
        const string expected = "～want→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiru_v5u()
    {
        const string termToDeconjugate = "言っている";
        const string expected = "～teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiruNegative_v5u()
    {
        const string termToDeconjugate = "言っていない";
        const string expected = "～teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruAffirmative_v5u()
    {
        const string termToDeconjugate = "言っていた";
        const string expected = "～teiru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruNegative_v5u()
    {
        const string termToDeconjugate = "言っていなかった";
        const string expected = "～teiru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiru_v5u()
    {
        const string termToDeconjugate = "言っています";
        const string expected = "～teiru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiruNegative_v5u()
    {
        const string termToDeconjugate = "言っていません";
        const string expected = "～teiru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiru_v5u()
    {
        const string termToDeconjugate = "言っていました";
        const string expected = "～teiru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiruNegative_v5u()
    {
        const string termToDeconjugate = "言っていませんでした";
        const string expected = "～teiru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeru_v5u()
    {
        const string termToDeconjugate = "言ってる";
        const string expected = "～teru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeruNegative_v5u()
    {
        const string termToDeconjugate = "言ってない";
        const string expected = "～teru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeru_v5u()
    {
        const string termToDeconjugate = "言ってた";
        const string expected = "～teru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeruNegative_v5u()
    {
        const string termToDeconjugate = "言ってなかった";
        const string expected = "～teru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeru_v5u()
    {
        const string termToDeconjugate = "言ってます";
        const string expected = "～teru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeruNegative_v5u()
    {
        const string termToDeconjugate = "言ってません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeru_v5u()
    {
        const string termToDeconjugate = "言ってました";
        const string expected = "～teru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative_v5u()
    {
        const string termToDeconjugate = "言ってません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative2_v5u()
    {
        const string termToDeconjugate = "言ってませんでした";
        const string expected = "～teru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってしまう";
        const string expected = "～finish/completely/end up";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauKansaibenAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってもう";
        const string expected = "～finish/completely/end up→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまわない";
        const string expected = "～finish/completely/end up→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってしまった";
        const string expected = "～finish/completely/end up→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまわなかった";
        const string expected = "～finish/completely/end up→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTeForm_v5u()
    {
        const string termToDeconjugate = "言ってしまって";
        const string expected = "～finish/completely/end up→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言ってしまえば";
        const string expected = "～finish/completely/end up→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまわなければ";
        const string expected = "～finish/completely/end up→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまわなかったら";
        const string expected = "～finish/completely/end up→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言ってしまったら";
        const string expected = "～finish/completely/end up→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauVolitional_v5u()
    {
        const string termToDeconjugate = "言ってしまおう";
        const string expected = "～finish/completely/end up→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってしまいます";
        const string expected = "～finish/completely/end up→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまいません";
        const string expected = "～finish/completely/end up→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってしまいました";
        const string expected = "～finish/completely/end up→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauNegative_v5u()
    {
        const string termToDeconjugate = "言ってしまいませんでした";
        const string expected = "～finish/completely/end up→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPotential_v5u()
    {
        const string termToDeconjugate = "言ってしまえる";
        const string expected = "～finish/completely/end up→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPassive_v5u()
    {
        const string termToDeconjugate = "言ってしまわれる";
        const string expected = "～finish/completely/end up→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauCausative_v5u()
    {
        const string termToDeconjugate = "言ってしまわせる";
        const string expected = "～finish/completely/end up→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言っちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauNegative_v5u()
    {
        const string termToDeconjugate = "言っちゃわない";
        const string expected = "～finish/completely/end up→contracted→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauAffirmative_v5u()
    {
        const string termToDeconjugate = "言っちゃった";
        const string expected = "～finish/completely/end up→contracted→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauNegative_v5u()
    {
        const string termToDeconjugate = "言っちゃわなかった";
        const string expected = "～finish/completely/end up→contracted→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTeForm_v5u()
    {
        const string termToDeconjugate = "言っちゃって";
        const string expected = "～finish/completely/end up→contracted→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言っちゃえば";
        const string expected = "～finish/completely/end up→contracted→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言っちゃわなければ";
        const string expected = "～finish/completely/end up→contracted→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTemporalConditionalNegative_v5u()
    {
        const string termToDeconjugate = "言っちゃわなかったら";
        const string expected = "～finish/completely/end up→contracted→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauVolitional_v5u()
    {
        const string termToDeconjugate = "言っちゃおう";
        const string expected = "～finish/completely/end up→contracted→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauPotential_v5u()
    {
        const string termToDeconjugate = "言っちゃえる";
        const string expected = "～finish/completely/end up→contracted→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastOkuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っておく";
        const string expected = "～for now";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastOkuNegative_v5u()
    {
        const string termToDeconjugate = "言っておかない";
        const string expected = "～for now→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っておいた";
        const string expected = "～for now→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuNegative_v5u()
    {
        const string termToDeconjugate = "言っておかなかった";
        const string expected = "～for now→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTeForm_v5u()
    {
        const string termToDeconjugate = "言っておいて";
        const string expected = "～for now→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言っておけば";
        const string expected = "～for now→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言っておいたら";
        const string expected = "～for now→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuVolitional_v5u()
    {
        const string termToDeconjugate = "言っておこう";
        const string expected = "～for now→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPotential_v5u()
    {
        const string termToDeconjugate = "言っておける";
        const string expected = "～for now→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPassive_v5u()
    {
        const string termToDeconjugate = "言っておかれる";
        const string expected = "～for now→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っとく";
        const string expected = "～toku (for now)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuNegative_v5u()
    {
        const string termToDeconjugate = "言っとかない";
        const string expected = "～toku (for now)→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っといた";
        const string expected = "～toku (for now)→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuNegative_v5u()
    {
        const string termToDeconjugate = "言っとかなかった";
        const string expected = "～toku (for now)→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTeForm_v5u()
    {
        const string termToDeconjugate = "言っといて";
        const string expected = "～toku (for now)→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言っとけば";
        const string expected = "～toku (for now)→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言っといたら";
        const string expected = "～toku (for now)→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuVolitional_v5u()
    {
        const string termToDeconjugate = "言っとこう";
        const string expected = "～toku (for now)→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPotential_v5u()
    {
        const string termToDeconjugate = "言っとける";
        const string expected = "～toku (for now)→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPassive_v5u()
    {
        const string termToDeconjugate = "言っとかれる";
        const string expected = "～toku (for now)→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTearuAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってある";
        const string expected = "～tearu";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTearuAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってあった";
        const string expected = "～tearu→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTeForm_v5u()
    {
        const string termToDeconjugate = "言ってあって";
        const string expected = "～tearu→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言ってあったら";
        const string expected = "～tearu→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言ってあれば";
        const string expected = "～tearu→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っていく";
        const string expected = "～teiku";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuNegative_v5u()
    {
        const string termToDeconjugate = "言っていかない";
        const string expected = "～teiku→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っていった";
        const string expected = "～teiku→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuNegative_v5u()
    {
        const string termToDeconjugate = "言っていかなかった";
        const string expected = "～teiku→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuTeForm_v5u()
    {
        const string termToDeconjugate = "言っていって";
        const string expected = "～teiku→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuVolitional_v5u()
    {
        const string termToDeconjugate = "言っていこう";
        const string expected = "～teiku→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPotential_v5u()
    {
        const string termToDeconjugate = "言っていける";
        const string expected = "～teiku→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPassive_v5u()
    {
        const string termToDeconjugate = "言っていかれる";
        const string expected = "～teiku→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuCausative_v5u()
    {
        const string termToDeconjugate = "言っていかせる";
        const string expected = "～teiku→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってくる";
        const string expected = "～tekuru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruNegative_v5u()
    {
        const string termToDeconjugate = "言ってこない";
        const string expected = "～tekuru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってきた";
        const string expected = "～tekuru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruNegative_v5u()
    {
        const string termToDeconjugate = "言ってこなかった";
        const string expected = "～tekuru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTeForm_v5u()
    {
        const string termToDeconjugate = "言ってきて";
        const string expected = "～tekuru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言ってくれば";
        const string expected = "～tekuru→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言ってきたら";
        const string expected = "～tekuru→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruPassivePotentialAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってこられる";
        const string expected = "～tekuru→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruCausativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってこさせる";
        const string expected = "～tekuru→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nagara_v5u()
    {
        const string termToDeconjugate = "言いながら";
        const string expected = "～while";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative_v5u()
    {
        const string termToDeconjugate = "言いすぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouAffirmative_v5u()
    {
        const string termToDeconjugate = "言いそう";
        const string expected = "～seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouNegative_v5u()
    {
        const string termToDeconjugate = "言わなそう";
        const string expected = "～negative→seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeFormNu_v5u()
    {
        const string termToDeconjugate = "言わぬ";
        const string expected = "～archaic negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu_v5u()
    {
        const string termToDeconjugate = "言わず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalAdverbialFormZuNi_v5u()
    {
        const string termToDeconjugate = "言わずに";
        const string expected = "～without doing so";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariAffirmative_v5u()
    {
        const string termToDeconjugate = "言ったり";
        const string expected = "～tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariNegative_v5u()
    {
        const string termToDeconjugate = "言わなかったり";
        const string expected = "～negative→tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSlurredAffirmative_v5u()
    {
        const string termToDeconjugate = "言わん";
        const string expected = "～slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastSlurredNegative_v5u()
    {
        const string termToDeconjugate = "言わんかった";
        const string expected = "～slurred negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Zaru_v5u()
    {
        const string termToDeconjugate = "言わざる";
        const string expected = "～archaic attributive negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialVolitional_v5u()
    {
        const string termToDeconjugate = "言えよう";
        const string expected = "～potential→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenPotentialVolitional_v5u()
    {
        const string termToDeconjugate = "言えよ";
        const string expected = "～potential→volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialImperative_v5u()
    {
        const string termToDeconjugate = "言えろ";
        const string expected = "～potential→imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTeForm_v5u()
    {
        const string termToDeconjugate = "言えて";
        const string expected = "～potential→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言えたら";
        const string expected = "～potential→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialProvisionalConditional_v5u()
    {
        const string termToDeconjugate = "言えれば";
        const string expected = "～potential→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialPassivePotential_v5u()
    {
        const string termToDeconjugate = "言えられる";
        const string expected = "～potential→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialCausative_v5u()
    {
        const string termToDeconjugate = "言えさせる";
        const string expected = "～potential→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってあげる";
        const string expected = "～do for someone";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruPassive_v5u()
    {
        const string termToDeconjugate = "言ってあげられる";
        const string expected = "～do for someone→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoru_v5u()
    {
        const string termToDeconjugate = "言っておる";
        const string expected = "～teoru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruNegative_v5u()
    {
        const string termToDeconjugate = "言っておらない";
        const string expected = "～teoru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruSlurredNegative_v5u()
    {
        const string termToDeconjugate = "言っておらん";
        const string expected = "～teoru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruAffirmative_v5u()
    {
        const string termToDeconjugate = "言っておった";
        const string expected = "～teoru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruNegative_v5u()
    {
        const string termToDeconjugate = "言っておらなかった";
        const string expected = "～teoru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoru_v5u()
    {
        const string termToDeconjugate = "言っております";
        const string expected = "～teoru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoruNegative_v5u()
    {
        const string termToDeconjugate = "言っておりません";
        const string expected = "～teoru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoru_v5u()
    {
        const string termToDeconjugate = "言っておりました";
        const string expected = "～teoru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruNegative_v5u()
    {
        const string termToDeconjugate = "言っておりませんでした";
        const string expected = "～teoru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruTeForm_v5u()
    {
        const string termToDeconjugate = "言っておって";
        const string expected = "～teoru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruVolitional_v5u()
    {
        const string termToDeconjugate = "言っておろう";
        const string expected = "～teoru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPotential_v5u()
    {
        const string termToDeconjugate = "言っておれる";
        const string expected = "～teoru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPassive_v5u()
    {
        const string termToDeconjugate = "言っておられる";
        const string expected = "～teoru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToru_v5u()
    {
        const string termToDeconjugate = "言っとる";
        const string expected = "～toru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruNegative_v5u()
    {
        const string termToDeconjugate = "言っとらない";
        const string expected = "～toru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruSlurredNegative_v5u()
    {
        const string termToDeconjugate = "言っとらん";
        const string expected = "～toru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruAffirmative_v5u()
    {
        const string termToDeconjugate = "言っとった";
        const string expected = "～toru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruNegative_v5u()
    {
        const string termToDeconjugate = "言っとらなかった";
        const string expected = "～toru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToru_v5u()
    {
        const string termToDeconjugate = "言っとります";
        const string expected = "～toru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToruNegative_v5u()
    {
        const string termToDeconjugate = "言っとりません";
        const string expected = "～toru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToru_v5u()
    {
        const string termToDeconjugate = "言っとりました";
        const string expected = "～toru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruNegative_v5u()
    {
        const string termToDeconjugate = "言っとりませんでした";
        const string expected = "～toru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruTeForm_v5u()
    {
        const string termToDeconjugate = "言っとって";
        const string expected = "～toru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruVolitional_v5u()
    {
        const string termToDeconjugate = "言っとろう";
        const string expected = "～toru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPotential_v5u()
    {
        const string termToDeconjugate = "言っとれる";
        const string expected = "～toru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPassive_v5u()
    {
        const string termToDeconjugate = "言っとられる";
        const string expected = "～toru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShortCausativeAffirmative_v5u()
    {
        const string termToDeconjugate = "言わす";
        const string expected = "～short causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TopicOrCondition_v5u()
    {
        const string termToDeconjugate = "言っては";
        const string expected = "～topic/condition";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedTopicOrConditionCha_v5u()
    {
        const string termToDeconjugate = "言っちゃ";
        const string expected = "～topic/condition→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedProvisionalConditionalNegativeKya_v5u()
    {
        const string termToDeconjugate = "言わなきゃ";
        const string expected = "～negative→provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChimau_v5u()
    {
        const string termToDeconjugate = "言っちまう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChau_v5u()
    {
        const string termToDeconjugate = "言っちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuAffirmative_v5u()
    {
        const string termToDeconjugate = "言っていらっしゃる";
        const string expected = "～honorific teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuNegative_v5u()
    {
        const string termToDeconjugate = "言っていらっしゃらない";
        const string expected = "～honorific teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Tsutsu_v5u()
    {
        const string termToDeconjugate = "言いつつ";
        const string expected = "～while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TsutsuNegative_v5u()
    {
        const string termToDeconjugate = "言わなつつ";
        const string expected = "～negative→while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってくれる";
        const string expected = "～statement/request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestNegative_v5u()
    {
        const string termToDeconjugate = "言ってくれない";
        const string expected = "～statement/request→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestAffirmative_v5u()
    {
        const string termToDeconjugate = "言ってくれます";
        const string expected = "～statement/request→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestNegative_v5u()
    {
        const string termToDeconjugate = "言ってくれません";
        const string expected = "～statement/request→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementImperative_v5u()
    {
        const string termToDeconjugate = "言ってくれ";
        const string expected = "～statement/request→imperative; statement/request→masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenNegative_v5u()
    {
        const string termToDeconjugate = "言わへん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenNegative_v5u()
    {
        const string termToDeconjugate = "言わへんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenSubDialectNegative_v5u()
    {
        const string termToDeconjugate = "言わひん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenSubDialectNegative_v5u()
    {
        const string termToDeconjugate = "言わひんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialCausativeNegative_v5u()
    {
        const string termToDeconjugate = "言わささない";
        const string expected = "～colloquial causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTemporalConditional_v5u()
    {
        const string termToDeconjugate = "言いましたら";
        const string expected = "～polite conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNinaru_v5u()
    {
        const string termToDeconjugate = "言いになる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNasaru_v5u()
    {
        const string termToDeconjugate = "言いなさる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificHaruKsbAffirmative_v5u()
    {
        const string termToDeconjugate = "言いはる";
        const string expected = "～honorific (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastHonorificNegativeNasaruna_v5u()
    {
        const string termToDeconjugate = "言いなさるな";
        const string expected = "～honorific→imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural_v5u()
    {
        const string termToDeconjugate = "言うまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "言う" && form.Tags[^1] is "v5u").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
