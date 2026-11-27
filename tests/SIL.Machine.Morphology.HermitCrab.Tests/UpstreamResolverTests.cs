using System.Diagnostics;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class UpstreamResolverTests
{
    private static readonly string Head = new('a', 40);

    private string _repo = "";
    private string _first = "";

    [OneTimeSetUp]
    public void CreateRepository()
    {
        _repo = Path.Combine(Path.GetTempPath(), $"upstream-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repo);
        Git("init", "-q");
        Git("commit", "-q", "--allow-empty", "-m", "one");
        _first = Git("rev-parse", "HEAD");
        Git("commit", "-q", "--allow-empty", "-m", "two");
    }

    [OneTimeTearDown]
    public void DeleteRepository()
    {
        foreach (string file in Directory.EnumerateFiles(_repo, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_repo, recursive: true);
    }

    private string Git(params string[] args)
    {
        ProcessStartInfo info = ChildProcessEnvironment.CreateStartInfo("git");
        info.RedirectStandardOutput = true;
        info.WorkingDirectory = _repo;
        foreach (string arg in new[] { "-c", "user.name=t", "-c", "user.email=t@t" }.Concat(args))
            info.ArgumentList.Add(arg);
        using Process process = Process.Start(info)!;
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0));
        return output;
    }

    /// <summary>Real git, scripted gh.</summary>
    private static Func<IReadOnlyList<string>, ProcessResult> FakeGh(string json = "", int code = 0, string error = "")
    {
        return argv =>
        {
            if (argv[0] == "git")
            {
                ProcessStartInfo info = ChildProcessEnvironment.CreateStartInfo("git");
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                foreach (string arg in argv.Skip(1))
                    info.ArgumentList.Add(arg);
                using Process process = Process.Start(info)!;
                process.WaitForExit();
                return new ProcessResult(process.ExitCode, "", "");
            }
            return new ProcessResult(code, json, error);
        };
    }

    private static Func<IReadOnlyList<string>, ProcessResult> NoGh() =>
        argv => argv[0] == "gh" ? throw new AssertionException("gh must not be called") : FakeGh()(argv);

    private static UpstreamEntry Pr(string affects = "engine", string? head = null) =>
        new("sillsdev/machine#615", "pr", head ?? Head, affects, "n");

    private UpstreamResolver Resolver(Func<IReadOnlyList<string>, ProcessResult> run, string? checkout = null) =>
        new(checkout ?? _repo, run, getEnv: _ => null);

    [Test]
    public void Ancestry_SatisfiesWithoutGh()
    {
        Assert.That(Resolver(NoGh()).Resolve(Pr(head: _first)).State, Is.EqualTo(UpstreamState.Satisfied));
    }

    [Test]
    public void UnknownHead_FallsToGh()
    {
        (UpstreamState state, string reason) = Resolver(FakeGh("""{"state":"OPEN"}""")).Resolve(Pr());
        Assert.That((state, reason), Is.EqualTo((UpstreamState.Pending, "open")));
    }

    [Test]
    public void Merged_IsSatisfiedOnlyWhenTheBuildContainsTheMergeCommit()
    {
        string contained = $$$"""{"state":"MERGED","mergeCommit":{"oid":"{{{_first}}}"}}""";
        string elsewhere = $$$"""{"state":"MERGED","mergeCommit":{"oid":"{{{new string('b', 40)}}}"}}""";
        Assert.That(Resolver(FakeGh(contained)).Resolve(Pr()).State, Is.EqualTo(UpstreamState.Satisfied));
        Assert.That(Resolver(FakeGh(elsewhere)).Resolve(Pr()).State, Is.EqualTo(UpstreamState.Merged));
        Assert.That(
            new UpstreamResolver(null, FakeGh(elsewhere)).Resolve(Pr("loader")).State,
            Is.EqualTo(UpstreamState.Merged)
        );
    }

    [Test]
    public void ClosedPr_IsClosedUnmerged()
    {
        Assert.That(
            Resolver(FakeGh("""{"state":"CLOSED"}""")).Resolve(Pr()).State,
            Is.EqualTo(UpstreamState.ClosedUnmerged)
        );
    }

    [Test]
    public void Issues_AreOpenOrClosed()
    {
        var issue = new UpstreamEntry("sillsdev/FieldWorks#9", "issue", "", "loader", "n");
        Assert.That(Resolver(FakeGh("""{"state":"OPEN"}""")).Resolve(issue).State, Is.EqualTo(UpstreamState.Pending));
        (UpstreamState state, string reason) = Resolver(FakeGh("""{"state":"CLOSED","stateReason":"COMPLETED"}"""))
            .Resolve(issue);
        Assert.That(state, Is.EqualTo(UpstreamState.IssueClosed));
        Assert.That(reason, Does.Contain("COMPLETED"));
    }

    [Test]
    public void GhMissingUnauthenticatedOrBroken_IsPendingNeverAnException()
    {
        var runs = new Func<IReadOnlyList<string>, ProcessResult>[]
        {
            argv => throw new System.ComponentModel.Win32Exception("gh not found"),
            FakeGh(code: 4, error: "auth required"),
            FakeGh("not json"),
        };
        foreach (var run in runs)
        {
            (UpstreamState state, string reason) = new UpstreamResolver(null, run).Resolve(Pr("loader"));
            Assert.That(state, Is.EqualTo(UpstreamState.Pending));
            Assert.That(reason, Does.Contain("treated as pending"));
        }
    }

    [Test]
    public void InvalidEntries_NeverReachGitOrGh()
    {
        Func<IReadOnlyList<string>, ProcessResult> boom = _ => throw new AssertionException("no process");
        foreach (
            UpstreamEntry entry in new[]
            {
                new UpstreamEntry("machine#615", "pr", Head, "engine", "n"),
                new UpstreamEntry("a/b#1", "pr", "abc123", "engine", "n"),
                new UpstreamEntry("a/b#1", "pr", "", "engine", "n"),
                new UpstreamEntry("a/b#1; rm -rf", "issue", "", "engine", "n"),
            }
        )
        {
            (UpstreamState state, string reason) = Resolver(boom).Resolve(entry);
            Assert.That(state, Is.EqualTo(UpstreamState.Pending));
            Assert.That(reason, Does.Contain("invalid entry"));
        }
    }

    [Test]
    public void Jira_IsCheckedOnlyWithCredentials()
    {
        var entry = new UpstreamEntry("jira:LT-22699", "issue", "", "loader", "n");
        var env = new Dictionary<string, string>
        {
            ["JIRA_URL"] = "https://jira.example/",
            ["JIRA_USERNAME"] = "u",
            ["JIRA_API_TOKEN"] = "t",
        };
        var urls = new List<(string, string)>();
        UpstreamResolver With(string? resolution) =>
            new(
                null,
                getEnv: name => env.GetValueOrDefault(name),
                jiraGet: (url, auth) =>
                {
                    urls.Add((url, auth));
                    return $$$"""{"fields":{"status":{"name":"x"},"resolution":{{{resolution ?? "null"}}}}}""";
                }
            );

        Assert.That(With(null).Resolve(entry).State, Is.EqualTo(UpstreamState.Pending));
        Assert.That(With("""{"name":"Fixed"}""").Resolve(entry).State, Is.EqualTo(UpstreamState.IssueClosed));
        Assert.That(With("""{"name":"Won't Fix"}""").Resolve(entry).State, Is.EqualTo(UpstreamState.Rejected));
        Assert.That(
            urls[0].Item1,
            Is.EqualTo("https://jira.example/rest/api/2/issue/LT-22699?fields=status,resolution")
        );
        Assert.That(urls[0].Item2, Does.StartWith("Basic "));

        (UpstreamState state, string reason) = new UpstreamResolver(
            null,
            getEnv: _ => null,
            jiraGet: (_, _) => throw new AssertionException("no http without credentials")
        ).Resolve(entry);
        Assert.That(state, Is.EqualTo(UpstreamState.Pending));
        Assert.That(reason, Does.Contain("jira unchecked"));
    }

    [Test]
    public void SkipReason_NamesEngineEntriesThatAreNotSatisfied_AndNeverLoaderOnes()
    {
        var words = new WordsYaml();
        words.Upstream.Add(Pr("loader"));
        Assert.That(Resolver(FakeGh("""{"state":"OPEN"}""")).SkipReason(words), Is.Empty);

        words.Upstream.Add(Pr("engine"));
        Assert.That(
            Resolver(FakeGh("""{"state":"OPEN"}""")).SkipReason(words),
            Is.EqualTo("pending sillsdev/machine#615 (head aaaaaaa not in this build) — deviation expected")
        );

        words.Upstream.Clear();
        words.Upstream.Add(Pr("engine", _first));
        Assert.That(Resolver(NoGh()).SkipReason(words), Is.Empty);
    }

    [Test]
    public void Runner_SkipsAnUnsatisfiedEngineFixture_InSelfCheckMode()
    {
        string root = Path.Combine(Path.GetTempPath(), $"upstream-fixture-{Guid.NewGuid():N}");
        string source = Path.Combine(ConformanceSuite.Root(), "edge-cases", "deep-optional-affix-nesting");
        string target = Path.Combine(root, "edge-cases", "tagged");
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        File.WriteAllText(
            Path.Combine(root, "status.yaml"),
            $"version: 1\nexceptions:\n  - case: edge-cases/tagged\n    kind: pending-upstream\n    ref: sillsdev/machine#615\n    ref_type: pr\n    head: {Head}\n    affects: engine\n    reason: x\n"
        );
        try
        {
            RunReport report = Runner.RunSelfCheck(
                Fixture.DiscoverAll(root),
                includePathological: true,
                engineCapabilities: Runner.DefaultCapabilities,
                propose: false,
                proposeOutput: TextWriter.Null,
                upstream: Resolver(FakeGh("""{"state":"OPEN"}"""))
            );
            FixtureResult result = report.Results.Single();
            Assert.That(result.Outcome, Is.EqualTo(FixtureOutcome.Skipped));
            Assert.That(result.UpstreamPending, Is.True);
            Assert.That(result.Reason, Does.StartWith("pending sillsdev/machine#615 (head aaaaaaa not in this build)"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void NeverShapeGate_AllowsOnlyAnOpenLoaderEntryForFieldWorks()
    {
        var loader = new UpstreamEntry("sillsdev/FieldWorks#1263", "pr", Head, "loader", "n");
        Assert.That(Resolver(FakeGh("""{"state":"OPEN"}""")).AllowsNeverShape([loader], out string note), Is.True);
        Assert.That(note, Is.Empty);
        Assert.That(Resolver(FakeGh("""{"state":"CLOSED"}""")).AllowsNeverShape([loader], out _), Is.False);
        Assert.That(Resolver(FakeGh("""{"state":"MERGED"}""")).AllowsNeverShape([loader], out _), Is.False);

        Assert.That(
            new UpstreamResolver(null, FakeGh(code: 1, error: "offline")).AllowsNeverShape([loader], out note),
            Is.True
        );
        Assert.That(note, Does.Contain("not confirmed open"));

        Assert.That(Resolver(FakeGh("""{"state":"OPEN"}""")).AllowsNeverShape([Pr()], out _), Is.False);
        Assert.That(Resolver(FakeGh("""{"state":"OPEN"}""")).AllowsNeverShape([], out _), Is.False);
        var jira = new UpstreamEntry("jira:LT-1", "issue", "", "loader", "n");
        Assert.That(new UpstreamResolver(null, getEnv: _ => null).AllowsNeverShape([jira], out _), Is.True);
    }
}
