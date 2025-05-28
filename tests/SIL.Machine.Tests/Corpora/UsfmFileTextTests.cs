using System.Text;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmFileTextTests
{
    [Test]
    public void GetRows_NonEmptyText()
    {
        var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

        IText text = corpus["MAT"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(24));

        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1", corpus.Versification)));
        Assert.That(rows[0].Text, Is.EqualTo("Chapter one, verse one."));

        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:2", corpus.Versification)));
        Assert.That(rows[1].Text, Is.EqualTo("Chapter one, verse two."));

        Assert.That(rows[4].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:5", corpus.Versification)));
        Assert.That(rows[4].Text, Is.EqualTo("Chapter one, verse five."));

        Assert.That(rows[8].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:1", corpus.Versification)));
        Assert.That(rows[8].Text, Is.EqualTo("Chapter two, verse one."));

        Assert.That(rows[9].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:2", corpus.Versification)));
        Assert.That(rows[9].Text, Is.EqualTo("Chapter two, verse two. Chapter two, verse three."));
        Assert.That(rows[9].IsInRange, Is.True);
        Assert.That(rows[9].IsRangeStart, Is.True);

        Assert.That(rows[10].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:3", corpus.Versification)));
        Assert.That(rows[10].Text, Is.Empty);
        Assert.That(rows[10].IsInRange, Is.True);
        Assert.That(rows[10].IsRangeStart, Is.False);

        Assert.That(rows[11].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:4a", corpus.Versification)));
        Assert.That(rows[11].Text, Is.Empty);
        Assert.That(rows[11].IsInRange, Is.True);
        Assert.That(rows[11].IsRangeStart, Is.False);

        Assert.That(rows[12].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:4b", corpus.Versification)));
        Assert.That(rows[12].Text, Is.EqualTo("Chapter two, verse four."));

        Assert.That(rows[13].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:5", corpus.Versification)));
        Assert.That(rows[13].Text, Is.EqualTo("Chapter two, verse five."));

        Assert.That(rows[14].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:6", corpus.Versification)));
        Assert.That(rows[14].Text, Is.EqualTo("Chapter two, verse six."));

        Assert.That(rows[18].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:9", corpus.Versification)));
        Assert.That(rows[18].Text, Is.EqualTo("Chapter 2 verse 9"));

        Assert.That(rows[19].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:10", corpus.Versification)));
        Assert.That(rows[19].Text, Is.EqualTo("Chapter 2 verse 10"));

        Assert.That(rows[20].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:11", corpus.Versification)));
        Assert.That(rows[20].Text, Is.Empty);
    }

    [Test]
    public void GetRows_NonEmptyText_AllText()
    {
        var corpus = new UsfmFileTextCorpus(
            "usfm.sty",
            Encoding.UTF8,
            CorporaTestHelpers.UsfmTestProjectPath,
            includeAllText: true
        );

        IText text = corpus["MAT"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(48));

        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/1:h", corpus.Versification)));
        Assert.That(rows[0].Text, Is.EqualTo("Matthew"));

        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/2:mt", corpus.Versification)));
        Assert.That(rows[1].Text, Is.EqualTo("Matthew"));

        Assert.That(rows[2].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/3:ip", corpus.Versification)));
        Assert.That(rows[2].Text, Is.EqualTo("An introduction to Matthew"));

        Assert.That(rows[3].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/4:p", corpus.Versification)));
        Assert.That(rows[3].Text, Is.EqualTo("MAT 1 Here is another paragraph."));

        Assert.That(
            rows[6].Ref,
            Is.EqualTo(ScriptureRef.Parse("MAT 1:0/7:weirdtaglookingthing", corpus.Versification))
        );
        Assert.That(rows[6].Text, Is.EqualTo("that is not an actual tag."));

        Assert.That(rows[7].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/8:s", corpus.Versification)));
        Assert.That(rows[7].Text, Is.EqualTo("Chapter One"));

        Assert.That(rows[16].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/1:tr/1:tc1", corpus.Versification)));
        Assert.That(rows[16].Text, Is.EqualTo("Row one, column one."));

        Assert.That(rows[17].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/1:tr/2:tc2", corpus.Versification)));
        Assert.That(rows[17].Text, Is.EqualTo("Row one, column two."));

        Assert.That(rows[18].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/2:tr/1:tc1", corpus.Versification)));
        Assert.That(rows[18].Text, Is.EqualTo("Row two, column one."));

        Assert.That(rows[19].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/2:tr/2:tc2", corpus.Versification)));
        Assert.That(rows[19].Text, Is.EqualTo("Row two, column two."));

        Assert.That(rows[20].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/3:s1", corpus.Versification)));
        Assert.That(rows[20].Text, Is.EqualTo("Chapter Two"));

        Assert.That(rows[21].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/4:p", corpus.Versification)));
        Assert.That(rows[21].Text, Is.Empty);

        Assert.That(rows[26].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:3/1:esb/1:ms", corpus.Versification)));
        Assert.That(rows[26].Text, Is.EqualTo("This is a sidebar"));

        Assert.That(rows[27].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:3/1:esb/2:p", corpus.Versification)));
        Assert.That(rows[27].Text, Is.EqualTo("Here is some sidebar content."));

        Assert.That(rows[33].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:7a/1:s", corpus.Versification)));
        Assert.That(rows[33].Text, Is.EqualTo("Section header"));

        Assert.That(rows[40].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:12/1:restore", corpus.Versification)));
        Assert.That(rows[40].Text, Is.EqualTo("restore information"));
    }

    [Test]
    public void GetRows_SentenceStart()
    {
        var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

        IText text = corpus["MAT"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(24));

        Assert.That(rows[3].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:4", corpus.Versification)));
        Assert.That(rows[3].Text, Is.EqualTo("Chapter one with odd whitespace, verse four,"));
        Assert.That(rows[3].IsSentenceStart, Is.True);

        Assert.That(rows[4].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:5", corpus.Versification)));
        Assert.That(rows[4].Text, Is.EqualTo("Chapter one, verse five."));
        Assert.That(rows[4].IsSentenceStart, Is.False);
    }

    [Test]
    public void GetRows_EmptyText()
    {
        var corpus = new UsfmFileTextCorpus("usfm.sty", Encoding.UTF8, CorporaTestHelpers.UsfmTestProjectPath);

        IText text = corpus["MRK"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void GetRows_IncludeMarkers()
    {
        var corpus = new UsfmFileTextCorpus(
            "usfm.sty",
            Encoding.UTF8,
            CorporaTestHelpers.UsfmTestProjectPath,
            includeMarkers: true
        );

        IText text = corpus["MAT"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(24));

        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1", corpus.Versification)));
        Assert.That(
            rows[0].Text,
            Is.EqualTo(
                "Chapter \\pn one\\+pro WON\\+pro*\\pn*, verse one.\\f + \\fr 1:1: \\ft This is a footnote for v1.\\f*"
            )
        );

        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:2", corpus.Versification)));
        Assert.That(
            rows[1].Text,
            Is.EqualTo("\\bd C\\bd*hapter one, \\li2 verse\\f + \\fr 1:2: \\ft This is a footnote for v2.\\f* two.")
        );

        Assert.That(rows[4].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:5", corpus.Versification)));
        Assert.That(
            rows[4].Text,
            Is.EqualTo(
                "Chapter one, \\li2 verse \\fig Figure 1|src=\"image1.png\" size=\"col\" ref=\"1:5\"\\fig* five."
            )
        );

        Assert.That(rows[8].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:1", corpus.Versification)));
        Assert.That(
            rows[8].Text,
            Is.EqualTo("Chapter \\add two\\add*, verse \\f + \\fr 2:1: \\ft This is a footnote.\\f*one.")
        );

        Assert.That(rows[9].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:2", corpus.Versification)));
        Assert.That(
            rows[9].Text,
            Is.EqualTo("Chapter two, // verse \\fm ∆\\fm*two. Chapter two, verse \\w three|lemma\\w*.")
        );
        Assert.That(rows[9].IsInRange, Is.True);
        Assert.That(rows[9].IsRangeStart, Is.True);

        Assert.That(rows[10].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:3", corpus.Versification)));
        Assert.That(rows[10].Text, Is.Empty);
        Assert.That(rows[10].IsInRange, Is.True);
        Assert.That(rows[10].IsRangeStart, Is.False);

        Assert.That(rows[11].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:4a", corpus.Versification)));
        Assert.That(rows[11].Text, Is.Empty);
        Assert.That(rows[11].IsInRange, Is.True);
        Assert.That(rows[11].IsRangeStart, Is.False);

        Assert.That(rows[12].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:4b", corpus.Versification)));
        Assert.That(rows[12].Text, Is.EqualTo("Chapter two, verse four."));

        Assert.That(rows[13].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:5", corpus.Versification)));
        Assert.That(rows[13].Text, Is.EqualTo("Chapter two, verse five \\rq (MAT 3:1)\\rq*."));

        Assert.That(rows[14].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:6", corpus.Versification)));
        Assert.That(rows[14].Text, Is.EqualTo("Chapter two, verse \\w six|strong=\"12345\" \\w*."));

        Assert.That(rows[18].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:9", corpus.Versification)));
        Assert.That(rows[18].Text, Is.EqualTo("Chapter\\tcr2 2\\tc3 verse\\tcr4 9"));

        Assert.That(rows[19].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:10", corpus.Versification)));
        Assert.That(rows[19].Text, Is.EqualTo("\\tc3-4 Chapter 2 verse 10"));
    }

    [Test]
    public void GetRows_IncludeMarkers_AllText()
    {
        var corpus = new UsfmFileTextCorpus(
            "usfm.sty",
            Encoding.UTF8,
            CorporaTestHelpers.UsfmTestProjectPath,
            includeMarkers: true,
            includeAllText: true
        );

        IText text = corpus["MAT"];
        TextRow[] rows = text.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(48));

        Assert.That(rows[2].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:0/3:ip", corpus.Versification)));
        Assert.That(rows[2].Text, Is.EqualTo("An introduction to Matthew\\fe + \\ft This is an endnote.\\fe*"));

        Assert.That(rows[8].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1", corpus.Versification)));
        Assert.That(
            rows[8].Text,
            Is.EqualTo(
                "Chapter \\pn one\\+pro WON\\+pro*\\pn*, verse one.\\f + \\fr 1:1: \\ft This is a footnote for v1.\\f*"
            )
        );

        Assert.That(rows[9].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:2", corpus.Versification)));
        Assert.That(
            rows[9].Text,
            Is.EqualTo("\\bd C\\bd*hapter one, \\li2 verse\\f + \\fr 1:2: \\ft This is a footnote for v2.\\f* two.")
        );

        Assert.That(rows[12].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:5", corpus.Versification)));
        Assert.That(
            rows[12].Text,
            Is.EqualTo(
                "Chapter one, \\li2 verse \\fig Figure 1|src=\"image1.png\" size=\"col\" ref=\"1:5\"\\fig* five."
            )
        );

        Assert.That(rows[20].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:0/3:s1", corpus.Versification)));
        Assert.That(rows[20].Text, Is.EqualTo("Chapter \\it Two \\it*"));

        Assert.That(rows[23].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:1", corpus.Versification)));
        Assert.That(
            rows[23].Text,
            Is.EqualTo("Chapter \\add two\\add*, verse \\f + \\fr 2:1: \\ft This is a footnote.\\f*one.")
        );

        Assert.That(rows[27].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 2:3/1:esb/2:p", corpus.Versification)));
        Assert.That(rows[27].Text, Is.EqualTo("Here is some sidebar // content."));
    }
}
