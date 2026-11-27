using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
[Explicit("Timing and optimization fixtures; run only with --filter TestCategory=Stress")]
[Category("Stress")]
public sealed class StressFixtureParityTests
{
    private static string StressRoot() => Path.Combine(ConformanceSuite.Root(), "stress");

    private static IEnumerable<string> FixtureIds() => Fixture.DiscoverAll(StressRoot()).Select(fixture => fixture.Id);

    [TestCaseSource(nameof(FixtureIds))]
    public void ParsesMatchTheMasterEngine(string fixtureId)
    {
        List<Fixture> fixtures = Fixture.DiscoverAll(StressRoot()).Where(f => f.Id == fixtureId).ToList();
        RunReport report = Runner.RunSelfCheck(
            fixtures,
            includePathological: true,
            engineCapabilities: Runner.DefaultCapabilities,
            propose: false,
            proposeOutput: TextWriter.Null
        );
        string reasons = string.Join("; ", report.Results.Select(r => r.Reason));
        Assert.That(report.Failed, Is.EqualTo(0), reasons);
        // A fixture with variants reports one result per variant, so pass means every run passed.
        Assert.That(report.Results, Is.Not.Empty, reasons);
        Assert.That(report.Passed, Is.EqualTo(report.Results.Count), reasons);
    }
}
