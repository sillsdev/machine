using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class CorporaExtensionsTests
{
    [Test]
    public void ExtractScripture()
    {
        var corpus = new ParatextTextCorpus(CorporaTestHelpers.UsfmTestProjectPath);

        var lines = corpus.ExtractScripture().ToList();
        Assert.That(lines.Count, Is.EqualTo(41899));

        (string text, VerseRef origRef, VerseRef? corpusRef) = lines[0];

        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("GEN 1:1", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("GEN 1:1", corpus.Versification)));

        (text, origRef, corpusRef) = lines[3167];
        Assert.That(text, Is.EqualTo("Chapter fourteen, verse fifty-five. Segment b."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("LEV 14:56", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("LEV 14:55", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10726];
        Assert.That(text, Is.EqualTo("Chapter twelve, verses three through seven."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:3", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:3", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10727];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:4", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:4", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10731];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:8", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:7", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10732];
        Assert.That(text, Is.EqualTo("Chapter twelve, verse eight."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:9", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:8", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23213];
        Assert.That(text, Is.EqualTo("Chapter one, verse one."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 1:1", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23240];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:3", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23248];
        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:11", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:11", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23249];
        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:12", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:12", corpus.Versification)));
    }
}
