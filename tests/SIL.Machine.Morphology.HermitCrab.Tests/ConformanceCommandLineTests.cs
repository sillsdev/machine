using System.Reflection;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceCommandLineTests
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
            Path.Combine(RepositoryRoot(), "conformance"),
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
}
