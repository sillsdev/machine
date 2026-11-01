using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Checks the source scan behind the `not-in-signature` proof kind. No checked-in proof uses the kind
/// now: the only one, for `dtd:element/Properties`, was retired with the two fixtures that declared the
/// element.
/// </summary>
[TestFixture]
public sealed class NotInSignatureCheckTests
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

    [Test]
    public void SignatureFormatSourceNeverReadsProperties()
    {
        Assert.That(NotInSignatureCheck.SignatureSourceNeverReads(RepositoryRoot(), "Properties"), Is.True);
    }

    // Sanity check for the negative direction of the source scan: a property BuildSignature genuinely
    // does read (Morpheme.Id, which every signature entry is built from) must not pass.
    [Test]
    public void SignatureSourceScanDoesDetectAPropertyThatIsActuallyRead()
    {
        Assert.That(NotInSignatureCheck.SignatureSourceNeverReads(RepositoryRoot(), "Id"), Is.False);
    }
}
