using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceDtdTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    [Test]
    public void ConformanceDtdMatchesTheDtdTheLoaderEmbeds()
    {
        // The coverage inventory is read from the conformance copy, so a DTD change that skips it
        // goes unnoticed: its new surfaces are never counted.
        using Stream? stream = typeof(XmlLanguageLoader).Assembly.GetManifestResourceStream(
            "SIL.Machine.Morphology.HermitCrab.HermitCrabInput.dtd"
        );
        Assert.That(stream, Is.Not.Null, "the loader's embedded DTD was not found");
        using var reader = new StreamReader(stream!);
        string embedded = reader.ReadToEnd();

        string conformance = File.ReadAllText(Path.Combine(RepositoryRoot(), "conformance", "HermitCrabInput.dtd"));

        Assert.That(
            NormalizeLineEndings(conformance),
            Is.EqualTo(NormalizeLineEndings(embedded)),
            "conformance/HermitCrabInput.dtd differs from src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd; "
                + "copy the library DTD over it and regenerate the coverage ledgers"
        );
    }
}
