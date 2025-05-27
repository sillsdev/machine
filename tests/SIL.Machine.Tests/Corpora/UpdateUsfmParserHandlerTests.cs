using System.Collections.Immutable;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UpdateUsfmParserHandlerTests
{
    [Test]
    public void GetUsfm_Verse_CharStyle()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "First verse of the first chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(
            target,
            Contains.Substring(
                "\\v 1 First verse of the first chapter. \\f + \\fr 1:1: \\ft This is a footnote for v1.\\f*\r\n\\li1\r\n\\v 2"
            )
        );
    }

    [Test]
    public void GetUsfm_IdText()
    {
        string target = UpdateUsfm(idText: "Updated");
        Assert.That(target, Contains.Substring("\\id MAT - Updated\r\n"));
    }

    [Test]
    public void GetUsfm_StripAllText()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update 1"),
            (ScrRef("MAT 1:3"), "Update 3")
        };
        var usfm =
            @"\id MAT - Test
\c 1
\r keep this reference
\rem and this reference too
\ip but remove this text
\v 1 Chapter \add one\add*, \p verse \f + \fr 1:1: \ft This is a \+bd ∆\+bd* footnote.\f*one.
\v 2 Chapter \add one\add*, \p verse \f + \fr 1:2: \ft This is a \+bd ∆\+bd* footnote.\f*two.
\v 3 Verse 3
\v 4 Verse 4
";

        string target = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve
        );

        var result =
            @"\id MAT
\c 1
\r keep this reference
\rem and this reference too
\ip
\v 1 Update 1 \add \add*
\p \f + \fr 1:1: \ft This is a \+bd ∆\+bd* footnote.\f*
\v 2 \add \add*
\p \f + \fr 1:2: \ft This is a \+bd ∆\+bd* footnote.\f*
\v 3 Update 3
\v 4
";
        AssertUsfmEquals(target, result);

        target = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip,
            embedBehavior: UpdateUsfmMarkerBehavior.Strip,
            styleBehavior: UpdateUsfmMarkerBehavior.Strip
        );

        result =
            @"\id MAT
\c 1
\r keep this reference
\rem and this reference too
\ip
\v 1 Update 1
\v 2
\v 3 Update 3
\v 4
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_StripParagraphs_PreserveParagraphStyles()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/1:rem"), "New remark"),
            (ScrRef("MAT 1:0/3:ip"), "Another new remark"),
            (ScrRef("MAT 1:1"), "Update 1"),
        };
        string usfm =
            @"\id MAT
\c 1
\rem Update remark
\r reference
\ip This is another remark, but with a different marker
\v 1 This is a verse
";

        string target = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip
        );
        string result =
            @"\id MAT
\c 1
\rem Update remark
\r reference
\ip Another new remark
\v 1 Update 1
";

        AssertUsfmEquals(target, result);

        var targetDiffParagraph = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip,
            preserveParagraphStyles: ImmutableHashSet.Create("ip")
        );
        string resultDiffParagraph =
            @"\id MAT
\c 1
\rem New remark
\r
\ip This is another remark, but with a different marker
\v 1 Update 1
";

        AssertUsfmEquals(targetDiffParagraph, resultDiffParagraph);
    }

    [Test]
    public void GetUsfm_PreserveParagraphs()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/1:rem"), "Update remark"),
            (ScrRef("MAT 1:1"), "Update 1"),
        };
        string usfm =
            @"\id MAT
\c 1
\rem Update remark
\r reference
\ip This is another remark, but with a different marker
\v 1 This is a verse
";

        string target = UpdateUsfm(rows, usfm, textBehavior: UpdateUsfmTextBehavior.StripExisting);
        string result =
            @"\id MAT
\c 1
\rem Update remark
\r reference
\ip
\v 1 Update 1
";

        AssertUsfmEquals(target, result);

        var targetDiffParagraph = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            preserveParagraphStyles: ImmutableHashSet.Create("ip")
        );
        string resultDiffParagraph =
            @"\id MAT
