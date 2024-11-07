using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsxMemoryTextTests
{
    [Test]
    public void TestGetRowsDescriptiveTitle()
    {
        IList<TextRow> rows = GetRows(
            """
<usx version="3.0">
<book code="MAT" style="id">- Test</book>
<chapter number="1" style="c" />
<para style="d">
<verse number="1" style="v" sid="MAT 1:1" />Descriptive title</para>
<para style="p">
The rest of verse one.<verse eid="MAT 1:1" />
<verse number="2" style="v" />This is verse two.</para>
</usx>
"""
        );

        Assert.That(rows.Count, Is.EqualTo(2));

        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
        Assert.That(rows[0].Text, Is.EqualTo("Descriptive title"));
    }

    [Test]
    public void TestGetRowsTable()
    {
        IList<TextRow> rows = GetRows(
            """
            <usx version="3.0">
  <book code="MAT" style="id">- Test</book>
  <chapter number="1" style="c" />
  <table>
    <row style="tr">
      <cell style="tc1" align="start"><verse number="1" style="v" />Chapter</cell>
      <cell style="tcr2" align="end">1</cell>
      <cell style="tc3" align="start">verse</cell>
      <cell style="tcr4" align="end">1</cell>
    </row>
    <row style="tr">
      <cell style="tc1" colspan="2" align="start"><verse number="2" style="v" /></cell>
      <cell style="tc3" colspan="2" align="start">Chapter 1 verse 2</cell>
    </row>
  </table>
</usx>
"""
        );

        Assert.That(rows.Count, Is.EqualTo(2));

        Assert.That(rows[0].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:1")));
        Assert.That(rows[0].Text, Is.EqualTo("Chapter 1 verse 1"));

        Assert.That(rows[1].Ref, Is.EqualTo(ScriptureRef.Parse("MAT 1:2")));
        Assert.That(rows[1].Text, Is.EqualTo("Chapter 1 verse 2"));
    }

    private static List<TextRow> GetRows(string usx)
    {
        var text = new UsxMemoryText("MAT", usx);
        return text.GetRows().ToList();
    }
}
