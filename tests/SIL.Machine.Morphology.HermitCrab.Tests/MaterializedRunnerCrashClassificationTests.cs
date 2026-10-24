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

    private static MaterializedFixture CreateFixture(
        string root,
        string id,
        bool expectCrash,
        bool requiresPhonology = false,
        bool pathological = false
    )
    {
        string directory = Path.Combine(root, id);
        Directory.CreateDirectory(directory);
        // A PhonologicalRule is what RequiresDerivation.Derive reads as "phonology"; without one the
        // manifest's empty Requires matches the grammar.
        File.WriteAllText(
            Path.Combine(directory, "grammar.xml"),
            requiresPhonology ? "<Root><PhonologicalRule/></Root>" : "<Root/>"
        );
        File.WriteAllText(Path.Combine(directory, "words.txt"), "w\n");
        File.WriteAllText(Path.Combine(directory, "expected.tsv"), "");
        var manifest = new FixtureManifest { Id = id, ExpectCrash = expectCrash };
        if (requiresPhonology)
            manifest.Requires.Add("phonology");
        if (pathological)
            manifest.Category = "pathological";
        return new MaterializedFixture(directory, manifest);
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

    [TestCase(true, false, TestName = "ExpectCrashSkipsAControlTheEngineLacksACapabilityFor")]
    [TestCase(false, true, TestName = "ExpectCrashSkipsAControlExcludedAsPathological")]
    public void ExpectCrashUsesOnlyAControlTheRunWouldExecute(bool requiresPhonology, bool pathological)
    {
        string root = Path.Combine(Path.GetTempPath(), $"hc-crash-classification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            // The stub declares no capabilities and fails the ineligible fixture, as an engine that
            // cannot run it would. Only the eligible control says whether the adapter works.
            var fixtures = new List<MaterializedFixture>
            {
                CreateFixture(root, "crash-fixture", expectCrash: true),
                CreateFixture(root, "ineligible-control", expectCrash: false, requiresPhonology, pathological),
                CreateFixture(root, "control-fixture", expectCrash: false),
            };
            MaterializedRunReport report = MaterializedRunner.Run(
                fixtures,
                new StubEngine("crash-fixture", "ineligible-control"),
                includePathological: false
            );

            FixtureResult result = report.Results.Single(r => r.FixtureId == "crash-fixture");
            Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Passed), result.Reason);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
