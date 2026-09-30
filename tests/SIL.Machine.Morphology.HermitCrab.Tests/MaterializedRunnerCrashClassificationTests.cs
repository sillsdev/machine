using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A harness-level problem is never a crash match, expectCrash or not. A misconfigured adapter
/// (wrong path, an argument it rejects, ...) exits nonzero on every fixture it is asked to run, not
/// just the one pinning a crash -- these tests use a stub <see cref="IEngine"/> (no real subprocess)
/// to prove <see cref="MaterializedRunner"/> tells the two apart.
/// </summary>
[TestFixture]
public sealed class MaterializedRunnerCrashClassificationTests
{
    private sealed class StubEngine(params string[] crashingFixtureIds) : IEngine
    {
        private readonly HashSet<string> _crashingFixtureIds = new(crashingFixtureIds, StringComparer.Ordinal);

        public string Name => "stub";
        public IReadOnlySet<string> Capabilities { get; } = new HashSet<string>();

        public List<TsvRow> Run(MaterializedFixture fixture)
        {
            if (_crashingFixtureIds.Contains(fixture.Id))
                throw new EngineCrashException($"stub crash for '{fixture.Id}'");
            return new List<TsvRow>();
        }
    }

    private static MaterializedFixture CreateFixture(string root, string id, bool expectCrash)
    {
        string directory = Path.Combine(root, id);
        Directory.CreateDirectory(directory);
        // No PhonologicalRule/MetathesisRule, so RequiresDerivation.Derive matches the manifest's
        // default empty Requires -- unrelated to crash classification.
        File.WriteAllText(Path.Combine(directory, "grammar.xml"), "<Root/>");
        File.WriteAllText(Path.Combine(directory, "words.txt"), "w\n");
        File.WriteAllText(Path.Combine(directory, "expected.tsv"), "");
        return new MaterializedFixture(directory, new FixtureManifest { Id = id, ExpectCrash = expectCrash });
    }

    private static MaterializedRunReport Run(string root, IEngine engine)
    {
        var fixtures = new List<MaterializedFixture>
        {
            CreateFixture(root, "crash-fixture", expectCrash: true),
            CreateFixture(root, "control-fixture", expectCrash: false),
        };
        return MaterializedRunner.Run(fixtures, engine, includePathological: false);
    }

    [Test]
    public void ExpectCrashPassesWhenTheSameEngineStillCompletesAnOrdinaryFixture()
    {
        string root = Path.Combine(Path.GetTempPath(), $"hc-crash-classification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            // Genuine crash: the stub only fails on the fixture that pins one.
            MaterializedRunReport report = Run(root, new StubEngine("crash-fixture"));

            FixtureResult result = report.Results.Single(r => r.FixtureId == "crash-fixture");
            Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Passed), result.Reason);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ExpectCrashDoesNotPassWhenTheEngineFailsEveryFixtureNotJustTheCrashOne()
    {
        string root = Path.Combine(Path.GetTempPath(), $"hc-crash-classification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            // Misconfigured adapter: rejects its own arguments and exits nonzero regardless of which
            // fixture it was asked to run. Must not be credited as reproducing the pinned crash.
            MaterializedRunReport report = Run(root, new StubEngine("crash-fixture", "control-fixture"));

            FixtureResult result = report.Results.Single(r => r.FixtureId == "crash-fixture");
            Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Failed), result.Reason);
            Assert.That(result.Reason, Does.Contain("control-fixture"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
