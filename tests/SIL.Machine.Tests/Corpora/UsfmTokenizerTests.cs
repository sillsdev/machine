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
        Assert.That(tokens, Has.Count.EqualTo(224));

        Assert.That(tokens[0].Type, Is.EqualTo(UsfmTokenType.Book));
        Assert.That(tokens[0].Marker, Is.EqualTo("id"));
        Assert.That(tokens[0].Data, Is.EqualTo("MAT"));

        Assert.That(tokens[34].Type, Is.EqualTo(UsfmTokenType.Text));
        Assert.That(tokens[34].Text, Is.EqualTo("Chapter One "));

        Assert.That(tokens[35].Type, Is.EqualTo(UsfmTokenType.Verse));
        Assert.That(tokens[35].Marker, Is.EqualTo("v"));
        Assert.That(tokens[35].Data, Is.EqualTo("1"));

        Assert.That(tokens[44].Type, Is.EqualTo(UsfmTokenType.Note));
        Assert.That(tokens[44].Marker, Is.EqualTo("f"));
        Assert.That(tokens[44].Data, Is.EqualTo("+"));
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
