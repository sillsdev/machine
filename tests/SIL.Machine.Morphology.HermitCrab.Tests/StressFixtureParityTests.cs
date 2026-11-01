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
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return Path.Combine(directory, "conformance-stress");
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
            engineCapabilities: new SelfCheckEngine().Capabilities,
            propose: false,
            proposeOutput: TextWriter.Null
        );
        string reasons = string.Join("; ", report.Results.Select(r => r.Reason));
        Assert.That(report.Failed, Is.EqualTo(0), reasons);
        Assert.That(report.Passed, Is.EqualTo(1), reasons);
    }
}
