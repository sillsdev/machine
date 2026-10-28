using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
[Explicit("Timing and optimization fixtures; run only with --filter TestCategory=Stress")]
[Category("Stress")]
public sealed class StressFixtureParityTests
{
    private static string StressRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            string candidate = Path.Combine(directory, "conformance-stress");
            if (Directory.Exists(candidate))
                return candidate;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate conformance-stress.");
        return string.Empty;
    }

    private static IEnumerable<string> FixtureIds() => Fixture.DiscoverAll(StressRoot()).Select(fixture => fixture.Id);

    [TestCaseSource(nameof(FixtureIds))]
    public void ParsesMatchTheMasterEngine(string fixtureId)
    {
        List<Fixture> fixtures = Fixture.DiscoverAll(StressRoot()).Where(f => f.Id == fixtureId).ToList();
        RunReport report = Runner.RunSelfCheck(
            fixtures,
            includePathological: true,
            engineCapabilities: new HashSet<string>(StringComparer.Ordinal) { "phonology", "morpher-settings" },
            propose: false,
            proposeOutput: TextWriter.Null
        );
        Assert.That(report.Failed, Is.EqualTo(0), string.Join("; ", report.Results.Select(r => r.Reason)));
        Assert.That(report.Passed, Is.EqualTo(1));
    }
}
