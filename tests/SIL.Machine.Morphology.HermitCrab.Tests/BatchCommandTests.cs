using ManyConsole;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class BatchCommandTests
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
    public void Run_AfterAResumedRun_StartsTheNextRunAtTheFirstWord()
    {
        Fixture fixture = Fixture.DiscoverAll(Path.Combine(RepositoryRoot(), "conformance")).First();
        var context = new HCContext(XmlLanguageLoader.Load(fixture.GrammarPath), TextWriter.Null);
        context.Compile();
        ConsoleCommand[] commands = [new BatchCommand(context)];
        string directory = Path.Combine(Path.GetTempPath(), $"batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string wordsPath = Path.Combine(directory, "words.txt");
        File.WriteAllLines(wordsPath, ["a", "b", "c"]);
        try
        {
            string resumedPath = Path.Combine(directory, "resumed.tsv");
            string freshPath = Path.Combine(directory, "fresh.tsv");
            ConsoleCommandDispatcher.DispatchCommand(
                commands,
                ["batch", "--start=2", wordsPath, resumedPath],
                TextWriter.Null
            );
            ConsoleCommandDispatcher.DispatchCommand(commands, ["batch", wordsPath, freshPath], TextWriter.Null);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllLines(resumedPath)[0], Does.StartWith("2\t"));
                Assert.That(File.ReadAllLines(freshPath)[0], Does.StartWith("0\t"));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
