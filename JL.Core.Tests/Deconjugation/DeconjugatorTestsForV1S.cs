using JL.Core.Deconjugation;
using JL.Core.Lookup;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class DeconjugatorTestsForV1S
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_MasuStem_v1s()
    {
        const string termToDeconjugate = "呉れ";
        const string expected = "～imperative; masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegative_v1s()
    {
        const string termToDeconjugate = "呉れない";
        const string expected = "～negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れます";
        const string expected = "～polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastVolitional_v1s()
    {
        const string termToDeconjugate = "呉れましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastNegative_v1s()
    {
        const string termToDeconjugate = "呉れません";
        const string expected = "～polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れた";
        const string expected = "～past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastNegative_v1s()
    {
        const string termToDeconjugate = "呉れなかった";
        const string expected = "～negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れました";
        const string expected = "～polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastNegative_v1s()
    {
        const string termToDeconjugate = "呉れませんでした";
        const string expected = "～polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れて";
        const string expected = "～te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative_v1s()
    {
        const string termToDeconjugate = "呉れなくて";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTeFormNegative2_v1s()
    {
        const string termToDeconjugate = "呉れないで";
        const string expected = "～negative→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteTeFormAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れまして";
        const string expected = "～polite te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassivePotentialHonorificAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れられる";
        const string expected = "～passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastPassivePotentialHonorificNegative_v1s()
    {
        const string termToDeconjugate = "呉れられない";
        const string expected = "～passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassivePotentialHonorificAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れられた";
        const string expected = "～passive/potential/honorific→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassivePotentialHonorificAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れられました";
        const string expected = "～passive/potential/honorific→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastPassivePotentialHonorificNegative_v1s()
    {
        const string termToDeconjugate = "呉れられなかった";
        const string expected = "～passive/potential/honorific→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastPassivePotentialHonorificNegative_v1s()
    {
        const string termToDeconjugate = "呉れられませんでした";
        const string expected = "～passive/potential/honorific→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassivePotentialHonorificAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れられます";
        const string expected = "～passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePassivePotentialHonorificNegative_v1s()
    {
        const string termToDeconjugate = "呉れられません";
        const string expected = "～passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainImperativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れるな";
        const string expected = "～imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteImperativeAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れなさい";
        const string expected = "～polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてください";
        const string expected = "～polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteRequestNegative_v1s()
    {
        const string termToDeconjugate = "呉れないでください";
        const string expected = "～negative→polite request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainVolitionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れよう";
        const string expected = "～volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainKansaibenVolitionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れよ";
        const string expected = "～volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteVolitionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れましょう";
        const string expected = "～polite volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れれば";
        const string expected = "～provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ProvisionalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れなければ";
        const string expected = "～negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れたら";
        const string expected = "～conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_FormalConditionalAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れたらば";
        const string expected = "～formal conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TemporalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れなかったら";
        const string expected = "～negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れさせる";
        const string expected = "～causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせない";
        const string expected = "～causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativeSlurred_v1s()
    {
        const string termToDeconjugate = "呉れさせん";
        const string expected = "～causative→slurred; causative→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れさせます";
        const string expected = "～causative→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせません";
        const string expected = "～causative→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePast_v1s()
    {
        const string termToDeconjugate = "呉れさせた";
        const string expected = "～causative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainCausativePastNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせなかった";
        const string expected = "～causative→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePast_v1s()
    {
        const string termToDeconjugate = "呉れさせました";
        const string expected = "～causative→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteCausativePastNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせませんでした";
        const string expected = "～causative→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れさせられる";
        const string expected = "～causative→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPlainNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせられない";
        const string expected = "～causative→passive/potential/honorific→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れさせられます";
        const string expected = "～causative→passive/potential/honorific→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_CausativePassivePotentialHonorificPoliteNegative_v1s()
    {
        const string termToDeconjugate = "呉れさせられません";
        const string expected = "～causative→passive/potential/honorific→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderative_v1s()
    {
        const string termToDeconjugate = "呉れたい";
        const string expected = "～want";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeFormalNegative_v1s()
    {
        const string termToDeconjugate = "呉れたくありません";
        const string expected = "～want→formal negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeFormalNegative_v1s()
    {
        const string termToDeconjugate = "呉れたくありませんでした";
        const string expected = "～want→formal negative past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastDesiderativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れたくない";
        const string expected = "～want→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderative_v1s()
    {
        const string termToDeconjugate = "呉れたかった";
        const string expected = "～want→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastDesiderativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れたくなかった";
        const string expected = "～want→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiru_v1s()
    {
        const string termToDeconjugate = "呉れている";
        const string expected = "～teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeiruNegative_v1s()
    {
        const string termToDeconjugate = "呉れていない";
        const string expected = "～teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れていた";
        const string expected = "～teiru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeiruNegative_v1s()
    {
        const string termToDeconjugate = "呉れていなかった";
        const string expected = "～teiru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiru_v1s()
    {
        const string termToDeconjugate = "呉れています";
        const string expected = "～teiru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeiruNegative_v1s()
    {
        const string termToDeconjugate = "呉れていません";
        const string expected = "～teiru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiru_v1s()
    {
        const string termToDeconjugate = "呉れていました";
        const string expected = "～teiru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeiruNegative_v1s()
    {
        const string termToDeconjugate = "呉れていませんでした";
        const string expected = "～teiru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeru_v1s()
    {
        const string termToDeconjugate = "呉れてる";
        const string expected = "～teru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてない";
        const string expected = "～teru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeru_v1s()
    {
        const string termToDeconjugate = "呉れてた";
        const string expected = "～teru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてなかった";
        const string expected = "～teru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeru_v1s()
    {
        const string termToDeconjugate = "呉れてます";
        const string expected = "～teru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeru_v1s()
    {
        const string termToDeconjugate = "呉れてました";
        const string expected = "～teru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてません";
        const string expected = "～teru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeruNegative2_v1s()
    {
        const string termToDeconjugate = "呉れてませんでした";
        const string expected = "～teru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてしまう";
        const string expected = "～finish/completely/end up";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauKansaibenAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてもう";
        const string expected = "～finish/completely/end up→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまわない";
        const string expected = "～finish/completely/end up→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてしまった";
        const string expected = "～finish/completely/end up→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまわなかった";
        const string expected = "～finish/completely/end up→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTeForm_v1s()
    {
        const string termToDeconjugate = "呉れてしまって";
        const string expected = "～finish/completely/end up→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてしまえば";
        const string expected = "～finish/completely/end up→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauProvisionalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまわなければ";
        const string expected = "～finish/completely/end up→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまわなかったら";
        const string expected = "～finish/completely/end up→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてしまったら";
        const string expected = "～finish/completely/end up→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauVolitional_v1s()
    {
        const string termToDeconjugate = "呉れてしまおう";
        const string expected = "～finish/completely/end up→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてしまいます";
        const string expected = "～finish/completely/end up→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまいません";
        const string expected = "～finish/completely/end up→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてしまいました";
        const string expected = "～finish/completely/end up→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れてしまいませんでした";
        const string expected = "～finish/completely/end up→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPotential_v1s()
    {
        const string termToDeconjugate = "呉れてしまえる";
        const string expected = "～finish/completely/end up→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauPassive_v1s()
    {
        const string termToDeconjugate = "呉れてしまわれる";
        const string expected = "～finish/completely/end up→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShimauCausative_v1s()
    {
        const string termToDeconjugate = "呉れてしまわせる";
        const string expected = "～finish/completely/end up→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れちゃわない";
        const string expected = "～finish/completely/end up→contracted→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れちゃった";
        const string expected = "～finish/completely/end up→contracted→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastContractedShimauNegative_v1s()
    {
        const string termToDeconjugate = "呉れちゃわなかった";
        const string expected = "～finish/completely/end up→contracted→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTeForm_v1s()
    {
        const string termToDeconjugate = "呉れちゃって";
        const string expected = "～finish/completely/end up→contracted→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れちゃえば";
        const string expected = "～finish/completely/end up→contracted→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauProvisionalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れちゃわなければ";
        const string expected = "～finish/completely/end up→contracted→negative→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauTemporalConditionalNegative_v1s()
    {
        const string termToDeconjugate = "呉れちゃわなかったら";
        const string expected = "～finish/completely/end up→contracted→negative→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauVolitional_v1s()
    {
        const string termToDeconjugate = "呉れちゃおう";
        const string expected = "～finish/completely/end up→contracted→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainContractedShimauPotential_v1s()
    {
        const string termToDeconjugate = "呉れちゃえる";
        const string expected = "～finish/completely/end up→contracted→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Deconjugate_PlainNonPastOkuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れておく";
        const string expected = "～for now";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastOkuNegative_v1s()
    {
        const string termToDeconjugate = "呉れておかない";
        const string expected = "～for now→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れておいた";
        const string expected = "～for now→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastOkuNegative_v1s()
    {
        const string termToDeconjugate = "呉れておかなかった";
        const string expected = "～for now→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTeForm_v1s()
    {
        const string termToDeconjugate = "呉れておいて";
        const string expected = "～for now→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れておけば";
        const string expected = "～for now→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れておいたら";
        const string expected = "～for now→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuVolitional_v1s()
    {
        const string termToDeconjugate = "呉れておこう";
        const string expected = "～for now→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPotential_v1s()
    {
        const string termToDeconjugate = "呉れておける";
        const string expected = "～for now→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainOkuPassive_v1s()
    {
        const string termToDeconjugate = "呉れておかれる";
        const string expected = "～for now→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れとく";
        const string expected = "～toku (for now)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTokuNegative_v1s()
    {
        const string termToDeconjugate = "呉れとかない";
        const string expected = "～toku (for now)→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れといた";
        const string expected = "～toku (for now)→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTokuNegative_v1s()
    {
        const string termToDeconjugate = "呉れとかなかった";
        const string expected = "～toku (for now)→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTeForm_v1s()
    {
        const string termToDeconjugate = "呉れといて";
        const string expected = "～toku (for now)→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れとけば";
        const string expected = "～toku (for now)→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れといたら";
        const string expected = "～toku (for now)→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuVolitional_v1s()
    {
        const string termToDeconjugate = "呉れとこう";
        const string expected = "～toku (for now)→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPotential_v1s()
    {
        const string termToDeconjugate = "呉れとける";
        const string expected = "～toku (for now)→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTokuPassive_v1s()
    {
        const string termToDeconjugate = "呉れとかれる";
        const string expected = "～toku (for now)→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTearuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてある";
        const string expected = "～tearu";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTearuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてあった";
        const string expected = "～tearu→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTeForm_v1s()
    {
        const string termToDeconjugate = "呉れてあって";
        const string expected = "～tearu→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてあったら";
        const string expected = "～tearu→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTearuProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてあれば";
        const string expected = "～tearu→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れていく";
        const string expected = "～teiku";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeikuNegative_v1s()
    {
        const string termToDeconjugate = "呉れていかない";
        const string expected = "～teiku→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れていった";
        const string expected = "～teiku→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeikuNegative_v1s()
    {
        const string termToDeconjugate = "呉れていかなかった";
        const string expected = "～teiku→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuTeForm_v1s()
    {
        const string termToDeconjugate = "呉れていって";
        const string expected = "～teiku→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuVolitional_v1s()
    {
        const string termToDeconjugate = "呉れていこう";
        const string expected = "～teiku→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPotential_v1s()
    {
        const string termToDeconjugate = "呉れていける";
        const string expected = "～teiku→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuPassive_v1s()
    {
        const string termToDeconjugate = "呉れていかれる";
        const string expected = "～teiku→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TeikuCausative_v1s()
    {
        const string termToDeconjugate = "呉れていかせる";
        const string expected = "～teiku→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてくる";
        const string expected = "～tekuru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTekuruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてこない";
        const string expected = "～tekuru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてきた";
        const string expected = "～tekuru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTekuruNegative_v1s()
    {
        const string termToDeconjugate = "呉れてこなかった";
        const string expected = "～tekuru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTeForm_v1s()
    {
        const string termToDeconjugate = "呉れてきて";
        const string expected = "～tekuru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてくれば";
        const string expected = "～tekuru→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TekuruTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れてきたら";
        const string expected = "～tekuru→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruPassivePotentialAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてこられる";
        const string expected = "～tekuru→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainTekuruCausativeAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてこさせる";
        const string expected = "～tekuru→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Nagara_v1s()
    {
        const string termToDeconjugate = "呉れながら";
        const string expected = "～while";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSugiruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れすぎる";
        const string expected = "～too much";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSouAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れそう";
        const string expected = "～seemingness";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeFormNu_v1s()
    {
        const string termToDeconjugate = "呉れぬ";
        const string expected = "～archaic negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalNegativeContinuativeFormZu_v1s()
    {
        const string termToDeconjugate = "呉れず";
        const string expected = "～adverbial negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ClassicalAdverbialFormZuNi_v1s()
    {
        const string termToDeconjugate = "呉れずに";
        const string expected = "～without doing so";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れたり";
        const string expected = "～tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTariNegative_v1s()
    {
        const string termToDeconjugate = "呉れなかったり";
        const string expected = "～negative→tari";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastSlurredAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れん";
        const string expected = "～slurred; slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastSlurredNegative_v1s()
    {
        const string termToDeconjugate = "呉れんかった";
        const string expected = "～slurred negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Zaru_v1s()
    {
        const string termToDeconjugate = "呉れざる";
        const string expected = "～archaic attributive negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れれる";
        const string expected = "～potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastColloquialPotentialAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れれます";
        const string expected = "～potential→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastColloquialPotentialAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れれた";
        const string expected = "～potential→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastColloquialPotentialAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れれました";
        const string expected = "～potential→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialNegative_v1s()
    {
        const string termToDeconjugate = "呉れれない";
        const string expected = "～potential→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastColloquialPotentialNegative_v1s()
    {
        const string termToDeconjugate = "呉れれません";
        const string expected = "～potential→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialVolitional_v1s()
    {
        const string termToDeconjugate = "呉れれよう";
        const string expected = "～potential→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenColloquialPotentialVolitional_v1s()
    {
        const string termToDeconjugate = "呉れれよ";
        const string expected = "～potential→volitional→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialImperative_v1s()
    {
        const string termToDeconjugate = "呉れれろ";
        const string expected = "～potential→imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialTeForm_v1s()
    {
        const string termToDeconjugate = "呉れれて";
        const string expected = "～potential→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れれたら";
        const string expected = "～potential→conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialProvisionalConditional_v1s()
    {
        const string termToDeconjugate = "呉れれれば";
        const string expected = "～potential→provisional conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialPassivePotential_v1s()
    {
        const string termToDeconjugate = "呉れれられる";
        const string expected = "～potential→passive/potential/honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialPotentialCausative_v1s()
    {
        const string termToDeconjugate = "呉れれさせる";
        const string expected = "～potential→causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてあげる";
        const string expected = "～do for someone";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastAgeruPassive_v1s()
    {
        const string termToDeconjugate = "呉れてあげられる";
        const string expected = "～do for someone→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoru_v1s()
    {
        const string termToDeconjugate = "呉れておる";
        const string expected = "～teoru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruNegative_v1s()
    {
        const string termToDeconjugate = "呉れておらない";
        const string expected = "～teoru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastTeoruSlurredNegative_v1s()
    {
        const string termToDeconjugate = "呉れておらん";
        const string expected = "～teoru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れておった";
        const string expected = "～teoru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastTeoruNegative_v1s()
    {
        const string termToDeconjugate = "呉れておらなかった";
        const string expected = "～teoru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoru_v1s()
    {
        const string termToDeconjugate = "呉れております";
        const string expected = "～teoru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTeoruNegative_v1s()
    {
        const string termToDeconjugate = "呉れておりません";
        const string expected = "～teoru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoru_v1s()
    {
        const string termToDeconjugate = "呉れておりました";
        const string expected = "～teoru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruNegative_v1s()
    {
        const string termToDeconjugate = "呉れておりませんでした";
        const string expected = "～teoru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruTeForm_v1s()
    {
        const string termToDeconjugate = "呉れておって";
        const string expected = "～teoru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruVolitional_v1s()
    {
        const string termToDeconjugate = "呉れておろう";
        const string expected = "～teoru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPotential_v1s()
    {
        const string termToDeconjugate = "呉れておれる";
        const string expected = "～teoru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastTeoruPassive_v1s()
    {
        const string termToDeconjugate = "呉れておられる";
        const string expected = "～teoru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToru_v1s()
    {
        const string termToDeconjugate = "呉れとる";
        const string expected = "～toru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruNegative_v1s()
    {
        const string termToDeconjugate = "呉れとらない";
        const string expected = "～toru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastToruSlurredNegative_v1s()
    {
        const string termToDeconjugate = "呉れとらん";
        const string expected = "～toru→slurred negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れとった";
        const string expected = "～toru→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastToruNegative_v1s()
    {
        const string termToDeconjugate = "呉れとらなかった";
        const string expected = "～toru→negative→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToru_v1s()
    {
        const string termToDeconjugate = "呉れとります";
        const string expected = "～toru→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastToruNegative_v1s()
    {
        const string termToDeconjugate = "呉れとりません";
        const string expected = "～toru→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToru_v1s()
    {
        const string termToDeconjugate = "呉れとりました";
        const string expected = "～toru→polite past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruNegative_v1s()
    {
        const string termToDeconjugate = "呉れとりませんでした";
        const string expected = "～toru→polite past negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruTeForm_v1s()
    {
        const string termToDeconjugate = "呉れとって";
        const string expected = "～toru→te";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruVolitional_v1s()
    {
        const string termToDeconjugate = "呉れとろう";
        const string expected = "～toru→volitional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPotential_v1s()
    {
        const string termToDeconjugate = "呉れとれる";
        const string expected = "～toru→potential";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PolitePastToruPassive_v1s()
    {
        const string termToDeconjugate = "呉れとられる";
        const string expected = "～toru→passive";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainShortCausativeAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れさす";
        const string expected = "～short causative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNa_v1s()
    {
        const string termToDeconjugate = "呉れな";
        const string expected = "～casual polite imperative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_TopicOrCondition_v1s()
    {
        const string termToDeconjugate = "呉れては";
        const string expected = "～topic/condition";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedTopicOrConditionCha_v1s()
    {
        const string termToDeconjugate = "呉れちゃ";
        const string expected = "～topic/condition→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedProvisionalConditionalNegativeKya_v1s()
    {
        const string termToDeconjugate = "呉れなきゃ";
        const string expected = "～negative→provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChimau_v1s()
    {
        const string termToDeconjugate = "呉れちまう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastContractedShimauChau_v1s()
    {
        const string termToDeconjugate = "呉れちゃう";
        const string expected = "～finish/completely/end up→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れていらっしゃる";
        const string expected = "～honorific teiru";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastIrassharuNegative_v1s()
    {
        const string termToDeconjugate = "呉れていらっしゃらない";
        const string expected = "～honorific teiru→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_Tsutsu_v1s()
    {
        const string termToDeconjugate = "呉れつつ";
        const string expected = "～while/although";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてくれる";
        const string expected = "～statement/request";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastStatementRequestNegative_v1s()
    {
        const string termToDeconjugate = "呉れてくれない";
        const string expected = "～statement/request→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れてくれます";
        const string expected = "～statement/request→polite";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementRequestNegative_v1s()
    {
        const string termToDeconjugate = "呉れてくれません";
        const string expected = "～statement/request→polite negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastStatementImperative_v1s()
    {
        const string termToDeconjugate = "呉れてくれ";
        const string expected = "～statement/request→imperative; statement/request→masu stem";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenNegative_v1s()
    {
        const string termToDeconjugate = "呉れへん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenNegative_v1s()
    {
        const string termToDeconjugate = "呉れへんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastKansaibenSubDialectNegative_v1s()
    {
        const string termToDeconjugate = "呉れひん";
        const string expected = "～negative→ksb";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainPastKansaibenSubDialectNegative_v1s()
    {
        const string termToDeconjugate = "呉れひんかった";
        const string expected = "～negative→ksb→past";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_KansaibenImperative_v1s()
    {
        const string termToDeconjugate = "呉れい";
        const string expected = "～imperative (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_ContractedProvisionalConditionalRya_v1s()
    {
        const string termToDeconjugate = "呉れりゃ";
        const string expected = "～provisional conditional→contracted";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastColloquialCausativeNegative_v1s()
    {
        const string termToDeconjugate = "呉れささない";
        const string expected = "～colloquial causative→negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastTemporalConditional_v1s()
    {
        const string termToDeconjugate = "呉れましたら";
        const string expected = "～polite conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNinaru_v1s()
    {
        const string termToDeconjugate = "呉れになる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificNasaru_v1s()
    {
        const string termToDeconjugate = "呉れなさる";
        const string expected = "～honorific";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PoliteNonPastHonorificHaruKsbAffirmative_v1s()
    {
        const string termToDeconjugate = "呉れはる";
        const string expected = "～honorific (ksb)";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastHonorificNegativeNasaruna_v1s()
    {
        const string termToDeconjugate = "呉れなさるな";
        const string expected = "～honorific→imperative negative";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConjectural_v1s()
    {
        const string termToDeconjugate = "呉れまい";
        const string expected = "～negative conjectural";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Deconjugate_PlainNonPastNegativeConditional_v1_s()
    {
        const string termToDeconjugate = "呉れねば";
        const string expected = "～negative conditional";
        string? actual = LookupResultUtils.DeconjugationProcessesToText(Deconjugator.Deconjugate(termToDeconjugate).Where(static form => form.Text is "呉れる" && form.Tags[^1] is "v1-s").Select(static form => form.Process).ToList().AsReadOnlySpan());
        Assert.That(actual, Is.EqualTo(expected));
    }
}
