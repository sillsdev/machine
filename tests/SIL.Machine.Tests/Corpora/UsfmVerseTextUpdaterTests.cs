using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmVerseTextUpdaterTests
{
    [Test]
    public void GetUsfm_CharStyle()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 1:1", ScrVers.English) }, "First verse of the first chapter.")
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
    }

    [Test]
    public void GetUsfm_Notes()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:1", ScrVers.English) }, "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_RowVerseSegment()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:1a", ScrVers.English) }, "First verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 1 First verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_UsfmVerseSegment()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:7", ScrVers.English) }, "Seventh verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 7a Seventh verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_MultipleParas()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 1:2", ScrVers.English) }, "Second verse of the first chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 2 Second verse of the first chapter.\r\n\\li2\r\n"));
    }

    [Test]
    public void GetUsfm_Table()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:9", ScrVers.English) }, "Ninth verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 9 Ninth verse of the second chapter. \\tcr2 \\tc3 \\tcr4\r\n"));
    }

    [Test]
    public void GetUsfm_RangeSingleRowMultipleVerses()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (
                new[] { new VerseRef("MAT 2:11", ScrVers.English), new VerseRef("MAT 2:12", ScrVers.English) },
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
    public void GetUsfm_RangeSingleRowSingleVerse()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:11", ScrVers.English) }, "Eleventh verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 11-12 Eleventh verse of the second chapter.\r\n"));
    }

    [Test]
    public void GetUsfm_RangeMultipleRowsSingleVerse()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:11", ScrVers.English) }, "Eleventh verse of the second chapter."),
            (new[] { new VerseRef("MAT 2:12", ScrVers.English) }, "Twelfth verse of the second chapter.")
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
    public void GetUsfm_OptBreak()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:2", ScrVers.English) }, "Second verse of the second chapter."),
            (new[] { new VerseRef("MAT 2:3", ScrVers.English) }, "Third verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\v 2-3 Second verse of the second chapter. Third verse of the second chapter.\r\n")
        );
    }

    [Test]
    public void GetUsfm_Milestone()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 2:10", ScrVers.English) }, "Tenth verse of the second chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(
            target,
            Contains.Substring("\\v 10 Tenth verse of the second chapter. \\tc3-4 \\qt-s |Jesus\\*\\qt-e\\*\r\n")
        );
    }

    [Test]
    public void GetUsfm_Unmatched()
    {
        var rows = new List<(IReadOnlyList<VerseRef>, string)>
        {
            (new[] { new VerseRef("MAT 1:3", ScrVers.English) }, "Third verse of the first chapter.")
        };

        string target = UpdateUsfm(rows);
        Assert.That(target, Contains.Substring("\\v 3 Third verse of the first chapter.\r\n"));
    }

    private static string UpdateUsfm(
        IReadOnlyList<(IReadOnlyList<VerseRef>, string)>? rows = null,
        string? idText = null,
        bool stripAllText = false
    )
    {
        string source = ReadUsfm();
        var updater = new UsfmVerseTextUpdater(rows, idText, stripAllText);
        UsfmParser.Parse(source, updater);
        return updater.GetUsfm();
    }

    private static string ReadUsfm()
    {
        return File.ReadAllText(Path.Combine(CorporaTestHelpers.UsfmTestProjectPath, "41MATTes.SFM"));
    }
}
