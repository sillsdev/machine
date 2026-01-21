using System.Text;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmMemoryTextTests
{
    [Test]
    public void GetRows_VerseDescriptiveTitle()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\d
\v 1 Descriptive title
\c 2
\b
\q1
\s
"
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(1));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[0].Text,
                Is.EqualTo("Descriptive title"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_LastSegment()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\v 1 Last segment
"
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(1));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[0].Text,
                Is.EqualTo("Last segment"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_DuplicateVerseWithTable()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\v 1 First verse
\periph Table of Contents Abbreviation
\rem non verse content 1
\v 1 duplicate first verse
\rem non verse content 2
\mt1 Table
\tr \tc1 row 1 cell 1 \tc2 row 1 cell 2
\tr \tc1 row 2 cell 1 \tc2 row 2 cell 2
",
            includeAllText: true
        );

        Assert.That(rows, Has.Length.EqualTo(5), string.Join(",", rows.Select(tr => tr.Text)));
    }

    [Test]
    public void GetRows_TriplicateVerse()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\v 1 First verse 1
\rem non verse 1
\v 1 First verse 2
\rem non verse 2
\v 1 First verse 3
\rem non verse 3
\v 2 Second verse
",
            includeAllText: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(5), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo("First verse 1"));
            Assert.That(rows[3].Text, Is.EqualTo("non verse 3"));
            Assert.That(rows[4].Text, Is.EqualTo("Second verse"));
        });
    }

    [Test]
    public void GetRows_OptBreak_MiddleIncludeMarkers()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\v 1 First verse in line // More text
\c 2
\v 1
",
            includeAllText: true,
            includeMarkers: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo(@"First verse in line // More text"));
        });
    }

    [Test]
    public void GetRows_Sidebar_FirstTag()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\esb
\ip My sidebar text
\esbe
\c 1
\p
\v 1 First verse
",
            includeAllText: true,
            includeMarkers: true
        );

        Assert.That(rows, Has.Length.EqualTo(3), string.Join(",", rows.Select(tr => tr.Text)));
        Assert.That(rows[0].Text, Is.EqualTo("My sidebar text"));
        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:esb/1:ip")));
        Assert.That(rows[1].Text, Is.Empty);
        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:p")));
        Assert.That(rows[2].Text, Is.EqualTo("First verse"));
        Assert.That(rows[2].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
    }

    [Test]
    public void GetRows_TableRow_FirstTag()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\tr \th1 Day \th2 Tribe \th3 Leader
\tr \tcr1 1st \tc2 Judah \tc3 Nahshon son of Amminadab
\c 1
\p
\v 1 First verse
",
            includeAllText: true,
            includeMarkers: true
        );

        Assert.That(rows, Has.Length.EqualTo(8), string.Join(",", rows.Select(tr => tr.Text)));
        Assert.That(rows[0].Text, Is.EqualTo("\\th1 Day"));
        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:tr/1:th1")));
        Assert.That(rows[6].Text, Is.Empty);
        Assert.That(rows[6].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/3:p")));
        Assert.That(rows[7].Text, Is.EqualTo("First verse"));
        Assert.That(rows[7].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
    }

    [Test]
    public void GetRows_VersePara_BeginningNonVerseSegment()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\q1
\f \fr 119 \ft World \f*
\v 1 First verse in line!?!
\c 2
\d
description
\b
",
            includeAllText: true
        );

        Assert.That(rows, Has.Length.EqualTo(4), string.Join(",", rows.Select(tr => tr.Text)));
        Assert.That(rows[0].Text, Is.EqualTo(""));
        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:q1")));
        Assert.That(rows[1].Text, Is.EqualTo("First verse in line!?!"));
        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
    }

    [Test]
    public void GetRows_VersePara_CommentFirst()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\f \fr 119 \ft World \f*
\ip This is a comment
\c 1
\v 1 First verse in line!?!
\c 2
",
            includeAllText: true
        );

        Assert.That(rows[0].Text, Is.EqualTo("This is a comment"));
        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:ip")));
        Assert.That(rows[1].Text, Is.EqualTo("First verse in line!?!"));
        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
        Assert.That(rows, Has.Length.EqualTo(2), string.Join(",", rows.Select(tr => tr.Text)));
    }

    [Test]
    public void GetRows_OptBreak_Beginning()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\li //
",
            includeAllText: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(1), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo(""));
        });
    }

    [Test]
    public void GetRows_OptBreak_BeginningIncludeMarkers()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\li //
",
            includeAllText: true,
            includeMarkers: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(1), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo("//"));
        });
    }

    [Test]
    public void GetRows_OptBreak_OutsideOfSegment()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
//
\p
\v 1 This is the first verse.
",
            includeAllText: true,
            includeMarkers: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo(""));
            Assert.That(rows[1].Text, Is.EqualTo("This is the first verse."));
        });
    }

    [Test]
    public void GetRows_ParagraphBeforeNonVerseParagraph()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v 1 verse 1
