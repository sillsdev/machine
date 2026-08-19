using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ImpossibilityProofsTests
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

    private static CounterfactualResult Result(string surfaceId, CounterfactualVerdict verdict) =>
        new(surfaceId, "fx", verdict, "mutation", "delta");

    [Test]
    public void AVerdictThatIsNotEvidenceAndHasNoProofIsUnaccounted()
    {
        var verdicts = new[]
        {
            Result("a", CounterfactualVerdict.Evidenced),
            Result("b", CounterfactualVerdict.RequiredByDtd),
            Result("e", CounterfactualVerdict.RequiredByLoader),
            Result("c", CounterfactualVerdict.Unobservable),
            Result("d", CounterfactualVerdict.Timeout),
        };

        IReadOnlyList<CounterfactualResult> unaccounted = ImpossibilityProofs.Unaccounted(
            verdicts,
            new[] { new ImpossibilityProof("c", ImpossibilityProofs.NoConsumer, "no reference in the engine") }
        );

        Assert.That(
            unaccounted.Select(item => item.SurfaceId),
            Is.EqualTo(new[] { "d" }),
            "a timeout is neither evidence nor a proof, so it must not pass unclaimed -- "
                + "and neither required-to-load verdict (b, e) may appear here either"
        );
    }

    [Test]
    public void AProofForASurfaceThatIsNowEvidencedIsStale()
    {
        var verdicts = new[] { Result("a", CounterfactualVerdict.Evidenced), Result("b", CounterfactualVerdict.Unobservable) };
        var proofs = new[]
        {
            new ImpossibilityProof("a", ImpossibilityProofs.NoConsumer, "no reference in the engine"),
            new ImpossibilityProof("b", ImpossibilityProofs.NoConsumer, "no reference in the engine"),
            new ImpossibilityProof("gone", ImpossibilityProofs.DtdDefault, "the parser supplies it"),
        };

        Assert.That(
            ImpossibilityProofs.Stale(verdicts, proofs),
            Is.EqualTo(new[] { "a", "gone" }),
            "a claim of impossibility must not outlive either the evidence appearing or the surface"
        );
    }

    [Test]
    public void TheCheckedInProofsFileIsWellFormedAndEveryClaimCarriesEvidence()
    {
        IReadOnlyList<ImpossibilityProof> proofs = ImpossibilityProofs.Read(RepositoryRoot());

        Assert.That(proofs, Is.Not.Empty);
        foreach (ImpossibilityProof proof in proofs)
        {
            Assert.That(proof.Evidence.Trim(), Is.Not.Empty, $"{proof.SurfaceId} claims {proof.Kind} with no evidence");
        }
    }

    // Only these four are checks rather than arguments, so the loader must refuse anything else.
    [Test]
    public void AnUnknownProofKindIsRefused()
    {
        string root = RepositoryRoot();
        string path = Path.Combine(root, "conformance", "semantic-coverage-proofs.tsv");
        string original = File.ReadAllText(path);
        try
        {
            File.AppendAllText(path, "dtd:element/Whatever\tit-seems-fine\tbecause I say so\n");
            Assert.Throws<FormatException>(() => ImpossibilityProofs.Read(root));
        }
        finally
        {
            File.WriteAllText(path, original);
        }
    }

    [Test]
    public void AClaimWithoutEvidenceIsRefused()
    {
        string root = RepositoryRoot();
        string path = Path.Combine(root, "conformance", "semantic-coverage-proofs.tsv");
        string original = File.ReadAllText(path);
        try
        {
            File.AppendAllText(path, $"dtd:element/Whatever\t{ImpossibilityProofs.NoConsumer}\t   \n");
            Assert.Throws<FormatException>(() => ImpossibilityProofs.Read(root));
        }
        finally
        {
            File.WriteAllText(path, original);
        }
    }
}
