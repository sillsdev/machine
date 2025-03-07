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
\v 1 Chapter \add one\add*, \p verse \f + \fr 2:1: \ft This is a \fm ∆\fm* footnote.\f*one.
\v 2 Chapter \add one\add*, \p verse \f + \fr 2:1: \ft This is a \fm ∆\fm* footnote.\f*two.
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
\p \f + \fr 2:1: \ft \fm ∆\fm*\f*
\v 2 \add \add*
\p \f + \fr 2:1: \ft \fm ∆\fm*\f*
\v 3 Update 3
\v 4
";
        Assess(target, result);

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
        Assess(target, result);
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
        Assess(target, result);
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
    public void GetUsfm_Verse_ReplaceNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "updated text"),
            (ScrRef("MAT 1:1/1:f"), "This is a new footnote.")
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Chapter \add one\add*, verse \f + \fr 2:1: \ft This is a footnote.\f*one.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 updated text \f + \fr 2:1: \ft This is a new footnote. \f*
";
        Assess(target, result);
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
        Assert.That(target, Contains.Substring("\\v 2-3 Verse 2. Verse 2a. Verse 2b. \\fm ∆\\fm*\r\n"));
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
            (ScrRef("MAT 2:3/2:esb/1:ms"), "The first paragraph of the sidebar.")
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
            (ScrRef("MAT 2:3/2:esb/2:p"), "The second paragraph of the sidebar.")
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
    public void GetUsfm_NonVerse_ReplaceNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph."),
            (ScrRef("MAT 1:0/3:ip/1:fe"), "This is a new endnote.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft This is a new endnote. \\fe*\r\n")
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
    public void EmbedStylePreservation()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update the greeting"),
            (ScrRef("MAT 1:1/1:f"), "Update the comment"),
            (ScrRef("MAT 1:2"), "Update the greeting only"),
            (ScrRef("MAT 1:3/1:f"), "Update the comment only"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Hello \f \fr 1.1 \ft Some \+bd note\+bd* \f*\bd World \bd*
\v 2 Good \f \fr 1.2 \ft Some other \+bd note\+bd* \f*\bd Morning \bd*
\v 3 Pleasant \f \fr 1.3 \ft A third \+bd note\+bd* \f*\bd Evening \bd*
";
        var target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve
        );
        var resultPp =
            @"\id MAT - Test
\c 1
\v 1 Update the greeting \f \fr 1.1 \ft Update the comment \+bd \+bd*\f*\bd \bd*
\v 2 Update the greeting only \f \fr 1.2 \ft Some other \+bd note\+bd* \f*\bd \bd*
\v 3 Pleasant \f \fr 1.3 \ft Update the comment only \+bd \+bd*\f*\bd Evening \bd*
";
        Assess(target, resultPp);

        target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            styleBehavior: UpdateUsfmMarkerBehavior.Strip
        );
        var resultPs =
            @"\id MAT - Test
\c 1
\v 1 Update the greeting \f \fr 1.1 \ft Update the comment \f*
\v 2 Update the greeting only \f \fr 1.2 \ft Some other \+bd note\+bd* \f*
\v 3 Pleasant \f \fr 1.3 \ft Update the comment only \f*\bd Evening \bd*
";
        Assess(target, resultPs);

        target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Strip,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve
        );
        var resultSp =
            @"\id MAT - Test
\c 1
\v 1 Update the greeting \bd \bd*
\v 2 Update the greeting only \bd \bd*
\v 3 Pleasant \bd Evening \bd*
";
        Assess(target, resultSp);

        target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Strip,
            styleBehavior: UpdateUsfmMarkerBehavior.Strip
        );
        var resultSs =
            @"\id MAT - Test
\c 1
\v 1 Update the greeting
\v 2 Update the greeting only
\v 3 Pleasant \bd Evening \bd*
";
        Assess(target, resultSs);
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
        Assess(target, resultP);

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
        Assess(target, resultS);
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
        Assess(target, result);
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
        Assess(target, result);
    }

    [Test]
    public void EmptyNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1/1:f"), "Update the note") };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 Empty Note \f \fr 1.1 \ft \f*
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Empty Note \f \fr 1.1 \ft Update the note \f*
";
        Assess(target, result);
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
        Assess(target, result);
    }

    [Test]
    public void PreserveFigAndFm()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 1:1"), "Update"), };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \fig stuff\fig* more text \fm * \fm* and more.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update \fig stuff\fig*\fm * \fm*
";
        Assess(target, result);
    }

    [Test]
    public void NestedXt()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update text"),
            (ScrRef("MAT 1:1/1:f"), "Update note"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \f + \fr 15.8 \ft Text (\+xt reference\+xt*). And more.\f* and the end.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update text \f + \fr 15.8 \ft Update note \+xt reference\+xt*\f*
";
        Assess(target, result);

        target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result2 =
            @"\id MAT - Test
\c 1
\v 1 Update text
";
        Assess(target, result2);
    }

    [Test]
    public void NonNestedXt()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update text"),
            (ScrRef("MAT 1:1/1:f"), "Update note"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \f + \fr 15.8 \ft Text \xt reference\f* and the end.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update text \f + \fr 15.8 \ft Update note \xt reference\f*
";
        Assess(target, result);

        target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result2 =
            @"\id MAT - Test
\c 1
\v 1 Update text
";
        Assess(target, result2);
    }

    [Test]
    public void MultipleFtOnlyUpdateFirst()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "Update text"),
            (ScrRef("MAT 1:1/1:f"), "Update note"),
        };
        var usfm =
            @"\id MAT - Test
\c 1
\v 1 initial text \f + \fr 15.8 \ft first note \ft second note\f* and the end.
";
        var target = UpdateUsfm(rows, usfm);
        var result =
            @"\id MAT - Test
\c 1
\v 1 Update text \f + \fr 15.8 \ft Update note \ft second note\f*
";
        Assess(target, result);

        target = UpdateUsfm(rows, usfm, embedBehavior: UpdateUsfmMarkerBehavior.Strip);
        var result2 =
            @"\id MAT - Test
\c 1
\v 1 Update text
";
        Assess(target, result2);
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
        UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip
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
                styleBehavior
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
                styleBehavior
            );
            UsfmParser.Parse(source, updater);
            return updater.GetUsfm();
        }
    }

    private static void Assess(string target, string truth)
    {
        Assert.That(target, Is.Not.Null);
        var target_lines = target.Split(new[] { "\n" }, StringSplitOptions.None);
        var truth_lines = truth.Split(new[] { "\n" }, StringSplitOptions.None);
        for (int i = 0; i < truth_lines.Length; i++)
        {
            Assert.That(target_lines[i].Trim(), Is.EqualTo(truth_lines[i].Trim()), message: $"Line {i}");
        }
    }
}