\c 1
\rem Update remark
\r
\ip This is another remark, but with a different marker
\v 1 Update 1
";

        AssertUsfmEquals(targetDiffParagraph, resultDiffParagraph);
    }

    [Test]
    public void GetUsfm_ParagraphInVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1"), };
        string usfm =
            @"\id MAT - Test
\c 1
\p paragraph not in a verse
\v 1 verse 1 \p inner verse paragraph
\s1 Section Header
\v 2 Verse 2 \p inner verse paragraph
";

        string target = UpdateUsfm(rows, usfm, paragraphBehavior: UpdateUsfmMarkerBehavior.Strip);

        string result =
            @"\id MAT - Test
\c 1
\p paragraph not in a verse
\v 1 Update 1
\s1 Section Header
\v 2 Verse 2
\p inner verse paragraph
";
        AssertUsfmEquals(target, result);

        string targetStrip = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip
        );

        string resultStrip =
            @"\id MAT
\c 1
\p
\v 1 Update 1
\s1
\v 2
";

        AssertUsfmEquals(targetStrip, resultStrip);
    }

    [Test]
    public void GetUsfm_PreferExisting()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update 1"),
            (ScrRef("MAT 1:2"), "Update 2"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Some text
\v 2
\v 3 Other text
";
        string target = UpdateUsfm(rows, usfm, textBehavior: UpdateUsfmTextBehavior.PreferExisting);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Some text
\v 2 Update 2
\v 3 Other text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_PreferRows()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:6"), "Text 6"),
            (ScrRef("MAT 1:7"), "Text 7"),
        };
        string target = UpdateUsfm(rows, textBehavior: UpdateUsfmTextBehavior.PreferNew);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(target, Contains.Substring("\\v 6 Text 6\r\n"));
        Assert.That(target, Contains.Substring("\\v 7 Text 7\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_StripNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1"), "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_ReplaceWithNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "updated text") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Chapter \add one\add*, verse \f + \fr 2:1: \ft This is a footnote.\f*one.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 updated text \f + \fr 2:1: \ft This is a footnote.\f*
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_Verse_RowVerseSegment()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1a"), "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 1 First verse of the second chapter. \\f + \\fr 2:1: \\ft This is a footnote.\\f*\r\n"
            )
        );
    }

    [Test]
    public void GetUsfm_Verse_UsfmVerseSegment()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:7"), "Seventh verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 7a Seventh verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_MultipleParas()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:2"), "Second verse of the first chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 2 Second verse of the first chapter.\r\n\\li2 \\f + \\fr 1:2: \\ft This is a footnote for v2.\\f*"
            )
        );
    }

    [Test]
    public void GetUsfm_Verse_Table()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:9"), "Ninth verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 9 Ninth verse of the second chapter. \\tcr2 \\tc3 \\tcr4\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_RangeSingleRowMultipleVerses()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (
                ScrRef("MAT 2:11", "MAT 2:12"),
                "Eleventh verse of the second chapter. Twelfth verse of the second chapter."
            )
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 11-12 Eleventh verse of the second chapter. Twelfth verse of the second chapter.\r\n"
            )
        );
    }

    [Test]
    public void GetUsfm_Verse_RangeSingleRowSingleVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:11"), "Eleventh verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 11-12 Eleventh verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_RangeMultipleRowsSingleVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:11"), "Eleventh verse of the second chapter."),
            (ScrRef("MAT 2:12"), "Twelfth verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 11-12 Eleventh verse of the second chapter. Twelfth verse of the second chapter.\r\n"
            )
        );
    }

    [Test]
    public void GetUsfm_MergeVerseSegments()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:2"), "Verse 2."),
            (ScrRef("MAT 2:2a"), "Verse 2a."),
            (ScrRef("MAT 2:2b"), "Verse 2b.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 2-3 Verse 2. Verse 2a. Verse 2b.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_OptBreak()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:2"), "Second verse of the second chapter."),
            (ScrRef("MAT 2:3"), "Third verse of the second chapter.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        Assert.That(
            target,
            Contains.Substring("\\v 2-3 Second verse of the second chapter. Third verse of the second chapter.\r\n")
        );
    }

    [Test]
    public void GetUsfm_Verse_Milestone()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:10"), "Tenth verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\v 10 Tenth verse of the second chapter. \\tc3-4 \\qt-s |Jesus\\*\\qt-e\\*\r\n")
        );
    }

    [Test]
    public void GetUsfm_Verse_Unmatched()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:3"), "Third verse of the first chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 3 Third verse of the first chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_CharStyle()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 2:0/3:s1"), "The second chapter.") };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\s1 The second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_Paragraph()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:0/8:s"), "The first chapter.") };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\s The first chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_Relaxed()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/s"), "The first chapter."),
            (ScrRef("MAT 1:1"), "First verse of the first chapter."),
            (ScrRef("MAT 2:0/tr/tc1"), "The first cell of the table."),
            (ScrRef("MAT 2:0/tr/tc2"), "The second cell of the table."),
            (ScrRef("MAT 2:0/tr/tc1"), "The third cell of the table.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\s The first chapter.\r\n"));
        Assert.That(
            target,
            Contains.Substring(
                "\\v 1 First verse of the first chapter. \\f + \\fr 1:1: \\ft This is a footnote for v1.\\f*\r\n"
            )
        );
        Assert.That(
            target,
            Contains.Substring("\\tr \\tc1 The first cell of the table. \\tc2 The second cell of the table.\r\n")
        );
        Assert.That(
            target,
            Contains.Substring("\\tr \\tc1 The third cell of the table. \\tc2 Row two, column two.\r\n")
        );
    }

    [Test]
    public void GetUsfm_NonVerse_Sidebar()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:3/1:esb/1:ms"), "The first paragraph of the sidebar.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\ms The first paragraph of the sidebar.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_Table()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:0/1:tr/1:tc1"), "The first cell of the table."),
            (ScrRef("MAT 2:0/2:tr/1:tc1"), "The third cell of the table.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\tr \\tc1 The first cell of the table. \\tc2 Row one, column two.\r\n")
        );
        Assert.That(
            target,
            Contains.Substring("\\tr \\tc1 The third cell of the table. \\tc2 Row two, column two.\r\n")
        );
    }

    [Test]
    public void GetUsfm_NonVerse_OptBreak()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:3/1:esb/2:p"), "The second paragraph of the sidebar.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\p The second paragraph of the sidebar.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_Milestone()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:7a/1:s"), "A new section header.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\s A new section header. \\ts-s\\*\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_SkipNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        Assert.That(target, Contains.Substring("\\ip The introductory paragraph.\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_ReplaceWithNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft This is an endnote.\\fe*\r\n")
        );
    }

    [Test]
    public void GetUsfm_Verse_DoubleVaVp()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 3:1"), "Updating later in the book to start.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(
            target,
            Contains.Substring("\\v 1 \\va 2\\va*\\vp 1 (2)\\vp*Updating later in the book to start.\r\n")
        );
    }

    [Test]
    public void GetUsfm_Verse_LastSegment()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Updating the last verse.") };
        string usfm =
            @"\id MAT - Test