\b
\s1 header
\q1
\v 2 verse 2
",
            includeAllText: true,
            includeMarkers: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(4), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[1].Text, Is.EqualTo("verse 1 \\b \\q1"));
            Assert.That(rows[2].Text, Is.EqualTo("header"));
        });
    }

    [Test]
    public void GetRows_StyleStartingNonVerseParagraphAfterEmptyParagraph()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v 1 verse 1
\b
\s1 \w header\w*
\q1
\v 2 verse 2
",
            includeAllText: true,
            includeMarkers: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(4), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[1].Text, Is.EqualTo("verse 1 \\b \\q1"));
            Assert.That(rows[2].Text, Is.EqualTo("\\w header\\w*"));
        });
    }

    [Test]
    public void GetRows_VerseZero()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\h
\mt
\c 1
\p \v 0
\s
\p \v 1 Verse one.
"
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[0].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[1].Text, Is.EqualTo("Verse one."), string.Join(",", rows.ToList().Select(tr => tr.Text)));
        });
    }

    [Test]
    public void GetRows_VerseZeroWithText()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\h
\mt
\c 1
\p \v 0 Verse zero.
\s
\p \v 1 Verse one.
"
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[0].Text, Is.EqualTo("Verse zero."), string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[1].Text, Is.EqualTo("Verse one."), string.Join(",", rows.ToList().Select(tr => tr.Text)));
        });
    }

    [Test]
    public void GetRows_PrivateUseMarker()
    {
        TextRow[] rows = GetRows(
            @"\id FRT - Test English Apocrypha
\zmt Ignore this paragraph
\mt1 Test English Apocrypha
\pc Copyright Statement \zimagecopyrights
\pc Further copyright statements
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(3));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("FRT 1:0/2:pc")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[1].Text,
                Is.EqualTo("Copyright Statement"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_NonLatinVerseNumber()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v १ Verse 1
\v 3,৪ Verses 3 and 4
\p
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(4));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:৪")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[0].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:p")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[1].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[2].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[2].Text, Is.EqualTo("Verse 1"), string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[3].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:3")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[3].Text,
                Is.EqualTo("Verses 3 and 4"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_EmptyVerseNumber()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v
\b
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:p")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[0].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:b")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[1].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));
        });
    }

    [Test]
    public void GetRows_MultipleEmptyVerseNumbers()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v
\p
\v
\p
\v
\p
",
            includeAllText: true
        );

        const int RowCount = 4;
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(RowCount));

            for (int i = 0; i < RowCount; i++)
            {
                Assert.That(
                    rows[i].Ref,
                    Is.EqualTo(ScriptureRef.Parse($"MAT 1:0/{i + 1}:p")),
                    string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
                );
                Assert.That(rows[i].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));
            }
        });
    }

    [Test]
    public void GetRows_EmptyVerseNumberWithText()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\s heading text
\v  \vn 1 verse text
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(2));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:s")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[0].Text,
                Is.EqualTo("heading text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:vn")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[1].Text,
                Is.EqualTo("1 verse text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_EmptyVerseNumberMidVerse()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v 1 verse 1 text
\v
\v 2 verse 2 text
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(3));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:p")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[0].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[1].Text,
                Is.EqualTo("verse 1 text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );

            Assert.That(
                rows[2].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:2")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[2].Text,
                Is.EqualTo("verse 2 text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_InvalidVerseNumbers()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\p
\v BK1 text goes here
\v BK 2 text goes here
\v BK 3 text goes here
\v BK 4 text goes here
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(1));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:p")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[0].Text,
                Is.EqualTo("text goes here 2 text goes here 3 text goes here 4 text goes here"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    [Test]
    public void GetRows_IncompleteVerseRange()
    {
        TextRow[] rows = GetRows(
            @"\id MAT - Test
\c 1
\s heading text
\p
\q1
\v 1,
\q1 verse 1 text
",
            includeAllText: true
        );

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Length.EqualTo(4));

            Assert.That(
                rows[0].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:s")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[0].Text,
                Is.EqualTo("heading text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );

            Assert.That(
                rows[1].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:p")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[1].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[2].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1/3:q1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(rows[2].Text, Is.Empty, string.Join(",", rows.ToList().Select(tr => tr.Text)));

            Assert.That(
                rows[3].Ref,
                Is.EqualTo(ScriptureRef.Parse("MAT 1:1/4:q1")),
                string.Join(",", rows.ToList().Select(tr => tr.Ref.ToString()))
            );
            Assert.That(
                rows[3].Text,
                Is.EqualTo("verse 1 text"),
                string.Join(",", rows.ToList().Select(tr => tr.Text))
            );
        });
    }

    private static TextRow[] GetRows(string usfm, bool includeMarkers = false, bool includeAllText = false)
    {
        UsfmMemoryText text =
            new(
                new UsfmStylesheet("usfm.sty"),
                Encoding.UTF8,
                "MAT",
                usfm.Trim().ReplaceLineEndings("\r\n") + "\r\n",
                includeMarkers: includeMarkers,
                includeAllText: includeAllText
            );
        return text.GetRows().ToArray();
    }
}
