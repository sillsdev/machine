using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class StatusYamlTests
{
    private static readonly string Head = new('b', 40);

    private static string MiniSuite(string statusYaml)
    {
        string root = Path.Combine(Path.GetTempPath(), $"status-suite-{Guid.NewGuid():N}");
        string source = Path.Combine(ConformanceSuite.Root(), "edge-cases", "deep-optional-affix-nesting");
        string target = Path.Combine(root, "edge-cases", "tagged");
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        if (statusYaml.Length > 0)
            File.WriteAllText(Path.Combine(root, "status.yaml"), statusYaml);
        return root;
    }

    private static string Row(string extra = "") =>
        "version: 1\nexceptions:\n"
        + "  - case: edge-cases/tagged\n    kind: pending-upstream\n    ref: sillsdev/machine#615\n"
        + $"    ref_type: pr\n    head: {Head}\n    affects: engine\n    reason: waits for the engine change\n{extra}";

    [Test]
    public void AWholeCaseRowBecomesTheTestCasesUpstreamEntry()
    {
        string root = MiniSuite(Row());
        try
        {
            Fixture fixture = Fixture.DiscoverAll(root).Single();
            Assert.That(fixture.Words.Exceptions, Has.Count.EqualTo(1));
            UpstreamEntry entry = fixture.Words.Upstream.Single();
            Assert.That(entry.Ref, Is.EqualTo("sillsdev/machine#615"));
            Assert.That(entry.Kind, Is.EqualTo("pr"));
            Assert.That(entry.Head, Is.EqualTo(Head));
            Assert.That(entry.IsEngine, Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void AbsentStatusYamlMeansNoExceptions()
    {
        string root = MiniSuite("");
        try
        {
            Fixture fixture = Fixture.DiscoverAll(root).Single();
            Assert.That(fixture.Words.Exceptions, Is.Empty);
            Assert.That(fixture.Words.Upstream, Is.Empty);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ARowNamingNoTestCaseIsAnError()
    {
        string root = MiniSuite(Row().Replace("edge-cases/tagged", "edge-cases/missing"));
        try
        {
            Assert.That(
                () => Fixture.DiscoverAll(root),
                Throws.TypeOf<WordsYamlException>().With.Message.Contain("names no test case")
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void AWordRowNeedsAnExistingWordAndAnIntendedAnswer()
    {
        string root = MiniSuite(Row("    word: nonexistent\n    intended: R0\n"));
        try
        {
            Assert.That(
                () => Fixture.DiscoverAll(root),
                Throws.TypeOf<WordsYamlException>().With.Message.Contain("has no word")
            );
            File.WriteAllText(Path.Combine(root, "status.yaml"), Row("    word: x\n"));
            Assert.That(
                () => StatusYamlLoader.LoadIfPresent(root),
                Throws.TypeOf<WordsYamlException>().With.Message.Contain("intended")
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void UnknownKeysAndBadKindsAreErrors()
    {
        string root = MiniSuite(Row("    surprise: 1\n"));
        try
        {
            Assert.That(
                () => StatusYamlLoader.LoadIfPresent(root),
                Throws.TypeOf<WordsYamlException>().With.Message.Contain("unknown key")
            );
            File.WriteAllText(Path.Combine(root, "status.yaml"), Row().Replace("pending-upstream", "aspirational"));
            Assert.That(
                () => StatusYamlLoader.LoadIfPresent(root),
                Throws.TypeOf<WordsYamlException>().With.Message.Contain("kind must be")
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
