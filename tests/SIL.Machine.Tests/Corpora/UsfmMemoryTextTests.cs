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

            Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
            Assert.That(rows[0].Text, Is.EqualTo("Descriptive title"));
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

            Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
            Assert.That(rows[0].Text, Is.EqualTo("Last segment"));
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
