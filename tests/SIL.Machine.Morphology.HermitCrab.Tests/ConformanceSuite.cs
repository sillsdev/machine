using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>Finds the conformance suite for the tests that read it: the clone in <c>conformance/</c> at the commit in
/// <c>conformance.commit</c>, or the directory named by <c>HC_CONFORMANCE_SUITE</c>.</summary>
internal static class ConformanceSuite
{
    public const string OverrideVariable = "HC_CONFORMANCE_SUITE";
    private const string SuiteUrl = "https://github.com/sillsdev/fieldworks-parser-conformance.git";

    private static string? VerifiedClone;

    /// <summary>The suite directory. Fails the running test, naming the command that fixes it, when the clone is
    /// missing or is not at the pinned commit. A suite named by <c>HC_CONFORMANCE_SUITE</c> is used as it is.</summary>
    public static string Root()
    {
        string? overridden = Environment.GetEnvironmentVariable(OverrideVariable);
        if (!string.IsNullOrWhiteSpace(overridden))
        {
            string directory = Path.GetFullPath(overridden);
            if (!File.Exists(Path.Combine(directory, "constructs.txt")))
                Assert.Fail($"{OverrideVariable}={directory} is not the conformance suite: it has no constructs.txt.");
            return directory;
        }

        if (VerifiedClone is not null)
            return VerifiedClone;

        string repositoryRoot = RepositoryRoot();
        string suite = Path.Combine(repositoryRoot, "conformance");
        string pinFile = Path.Combine(repositoryRoot, "conformance.commit");
        string pin = File.Exists(pinFile) ? File.ReadAllText(pinFile).Trim() : "";
        string manual =
            $"{Environment.NewLine}  git clone --filter=blob:none {SuiteUrl} conformance"
            + $"{Environment.NewLine}  git -C conformance checkout --detach {pin}";

        if (!File.Exists(Path.Combine(suite, "constructs.txt")))
        {
            Assert.Fail($"The conformance suite is missing: {suite} does not hold it. Run these two commands:{manual}");
            return suite;
        }

        ProcessStartInfo startInfo = ChildProcessEnvironment.CreateStartInfo("git");
        startInfo.WorkingDirectory = suite;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("HEAD");
        string head;
        try
        {
            using Process process = Process.Start(startInfo)!;
            head = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0)
                head = "";
        }
        catch (System.ComponentModel.Win32Exception)
        {
            head = "";
        }

        if (head != pin)
        {
            Assert.Fail(
                $"The conformance suite in {suite} is at '{head}', not the commit in conformance.commit ({pin}). "
                    + $"Run these two commands:{Environment.NewLine}  git -C conformance fetch origin"
                    + $"{Environment.NewLine}  git -C conformance checkout --detach {pin}"
            );
        }

        VerifiedClone = suite;
        return suite;
    }

    /// <summary>The directory holding <c>Machine.sln</c>; it does not need the suite.</summary>
    public static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "Machine.sln")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root (the directory holding Machine.sln).");
        return string.Empty;
    }
}
