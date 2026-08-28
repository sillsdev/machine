using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ConvertUsfmVersificationHandlerTests
{
    [Test]
    public void GetUsfm_OneFewerChapter()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24

        string usfm =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1 Text
\v 2-14
\c 2
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\c 4
\p
\s1 Section
\v 1-5
\v 6 Text
";

        string target = UpdateUsfm(
            "MAL",
            usfm,
            sourceVersification: ScrVers.English,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1 Text
\v 2-14
\c 2
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\p
\s1 Section
\v 19-23
\v 24 Text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_OneMoreChapter()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24

        string usfm =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1 Text
\v 2-14
\c 2
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\v 19-23
\v 24 Text
";

        string target = UpdateUsfm(
            "MAL",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1 Text
\v 2-14
\c 2
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\c 4
\v 1-5
\v 6 Text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_OneFewerBook()
    {
        // Russian Orthodox vs. Original
        // PSA 151:1-7 = PS2 1:1-7

        string usfm =
            @"\id PSA - Test
\h Psalms
\c 150
\v 1-5 Lines
\v 6 Line
\q Another line
\c 151
\v 1-7 More lines
";

        string target = UpdateUsfm(
            "PSA",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id PSA - Test
\h Psalms
\c 150
\v 1-5 Lines
\v 6 Line
\q Another line
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_OneMoreBook()
    {
        // Russian Orthodox vs. Original
        // DAN 3:24-90 = DAG 3:24-90
        // DAN 3:91-100 = DAN 3:24-33

        // Original
        // S3Y 1:1-29 = DAG 3:24-52
        // S3Y 1:30-31 = DAG 3:52-53
        // S3Y 1:33 = DAG 3:54
        // S3Y 1:32 = DAG 3:55
        // S3Y 1:34-35 = DAG 3:56-57
        // S3Y 1:37 = DAG 3:58
        // S3Y 1:36 = DAG 3:59
        // S3Y 1:38-68 = DAG 3:60-90

        string usfm =
            @"\id DAN - Test
\h Daniel
\c 3
\v 1-23
\v 24-90
\p
\v 91-100
\c 4
\v 1
";

        string target = UpdateUsfm(
            "DAN",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\h Daniel
\c 3
\v 1-23
\v 24-33
\c 4
\v 1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_BackOneVerseToPreviousChapter()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23

        string usfm =
            @"\id ISA - Test
\c 8
\v 22
\v 23
\c 9
\v 1
";

        string target = UpdateUsfm(
            "ISA",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id ISA - Test
\c 8
\v 22
\c 9
\v 1
\v 2
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_ForwardOneVerseToNextChapter()
    {
        // Original vs. English
        // ISA 8:23 = ISA 9:1

        string usfm =
            @"\id ISA - Test
\c 8
\v 22
\c 9
\v 1
\v 2
";

        string target = UpdateUsfm(
            "ISA",
            usfm,
            sourceVersification: ScrVers.English,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id ISA - Test
\c 8
\v 22
\v 23
\c 9
\v 1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_CrossChapterVerseRange()
    {
        // English vd. Original
        // ISA 9:1 = ISA 8:23

        string usfm =
            @"\id ISA - Test
\c 8
\v 22-23
\c 9
\v 1
";

        string target = UpdateUsfm(
            "ISA",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id ISA - Test
\c 8
\v 22
\c 9
\v 1
\v 2
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_CrossChapterVerseRange_CrossBook()
    {
        // Russian Orthodox vs. Original
        // DAN 3:24-90 = DAG 3:24-90
        // DAN 3:91-100 = DAN 3:24-33

        // Original
        // S3Y 1:1-29 = DAG 3:24-52
        // S3Y 1:30-31 = DAG 3:52-53
        // S3Y 1:33 = DAG 3:54
        // S3Y 1:32 = DAG 3:55
        // S3Y 1:34-35 = DAG 3:56-57
        // S3Y 1:37 = DAG 3:58
        // S3Y 1:36 = DAG 3:59
        // S3Y 1:38-68 = DAG 3:60-90

        string usfm =
            @"\id DAN - Test
\c 3
\v 1-22
\v 23-89
\v 90-100
\c 4
\v 1
";

        string target = UpdateUsfm(
            "DAN",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\v 1-22
\v 23
\v 24-33
\c 4
\v 1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_CrossChapterVerseRange_CrossBookWithinSingleRange()
    {
        // Russian Orthodox vs. Original
        // DAN 3:24-90 = DAG 3:24-90
        // DAN 3:91-100 = DAN 3:24-33

        // Original
        // S3Y 1:1-29 = DAG 3:24-52
        // S3Y 1:30-31 = DAG 3:52-53
        // S3Y 1:33 = DAG 3:54
        // S3Y 1:32 = DAG 3:55
        // S3Y 1:34-35 = DAG 3:56-57
        // S3Y 1:37 = DAG 3:58
        // S3Y 1:36 = DAG 3:59
        // S3Y 1:38-68 = DAG 3:60-90

        string usfm =
            @"\id DAN - Test
\c 3
\v 1-100
\c 4
\v 1
";

        string target = UpdateUsfm(
            "DAN",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\v 1-33
\c 4
\v 1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_HeadingIntroducingKeptVerse_AfterDroppedVerse()
    {
        // Russian Orthodox vs. Original
        // DAN 3:24-90 = DAG 3:24-90  (leaves the book, so it is dropped)
        // DAN 3:91-100 = DAN 3:24-33 (stays in the book, so it is kept)
        // The heading and paragraph marker between them introduce the verse that is kept.

        string usfm =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\v 24-90 Dropped text
\s1 Section
\p
\v 91-100 More text
";

        string target = UpdateUsfm(
            "DAN",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\s1 Section
\p
\v 24-33 More text
";
        AssertUsfmEqualsExactly(target, result);
    }

    [Test]
    public void GetUsfm_HeadingIntroducingDroppedVerse_IsDropped()
    {
        // Russian Orthodox vs. Original
        // PSA 151:1-7 = PS2 1:1-7 (the whole chapter leaves the book)
        // The heading introduces the dropped verse, so it goes with it. The \q belongs to the
        // preceding verse and stays.

        string usfm =
            @"\id PSA - Test
\c 150
\v 1-5 Lines
\v 6 Line
\q Another line
\c 151
\s1 Section
\p
\v 1-7 More lines
";

        string target = UpdateUsfm(
            "PSA",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id PSA - Test
\c 150
\v 1-5 Lines
\v 6 Line
\q Another line
";
        AssertUsfmEqualsExactly(target, result);
    }

    [Test]
    public void GetUsfm_DroppedVerseText_IsDroppedWhenKeptVerseFollows()
    {
        // Guards against rescuing the dropped verse's own text along with the markers that
        // introduce the verse after it.

        string usfm =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\v 24-90 Dropped text
\v 91-100 More text
";

        string target = UpdateUsfm(
            "DAN",
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\v 24-33 More text
";
        AssertUsfmEqualsExactly(target, result);
    }

    [Test]
    public void GetUsfm_SynthesizedChapter_IsFollowedByParagraphMarker()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24
        // Converting Original to English splits chapter 3 mid-paragraph, so the synthesized \c 4
        // has no paragraph marker of its own. \nb continues the paragraph across the chapter break;
        // without it the first verse of chapter 4 sits outside any paragraph.

        string usfm =
            @"\id MAL - Test
\c 3
\p
\v 1-18 Text
\v 19-23 More text
\v 24 Last text
";

        string target = UpdateUsfm(
            "MAL",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id MAL - Test
\c 3
\p
\v 1-18 Text
\c 4
\nb
\v 1-5 More text
\v 6 Last text
";
        AssertUsfmEqualsExactly(target, result);
    }

    [Test]
    public void GetUsfm_SynthesizedChapter_FromSplitVerseRange_IsFollowedByParagraphMarker()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23
        // The range \v 22-23 straddles the mapped chapter boundary, so it is split and a chapter
        // marker is synthesized between the two halves. That marker needs a paragraph too.
        // The verses are left without text deliberately: where the text of a split range should
        // end up is a separate question from whether the synthesized chapter has a paragraph.

        string usfm =
            @"\id ISA - Test
\c 8
\p
\v 22-23
\c 9
\p
\v 1
";

        string target = UpdateUsfm(
            "ISA",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id ISA - Test
\c 8
\p
\v 22
\c 9
\nb
\v 1
\p
\v 2
";
        AssertUsfmEqualsExactly(target, result);
    }

    [Test]
    public void GetUsfm_SplitVerseRange_TextStaysWithFirstVerse()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23
        // \v 22-23 straddles the mapped chapter boundary and is split. The text covers both
        // verses, and no single \v can express that, so it stays with the verse the range starts
        // at rather than being carried into the next chapter.
        //
        // This expects the synthesized \c 9 to be followed by \nb as well, so it needs both that
        // fix and the text placement fix to pass.

        string usfm =
            @"\id ISA - Test
\c 8
\p
\v 22-23 Verse twenty-two and twenty-three text
\c 9
\p
\v 1 Chapter nine verse one text
";

        string target = UpdateUsfm(
            "ISA",
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.English
        );
        string result =
            @"\id ISA - Test
\c 8
\p
\v 22 Verse twenty-two and twenty-three text
\c 9
\nb
\v 1
\p
\v 2 Chapter nine verse one text
";
        AssertUsfmEqualsExactly(target, result);
    }

    private static string UpdateUsfm(
        string bookId,
        string source,
        ScrVers sourceVersification,
        ScrVers targetVersification
    )
    {
        source = source.Trim().ReplaceLineEndings("\r\n") + "\r\n";
        var settings = new DefaultParatextProjectSettings(
            versification: sourceVersification,
            fileNameForm: "MAT",
            fileNameSuffix: string.Empty,
            fileNamePrefix: string.Empty
        );
        var files = new Dictionary<string, string> { [bookId] = source };
        var updater = new MemoryParatextProjectVersificationConverter(files, settings);
        return updater.UpdateUsfm(bookId, targetVersification);
    }

    private static void AssertUsfmEquals(string target, string truth)
    {
        Assert.That(target, Is.Not.Null);
        string[] targetLines = target.Split('\n');
        string[] truthLines = truth.Split('\n');
        for (int i = 0; i < truthLines.Length; i++)
            Assert.That(targetLines[i].Trim(), Is.EqualTo(truthLines[i].Trim()), message: $"Line {i}");
    }

    // AssertUsfmEquals only walks the expected lines, so output that runs past the end of the
    // expected USFM slips through. These cases turn on content being dropped, so they need the
    // line count checked too.
    private static void AssertUsfmEqualsExactly(string target, string truth)
    {
        AssertUsfmEquals(target, truth);
        Assert.That(NonEmptyLineCount(target), Is.EqualTo(NonEmptyLineCount(truth)), message: "extra output");

        static int NonEmptyLineCount(string usfm) => usfm.Split('\n').Count(l => l.Trim().Length > 0);
    }
}
