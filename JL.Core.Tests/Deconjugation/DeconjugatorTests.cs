using JL.Core.Deconjugation;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
#pragma warning disable CA1812
internal sealed class DeconjugatorTests
#pragma warning restore CA1812
{
    [OneTimeSetUp]
    public void ClassInit()
    {
        DeconjugatorUtils.DeserializeRules().Wait();
    }

    [Test]
    public void Deconjugate_わからない()
    {
        // Arrange
        const string expectedText = "わかる";
        const string expectedProcess = "negative";

        // Act
        List<Form> result = Deconjugator.Deconjugate("わからない");

        bool success = false;
        foreach (Form form in result)
        {
            if (form is { Text: expectedText, Process.Count: > 0 } && form.Process[0] is expectedProcess)
            {
                success = true;
                break;
            }
        }

        // Assert
        Assert.That(success);
    }

    [Test]
    public void Deconjugate_泳げなかった()
    {
        // Arrange
        const string expectedText = "泳ぐ";

        // Act
        List<Form> result = Deconjugator.Deconjugate("泳げなかった");

        bool success = false;
        foreach (Form form in result)
        {
            if (form.Text is expectedText
                && form.Process.Contains("potential")
                && form.Process.Contains("negative")
                && form.Process.Contains("past"))
            {
                success = true;
                break;
            }
        }

        // Assert
        Assert.That(success);
    }
}