\c 1
\v 1
";
        string target = UpdateUsfm(rows, usfm);

        Assert.That(
            target,
            Is.EqualTo(
                    @"\id MAT - Test
\c 1
\v 1 Updating the last verse.
"
                )
                .IgnoreLineEndings()
        );
    }

    [Test]
    public void GetUsfm_Verse_PretranslationsBeforeText()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("GEN 1:1"), "Pretranslations before the start"),
            (ScrRef("GEN 1:2"), "Pretranslations before the start"),
            (ScrRef("GEN 1:3"), "Pretranslations before the start"),
            (ScrRef("GEN 1:4"), "Pretranslations before the start"),
            (ScrRef("GEN 1:5"), "Pretranslations before the start"),
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft This is an endnote.\\fe*\r\n")
        );
    }

    [Test]
    public void GetUsfm_StripParagraphs()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/2:p"), "Update Paragraph"),
            (ScrRef("MAT 1:1"), "Update Verse 1")
        };

        var usfm =
            @"\id MAT - Test
\c 1
\p This is a paragraph before any verses
\p This is a second paragraph before any verses
\v 1 Hello
\p World
\v 2 Hello
\p World
";

        string target = UpdateUsfm(rows, usfm, paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve);
        var resultP =
            @"\id MAT - Test
