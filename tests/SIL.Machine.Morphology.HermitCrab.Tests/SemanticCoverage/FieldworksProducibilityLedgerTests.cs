using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

// conformance/fieldworks-producibility.tsv records whether FieldWorks' HCLoader (a component that
// lives in the separate FieldWorks repo, not this one) can ever produce each construct this suite's
// other ledgers measure. That repo is not a dependency of this one, so nothing here may open it or
// fail when its content drifts -- see conformance/docs/how-it-is-computed.md's "third layer" section
// for why that is a deliberate, permanent limitation rather than an oversight. What these tests DO
// check is the file's own internal shape: well-formed, and every subject named by two sources
// already inside this repo (the FailureReason enum and interface-inventory.tsv) appears exactly
// once. That is exactly what conformance/tools/generate-fieldworks-producibility.ps1 already
// enforces at generation time; these tests pin the same guarantee against the checked-in file so a
// hand-edit that bypasses the generator is also caught.
[TestFixture]
public sealed class FieldworksProducibilityLedgerTests
{
    private const string RelativePath = "conformance/fieldworks-producibility.tsv";
    private const string TraceManagerRelativePath = "src/SIL.Machine.Morphology.HermitCrab/ITraceManager.cs";
    private static readonly string[] ExpectedHeader =
    {
        "subject_kind",
        "subject",
        "producible",
        "hcloader_sites",
        "notes",
    };
    private static readonly HashSet<string> AllowedProducible = new() { "Yes", "No", "Conditional" };
    private static readonly HashSet<string> AllowedKinds = new() { "failure-reason", "interface-attribute" };

    private sealed record Row(string SubjectKind, string Subject, string Producible, string HcloaderSites, string Notes);

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

    private static IReadOnlyList<Row> LoadRows(string root)
    {
        string path = Path.Combine(root, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.That(File.Exists(path), Is.True, $"{RelativePath} does not exist");

        string[] dataLines = File
            .ReadAllLines(path)
            .Where(line => line.Length > 0 && !line.StartsWith("#"))
            .ToArray();

        Assert.That(dataLines, Is.Not.Empty, $"{RelativePath} has no data");
        string[] header = dataLines[0].Split('\t');
        Assert.That(header, Is.EqualTo(ExpectedHeader), $"{RelativePath} header does not match the expected columns");

        var rows = new List<Row>();
        foreach (string line in dataLines.Skip(1))
        {
            string[] cols = line.Split('\t');
            Assert.That(cols, Has.Length.EqualTo(ExpectedHeader.Length), $"malformed row (wrong column count): {line}");
            rows.Add(new Row(cols[0], cols[1], cols[2], cols[3], cols[4]));
        }

        return rows;
    }

    // Mirrors what generate-fieldworks-producibility.ps1 extracts mechanically from ITraceManager.cs,
    // so a FailureReason added or removed there is caught here too, independent of the generator.
    private static IReadOnlyList<string> ExpectedFailureReasons(string root)
    {
        string path = Path.Combine(root, TraceManagerRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string text = File.ReadAllText(path);
        Match match = Regex.Match(text, @"public enum FailureReason\s*\{(?<body>[^}]*)\}");
        Assert.That(match.Success, Is.True, "Could not locate FailureReason enum in ITraceManager.cs");
        return match
            .Groups["body"]
            .Value.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0 && s != "None")
            .ToArray();
    }

    private static IReadOnlyList<string> ExpectedInterfaceAttributes(string root)
    {
        IReadOnlyList<InterfaceInventoryLedger.Row> rows = InterfaceInventoryLedger.Compute(root);
        return rows.Select(r => $"{r.Element}.{r.Attribute}").ToArray();
    }

    [Test]
    public void FileIsWellFormedAndEveryProducibleValueIsRecognized()
    {
        IReadOnlyList<Row> rows = LoadRows(RepositoryRoot());

        Assert.Multiple(() =>
        {
            foreach (Row row in rows)
            {
                Assert.That(AllowedKinds, Does.Contain(row.SubjectKind), $"unrecognized subject_kind for {row.Subject}");
                Assert.That(AllowedProducible, Does.Contain(row.Producible), $"unrecognized producible value for {row.Subject}");
                Assert.That(row.Subject, Is.Not.Empty, "subject must not be empty");

                if (row.Producible == "Yes")
                {
                    Assert.That(row.HcloaderSites, Is.Not.Empty, $"{row.Subject} is Yes but cites no hcloader_sites");
                }

                if (row.Producible == "No")
                {
                    Assert.That(row.Notes, Is.Not.Empty, $"{row.Subject} is No but has no notes explaining what was searched");
                }
            }
        });
    }

    [Test]
    public void EverySubjectAppearsExactlyOnce()
    {
        IReadOnlyList<Row> rows = LoadRows(RepositoryRoot());

        var seen = new HashSet<(string Kind, string Subject)>();
        var duplicates = new List<string>();
        foreach (Row row in rows)
        {
            if (!seen.Add((row.SubjectKind, row.Subject)))
                duplicates.Add($"{row.SubjectKind}/{row.Subject}");
        }

        Assert.That(duplicates, Is.Empty, $"duplicate subjects: {string.Join(", ", duplicates)}");
    }

    // The completeness guarantee: every FailureReason and every interface-inventory attribute
    // ALREADY IN THIS REPO must be covered, and nothing else. This can drift only when this repo's
    // own sources change -- never when the separate FieldWorks repo changes underneath the verdicts,
    // which is exactly the limitation conformance/docs/how-it-is-computed.md documents.
    [Test]
    public void CoversExactlyEveryFailureReasonAndEveryInterfaceAttributeInThisRepo()
    {
        string root = RepositoryRoot();
        IReadOnlyList<Row> rows = LoadRows(root);

        IReadOnlyList<string> expectedFailureReasons = ExpectedFailureReasons(root);
        IReadOnlyList<string> expectedInterfaceAttributes = ExpectedInterfaceAttributes(root);

        var actualFailureReasons = rows.Where(r => r.SubjectKind == "failure-reason").Select(r => r.Subject).ToArray();
        var actualInterfaceAttributes = rows
            .Where(r => r.SubjectKind == "interface-attribute")
            .Select(r => r.Subject)
            .ToArray();

        Assert.That(actualFailureReasons, Is.EquivalentTo(expectedFailureReasons));
        Assert.That(actualInterfaceAttributes, Is.EquivalentTo(expectedInterfaceAttributes));
        Assert.That(rows, Has.Count.EqualTo(expectedFailureReasons.Count + expectedInterfaceAttributes.Count));
    }

    // Pins the headline counts reported alongside this ledger, so a silent hand-edit that flips a
    // verdict without updating the report is caught the same way OrderingGeneratorTests pins other
    // ledgers' checked-in numbers.
    [Test]
    public void PinsTheHeadlineProducibleCounts()
    {
        IReadOnlyList<Row> rows = LoadRows(RepositoryRoot());

        int yes = rows.Count(r => r.Producible == "Yes");
        int no = rows.Count(r => r.Producible == "No");
        int conditional = rows.Count(r => r.Producible == "Conditional");

        TestContext.Out.WriteLine($"producible: Yes={yes} No={no} Conditional={conditional}");

        Assert.That(rows, Has.Count.EqualTo(83));
        Assert.That(yes, Is.EqualTo(61));
        Assert.That(no, Is.EqualTo(22));
        Assert.That(conditional, Is.EqualTo(0));
    }
}
