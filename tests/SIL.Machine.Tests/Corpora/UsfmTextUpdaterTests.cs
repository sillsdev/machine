using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmTextUpdaterTests
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
        Assert.That(target, Contains.Substring("\\v 1 First verse of the first chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_IdText()
    {
        string target = UpdateUsfm(idText: "- Updated");
        Assert.That(target, Contains.Substring("\\id MAT - Updated\r\n"));
    }

    [Test]
    public void GetUsfm_StripAllText()
    {
        string target = UpdateUsfm(stripAllText: true);
        Assert.That(target, Contains.Substring("\\id MAT\r\n"));
        Assert.That(target, Contains.Substring("\\v 1\r\n"));
        Assert.That(target, Contains.Substring("\\s\r\n"));
        Assert.That(target, Contains.Substring("\\ms\r\n"));
    }

    [Test]
    public void GetUsfm_PreferExisting()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:6"), "Text 6"),
            (ScrRef("MAT 1:7"), "Text 7"),
        };
        string target = UpdateUsfm(rows, preferExistingText: true);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(target, Contains.Substring("\\v 6 Verse 6 content.\r\n"));
        Assert.That(target, Contains.Substring("\\v 7 Text 7\r\n"));
    }

    [Test]
    public void GetUsfm_PreferRows()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:6"), "Text 6"),
            (ScrRef("MAT 1:7"), "Text 7"),
        };
        string target = UpdateUsfm(rows, preferExistingText: false);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(target, Contains.Substring("\\v 6 Text 6\r\n"));
        Assert.That(target, Contains.Substring("\\v 7 Text 7\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_SkipNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1"), "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_ReplaceNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1"), "First verse of the second chapter."),
            (ScrRef("MAT 2:1/1:f"), "This is a new footnote.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\v 1 First verse of the second chapter. \\f + \\ft This is a new footnote.\\f*\r\n")
        );
    }

    [Test]
    public void GetUsfm_Verse_RowVerseSegment()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1a"), "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
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
        Assert.That(target, Contains.Substring("\\v 2 Second verse of the first chapter.\r\n\\li2\r\n"));
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
    public void GetUsfm_Verse_OptBreak()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:2"), "Second verse of the second chapter."),
            (ScrRef("MAT 2:3"), "Third verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
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
        Assert.That(target, Contains.Substring("\\v 1 First verse of the first chapter.\r\n"));
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

        string target = UpdateUsfm(rows);
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
            Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft This is a new endnote.\\fe*\r\n")
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
    public void GetUsfm_Verse_LastVerse()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)> { (ScrRef("MAT 4:1"), "Updating the last verse.") };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(target, Contains.Substring("\\v 1 Updating the last verse.\r\n"));
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
        Assert.That(target, Contains.Substring("\\ip The introductory paragraph.\r\n"));
    }

    private static ScriptureRef[] ScrRef(params string[] refs)
    {
        return refs.Select(r => ScriptureRef.Parse(r)).ToArray();
    }

    private static string UpdateUsfm(
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)>? rows = null,
        string? idText = null,
        bool stripAllText = false,
        bool preferExistingText = false
    )
    {
        string source = ReadUsfm();
        var updater = new UsfmTextUpdater(
            rows,
            idText,
            stripAllText: stripAllText,
            preferExistingText: preferExistingText
        );
        UsfmParser.Parse(source, updater);
        return updater.GetUsfm();
    }

    private static string ReadUsfm()
    {
        return File.ReadAllText(Path.Combine(CorporaTestHelpers.UsfmTestProjectPath, "41MATTes.SFM"));
    }
}
