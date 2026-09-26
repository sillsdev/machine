using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A child process spawned while Xplat Code Coverage is attached inherits its CLR-profiler
/// environment variables unless something strips them first; a profiled child that then crashes or
/// is killed has been observed to destabilize the coverage channel the parent test host shares,
/// producing a trace-less test host crash. <c>ChildProcessEnvironment.CreateStartInfo</c> is the one
/// place that strips them, so this gate fails, naming the file, if any other source file under the
/// conformance library or its test project constructs a <c>ProcessStartInfo</c> directly.
/// </summary>
[TestFixture]
public sealed class ProcessStartInfoConstructionGateTests
{
    // Tolerates a namespace-qualified spelling of the type name too; a plain substring search on the
    // unqualified name missed that spelling on first falsification.
    private static readonly Regex RawConstruction = new(
        @"new\s+(?:[\w.]+\.)?ProcessStartInfo\b",
        RegexOptions.Compiled
    );
    private const string FactoryFileName = "ChildProcessEnvironment.cs";

    // This file's own doc comment and assertion messages necessarily describe the pattern in prose,
    // which the regex above would otherwise flag as a violation of itself.
    private const string SelfFileName = nameof(ProcessStartInfoConstructionGateTests) + ".cs";

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

    private static IReadOnlyList<string> SourceFilesUnder(string relativeRoot)
    {
        string root = Path.Combine(RepositoryRoot(), relativeRoot);
        Assert.That(Directory.Exists(root), Is.True, $"expected a source directory at '{root}'");
        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    [Test]
    public void OnlyChildProcessEnvironmentConstructsAProcessStartInfo()
    {
        IReadOnlyList<string> files = SourceFilesUnder(
                Path.Combine("src", "SIL.Machine.Morphology.HermitCrab.Conformance")
            )
            .Concat(SourceFilesUnder(Path.Combine("tests", "SIL.Machine.Morphology.HermitCrab.Tests")))
            .ToArray();

        // A wrong root or a typo'd search pattern would make every assertion below pass vacuously by
        // finding nothing to complain about, so first prove the walk actually reached real content.
        Assert.That(files.Count, Is.GreaterThan(50), "the source walk found suspiciously few .cs files");
        string[] factoryFiles = files.Where(path => Path.GetFileName(path) == FactoryFileName).ToArray();
        Assert.That(factoryFiles, Has.Length.EqualTo(1), $"expected exactly one {FactoryFileName} on the walk");
        Assert.That(
            RawConstruction.IsMatch(File.ReadAllText(factoryFiles[0])),
            Is.True,
            $"{FactoryFileName} itself no longer matches the construction pattern this gate looks for"
        );

        List<string> violations = new();
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            if (name == FactoryFileName || name == SelfFileName)
                continue;
            if (RawConstruction.IsMatch(File.ReadAllText(file)))
                violations.Add(file);
        }

        Assert.That(
            violations,
            Is.Empty,
            "these files construct a ProcessStartInfo directly instead of through "
                + "ChildProcessEnvironment.CreateStartInfo, so a coverage-collector profiler attachment "
                + $"is not stripped before the child spawns:{Environment.NewLine}"
                + string.Join(Environment.NewLine, violations)
        );
    }
}
