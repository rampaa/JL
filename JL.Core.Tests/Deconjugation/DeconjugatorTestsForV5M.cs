using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForV5M
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_MasuStem_V5M()
    {
        const string termToDeconjugate = "読み";
        const string expected = "～masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegative_V5M()
    {
        const string termToDeconjugate = "読まない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastAffirmative_V5M()
    {
        const string termToDeconjugate = "読みます";
        const string expected = "～polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastVolitional_V5M()
    {
        const string termToDeconjugate = "読みましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegative_V5M()
    {
        const string termToDeconjugate = "読みません";
        const string expected = "～polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastAffirmative_V5M()
    {
        const string termToDeconjugate = "読んだ";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastNegative_V5M()
    {
        const string termToDeconjugate = "読まなかった";
        const string expected = "～negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastAffirmative_V5M()
    {
        const string termToDeconjugate = "読みました";
        const string expected = "～polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastNegative_V5M()
    {
        const string termToDeconjugate = "読みませんでした";
        const string expected = "～polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormAffirmative_V5M()
    {
        const string termToDeconjugate = "読んで";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative_V5M()
    {
        const string termToDeconjugate = "読まなくて";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative2_V5M()
    {
        const string termToDeconjugate = "読まないで";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteTeFormAffirmative_V5M()
    {
        const string termToDeconjugate = "読みまして";
        const string expected = "～polite te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialAffirmative_V5M()
    {
        const string termToDeconjugate = "読める";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveAffirmative_V5M()
    {
        const string termToDeconjugate = "読まれる";
        const string expected = "～passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialNegative_V5M()
    {
        const string termToDeconjugate = "読めない";
        const string expected = "～potential→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassiveNegative_V5M()
    {
        const string termToDeconjugate = "読まれない";
        const string expected = "～passive→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialAffirmative_V5M()
    {
        const string termToDeconjugate = "読めた";
        const string expected = "～potential→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveAffirmative_V5M()
    {
        const string termToDeconjugate = "読まれた";
        const string expected = "～passive→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialAffirmative_V5M()
    {
        const string termToDeconjugate = "読めました";
        const string expected = "～potential→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveAffirmative_V5M()
    {
        const string termToDeconjugate = "読まれました";
        const string expected = "～passive→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPotentialNegative_V5M()
    {
        const string termToDeconjugate = "読めなかった";
        const string expected = "～potential→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassiveNegative_V5M()
    {
        const string termToDeconjugate = "読まれなかった";
        const string expected = "～passive→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPotentialNegative_V5M()
    {
        const string termToDeconjugate = "読めませんでした";
        const string expected = "～potential→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassiveNegative_V5M()
    {
        const string termToDeconjugate = "読まれませんでした";
        const string expected = "～passive→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialAffirmative_V5M()
    {
        const string termToDeconjugate = "読めます";
        const string expected = "～potential→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveAffirmative_V5M()
    {
        const string termToDeconjugate = "読まれます";
        const string expected = "～passive→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePotentialNegative_V5M()
    {
        const string termToDeconjugate = "読めません";
        const string expected = "～potential→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassiveNegative_V5M()
    {
        const string termToDeconjugate = "読まれません";
        const string expected = "～passive→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読め";
        const string expected = "～imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeNegative_V5M()
    {
        const string termToDeconjugate = "読むな";
        const string expected = "～imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteImperativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読みなさい";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでください";
        const string expected = "～polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestNegative_V5M()
    {
        const string termToDeconjugate = "読まないでください";
        const string expected = "～negative→polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読もう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainKansaibenVolitionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読も";
        const string expected = "～volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteVolitionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読みましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読めば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読まなければ";
        const string expected = "～negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読んだら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_FormalConditionalAffirmative_V5M()
    {
        const string termToDeconjugate = "読んだらば";
        const string expected = "～formal conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読まなかったら";
        const string expected = "～negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読ませる";
        const string expected = "～causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeNegative_V5M()
    {
        const string termToDeconjugate = "読ませない";
        const string expected = "～causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeSlurred_V5M()
    {
        const string termToDeconjugate = "読ません";
        const string expected = "～causative→slurred; causative→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読ませます";
        const string expected = "～causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeNegative_V5M()
    {
        const string termToDeconjugate = "読ませません";
        const string expected = "～causative→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePast_V5M()
    {
        const string termToDeconjugate = "読ませた";
        const string expected = "～causative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePastNegative_V5M()
    {
        const string termToDeconjugate = "読ませなかった";
        const string expected = "～causative→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePast_V5M()
    {
        const string termToDeconjugate = "読ませました";
        const string expected = "～causative→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePastNegative_V5M()
    {
        const string termToDeconjugate = "読ませませんでした";
        const string expected = "～causative→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainAffirmative_V5M()
    {
        const string termToDeconjugate = "読ませられる";
        const string expected = "～causative→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainNegative_V5M()
    {
        const string termToDeconjugate = "読ませられない";
        const string expected = "～causative→passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteAffirmative_V5M()
    {
        const string termToDeconjugate = "読ませられます";
        const string expected = "～causative→passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteNegative_V5M()
    {
        const string termToDeconjugate = "読ませられません";
        const string expected = "～causative→passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderative_V5M()
    {
        const string termToDeconjugate = "読みたい";
        const string expected = "～want";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeFormalNegative_V5M()
    {
        const string termToDeconjugate = "読みたくありません";
        const string expected = "～want→formal negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeFormalNegative_V5M()
    {
        const string termToDeconjugate = "読みたくありませんでした";
        const string expected = "～want→formal negative past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeNegative_V5M()
    {
        const string termToDeconjugate = "読みたくない";
        const string expected = "～want→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderative_V5M()
    {
        const string termToDeconjugate = "読みたかった";
        const string expected = "～want→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeNegative_V5M()
    {
        const string termToDeconjugate = "読みたくなかった";
        const string expected = "～want→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiru_V5M()
    {
        const string termToDeconjugate = "読んでいる";
        const string expected = "～teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiruNegative_V5M()
    {
        const string termToDeconjugate = "読んでいない";
        const string expected = "～teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでいた";
        const string expected = "～teiru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruNegative_V5M()
    {
        const string termToDeconjugate = "読んでいなかった";
        const string expected = "～teiru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiru_V5M()
    {
        const string termToDeconjugate = "読んでいます";
        const string expected = "～teiru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiruNegative_V5M()
    {
        const string termToDeconjugate = "読んでいません";
        const string expected = "～teiru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiru_V5M()
    {
        const string termToDeconjugate = "読んでいました";
        const string expected = "～teiru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiruNegative_V5M()
    {
        const string termToDeconjugate = "読んでいませんでした";
        const string expected = "～teiru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeru_V5M()
    {
        const string termToDeconjugate = "読んでる";
        const string expected = "～teru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeruNegative_V5M()
    {
        const string termToDeconjugate = "読んでない";
        const string expected = "～teru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeru_V5M()
    {
        const string termToDeconjugate = "読んでた";
        const string expected = "～teru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeruNegative_V5M()
    {
        const string termToDeconjugate = "読んでなかった";
        const string expected = "～teru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeru_V5M()
    {
        const string termToDeconjugate = "読んでます";
        const string expected = "～teru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeruNegative_V5M()
    {
        const string termToDeconjugate = "読んでません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeru_V5M()
    {
        const string termToDeconjugate = "読んでました";
        const string expected = "～teru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative_V5M()
    {
        const string termToDeconjugate = "読んでません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative2_V5M()
    {
        const string termToDeconjugate = "読んでませんでした";
        const string expected = "～teru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでしまう";
        const string expected = "～finish/completely/end up";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauKansaibenAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでもう";
        const string expected = "～finish/completely/end up→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまわない";
        const string expected = "～finish/completely/end up→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでしまった";
        const string expected = "～finish/completely/end up→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまわなかった";
        const string expected = "～finish/completely/end up→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTeForm_V5M()
    {
        const string termToDeconjugate = "読んでしまって";
        const string expected = "～finish/completely/end up→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んでしまえば";
        const string expected = "～finish/completely/end up→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまわなければ";
        const string expected = "～finish/completely/end up→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまわなかったら";
        const string expected = "～finish/completely/end up→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読んでしまったら";
        const string expected = "～finish/completely/end up→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauVolitional_V5M()
    {
        const string termToDeconjugate = "読んでしまおう";
        const string expected = "～finish/completely/end up→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでしまいます";
        const string expected = "～finish/completely/end up→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまいません";
        const string expected = "～finish/completely/end up→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでしまいました";
        const string expected = "～finish/completely/end up→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んでしまいませんでした";
        const string expected = "～finish/completely/end up→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPotential_V5M()
    {
        const string termToDeconjugate = "読んでしまえる";
        const string expected = "～finish/completely/end up→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPassive_V5M()
    {
        const string termToDeconjugate = "読んでしまわれる";
        const string expected = "～finish/completely/end up→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauCausative_V5M()
    {
        const string termToDeconjugate = "読んでしまわせる";
        const string expected = "～finish/completely/end up→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んじゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んじゃわない";
        const string expected = "～finish/completely/end up→contracted→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauAffirmative_V5M()
    {
        const string termToDeconjugate = "読んじゃった";
        const string expected = "～finish/completely/end up→contracted→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauNegative_V5M()
    {
        const string termToDeconjugate = "読んじゃわなかった";
        const string expected = "～finish/completely/end up→contracted→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTeForm_V5M()
    {
        const string termToDeconjugate = "読んじゃって";
        const string expected = "～finish/completely/end up→contracted→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んじゃえば";
        const string expected = "～finish/completely/end up→contracted→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読んじゃわなければ";
        const string expected = "～finish/completely/end up→contracted→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTemporalConditionalNegative_V5M()
    {
        const string termToDeconjugate = "読んじゃわなかったら";
        const string expected = "～finish/completely/end up→contracted→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauVolitional_V5M()
    {
        const string termToDeconjugate = "読んじゃおう";
        const string expected = "～finish/completely/end up→contracted→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauPotential_V5M()
    {
        const string termToDeconjugate = "読んじゃえる";
        const string expected = "～finish/completely/end up→contracted→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastOkuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでおく";
        const string expected = "～for now";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastOkuNegative_V5M()
    {
        const string termToDeconjugate = "読んでおかない";
        const string expected = "～for now→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでおいた";
        const string expected = "～for now→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuNegative_V5M()
    {
        const string termToDeconjugate = "読んでおかなかった";
        const string expected = "～for now→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTeForm_V5M()
    {
        const string termToDeconjugate = "読んでおいて";
        const string expected = "～for now→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んでおけば";
        const string expected = "～for now→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読んでおいたら";
        const string expected = "～for now→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuVolitional_V5M()
    {
        const string termToDeconjugate = "読んでおこう";
        const string expected = "～for now→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPotential_V5M()
    {
        const string termToDeconjugate = "読んでおける";
        const string expected = "～for now→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPassive_V5M()
    {
        const string termToDeconjugate = "読んでおかれる";
        const string expected = "～for now→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んどく";
        const string expected = "～toku (for now)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuNegative_V5M()
    {
        const string termToDeconjugate = "読んどかない";
        const string expected = "～toku (for now)→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んどいた";
        const string expected = "～toku (for now)→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuNegative_V5M()
    {
        const string termToDeconjugate = "読んどかなかった";
        const string expected = "～toku (for now)→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTeForm_V5M()
    {
        const string termToDeconjugate = "読んどいて";
        const string expected = "～toku (for now)→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んどけば";
        const string expected = "～toku (for now)→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読んどいたら";
        const string expected = "～toku (for now)→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuVolitional_V5M()
    {
        const string termToDeconjugate = "読んどこう";
        const string expected = "～toku (for now)→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPotential_V5M()
    {
        const string termToDeconjugate = "読んどける";
        const string expected = "～toku (for now)→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPassive_V5M()
    {
        const string termToDeconjugate = "読んどかれる";
        const string expected = "～toku (for now)→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTearuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んである";
        const string expected = "～tearu";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTearuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んであった";
        const string expected = "～tearu→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTeForm_V5M()
    {
        const string termToDeconjugate = "読んであって";
        const string expected = "～tearu→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読んであったら";
        const string expected = "～tearu→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んであれば";
        const string expected = "～tearu→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでいく";
        const string expected = "～teiku";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuNegative_V5M()
    {
        const string termToDeconjugate = "読んでいかない";
        const string expected = "～teiku→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでいった";
        const string expected = "～teiku→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuNegative_V5M()
    {
        const string termToDeconjugate = "読んでいかなかった";
        const string expected = "～teiku→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuTeForm_V5M()
    {
        const string termToDeconjugate = "読んでいって";
        const string expected = "～teiku→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuVolitional_V5M()
    {
        const string termToDeconjugate = "読んでいこう";
        const string expected = "～teiku→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPotential_V5M()
    {
        const string termToDeconjugate = "読んでいける";
        const string expected = "～teiku→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPassive_V5M()
    {
        const string termToDeconjugate = "読んでいかれる";
        const string expected = "～teiku→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuCausative_V5M()
    {
        const string termToDeconjugate = "読んでいかせる";
        const string expected = "～teiku→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでくる";
        const string expected = "～tekuru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruNegative_V5M()
    {
        const string termToDeconjugate = "読んでこない";
        const string expected = "～tekuru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んできた";
        const string expected = "～tekuru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruNegative_V5M()
    {
        const string termToDeconjugate = "読んでこなかった";
        const string expected = "～tekuru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTeForm_V5M()
    {
        const string termToDeconjugate = "読んできて";
        const string expected = "～tekuru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読んでくれば";
        const string expected = "～tekuru→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読んできたら";
        const string expected = "～tekuru→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruPassivePotentialAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでこられる";
        const string expected = "～tekuru→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruCausativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでこさせる";
        const string expected = "～tekuru→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nagara_V5M()
    {
        const string termToDeconjugate = "読みながら";
        const string expected = "～while";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative_V5M()
    {
        const string termToDeconjugate = "読みすぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouAffirmative_V5M()
    {
        const string termToDeconjugate = "読みそう";
        const string expected = "～seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeFormNu_V5M()
    {
        const string termToDeconjugate = "読まぬ";
        const string expected = "～archaic negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu_V5M()
    {
        const string termToDeconjugate = "読まず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalAdverbialFormZuNi_V5M()
    {
        const string termToDeconjugate = "読まずに";
        const string expected = "～without doing so";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariAffirmative_V5M()
    {
        const string termToDeconjugate = "読んだり";
        const string expected = "～tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariNegative_V5M()
    {
        const string termToDeconjugate = "読まなかったり";
        const string expected = "～negative→tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSlurredAffirmative_V5M()
    {
        const string termToDeconjugate = "読まん";
        const string expected = "～slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastSlurredNegative_V5M()
    {
        const string termToDeconjugate = "読まんかった";
        const string expected = "～slurred negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Zaru_V5M()
    {
        const string termToDeconjugate = "読まざる";
        const string expected = "～archaic attributive negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialVolitional_V5M()
    {
        const string termToDeconjugate = "読めよう";
        const string expected = "～potential→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenPotentialVolitional_V5M()
    {
        const string termToDeconjugate = "読めよ";
        const string expected = "～potential→volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialImperative_V5M()
    {
        const string termToDeconjugate = "読めろ";
        const string expected = "～potential→imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTeForm_V5M()
    {
        const string termToDeconjugate = "読めて";
        const string expected = "～potential→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読めたら";
        const string expected = "～potential→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialProvisionalConditional_V5M()
    {
        const string termToDeconjugate = "読めれば";
        const string expected = "～potential→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialPassivePotential_V5M()
    {
        const string termToDeconjugate = "読められる";
        const string expected = "～potential→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPotentialCausative_V5M()
    {
        const string termToDeconjugate = "読めさせる";
        const string expected = "～potential→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んであげる";
        const string expected = "～do for someone";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruPassive_V5M()
    {
        const string termToDeconjugate = "読んであげられる";
        const string expected = "～do for someone→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoru_V5M()
    {
        const string termToDeconjugate = "読んでおる";
        const string expected = "～teoru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruNegative_V5M()
    {
        const string termToDeconjugate = "読んでおらない";
        const string expected = "～teoru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruSlurredNegative_V5M()
    {
        const string termToDeconjugate = "読んでおらん";
        const string expected = "～teoru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでおった";
        const string expected = "～teoru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruNegative_V5M()
    {
        const string termToDeconjugate = "読んでおらなかった";
        const string expected = "～teoru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoru_V5M()
    {
        const string termToDeconjugate = "読んでおります";
        const string expected = "～teoru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoruNegative_V5M()
    {
        const string termToDeconjugate = "読んでおりません";
        const string expected = "～teoru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoru_V5M()
    {
        const string termToDeconjugate = "読んでおりました";
        const string expected = "～teoru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruNegative_V5M()
    {
        const string termToDeconjugate = "読んでおりませんでした";
        const string expected = "～teoru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruTeForm_V5M()
    {
        const string termToDeconjugate = "読んでおって";
        const string expected = "～teoru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruVolitional_V5M()
    {
        const string termToDeconjugate = "読んでおろう";
        const string expected = "～teoru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPotential_V5M()
    {
        const string termToDeconjugate = "読んでおれる";
        const string expected = "～teoru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPassive_V5M()
    {
        const string termToDeconjugate = "読んでおられる";
        const string expected = "～teoru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToru_V5M()
    {
        const string termToDeconjugate = "読んどる";
        const string expected = "～toru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruNegative_V5M()
    {
        const string termToDeconjugate = "読んどらない";
        const string expected = "～toru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruSlurredNegative_V5M()
    {
        const string termToDeconjugate = "読んどらん";
        const string expected = "～toru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruAffirmative_V5M()
    {
        const string termToDeconjugate = "読んどった";
        const string expected = "～toru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruNegative_V5M()
    {
        const string termToDeconjugate = "読んどらなかった";
        const string expected = "～toru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToru_V5M()
    {
        const string termToDeconjugate = "読んどります";
        const string expected = "～toru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToruNegative_V5M()
    {
        const string termToDeconjugate = "読んどりません";
        const string expected = "～toru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToru_V5M()
    {
        const string termToDeconjugate = "読んどりました";
        const string expected = "～toru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruNegative_V5M()
    {
        const string termToDeconjugate = "読んどりませんでした";
        const string expected = "～toru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruTeForm_V5M()
    {
        const string termToDeconjugate = "読んどって";
        const string expected = "～toru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruVolitional_V5M()
    {
        const string termToDeconjugate = "読んどろう";
        const string expected = "～toru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPotential_V5M()
    {
        const string termToDeconjugate = "読んどれる";
        const string expected = "～toru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPassive_V5M()
    {
        const string termToDeconjugate = "読んどられる";
        const string expected = "～toru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShortCausativeAffirmative_V5M()
    {
        const string termToDeconjugate = "読ます";
        const string expected = "～short causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TopicOrCondition_V5M()
    {
        const string termToDeconjugate = "読んでは";
        const string expected = "～topic/condition";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedTopicOrConditionCha_V5M()
    {
        const string termToDeconjugate = "読んじゃ";
        const string expected = "～topic/condition→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedProvisionalConditionalNegativeKya_V5M()
    {
        const string termToDeconjugate = "読まなきゃ";
        const string expected = "～negative→provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChimau_V5M()
    {
        const string termToDeconjugate = "読んじまう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChau_V5M()
    {
        const string termToDeconjugate = "読んじゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでいらっしゃる";
        const string expected = "～honorific teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuNegative_V5M()
    {
        const string termToDeconjugate = "読んでいらっしゃらない";
        const string expected = "～honorific teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Tsutsu_V5M()
    {
        const string termToDeconjugate = "読みつつ";
        const string expected = "～while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでくれる";
        const string expected = "～statement/request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestNegative_V5M()
    {
        const string termToDeconjugate = "読んでくれない";
        const string expected = "～statement/request→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestAffirmative_V5M()
    {
        const string termToDeconjugate = "読んでくれます";
        const string expected = "～statement/request→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestNegative_V5M()
    {
        const string termToDeconjugate = "読んでくれません";
        const string expected = "～statement/request→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementImperative_V5M()
    {
        const string termToDeconjugate = "読んでくれ";
        const string expected = "～statement/request→imperative; statement/request→masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenNegative_V5M()
    {
        const string termToDeconjugate = "読まへん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenNegative_V5M()
    {
        const string termToDeconjugate = "読まへんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenSubDialectNegative_V5M()
    {
        const string termToDeconjugate = "読まひん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenSubDialectNegative_V5M()
    {
        const string termToDeconjugate = "読まひんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialCausativeNegative_V5M()
    {
        const string termToDeconjugate = "読まささない";
        const string expected = "～colloquial causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTemporalConditional_V5M()
    {
        const string termToDeconjugate = "読みましたら";
        const string expected = "～polite conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNinaru_V5M()
    {
        const string termToDeconjugate = "読みになる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNasaru_V5M()
    {
        const string termToDeconjugate = "読みなさる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificHaruKsbAffirmative_V5M()
    {
        const string termToDeconjugate = "読みはる";
        const string expected = "～honorific (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastHonorificNegativeNasaruna_V5M()
    {
        const string termToDeconjugate = "読みなさるな";
        const string expected = "～honorific→imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural_V5M()
    {
        const string termToDeconjugate = "読むまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastClassicalHypotheticalConditional_V5M()
    {
        const string termToDeconjugate = "読まば";
        const string expected = "～classical hypothetical conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConditional_V5M()
    {
        const string termToDeconjugate = "読まねば";
        const string expected = "～negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialNegativeConditional_V5M()
    {
        const string termToDeconjugate = "読まにゃ";
        const string expected = "～colloquial negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "読む" && form.Tags[^1] is "v5m").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
