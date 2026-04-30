using System.IO.Compression;
using NUnit.Framework;
using SIL.ObjectModel;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

public class ZipParatextProjectSettingsParserTests
{
    [Test]
    public void Parse()
    {
        using var env = new TestEnvironment();
        ParatextProjectSettings settings = env.Parser.Parse();

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
        using var env = new TestEnvironment();
        ParatextProjectSettings settings = env.Parser.Parse();
        UsfmTag testTag = settings.Stylesheet.GetTag("test");
        Assert.That(testTag.StyleType, Is.EqualTo(UsfmStyleType.Character));
        Assert.That(testTag.TextType, Is.EqualTo(UsfmTextType.Other));
    }

    [Test]
    public void Parse_ParentProject()
    {
        using var env = new TestEnvironment();
        ParatextProjectSettings settings = env.Parser.Parse();

        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.Parent, Is.Null);

        env.Parser = new ZipParatextProjectSettingsParser(env.Archive, settings);
        settings = env.Parser.Parse();
        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.Parent, Is.Not.Null);
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly string _backupPath;

        public TestEnvironment()
        {
            _backupPath = CorporaTestHelpers.CreateTestParatextBackup();
            Archive = ZipFile.OpenRead(_backupPath);
            Parser = new ZipParatextProjectSettingsParser(Archive);
        }

        public ZipParatextProjectSettingsParser Parser { get; set; }
        public ZipArchive Archive { get; }

        protected override void DisposeManagedResources()
        {
            Archive.Dispose();
            if (File.Exists(_backupPath))
                File.Delete(_backupPath);
        }
    }
}
