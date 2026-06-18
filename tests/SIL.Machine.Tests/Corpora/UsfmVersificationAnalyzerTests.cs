using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmVersificationAnalyzerTests
{
    [Test]
    public void GetUsfmVersificationErrors_NoErrors()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(0), JsonSerializer.Serialize(analysis.Diagnostics));
    }

    [Test]
    public void GetUsfmVersificationErrors_MissingVerse()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(14));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].Filename, Is.EqualTo("653JNTest.SFM"));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([16]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:15"));
    }

    [Test]
    public void GetUsfmVersificationErrors_MissingChapter()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(0));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([1]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:1-15"));
    }

    [Test]
    public void GetUsfmVersificationErrors_ExtraVerse()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        \v 15
        \v 16
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(16));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Extra));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([18]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:16"));
    }

    [Test]
    public void GetUsfmVersificationErrors_InvalidVerse()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 13-12
        \v 14
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Invalid));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(2));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([14]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:13-12"));
    }

    [Test]
    public void GetUsfmVersificationErrors_ExtraVerseSegment()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14a
        \v 14b
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(2), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.IncorrectVerseSegment));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([16]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:14a"));
    }

    [Test]
    public void GetUsfmVersificationErrors_MissingVerseSegment()
    {
        var env = new TestEnvironment(
            settings: new DefaultParatextProjectSettings(versification: GetCustomVersification(@"*3JN 1:13,a,b")),
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.IncorrectVerseSegment));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([15]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:13"));
    }

    [Test]
    public void GetUsfmVersificationErrors_IgnoreNonCanonicals()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "98XXETest.SFM",
                    @"\id XXE
        \c 1
        \v 3-2
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["XXE"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(0), JsonSerializer.Serialize(analysis.Diagnostics));
    }

    [Test]
    public void GetUsfmVersificationErrors_ExtraVerse_ExcludedInCustomVrs()
    {
        var env = new TestEnvironment(
            settings: new DefaultParatextProjectSettings(versification: GetCustomVersification(@"-3JN 1:13")),
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Extra));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([15]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:13"));
    }

    [Test]
    public void GetUsfmVersificationErrors_MultipleBooks()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "642JNTest.SFM",
                    @"\id 2JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        "
                },
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \v 13
        \v 14
        \v 15
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["2JN", "3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(27));
        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([14]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("2JN 1:13"));
    }

    [Test]
    public void GetUsfmVersificationErrors_MultipleChapters()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "642JNTest.SFM",
                    @"\id 2JN
        \c 1
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6
        \v 7
        \v 8
        \v 9
        \v 10
        \v 11
        \v 12
        \c 2
        \v 1
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["2JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(2), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(13));

        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([14]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("2JN 1:13"));

        Assert.That(analysis.Diagnostics[1].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Extra));
        Assert.That(analysis.Diagnostics[1].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[1].LineNumbers.SequenceEqual([16]));
        Assert.That(analysis.Diagnostics[1].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[1].References[0].ToString(), Is.EqualTo("2JN 2:1"));
    }

    [Test]
    public void GetUsfmVersificationErrors_InvalidChapterNumber()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1.
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(2), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(0));

        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Invalid));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([2]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN :0"));

        Assert.That(analysis.Diagnostics[1].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[1].NumAffectedVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[1].LineNumbers.SequenceEqual([1]));
        Assert.That(analysis.Diagnostics[1].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[1].References[0].ToString(), Is.EqualTo("3JN 1:1-15"));
    }

    [Test]
    public void GetUsfmVersificationErrors_InvalidVerseNumber()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        \c 1
        \v v1
        "
                },
            }
        );
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["3JN"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(2), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(1));

        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Invalid));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([3]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("3JN 1:v1"));

        Assert.That(analysis.Diagnostics[1].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[1].NumAffectedVerses, Is.EqualTo(15));
        Assert.That(analysis.Diagnostics[1].LineNumbers.SequenceEqual([3]));
        Assert.That(analysis.Diagnostics[1].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[1].References[0].ToString(), Is.EqualTo("3JN 1:1-15"));
    }

    [Test]
    public void GetUsfmVersificationErrors_UnsupportedCrossChapterVerseReference()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "03LEVTest.SFM",
                    @"\id LEV
        \c 6
        \v 1
        \v 2
        \v 3
        \v 4
        \v 5
        \v 6-9
        \v 10-30
        "
                },
            }
        ); // LEV 6:6-9 maps to 5:25-6:2 in the Original versification
        UsfmVersificationAnalysis analysis = env.AnalyzeUsfmVersification(["LEV"]);
        Assert.That(analysis.Diagnostics, Has.Count.EqualTo(3), JsonSerializer.Serialize(analysis.Diagnostics));
        Assert.That(analysis.TotalNumEncounteredVerses, Is.EqualTo(30));
        Assert.That(analysis.TotalNumAffectedVerses, Is.EqualTo(833));

        Assert.That(analysis.Diagnostics[0].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[0].NumAffectedVerses, Is.EqualTo(104));
        Assert.That(analysis.Diagnostics[0].LineNumbers.SequenceEqual([1]));
        Assert.That(analysis.Diagnostics[0].References, Has.Count.EqualTo(5));
        Assert.That(analysis.Diagnostics[0].References[0].ToString(), Is.EqualTo("LEV 1:1-17"));

        Assert.That(analysis.Diagnostics[1].Type, Is.EqualTo(UsfmVersificationDiagnosticType.UnsupportedVerseRange));
        Assert.That(analysis.Diagnostics[1].NumAffectedVerses, Is.EqualTo(4));
        Assert.That(analysis.Diagnostics[1].LineNumbers.SequenceEqual([8]));
        Assert.That(analysis.Diagnostics[1].References, Has.Count.EqualTo(1));
        Assert.That(analysis.Diagnostics[1].References[0].ToString(), Is.EqualTo("LEV 6:6-9"));

        Assert.That(analysis.Diagnostics[2].Type, Is.EqualTo(UsfmVersificationDiagnosticType.Missing));
        Assert.That(analysis.Diagnostics[2].NumAffectedVerses, Is.EqualTo(725));
        Assert.That(analysis.Diagnostics[2].LineNumbers.SequenceEqual([9]));
        Assert.That(analysis.Diagnostics[2].References, Has.Count.EqualTo(21));
        Assert.That(analysis.Diagnostics[2].References[0].ToString(), Is.EqualTo("LEV 7:1-38"));
    }

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public UsfmVersificationAnalyzerBase Analyzer { get; } = new MemoryUsfmVersificationAnalyzer(files, settings);

        public UsfmVersificationAnalysis AnalyzeUsfmVersification(HashSet<string>? onlyBooks = null)
        {
            return Analyzer.AnalyzeUsfmVersification(onlyBooks);
        }
    }

    private static ScrVers GetCustomVersification(string customVrsContents, ScrVers? baseVersification = null)
    {
        baseVersification ??= ScrVers.English;
        ScrVers customVersification = baseVersification;
        using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(customVrsContents))))
        {
            customVersification = Versification.Table.Implementation.Load(
                reader,
                "custom.vrs",
                baseVersification,
                baseVersification.ToString() + "-" + customVrsContents.GetHashCode()
            );
        }
        Versification.Table.Implementation.RemoveAllUnknownVersifications();
        return customVersification;
    }
}
