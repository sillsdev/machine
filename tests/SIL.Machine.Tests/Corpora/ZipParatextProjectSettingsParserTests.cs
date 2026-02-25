using System.IO.Compression;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora;

public class ZipParatextProjectSettingsParserTests
{
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
        Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
        Assert.That(settings.Parent, Is.Null);

        env.Parser = new ZipParatextProjectSettingsParser(env.Archive, settings);
        settings = env.Parser.Parse();
        Assert.That(settings.HasParent);
        Assert.That(settings.IsDaughterProjectOf(settings));
        Assert.That(settings.TranslationType, Is.EqualTo("Standard"));
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
