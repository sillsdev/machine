using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Warns when a run reads the repository's <c>conformance/</c> clone while it is not at the commit in
/// <c>conformance.commit</c>, which <c>dotnet run --no-build</c> would otherwise do silently after a pin edit,
/// or while it holds local changes. A <c>--fixtures</c> path outside such a clone, or
/// <c>HC_CONFORMANCE_SUITE</c> being set, means the caller chose the suite and nothing is checked.
/// </summary>
internal static class SuitePinCheck
{
    private const string OverrideVariable = "HC_CONFORMANCE_SUITE";

    public static IReadOnlyList<string> Warnings(string fixturesRoot)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OverrideVariable)))
            return [];

        string pinFile = null;
        string suite = null;
        for (
            DirectoryInfo directory = new DirectoryInfo(Path.GetFullPath(fixturesRoot));
            directory?.Parent != null;
            directory = directory.Parent
        )
        {
            string candidate = Path.Combine(directory.Parent.FullName, "conformance.commit");
            if (directory.Name == "conformance" && File.Exists(candidate))
            {
                pinFile = candidate;
                suite = directory.FullName;
                break;
            }
        }
        if (suite == null || !Directory.Exists(Path.Combine(suite, ".git")))
            return [];

        string pin = File.ReadAllText(pinFile).Trim();
        string head = Git(suite, "rev-parse", "HEAD")?.Trim();
        if (head == null)
            return [$"could not run git in {suite}, so it was not checked against conformance.commit ({pin})"];

        var warnings = new List<string>();
        if (head != pin)
        {
            warnings.Add(
                $"{suite} is at {head}, not the commit in conformance.commit ({pin}), so this run reads a stale suite. "
                    + "Build the runner (dotnet build src/SIL.Machine.Morphology.HermitCrab.Conformance) to move it."
            );
        }

        string status = Git(suite, "status", "--porcelain");
        string[] changed = (status ?? "")
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 3)
            .Select(line => line.Substring(3))
            .ToArray();
        if (changed.Length > 0)
        {
            warnings.Add(
                $"{suite} has {changed.Length} local change(s): {string.Join(", ", changed.Take(5))}"
                    + (changed.Length > 5 ? ", ..." : "")
                    + ". This run uses what is on disk."
            );
        }
        return warnings;
    }

    private static string Git(string workingDirectory, params string[] arguments)
    {
        var startInfo = ChildProcessEnvironment.CreateStartInfo("git");
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.EnvironmentVariables["GIT_OPTIONAL_LOCKS"] = "0";
        foreach (string variable in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE" })
            startInfo.EnvironmentVariables.Remove(variable);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo);
            string output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
