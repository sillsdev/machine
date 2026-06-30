using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ScrVersExtensionsTests
{
    [Test]
    public void AllIncludedVerses()
    {
        List<VerseRef> originalVerses = ScrVers.Original.AllIncludedVerses().ToList();
        Assert.That(originalVerses, Has.Count.EqualTo(41899));
        Assert.That(originalVerses[21899].BBBCCCVVV, Is.EqualTo(27003024));
        List<VerseRef> englishVerses = ScrVers.English.AllIncludedVerses().ToList();
        Assert.That(englishVerses, Has.Count.EqualTo(38393));
        Assert.That(englishVerses[englishVerses.Count - 1].BBBCCCVVV, Is.EqualTo(123001020));
        List<VerseRef> russianOrthodoxVerses = ScrVers.RussianOrthodox.AllIncludedVerses().ToList();
        Assert.That(russianOrthodoxVerses, Has.Count.EqualTo(37280));
        Assert.That(russianOrthodoxVerses[russianOrthodoxVerses.Count - 1].BBBCCCVVV, Is.EqualTo(83001015));
    }

    [Test]
    public void HasCrossBookMappings()
    {
        Assert.That(!ScrVers.Original.HasCrossBookMappings());
        Assert.That(ScrVers.English.HasCrossBookMappings());
        Assert.That(ScrVers.RussianOrthodox.HasCrossBookMappings());
        Assert.That(!ScrVers.RussianProtestant.HasCrossBookMappings());
        Assert.That(ScrVers.Vulgate.HasCrossBookMappings());
        Assert.That(ScrVers.Vulgate.HasCrossBookMappings(ScrVers.English));
    }
}
