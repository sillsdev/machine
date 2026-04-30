using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

public class FileParatextProjectSettingsParserTests
{
    [Test]
    public void Parse()
    {
        FileParatextProjectSettingsParser parser = new(CorporaTestHelpers.UsfmTestProjectPath);
        ParatextProjectSettings settings = parser.Parse();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.Guid, Is.EqualTo("a7e0b3ce0200736062f9f810a444dbfbe64aca35"));
            Assert.That(settings.Name, Is.EqualTo("Tes"));
            Assert.That(settings.FullName, Is.EqualTo("Test"));
            Assert.That(settings.Encoding, Is.EqualTo(System.Text.Encoding.UTF8));
            Assert.That(settings.Versification.BaseVersification, Is.EqualTo(ScrVers.English));
            Assert.That(settings.FileNamePrefix, Is.EqualTo(string.Empty));
            Assert.That(settings.FileNameForm, Is.EqualTo("41MAT"));
            Assert.That(settings.FileNameSuffix, Is.EqualTo("Tes.SFM"));
            Assert.That(settings.BiblicalTermsListType, Is.EqualTo("Project"));
            Assert.That(settings.BiblicalTermsProjectName, Is.EqualTo("Tes"));
            Assert.That(settings.BiblicalTermsFileName, Is.EqualTo("ProjectBiblicalTerms.xml"));
            Assert.That(settings.LanguageCode, Is.EqualTo("en"));
            Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
            Assert.That(settings.Visibility, Is.EqualTo("Public"));
        }
    }

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

        Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
        Assert.That(settings.Visibility, Is.EqualTo("Public"));

        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.Parent, Is.Null);

        parser = new(CorporaTestHelpers.UsfmTestProjectPath, parentSettings: settings);
        settings = parser.Parse();
        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.Parent, Is.Not.Null);
    }
}
