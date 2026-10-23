using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ProposePatchWriterTests
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
    public void ProposeForAWordTheOracleSkipsSuggestsExpectSkip()
    {
        Fixture fixture = Fixture
            .DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"))
            .Single(f => f.Id == "edge-cases/loader-pattern-shapes");
        // "bit" is a declared expect_skip word. Declaring a parse for it instead makes self-check
        // see a mismatch, and the oracle still skips it.
        WordEntry word = fixture.Words.Words.Single(w => w.Word == "bit");
        word.ExpectSkip = false;
        word.Parses.Add(new ParseEntry { Signature = "deliberately-wrong" });

        var proposal = new StringWriter();
        Runner.RunSelfCheck(
            new List<Fixture> { fixture },
            includePathological: true,
            new HashSet<string> { "phonology", MorpherSettings.Capability },
            propose: true,
            proposal
        );

        Assert.Multiple(() =>
        {
            Assert.That(proposal.ToString(), Does.Contain("word 'bit' signature mismatch"));
            Assert.That(proposal.ToString(), Does.Contain("expect_skip: true"));
            Assert.That(proposal.ToString(), Does.Not.Contain("expect_fail: true"));
        });
    }
}
