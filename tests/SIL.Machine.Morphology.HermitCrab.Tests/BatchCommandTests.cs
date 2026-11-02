using ManyConsole;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class BatchCommandTests
{
    [Test]
    public void Run_AfterAResumedRun_StartsTheNextRunAtTheFirstWord()
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
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

    [Test]
    public void Run_AfterARejectedLineWithStart_StartsTheNextRunAtTheFirstWord()
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
        var context = new HCContext(XmlLanguageLoader.Load(fixture.GrammarPath), TextWriter.Null);
        context.Compile();
        ConsoleCommand[] commands = [new BatchCommand(context)];
        string directory = Path.Combine(Path.GetTempPath(), $"batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string wordsPath = Path.Combine(directory, "words.txt");
        File.WriteAllLines(wordsPath, ["a", "b", "c"]);
        try
        {
            string freshPath = Path.Combine(directory, "fresh.tsv");
            // The output path is missing, so the dispatcher rejects the line after it has parsed --start.
            int rejected = ConsoleCommandDispatcher.DispatchCommand(
                commands,
                ["batch", "--start=2", wordsPath],
                TextWriter.Null
            );
            ConsoleCommandDispatcher.DispatchCommand(commands, ["batch", wordsPath, freshPath], TextWriter.Null);

            Assert.Multiple(() =>
            {
                Assert.That(rejected, Is.Not.EqualTo(0));
                Assert.That(File.ReadAllLines(freshPath)[0], Does.StartWith("0\t"));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase("-1")]
    [TestCase("x")]
    [TestCase("+1")]
    public void Run_WithAnInvalidStart_ReportsItWithoutThrowing(string start)
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
        var context = new HCContext(XmlLanguageLoader.Load(fixture.GrammarPath), TextWriter.Null);
        context.Compile();
        ConsoleCommand[] commands = [new BatchCommand(context)];
        string directory = Path.Combine(Path.GetTempPath(), $"batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string wordsPath = Path.Combine(directory, "words.txt");
        File.WriteAllLines(wordsPath, ["a"]);
        try
        {
            var console = new StringWriter();
            int result = ConsoleCommandDispatcher.DispatchCommand(
                commands,
                ["batch", $"--start={start}", wordsPath, Path.Combine(directory, "out.tsv")],
                console
            );

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.EqualTo(0));
                Assert.That(console.ToString(), Does.Contain("--start must be a non-negative integer"));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
