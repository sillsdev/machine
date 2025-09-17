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

    private class TestEnvironment : DisposableBase
    {
        private readonly string _backupPath;
        private readonly ZipArchive _archive;

        public TestEnvironment()
        {
            _backupPath = CorporaTestHelpers.CreateTestParatextBackup();
            _archive = ZipFile.OpenRead(_backupPath);
            Parser = new ZipParatextProjectSettingsParser(_archive);
        }

        public ZipParatextProjectSettingsParser Parser { get; }

        protected override void DisposeManagedResources()
        {
            _archive.Dispose();
            if (File.Exists(_backupPath))
                File.Delete(_backupPath);
        }
    }
}
