using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class SuitePinCheckTests
{
    private string _root = "";
    private string _suite = "";
    private string _head = "";
    private string? _override;

    [SetUp]
    public void SetUp()
    {
        _override = Environment.GetEnvironmentVariable(ConformanceSuite.OverrideVariable);
        Environment.SetEnvironmentVariable(ConformanceSuite.OverrideVariable, null);
        _root = Path.Combine(Path.GetTempPath(), $"pin-check-{Guid.NewGuid():N}");
        _suite = Path.Combine(_root, "conformance");
        Directory.CreateDirectory(Path.Combine(_suite, "stress"));
        File.WriteAllText(Path.Combine(_suite, "constructs.txt"), "a\n");
        Git(_suite, "init", "-q");
        Git(_suite, "add", ".");
        Git(
            _suite,
            "-c",
            "user.name=t",
            "-c",
            "user.email=t@example.com",
            "-c",
            "commit.gpgsign=false",
            "commit",
            "-q",
            "-m",
            "x"
        );
        _head = Git(_suite, "rev-parse", "HEAD").Trim();
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(ConformanceSuite.OverrideVariable, _override);
        foreach (string path in Directory.EnumerateFileSystemEntries(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(path, FileAttributes.Normal);
        Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void Warnings_ForAnUntouchedCloneAtThePin_AreEmpty()
    {
        File.WriteAllText(Path.Combine(_root, "conformance.commit"), _head + "\n");

        Assert.That(SuitePinCheck.Warnings(_suite), Is.Empty);
        Assert.That(SuitePinCheck.Warnings(Path.Combine(_suite, "stress")), Is.Empty);
    }

    [Test]
    public void Warnings_ForACloneAtAnotherCommit_SayTheSuiteIsStale()
    {
        File.WriteAllText(Path.Combine(_root, "conformance.commit"), new string('a', 40) + "\n");

        Assert.That(SuitePinCheck.Warnings(_suite), Has.Exactly(1).Contain("stale suite"));
    }

    [Test]
    public void Warnings_ForAnUntrackedFixtureAtThePin_NameIt()
    {
        File.WriteAllText(Path.Combine(_root, "conformance.commit"), _head + "\n");
        File.WriteAllText(Path.Combine(_suite, "extra.txt"), "x");

        Assert.That(SuitePinCheck.Warnings(_suite), Has.Exactly(1).Contain("extra.txt"));
    }

    [Test]
    public void Warnings_WhenTheOverrideIsSetOrTheFixturesAreElsewhere_AreEmpty()
    {
        File.WriteAllText(Path.Combine(_root, "conformance.commit"), new string('a', 40) + "\n");
        string elsewhere = Path.Combine(_root, "other");
        Directory.CreateDirectory(elsewhere);
        Assert.That(SuitePinCheck.Warnings(elsewhere), Is.Empty);

        Environment.SetEnvironmentVariable(ConformanceSuite.OverrideVariable, _suite);
        Assert.That(SuitePinCheck.Warnings(_suite), Is.Empty);
    }

    private static string Git(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = ChildProcessEnvironment.CreateStartInfo("git");
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.RedirectStandardOutput = true;
        foreach (string variable in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE" })
            startInfo.EnvironmentVariables.Remove(variable);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0), $"git {string.Join(' ', arguments)}");
        return output;
    }
}
