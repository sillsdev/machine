using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The fixture overview page must list every fixture directory exactly once. A fixture added, renamed
/// or deleted without the page fails here.
/// </summary>
[TestFixture]
public sealed class GrammarsOverviewTests
{
    private static readonly Regex ListedFixture = new(
        @"^- `((?:languages|edge-cases)/[^`\s]+)`",
        RegexOptions.Multiline | RegexOptions.CultureInvariant
    );

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

    private static List<string> FixtureIdsOnDisk(string conformanceRoot)
    {
        var ids = new List<string>();
        foreach (string category in new[] { "languages", "edge-cases" })
        {
            string directory = Path.Combine(conformanceRoot, category);
            if (!Directory.Exists(directory))
                continue;
            ids.AddRange(Directory.GetDirectories(directory).Select(path => category + "/" + Path.GetFileName(path)));
        }

        return ids;
    }

    private static List<string> ListedIds(string markdown) =>
        ListedFixture.Matches(markdown).Select(match => match.Groups[1].Value).ToList();

    /// <summary>The problems with a page, one line each; empty when it lists exactly the fixtures on disk.</summary>
    internal static List<string> Problems(IEnumerable<string> onDisk, string markdown)
    {
        var problems = new List<string>();
        var listed = ListedIds(markdown);
        var disk = new HashSet<string>(onDisk, StringComparer.Ordinal);

        foreach (string id in disk.Except(listed).OrderBy(id => id, StringComparer.Ordinal))
            problems.Add("missing from grammars.md: " + id);
        foreach (
            string id in listed
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
        )
        {
            problems.Add("listed more than once in grammars.md: " + id);
        }
        foreach (string id in listed.Distinct().Except(disk).OrderBy(id => id, StringComparer.Ordinal))
            problems.Add("listed in grammars.md but no such fixture directory: " + id);

        return problems;
    }

    [Test]
    public void GrammarsPageListsEveryFixtureExactlyOnce()
    {
        string conformance = Path.Combine(RepositoryRoot(), "conformance");
        string markdown = File.ReadAllText(Path.Combine(conformance, "docs", "grammars.md"));

        var onDisk = FixtureIdsOnDisk(conformance);

        Assert.That(onDisk, Is.Not.Empty);
        Assert.That(Problems(onDisk, markdown), Is.Empty);
    }

    [Test]
    public void ReportsAFixtureMissingFromThePage()
    {
        var problems = Problems(new[] { "edge-cases/a", "edge-cases/b" }, "- `edge-cases/a`: first.\n");

        Assert.That(problems, Is.EqualTo(new[] { "missing from grammars.md: edge-cases/b" }));
    }

    [Test]
    public void ReportsAFixtureListedTwice()
    {
        var problems = Problems(new[] { "languages/a" }, "- `languages/a`: first.\n- `languages/a`: again.\n");

        Assert.That(problems, Is.EqualTo(new[] { "listed more than once in grammars.md: languages/a" }));
    }

    [Test]
    public void ReportsAListedFixtureThatIsGone()
    {
        var problems = Problems(new[] { "edge-cases/a" }, "- `edge-cases/a`: first.\n- `edge-cases/gone`: removed.\n");

        Assert.That(
            problems,
            Is.EqualTo(new[] { "listed in grammars.md but no such fixture directory: edge-cases/gone" })
        );
    }

    [Test]
    public void IgnoresBulletsThatAreNotFixtureIds()
    {
        var problems = Problems(
            new[] { "edge-cases/a" },
            "- `edge-cases/a`: first.\n- `other/thing`: not a fixture.\n"
        );

        Assert.That(problems, Is.Empty);
    }
}
