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

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.English, targetVersification: ScrVers.Original);
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

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
\nb
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
\p
\v 1-5 Lines
\v 6 Line
\q Another line
\c 151
\p
\v 1-7 More lines
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id PSA - Test
\h Psalms
\c 150
\p
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
\p
\v 1-23 Text 1
\v 24-90 Text 2
\p More text 2
\v 91-100 Text 3
\c 4
\p
\v 1 Text 4
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\h Daniel
\c 3
\p
\v 1-23 Text 1
\v 24-33 Text 3
\c 4
\p
\v 1 Text 4
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
\p
\v 22
\v 23
\c 9
\p
\v 1
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
\p
\v 22
\c 9
\p
\v 1
\v 2
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.English, targetVersification: ScrVers.Original);
        string result =
            @"\id ISA - Test
\c 8
\p
\v 22
\p
\v 23
\c 9
\nb
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
\p
\v 22-23
\c 9
\p
\v 1
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
\p
\v 1-22
\v 23-89
\v 90-100
\c 4
\p
\v 1
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\p
\v 1-22
\v 23
\v 24-33
\c 4
\p
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
\p
\v 1-100
\c 4
\p
\v 1
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\p
\v 1-33
\c 4
\p
\v 1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_HeadingIntroducingKeptVerse_IsPreserved()
    {
        // Russian Orthodox vs. Original
        // DAN 3:24-90 = DAG 3:24-90
        // DAN 3:91-100 = DAN 3:24-33

        string usfm =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\v 24-90 Dropped text
\s1 \nd Section\nd*
\p
\v 91-100 More text
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\s1 \nd Section\nd*
\p
\v 24-33 More text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_HeadingIntroducingDroppedVerse_IsDropped()
    {
        // Russian Orthodox vs. Original
        // PSA 151:1-7 = PS2 1:1-7

        string usfm =
            @"\id PSA - Test
\c 150
\p
\v 1-5 Lines
\v 6 Line
\q Another line
\c 151
\s1 \nd Section\nd*
\p
\v 1-7 More lines
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.RussianOrthodox,
            targetVersification: ScrVers.Original
        );
        string result =
            @"\id PSA - Test
\c 150
\p
\v 1-5 Lines
\v 6 Line
\q Another line
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_DropVerseText()
    {
        string usfm =
            @"\id DAN - Test
\c 3
\p
\v 1-23 Text
\v 24-90 Dropped text
\v 91-100 More text
";

        string target = UpdateUsfm(
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
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_ChapterMarkerIsFollowedByParagraphMarker()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24

        string usfm =
            @"\id MAL - Test
\c 3
\p
\v 1-18 Text
\v 19-23 More text
\v 24 Last text
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_ChapterMarkerIsFollowedByParagraphMarker_CrossChapterVerseRange()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23

        string usfm =
            @"\id ISA - Test
\c 8
\p
\v 22-23
\c 9
\p
\v 1
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_ChapterMarkerIsFollowedByParagraphMarker_HeadingOpensParagraph()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24

        string usfm =
            @"\id MAL - Test
\c 3
\p
\v 18 Text
\s1 Section
\p
\v 19-23 More text
\v 24 Last text
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
        string result =
            @"\id MAL - Test
\c 3
\p
\v 18 Text
\c 4
\s1 Section
\p
\v 1-5 More text
\v 6 Last text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_HeadingAfterChapterLabel_KeepsMarkerContent()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23

        string usfm =
            @"\id ISA - Test
\c 8
\p
\v 22 Text
\c 9
\cl Chapter Nine
\s1 Section
\p
\v 1 Nine one
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
        string result =
            @"\id ISA - Test
\c 8
\p
\v 22 Text
\c 9
\cl Chapter Nine
\s1 Section
\p
\v 2 Nine one
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_CrossChapterVerseRange_TextStaysWithFirstVerse()
    {
        // English vs. Original
        // ISA 9:1 = ISA 8:23

        string usfm =
            @"\id ISA - Test
\c 8
\p
\v 22-23 Verse twenty-two and twenty-three text
\c 9
\p
\v 1 Chapter nine verse one text
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_IgnoreInvalidChapter()
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
\c 2@
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\v 19-23
\v 24 Text
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);

        // Strip out invalid chapters since we can't reliably convert them
        string result =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1 Text
\v 2-14
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\c 4
\nb
\v 1-5
\v 6 Text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_IgnoreInvalidVerse()
    {
        // English vs. Original
        // MAL 4:1-6 = MAL 3:19-24

        string usfm =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1@ Text
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

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);

        // Just pass invalid verses through to target
        string result =
            @"\id MAL
\h Malachi
\c 1
\s1 Section
\p
\v 1@ Text
\v 2-14
\c 2
\v 1-17
\c 3
\p
\v 1-17
\v 18 Text \f More text \f*
\c 4
\nb
\v 1-5
\v 6 Text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_MissingVerseInRange()
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
\v 19-21,23 Text
\v 24 Text
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
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
\nb
\v 1-3 Text
\v 5
\v 6 Text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_SameSourceAndTargetVersification()
    {
        string usfm =
            @"\id MAT - Test
\h Matthew
\mt Matthew
\ip An introduction to Matthew\fe + \ft This is an endnote.\fe*
\p \rq MAT 1\rq* Here is another paragraph.
\p and with a \w keyword|a special concept\w* in it.
\p and a \weirdtaglookingthing that is not an actual tag.
\c 1
\s Chapter One
\v 1 Chapter \pn one\+pro WON\+pro*\pn*, verse one.\f + \fr 1:1: \ft This is a footnote for v1.\f*
\li1
\v 2 \bd C\bd*hapter one,
\li2 verse\f + \fr 1:2: \ft This is a footnote for v2.\f* two.
\v 3 Chapter one \w*,
\li2 verse three.
\v 4 Chapter one with odd whitespace, 
\li2 verse four,
\v 5 Chapter one,
\li2 verse \fig Figure 1|src=""image1.png"" size=""col"" ref=""1:5""\fig* five.
\v 6 Verse 6 content.
\v 7
\v 8
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.English, targetVersification: ScrVers.English);
        AssertUsfmEquals(target, usfm);
    }

    [Test]
    public void GetUsfm_PreceedingHeadingsNotMoved()
    {
        // English vs. Original
        // JOL 2:27-28 = JOL 2:27-3:1

        string usfm =
            @"\id JOL
\c 2
\v 27 Then you will know that I am present in Israel
\q2 and that I am the LORD your God,
\q2 and there is no other.
\q1 My people will never again
\q2 be put to shame.
\s1 I Will Pour Out My Spirit
\r (Acts 2:14–36)
\q1
\v 28 And afterward, I will pour out My Spirit on all people.
\q2 Your sons and daughters will prophesy,
\q1 your old men will dream dreams,
\q2 your young men will see visions.
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.English, targetVersification: ScrVers.Original);
        string result =
            @"\id JOL
\c 2
\v 27 Then you will know that I am present in Israel
\q2 and that I am the LORD your God,
\q2 and there is no other.
\q1 My people will never again
\q2 be put to shame.
\c 3
\s1 I Will Pour Out My Spirit
\r (Acts 2:14–36)
\q1
\v 1 And afterward, I will pour out My Spirit on all people.
\q2 Your sons and daughters will prophesy,
\q1 your old men will dream dreams,
\q2 your young men will see visions.
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_MergedVerses()
    {
        // Original vs. English
        // PSA 51:1-3 = PSA 51:0-1

        string usfm =
            @"\id PSA
\c 51
\s1 Create in Me a Clean Heart, O God
\r (2 Samuel 12:1–12)
\p
\v 1 For the choirmaster. A Psalm of David.
\v 2 When Nathan the prophet came to him after his adultery with Bathsheba.
\b
\q1
\v 3 Have mercy on me, O God,
\q2 according to Your loving devotion;
\q1 according to Your great compassion,
\q2 blot out my transgressions.
";

        string target = UpdateUsfm(usfm, sourceVersification: ScrVers.Original, targetVersification: ScrVers.English);
        string result =
            @"\id PSA
\c 51
\s1 Create in Me a Clean Heart, O God
\r (2 Samuel 12:1–12)
\p
\v 0 For the choirmaster. A Psalm of David. When Nathan the prophet came to him after his adultery with Bathsheba.
\b
\q1
\v 1 Have mercy on me, O God,
\q2 according to Your loving devotion;
\q1 according to Your great compassion,
\q2 blot out my transgressions.
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_MergedVerses_RangeExtendsPastMergedVerse()
    {
        // Original vs. Russian Orthodox
        // LEV 14:55-56 = LEV 14:55

        string usfm =
            @"\id LEV
\c 14
\p
\v 55 for leprosy in a garment or in a house,
\v 56-57 for a swelling, a rash, or a spot, to determine when something is clean or unclean.
";

        string target = UpdateUsfm(
            usfm,
            sourceVersification: ScrVers.Original,
            targetVersification: ScrVers.RussianOrthodox
        );
        string result =
            @"\id LEV
\c 14
\p
\v 55 for leprosy in a garment or in a house,
\v 56 for a swelling, a rash, or a spot, to determine when something is clean or unclean.
";
        AssertUsfmEquals(target, result);
    }

    private static string UpdateUsfm(string source, ScrVers sourceVersification, ScrVers targetVersification)
    {
        source = source.Trim().ReplaceLineEndings("\r\n") + "\r\n";
        var settings = new DefaultParatextProjectSettings(
            versification: sourceVersification,
            fileNameForm: "MAT",
            fileNameSuffix: string.Empty,
            fileNamePrefix: string.Empty
        );
        var handler = new ConvertUsfmVersificationHandler(targetVersification);
        var tokenizer = new UsfmTokenizer(settings.Stylesheet);
        IReadOnlyList<UsfmToken> tokens = tokenizer.Tokenize(source);
        UsfmParser.Parse(tokens, handler, settings.Stylesheet, settings.Versification);
        return handler.GetUsfm(settings.Stylesheet);
    }

    private static void AssertUsfmEquals(string target, string truth)
    {
        Assert.That(target, Is.Not.Null);
        string[] targetLines = target.Split('\n');
        string[] truthLines = truth.Split('\n');
        // Assert.That(targetLines.Length, Is.EqualTo(truthLines.Length));
        for (int i = 0; i < truthLines.Length; i++)
        {
            Assert.That(
                targetLines[i].Trim(),
                Is.EqualTo(truthLines[i].Trim()),
                message: string.Join(
                    "\n",
                    [
                        "Expected vs. \n\tActual",
                        .. truthLines
                            .Zip(targetLines)
                            .Select(pair =>
                                $"\n{pair.First}\n\t{(pair.First.Trim() != pair.Second.Trim() ? "***" : "")}{pair.Second}"
                            ),
                    ]
                )
            );
        }
    }
}
