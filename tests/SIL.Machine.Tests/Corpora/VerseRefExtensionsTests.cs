using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class VerseRefExtensionsTests
{
    [Test]
    [TestCase("MAT 1:1", "MAT 1:1")]
    [TestCase("MAT 1:1a", "MAT 1:1")]
    [TestCase("MAT 1:1a-2b,5a", "MAT 1:1,2,5")]
    [TestCase("MAT 1:1a-3b", "MAT 1:1,2,3")]
    public void RemoveSegments(string verseRefStrWithSegments, string verseRefStrWithoutSegments)
    {
        var verseRef = new VerseRef(verseRefStrWithSegments, ScrVers.English);
        var result = verseRef.RemoveSegments();
        Assert.That(result.ToString(), Is.EqualTo(verseRefStrWithoutSegments));
    }

    [Test]
    public void ChangeVersificationWithSegments()
    {
        // English vs. Original
        // NUM 16:36-50 = NUM 17:1-15
        // NUM 17:1-13 = NUM 17:16-28
        // ESG 1:1 = ESG 1:1a
        // ESG 1:2 = ESG 1:1b

        VerseRef verseRef = new VerseRef("NUM 17:1", ScrVers.English);
        VerseRef result = verseRef.ChangeVersificationWithSegments(ScrVers.Original);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.Original));
        Assert.That(result.ToString(), Is.EqualTo("NUM 17:16"));

        verseRef = new VerseRef("NUM 17:1a", ScrVers.English);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.Original);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.Original));
        Assert.That(result.ToString(), Is.EqualTo("NUM 17:16a"));

        verseRef = new VerseRef("NUM 17:1a-2b,5a", ScrVers.English);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.Original);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.Original));
        Assert.That(result.ToString(), Is.EqualTo("NUM 17:16a,17b,20a"));

        verseRef = new VerseRef("NUM 17:13a-15a", ScrVers.Original);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.English);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.English));
        Assert.That(result.ToString(), Is.EqualTo("NUM 16:48a,49,50a"));

        verseRef = new VerseRef("NUM 17:1a", ScrVers.English);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.English);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.English));
        Assert.That(result.ToString(), Is.EqualTo("NUM 17:1a"));

        verseRef = new VerseRef("ESG 1:1b", ScrVers.Original);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.English);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.English));
        Assert.That(result.ToString(), Is.EqualTo("ESG 1:2"));

        verseRef = new VerseRef("ESG 1:2", ScrVers.English);
        result = verseRef.ChangeVersificationWithSegments(ScrVers.Original);
        Assert.That(result.Versification, Is.EqualTo(ScrVers.Original));
        Assert.That(result.ToString(), Is.EqualTo("ESG 1:1b"));
    }
}
