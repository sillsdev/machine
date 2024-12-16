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

        Assert.That(rows[0].Text, Is.EqualTo("World"));
        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:f")));
        Assert.That(rows[1].Text, Is.EqualTo("This is a comment"));
        Assert.That(rows[2].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0:2/ip")));
        Assert.That(rows, Has.Length.EqualTo(3), string.Join(",", rows.Select(tr => tr.Text)));
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
            Assert.That(rows, Has.Length.EqualTo(1), string.Join(",", rows.Select(tr => tr.Text)));
            Assert.That(rows[0].Text, Is.EqualTo("This is the first verse."));
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
