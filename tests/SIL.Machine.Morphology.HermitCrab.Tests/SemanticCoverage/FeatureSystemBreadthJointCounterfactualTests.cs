using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins the four catch-22 <c>isActive="no"</c> surfaces in edge-cases/feature-system-breadth
/// (ComplexFeature, SymbolicFeature, SegmentNaturalClass, FeatureNaturalClass) to
/// <see cref="CounterfactualVerdict.EvidencedJointly"/>, each via its dedicated inactive partner.
/// </summary>
[TestFixture]
public sealed class FeatureSystemBreadthJointCounterfactualTests
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

    private static Fixture LoadFixture()
    {
        string root = RepositoryRoot();
        return Fixture
            .DiscoverAll(Path.Combine(root, "conformance"))
            .Single(f => f.Id == "edge-cases/feature-system-breadth");
    }

    private string _scratchDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _scratchDirectory = Path.Combine(
            Path.GetTempPath(),
            "hc-feature-system-breadth-joint-tests",
            Guid.NewGuid().ToString("N")
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_scratchDirectory))
            Directory.Delete(_scratchDirectory, recursive: true);
    }

    [TestCase("dtd:enum/SymbolicFeature/isActive/no")]
    [TestCase("dtd:enum/ComplexFeature/isActive/no")]
    [TestCase("dtd:enum/SegmentNaturalClass/isActive/no")]
    [TestCase("dtd:enum/FeatureNaturalClass/isActive/no")]
    public void TheSurfaceIsEvidencedOnlyJointly(string surfaceId)
    {
        Fixture fixture = LoadFixture();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(RepositoryRoot());
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult result = CounterfactualGate.EvaluateJointly(
            fixture,
            surfaceId,
            inventory,
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.EvidencedJointly), $"{surfaceId}: {result.Delta}");
    }
}
