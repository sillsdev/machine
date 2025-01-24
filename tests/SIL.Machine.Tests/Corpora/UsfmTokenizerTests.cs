using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmTokenizerTests
{
    [Test]
    public void Tokenize()
    {
        string usfm = ReadUsfm();
        var tokenizer = new UsfmTokenizer();
        IReadOnlyList<UsfmToken> tokens = tokenizer.Tokenize(usfm);
        Assert.That(tokens, Has.Count.EqualTo(236));

        Assert.That(tokens[0].Type, Is.EqualTo(UsfmTokenType.Book));
        Assert.That(tokens[0].Marker, Is.EqualTo("id"));
        Assert.That(tokens[0].Data, Is.EqualTo("MAT"));
        Assert.That(tokens[0].LineNumber, Is.EqualTo(1));
        Assert.That(tokens[0].ColumnNumber, Is.EqualTo(1));

        Assert.That(tokens[30].Type, Is.EqualTo(UsfmTokenType.Text));
        Assert.That(tokens[30].Text, Is.EqualTo("Chapter One "));
        Assert.That(tokens[30].LineNumber, Is.EqualTo(9));
        Assert.That(tokens[30].ColumnNumber, Is.EqualTo(4));

        Assert.That(tokens[31].Type, Is.EqualTo(UsfmTokenType.Verse));
        Assert.That(tokens[31].Marker, Is.EqualTo("v"));
        Assert.That(tokens[31].Data, Is.EqualTo("1"));
        Assert.That(tokens[31].LineNumber, Is.EqualTo(10));
        Assert.That(tokens[31].ColumnNumber, Is.EqualTo(1));

        Assert.That(tokens[40].Type, Is.EqualTo(UsfmTokenType.Note));
        Assert.That(tokens[40].Marker, Is.EqualTo("f"));
        Assert.That(tokens[40].Data, Is.EqualTo("+"));
        Assert.That(tokens[40].LineNumber, Is.EqualTo(10));
        Assert.That(tokens[40].ColumnNumber, Is.EqualTo(48));
    }

    [Test]
    public void Detokenize()
    {
        string usfm = ReadUsfm();
        var tokenizer = new UsfmTokenizer();
        IReadOnlyList<UsfmToken> tokens = tokenizer.Tokenize(usfm);
        string result = tokenizer.Detokenize(tokens);
        Assert.That(result, Is.EqualTo(usfm));
    }

    [Test]
    public void Tokenize_Ending_ParagraphMarker()
    {
        //The ending paragraph marker should not crash the parser.
        string usfm =
            @"\id MAT - Test
\c 1
\v 1 Descriptive title\x - \xo 18:16 \xt  hello world\x*\p
";
        IReadOnlyList<UsfmToken> tokens = new UsfmTokenizer().Tokenize(usfm);
        Assert.That(tokens, Has.Count.EqualTo(13));
    }

    private static string ReadUsfm()
    {
        return File.ReadAllText(Path.Combine(CorporaTestHelpers.UsfmTestProjectPath, "41MATTes.SFM"));
    }
}
