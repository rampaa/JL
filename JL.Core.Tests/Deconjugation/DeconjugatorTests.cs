using System.Text.Json;
using JL.Core.Deconjugation;
using JL.Core.Utilities;
using NUnit.Framework;

namespace JL.Core.Tests.Deconjugation;

[TestFixture]
public class DeconjugatorTests
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
        HashSet<Form> result = Deconjugator.Deconjugate("わからない");

        bool success = false;
        foreach (Form form in result)
        {
            if (form.Text is expectedText && form.Process.FirstOrDefault() is expectedProcess)
            {
                success = true;
                break;
            }
        }

        // Assert
        Assert.IsTrue(success);
    }

    [Test]
    public void Deconjugate_泳げなかった()
    {
        // Arrange
        const string expectedText = "泳ぐ";
        List<string> expectedProcess = new() { "negative", "past" };

        // Act
        HashSet<Form> result = Deconjugator.Deconjugate("わからない");

        bool success = false;
        foreach (Form form in result)
        {
            if (form.Text is expectedText && form.Process.SequenceEqual(expectedProcess))
            {
                success = true;
                break;
            }
        }

        // Assert
        Assert.IsTrue(success);
    }
}
