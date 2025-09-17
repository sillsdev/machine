using NUnit.Framework;

namespace SIL.Machine.Corpora;

public class FileParatextProjectSettingsParserTests
{
    [Test]
    public void Parse_CustomStylesheet()
    {
        FileParatextProjectSettingsParser parser = new(CorporaTestHelpers.UsfmTestProjectPath);
        ParatextProjectSettings settings = parser.Parse();
        UsfmTag testTag = settings.Stylesheet.GetTag("test");
        Assert.That(testTag.StyleType, Is.EqualTo(UsfmStyleType.Character));
        Assert.That(testTag.TextType, Is.EqualTo(UsfmTextType.Other));
    }
}
