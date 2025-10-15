using System.Text.Json;
using NUnit.Framework;

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

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public ParatextProjectVersificationMismatchDetector Detector { get; } =
            new MemoryParatextProjectVersificationMismatchDetector(files, settings);

        public IReadOnlyList<UsfmVersificationMismatch> GetUsfmVersificationMismatches()
        {
            return Detector.GetUsfmVersificationMismatches();
        }
    }
}
