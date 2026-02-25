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

    [Test]
    public void Parse_ParentProject()
    {
        FileParatextProjectSettingsParser parser = new(CorporaTestHelpers.UsfmTestProjectPath);
        ParatextProjectSettings settings = parser.Parse();
        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
        Assert.That(settings.Parent, Is.Null);

        parser = new(CorporaTestHelpers.UsfmTestProjectPath, parentSettings: settings);
        settings = parser.Parse();
        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
        Assert.That(settings.Parent, Is.Not.Null);
    }
}
