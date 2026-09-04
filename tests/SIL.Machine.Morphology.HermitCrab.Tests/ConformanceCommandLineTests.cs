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
    public void NoMemoizationOptionIsRecognized()
    {
        (int exitCode, string output, string error) = InvokeProgram("--no-memoization", "--help");

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(0), error);
            Assert.That(output, Does.Contain("--no-memoization"));
        });
    }

    [Test]
    [NonParallelizable]
    public void NoMemoizationOptionRejectsAdapterMode()
    {
        (int exitCode, _, string error) = InvokeProgram("--no-memoization", "--adapter", "noop");

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(error, Does.Contain("--no-memoization is only valid in self-check mode"));
        });
    }
}
