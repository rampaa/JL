using JL.Core.Deconjugation;
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

        // Act
        HashSet<Form> result = Deconjugator.Deconjugate("泳げなかった");

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
        Assert.IsTrue(success);
    }
}
