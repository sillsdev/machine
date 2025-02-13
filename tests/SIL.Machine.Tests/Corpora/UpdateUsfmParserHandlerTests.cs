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
        string target = UpdateUsfm(textBehavior: UpdateUsfmTextBehavior.StripExisting);
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
        string target = UpdateUsfm(rows, textBehavior: UpdateUsfmTextBehavior.PreferExisting);
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

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmIntraVerseMarkerBehavior.Strip);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_Verse_StripNotesWithUpdatedVerseText()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:1"), "First verse of the first chapter.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmIntraVerseMarkerBehavior.Strip);
        Assert.That(target, Contains.Substring("\\id MAT - Test\r\n"));
        Assert.That(
            target,
            Contains.Substring("\\ip An introduction to Matthew with an empty comment\\fe + \\ft \\fe*")
        );
        Assert.That(target, Contains.Substring("\\v 1 First verse of the first chapter.\r\n\\li1\r\n\\v 2"));
    }

    [Test]
    public void GetUsfm_Verse_ReplaceNoteKeepReference()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 2:1"), "First verse of the second chapter."),
            (ScrRef("MAT 2:1/1:f"), "This is a new footnote.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 1 First verse of the second chapter. \\f + \\fr 2:1: \\ft This is a new footnote. \\f*\r\n"
            )
        );
    }

    [Test]
    public void GetUsfm_Verse_PreserveFiguresAndReferences()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            // fig
            (ScrRef("MAT 1:5"), "Fifth verse of the first chapter."),
            (ScrRef("MAT 1:5/1:fig"), "figure text not updated"),
            // r
            (ScrRef("MAT 2:0/1:r"), "parallel reference not updated"),
            // rq
            (ScrRef("MAT 2:5/1:rq"), "quote reference not updated"),
            // xo
            (ScrRef("MAT 2:6/3:xo"), "Cross reference not update"),
            // xt
            (ScrRef("MAT 2:6/4:xt"), "cross reference - target reference not updated"),
            // xta
            (ScrRef("MAT 2:6/5:xta"), "cross reference annotation updated"),
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring(
                "\\v 5 Fifth verse of the first chapter.\r\n\\li2 \\fig Figure 1|src=\"image1.png\" size=\"col\" ref=\"1:5\"\\fig*\r\n\\v 6"
            )
        );
        Assert.That(target, Contains.Substring("\\r (Mark 1:2-3; Luke 4:5-6)\r\n"));
        Assert.That(
            target,
            Contains.Substring(
                "\\v 6 Bad verse. \\x - \\xo 2:3-4 \\xt Cool Book 3:24 \\xta The annotation \\x* and more content.\r\n"
            )
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
        Assert.That(
            target,
            Contains.Substring(
                "\\v 1 First verse of the second chapter. \\f + \\fr 2:1: \\ft This is a \\bd footnote.\\bd*\\f*\r\n"
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

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmIntraVerseMarkerBehavior.Strip);
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
    public void GetUsfm_NonVerse_KeepNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmIntraVerseMarkerBehavior.Preserve);
        Assert.That(target, Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft \\fe*\r\n"));
    }

    [Test]
    public void GetUsfm_NonVerse_SkipNote()
    {
        var rows = new List<(IReadOnlyList<ScriptureRef>, string)>
        {
            (ScrRef("MAT 1:0/3:ip"), "The introductory paragraph.")
        };

        string target = UpdateUsfm(rows, embedBehavior: UpdateUsfmIntraVerseMarkerBehavior.Strip);
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
        Assert.That(target, Contains.Substring("\\ip The introductory paragraph. \\fe + \\ft \\fe*\r\n"));
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
        UpdateUsfmIntraVerseMarkerBehavior embedBehavior = UpdateUsfmIntraVerseMarkerBehavior.Preserve,
        UpdateUsfmIntraVerseMarkerBehavior styleBehavior = UpdateUsfmIntraVerseMarkerBehavior.Strip
    )
    {
        if (source is null)
        {
            var updater = new FileParatextProjectTextUpdater(CorporaTestHelpers.UsfmTestProjectPath);
            return updater.UpdateUsfm("MAT", rows, idText, textBehavior, embedBehavior, styleBehavior);
        }
        else
        {
            source = source.Trim().ReplaceLineEndings("\r\n") + "\r\n";
            var updater = new UpdateUsfmParserHandler(rows, idText, textBehavior, embedBehavior, styleBehavior);
            UsfmParser.Parse(source, updater);
            return updater.GetUsfm();
        }
    }
}
