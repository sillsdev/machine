using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectQuoteConventionDetectorTests
{
    [Test]
    public void GetUsfmVersificationMismatches_NoMismatches()
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
                }
            }
        );
        Assert.That(
            env.GetUsfmVersificationMismatches(),
            Has.Count.EqualTo(0),
            JsonSerializer.Serialize(env.GetUsfmVersificationMismatches())
        );
    }

    [Test]
    public void GetUsfmVersificationMismatches_MissingVerse()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.MissingVerse));
    }

    [Test]
    public void GetUsfmVersificationMismatches_MissingChapter()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "653JNTest.SFM",
                    @"\id 3JN
        "
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.MissingChapter));
    }

    [Test]
    public void GetUsfmVersificationMismatches_ExtraVerse()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.ExtraVerse));
    }

    [Test]
    public void GetUsfmVersificationMismatches_InvalidVerse()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.InvalidVerseRange));
    }

    [Test]
    public void GetUsfmVersificationMismatches_ExtraVerseSegment()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(2), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.ExtraVerseSegment));
    }

    [Test]
    public void GetUsfmVersificationMismatches_MissingVerseSegment()
    {
        var env = new TestEnvironment(
            settings: new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                versification: GetCustomVersification(@"*3JN 1:13,a,b")
            ),
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.MissingVerseSegment));
    }

    [Test]
    public void GetUsfmVersificationMismatches_IgnoreNonCanonicals()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(0), JsonSerializer.Serialize(mismatches));
    }

    [Test]
    public void GetUsfmVersificationMismatches_ExtraVerse_ExcludedInCustomVrs()
    {
        var env = new TestEnvironment(
            settings: new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                versification: GetCustomVersification(@"-3JN 1:13")
            ),
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.ExtraVerse));
    }

    [Test]
    public void GetUsfmVersificationMismatches_MultipleBooks()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(1), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.MissingVerse));
    }

    [Test]
    public void GetUsfmVersificationMismatches_MultipleChapters()
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationMismatch> mismatches = env.GetUsfmVersificationMismatches();
        Assert.That(mismatches, Has.Count.EqualTo(2), JsonSerializer.Serialize(mismatches));
        Assert.That(mismatches[0].Type, Is.EqualTo(UsfmVersificationMismatchType.MissingVerse));
        Assert.That(mismatches[1].Type, Is.EqualTo(UsfmVersificationMismatchType.ExtraVerse));
    }

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public ParatextProjectVersificationMismatchDetector Detector { get; } =
            new MemoryParatextProjectVersificationMismatchDetector(files, settings);

        public IReadOnlyList<UsfmVersificationMismatch> GetUsfmVersificationMismatches()
        {
            return Detector.GetUsfmVersificationMismatches();
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
