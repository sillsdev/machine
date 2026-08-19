using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Recomputes the checked-in `not-in-signature` proof for `dtd:element/Properties` in
/// conformance/semantic-coverage-proofs.tsv, so the claim is re-verified rather than trusted from the
/// file the moment either the signature builder or a fixture's grammar changes underneath it.
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

    private string _scratchDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _scratchDirectory = Path.Combine(Path.GetTempPath(), "hc-not-in-signature-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_scratchDirectory))
            Directory.Delete(_scratchDirectory, recursive: true);
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

    [Test]
    public void PropertiesElementIsDeclaredInAtLeastOneFixture()
    {
        IReadOnlyList<string> fixtures = NotInSignatureCheck.FixturesContaining(RepositoryRoot(), "Properties");
        Assert.That(fixtures, Is.Not.Empty);
    }

    [Test]
    public void NeutralizingPropertiesChangesNoWordInAnyFixtureThatDeclaresIt()
    {
        bool verified = NotInSignatureCheck.MutationChangesNoWordInAnyContainingFixture(
            RepositoryRoot(),
            "dtd:element/Properties",
            "Properties",
            _scratchDirectory
        );

        Assert.That(verified, Is.True);
    }

    [Test]
    public void TheCheckedInPropertiesProofIsNotInSignatureAndReVerifies()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ImpossibilityProof> proofs = ImpossibilityProofs.Read(root);
        ImpossibilityProof proof = proofs.Single(p => p.SurfaceId == "dtd:element/Properties");

        Assert.That(proof.Kind, Is.EqualTo(ImpossibilityProofs.NotInSignature));
        Assert.That(NotInSignatureCheck.SignatureSourceNeverReads(root, "Properties"), Is.True);
        Assert.That(
            NotInSignatureCheck.MutationChangesNoWordInAnyContainingFixture(root, proof.SurfaceId, "Properties", _scratchDirectory),
            Is.True
        );
    }
}
