using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectSettingsParserTests
{
    [Test]
    public void TranslationInfoEmptyValues()
    {
        ParatextProjectSettings settings = CreateSettings("<TranslationInfo></TranslationInfo>");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
            Assert.That(settings.ParentName, Is.Null);
            Assert.That(settings.ParentGuid, Is.Null);
        }
    }

    [Test]
    public void TranslationInfoNoParentSpecified()
    {
        ParatextProjectSettings settings = CreateSettings("<TranslationInfo>BackTranslation::</TranslationInfo>");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.TranslationType, Is.EqualTo("BackTranslation"));
            Assert.That(settings.ParentName, Is.Null);
            Assert.That(settings.ParentGuid, Is.Null);
        }
    }

    [Test]
    public void TranslationInfoSpecified()
    {
        ParatextProjectSettings settings = CreateSettings(
            "<TranslationInfo>Daughter:DEF:22222222222222222222222222222222</TranslationInfo>"
        );
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.TranslationType, Is.EqualTo("Daughter"));
            Assert.That(settings.ParentName, Is.EqualTo("DEF"));
            Assert.That(settings.ParentGuid, Is.EqualTo("22222222222222222222222222222222"));
        }
    }

    private static ParatextProjectSettings CreateSettings(string additionalSettingsXml = "")
    {
        var files = new Dictionary<string, string>
        {
            ["Settings.xml"] =
                "<ScriptureText>"
                + "<Guid>11111111111111111111111111111111</Guid>"
                + "<Name>ABC</Name>"
                + "<FullName>Test Project</FullName>"
                + additionalSettingsXml
                + "</ScriptureText>",
        };
        var parser = new MemoryParatextProjectSettingsParser(files);
        return parser.Parse();
    }
}
