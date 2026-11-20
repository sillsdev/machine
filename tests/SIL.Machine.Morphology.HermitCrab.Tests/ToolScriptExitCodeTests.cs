using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ToolScriptExitCodeTests
{
    [Test]
    public void Main_WithAScriptWhoseFirstCommandFails_ExitsWithThatFailureEvenWhenALaterCommandSucceeds()
    {
        (int failing, int passing) = RunScripts(["batch --start=x {words} {out}", "batch {words} {out}"]);

        Assert.Multiple(() =>
        {
            Assert.That(failing, Is.Not.EqualTo(0));
            Assert.That(passing, Is.EqualTo(0));
        });
    }

    private static (int Failing, int Passing) RunScripts(string[] failingScript)
    {
        Fixture fixture = Fixture.DiscoverAll(ConformanceSuite.Root()).First();
        string directory = Path.Combine(Path.GetTempPath(), $"tool-script-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string words = Path.Combine(directory, "words.txt");
        File.WriteAllLines(words, ["a"]);
        string output = Path.Combine(directory, "out.tsv");
        TextWriter originalOut = Console.Out;
        try
        {
            Console.SetOut(TextWriter.Null);
            int Run(string[] lines)
            {
                string script = Path.Combine(directory, $"script-{Guid.NewGuid():N}.txt");
                File.WriteAllLines(
                    script,
                    lines.Select(line => line.Replace("{words}", words).Replace("{out}", output))
                );
                return Program.Main(["-i", fixture.GrammarPath, "-s", script]);
            }
            return (Run(failingScript), Run(["batch {words} {out}"]));
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(directory, recursive: true);
        }
    }
}
