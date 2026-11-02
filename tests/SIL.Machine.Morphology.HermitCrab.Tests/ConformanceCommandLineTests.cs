using System.Reflection;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceCommandLineTests
{
    private static (int ExitCode, string Output, string Error) InvokeProgram(params string[] args)
    {
        Type programType = typeof(Runner).Assembly.GetType("SIL.Machine.Morphology.HermitCrab.Conformance.Program")!;
        MethodInfo main = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            int exitCode = (int)main.Invoke(null, new object[] { args })!;
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Test]
    [NonParallelizable]
    public void CoverageReport_MissingConstructChecklist_Fails()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"constructs-{Guid.NewGuid():N}.txt");

        (int exitCode, string output, string error) = InvokeProgram(
            "--fixtures",
            ConformanceSuite.Root(),
            "--coverage-report",
            "--constructs",
            missing
        );

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1), error);
            Assert.That(output, Does.Contain("CONSTRUCT CHECKLIST NOT FOUND"));
        });
    }

    [Test]
    [NonParallelizable]
    public void CoverageReport_WritesNothingUnderTheFixturesRoot()
    {
        string suite = ConformanceSuite.Root();
        string[] files = { Path.Combine(suite, "coverage.csv"), Path.Combine(suite, "rules.csv") };
        DateTime[] before = files.Select(File.GetLastWriteTimeUtc).ToArray();
        string[] entriesBefore = Directory.GetFileSystemEntries(suite);
        string output = Path.Combine(Path.GetTempPath(), $"hc-coverage-out-{Guid.NewGuid():N}");
        try
        {
            (int exitCode, string text, string error) = InvokeProgram(
                "--fixtures",
                suite,
                "--coverage-report",
                "--coverage-output",
                output
            );

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(0), text + error);
                Assert.That(files.Select(File.GetLastWriteTimeUtc), Is.EqualTo(before));
                Assert.That(Directory.GetFileSystemEntries(suite), Is.EquivalentTo(entriesBefore));
                foreach (string file in files)
                {
                    Assert.That(
                        File.ReadAllText(Path.Combine(output, Path.GetFileName(file))).ReplaceLineEndings("\n"),
                        Is.EqualTo(File.ReadAllText(file).ReplaceLineEndings("\n")),
                        Path.GetFileName(file)
                    );
                }
            });
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    [Test]
    [NonParallelizable]
    public void CoverageReport_RefusesAnOutputInsideTheFixturesRoot()
    {
        string suite = ConformanceSuite.Root();
        string inside = Path.Combine(suite, $"hc-coverage-out-{Guid.NewGuid():N}");

        (int exitCode, string _, string error) = InvokeProgram(
            "--fixtures",
            suite,
            "--coverage-report",
            "--coverage-output",
            inside
        );

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2), error);
            Assert.That(error, Does.Contain("not the suite clone"));
            Assert.That(Directory.Exists(inside), Is.False);
        });
    }

    [Test]
    [NonParallelizable]
    public void CheckManifest_MalformedGrammar_NamesTheFixtureAndFails()
    {
        const string FixtureId = "edge-cases/strrep-identity";
        string root = ConformanceManifestTests.CreateMiniSuite(FixtureId);
        try
        {
            File.WriteAllText(Path.Combine(root, "edge-cases", "strrep-identity", "grammar.xml"), "<HermitCrabInput><broken");

            (int exitCode, string _, string error) = InvokeProgram("--fixtures", root, "--check-manifest");

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(2), error);
                Assert.That(error, Does.Contain(FixtureId));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