\c 1
\p This is a paragraph before any verses
\p Update Paragraph
\v 1 Update Verse 1
\p
\v 2 Hello
\p World
";
        AssertUsfmEquals(target, resultP);

        target = UpdateUsfm(rows, usfm, paragraphBehavior: UpdateUsfmMarkerBehavior.Strip);
        var resultS =
            @"\id MAT - Test
\c 1
\p This is a paragraph before any verses
\p Update Paragraph
\v 1 Update Verse 1
\v 2 Hello
\p World
";
        AssertUsfmEquals(target, resultS);
    }

    [Test]
    public void GetUsfm_PreservationRawStrings()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), @"Update all in one row \f \fr 1.1 \ft Some note \f*")
        };

        var usfm =
            @"\id MAT - Test
\c 1
\v 1 \f \fr 1.1 \ft Some note \f*Hello World
";

        string target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update all in one row \f \fr 1.1 \ft Some note \f*
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void GetUsfm_BeginningOfVerseEmbed()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), @"Updated text") };

        var usfm =
            @"\id MAT - Test
\c 1
\v 1 \f \fr 1.1 \ft Some note \f* Text after note
";

        string target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Updated text
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void CrossReferenceDontUpdate()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1/1:x"), "Update the cross reference"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Cross reference verse \x - \xo 2:3-4 \xt Cool Book 3:24 \xta The annotation \x* and more content.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Cross reference verse \x - \xo 2:3-4 \xt Cool Book 3:24 \xta The annotation \x* and more content.
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void PreserveFig()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update"), };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \fig stuff\fig* more text and more.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update \fig stuff\fig*
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void NoteExplicitEndMarkers()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update text"),
            (ScrRef("MAT 1:1/1:f"), "Update note"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \f + \fr 2.4\fr* \fk The \+nd Lord\+nd*:\fk* \ft See \+nd Lord\+nd* in Word List.\ft*\f* and the end.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update text \f + \fr 2.4\fr* \fk The \+nd Lord\+nd*:\fk* \ft See \+nd Lord\+nd* in Word List.\ft*\f*
";
        AssertUsfmEquals(target, result);

        target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result2 =
            @"\id MAT - Test
\c 1
\v 1 Update text
";
        AssertUsfmEquals(target, result2);
    }

    [Test]
    public void UpdateBlock_Verse_PreserveParas()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse 1 \p inner verse paragraph
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse 1 ", true),
            (UsfmUpdateBlockElementType.Paragraph, "\\p ", false),
            (UsfmUpdateBlockElementType.Text, "inner verse paragraph ", true)
        );
    }

    [Test]
    public void UpdateBlock_Verse_StripParas()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse 1 \p inner verse paragraph
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse 1 ", true),
            (UsfmUpdateBlockElementType.Paragraph, "\\p ", true),
            (UsfmUpdateBlockElementType.Text, "inner verse paragraph ", true)
        );
    }

    [Test]
    public void UpdateBlock_Verse_Range()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1-3 verse 1 through 3
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1", "MAT 1:2", "MAT 1:3"],
            (UsfmUpdateBlockElementType.Other, "\\v 1-3 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse 1 through 3 ", true)
        );
    }

    [Test]
    public void UpdateBlock_Footnote_PreserveEmbeds()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse\f \fr 1.1 \ft Some note \f* 1
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse", true),
            (UsfmUpdateBlockElementType.Embed, "\\f \\fr 1.1 \\ft Some note \\f*", false),
            (UsfmUpdateBlockElementType.Text, " 1 ", true)
        );
    }

    [Test]
    public void UpdateBlock_Footnote_StripEmbeds()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse\f \fr 1.1 \ft Some note \f* 1
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse", true),
            (UsfmUpdateBlockElementType.Embed, "\\f \\fr 1.1 \\ft Some note \\f*", true),
            (UsfmUpdateBlockElementType.Text, " 1 ", true)
        );
    }

    [Test]
    public void UpdateBlock_NonVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/1:s"), "Updated section Header")
        };
        var usfm =
            @"\id MAT - Test
