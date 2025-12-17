using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectVersificationErrorDetectorTests
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
                }
            }
        );
        Assert.That(
            env.GetUsfmVersificationErrors(),
            Has.Count.EqualTo(0),
            JsonSerializer.Serialize(env.GetUsfmVersificationErrors())
        );
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.MissingVerse));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("3JN 1:15"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:14"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.MissingChapter));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("3JN 1:15"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 0:0"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.ExtraVerse));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo(""));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:16"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.InvalidVerseRange));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("3JN 1:12-13"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:13-12"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(2), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.ExtraVerseSegment));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("3JN 1:14"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:14a"));
    }

    [Test]
    public void GetUsfmVersificationErrors_MissingVerseSegment()
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
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.MissingVerseSegment));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("3JN 1:13a"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:13"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(0), JsonSerializer.Serialize(errors));
    }

    [Test]
    public void GetUsfmVersificationErrors_ExtraVerse_ExcludedInCustomVrs()
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
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.ExtraVerse));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo(""));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("3JN 1:13"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(1), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.MissingVerse));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("2JN 1:13"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("2JN 1:12"));
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
                }
            }
        );
        IReadOnlyList<UsfmVersificationError> errors = env.GetUsfmVersificationErrors();
        Assert.That(errors, Has.Count.EqualTo(2), JsonSerializer.Serialize(errors));
        Assert.That(errors[0].Type, Is.EqualTo(UsfmVersificationErrorType.MissingVerse));
        Assert.That(errors[1].Type, Is.EqualTo(UsfmVersificationErrorType.ExtraVerse));
        Assert.That(errors[0].ExpectedVerseRef, Is.EqualTo("2JN 1:13"));
        Assert.That(errors[0].ActualVerseRef, Is.EqualTo("2JN 1:12"));
        Assert.That(errors[1].ExpectedVerseRef, Is.EqualTo(""));
        Assert.That(errors[1].ActualVerseRef, Is.EqualTo("2JN 2:1"));
    }

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public ParatextProjectVersificationErrorDetectorBase Detector { get; } =
            new MemoryParatextProjectVersificationErrorDetector(files, settings);

        public IReadOnlyList<UsfmVersificationError> GetUsfmVersificationErrors()
        {
            return Detector.GetUsfmVersificationErrors();
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
