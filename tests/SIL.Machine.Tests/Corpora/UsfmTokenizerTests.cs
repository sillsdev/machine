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
        Assert.That(tokens, Has.Count.EqualTo(151));

        Assert.That(tokens[0].Type, Is.EqualTo(UsfmTokenType.Book));
        Assert.That(tokens[0].Marker, Is.EqualTo("id"));
        Assert.That(tokens[0].Data, Is.EqualTo("MAT"));

        Assert.That(tokens[10].Type, Is.EqualTo(UsfmTokenType.Text));
        Assert.That(tokens[10].Text, Is.EqualTo("Chapter One "));

        Assert.That(tokens[11].Type, Is.EqualTo(UsfmTokenType.Verse));
        Assert.That(tokens[11].Marker, Is.EqualTo("v"));
        Assert.That(tokens[11].Data, Is.EqualTo("1"));

        Assert.That(tokens[20].Type, Is.EqualTo(UsfmTokenType.Note));
        Assert.That(tokens[20].Marker, Is.EqualTo("f"));
        Assert.That(tokens[20].Data, Is.EqualTo("+"));
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

    private static string ReadUsfm()
    {
        return File.ReadAllText(Path.Combine(CorporaTestHelpers.UsfmTestProjectPath, "41MATTes.SFM"));
    }
}