\s Section header
\c 1
\v 1 verse 1
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(rows, usfm, usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]);

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(2));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:0/1:s"],
            (UsfmUpdateBlockElementType.Text, "Updated section Header ", false),
            (UsfmUpdateBlockElementType.Text, "Section header ", true)
        );
    }

    [Test]
    public void UpdateBlock_Verse_PreserveStyles()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse \bd 1\bd*
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse ", true),
            (UsfmUpdateBlockElementType.Style, "\\bd ", false),
            (UsfmUpdateBlockElementType.Text, "1", true),
            (UsfmUpdateBlockElementType.Style, "\\bd*", false),
            (UsfmUpdateBlockElementType.Text, " ", true)
        );
    }

    [Test]
    public void UpdateBlock_Verse_StripStyles()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 verse \bd 1\bd*
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(
            rows,
            usfm,
            styleBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]
        );

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(1));

        UsfmUpdateBlock usfmUpdateBlock = usfmUpdateBlockHandler.Blocks[0];
        AssertUpdateBlockEquals(
            usfmUpdateBlock,
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "verse ", true),
            (UsfmUpdateBlockElementType.Style, "\\bd ", true),
            (UsfmUpdateBlockElementType.Text, "1", true),
            (UsfmUpdateBlockElementType.Style, "\\bd*", true),
            (UsfmUpdateBlockElementType.Text, " ", true)
        );
    }

    [Test]
    public void UpdateBlock_Verse_SectionHeader()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\p
\v 1 Verse 1
\s Section header
\p
\v 2 Verse 2
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(rows, usfm, usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]);

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(4));
        AssertUpdateBlockEquals(usfmUpdateBlockHandler.Blocks[0], ["MAT 1:0/1:p"]);
        AssertUpdateBlockEquals(
            usfmUpdateBlockHandler.Blocks[1],
            ["MAT 1:1/1:s"],
            (UsfmUpdateBlockElementType.Text, "Section header ", false)
        );
        AssertUpdateBlockEquals(
            usfmUpdateBlockHandler.Blocks[2],
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Verse 1 ", true),
            (UsfmUpdateBlockElementType.Paragraph, "\\s Section header ", false),
            (UsfmUpdateBlockElementType.Paragraph, "\\p ", false)
        );
        AssertUpdateBlockEquals(
            usfmUpdateBlockHandler.Blocks[3],
            ["MAT 1:2"],
            (UsfmUpdateBlockElementType.Other, "\\v 2 ", false),
            (UsfmUpdateBlockElementType.Text, "Verse 2 ", false)
        );
    }

    [Test]
    public void UpdateBlock_Verse_SectionHeaderInVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update 1") };
        var usfm =
            @"\id MAT - Test
\c 1
\p
\v 1 Beginning of verse
\s Section header
\p end of verse
";
        TestUsfmUpdateBlockHandler usfmUpdateBlockHandler = new TestUsfmUpdateBlockHandler();
        UpdateUsfm(rows, usfm, usfmUpdateBlockHandlers: [usfmUpdateBlockHandler]);

        Assert.That(usfmUpdateBlockHandler.Blocks.Count, Is.EqualTo(3));
        AssertUpdateBlockEquals(usfmUpdateBlockHandler.Blocks[0], ["MAT 1:0/1:p"]);
        AssertUpdateBlockEquals(
            usfmUpdateBlockHandler.Blocks[1],
            ["MAT 1:1/1:s"],
            (UsfmUpdateBlockElementType.Text, "Section header ", false)
        );
        AssertUpdateBlockEquals(
            usfmUpdateBlockHandler.Blocks[2],
            ["MAT 1:1"],
            (UsfmUpdateBlockElementType.Other, "\\v 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Update 1 ", false),
            (UsfmUpdateBlockElementType.Text, "Beginning of verse ", true),
            (UsfmUpdateBlockElementType.Paragraph, "\\s Section header ", false),
            (UsfmUpdateBlockElementType.Paragraph, "\\p ", false),
            (UsfmUpdateBlockElementType.Text, "end of verse ", true)
        );
    }

    private static ScriptureRef[] ScrRef(params string[] refs)
    {
        return refs.Select(r => ScriptureRef.Parse(r)).ToArray();
    }

    private static string UpdateUsfm(
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)>? rows = null,
        string? source = null,
        string? idText = null,
        UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferNew,
        UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
        UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
        UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
        IEnumerable<string>? preserveParagraphStyles = null,
        IEnumerable<UsfmUpdateBlockHandler>? usfmUpdateBlockHandlers = null
    )
    {
        if (source is null)
        {
            var updater = new FileParatextProjectTextUpdater(CorporaTestHelpers.UsfmTestProjectPath);
            return updater.UpdateUsfm(
                "MAT",
                rows,
                idText,
                textBehavior,
                paragraphBehavior,
                embedBehavior,
                styleBehavior,
                preserveParagraphStyles,
                usfmUpdateBlockHandlers
            );
        }
        else
        {
            source = source.Trim().ReplaceLineEndings("\r\n") + "\r\n";
            var updater = new UpdateUsfmParserHandler(
                rows,
                idText,
                textBehavior,
                paragraphBehavior,
                embedBehavior,
                styleBehavior,
                preserveParagraphStyles,
                usfmUpdateBlockHandlers
            );
            UsfmParser.Parse(source, updater);
            return updater.GetUsfm();
        }
    }

    private static void AssertUsfmEquals(string target, string truth)
    {
        Assert.That(target, Is.Not.Null);
        var target_lines = target.Split(["\n"], StringSplitOptions.None);
        var truth_lines = truth.Split(["\n"], StringSplitOptions.None);
        for (int i = 0; i < truth_lines.Length; i++)
        {
            Assert.That(target_lines[i].Trim(), Is.EqualTo(truth_lines[i].Trim()), message: $"Line {i}");
        }
    }

    private static void AssertUpdateBlockEquals(
        UsfmUpdateBlock block,
        string[] expectedRefs,
        params (UsfmUpdateBlockElementType, string, bool)[] expectedElements
    )
    {
        var parsedExtractedRefs = expectedRefs.Select(r => ScriptureRef.Parse(r));
        Assert.That(block.Refs.SequenceEqual(parsedExtractedRefs));
        Assert.That(block.Elements.Count, Is.EqualTo(expectedElements.Length));
        foreach (
            (
                UsfmUpdateBlockElement element,
                (UsfmUpdateBlockElementType expectedType, string expectedUsfm, bool expectedMarkedForRemoval)
            ) in block.Elements.Zip(expectedElements)
        )
        {
            Assert.That(element.Type, Is.EqualTo(expectedType));
            Assert.That(string.Join("", element.Tokens.Select(t => t.ToUsfm())), Is.EqualTo(expectedUsfm));
            Assert.That(element.MarkedForRemoval, Is.EqualTo(expectedMarkedForRemoval));
        }
    }

    private class TestUsfmUpdateBlockHandler : UsfmUpdateBlockHandler
    {
        public List<UsfmUpdateBlock> Blocks { get; }

        public TestUsfmUpdateBlockHandler()
        {
            Blocks = new List<UsfmUpdateBlock>();
        }

        public override UsfmUpdateBlock ProcessBlock(UsfmUpdateBlock block)
        {
            UsfmUpdateBlock newBlock = block.Clone();
            Blocks.Add(newBlock);
            return newBlock;
        }
    }
}
