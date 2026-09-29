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

        List<VerseRef> originalVersesGenesis = ScrVers.Original.AllIncludedVerses(new() { [1] = null }).ToList();
        Assert.That(originalVersesGenesis, Has.Count.EqualTo(1533));

        List<VerseRef> originalVersesGenesisChapterOne = ScrVers
            .Original.AllIncludedVerses(new() { [1] = [1] })
            .ToList();
        Assert.That(originalVersesGenesisChapterOne, Has.Count.EqualTo(31));
    }

    [Test]
    public void HasCrossBookMappings()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(!ScrVers.Original.HasCrossBookMappings());
            Assert.That(ScrVers.English.HasCrossBookMappings());
            Assert.That(ScrVers.RussianOrthodox.HasCrossBookMappings());
            Assert.That(!ScrVers.RussianProtestant.HasCrossBookMappings());
            Assert.That(ScrVers.Vulgate.HasCrossBookMappings());
            Assert.That(ScrVers.Vulgate.HasCrossBookMappings(ScrVers.English));
        }
    }

    [Test]
    public void IsEquivalent()
    {
        ScrVers customVrs1;
        using (CorporaUtils.VersificationLock.Lock())
        {
            string src = "MAT 1:2 = MAT 1:1\nMAT 1:1 = MAT 1:2\n";
            using var reader = new StringReader(src);
            customVrs1 = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
        }

        ScrVers customVrs2;
        using (CorporaUtils.VersificationLock.Lock())
        {
            string src = "MAT 1:1 = MAT 1:2\nMAT 1:2 = MAT 1:1\n";
            using var reader = new StringReader(src);
            customVrs2 = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
        }

        ScrVers customVrs3;
        using (CorporaUtils.VersificationLock.Lock())
        {
            string src = "MAT 1:1 = MAT 1:2\n# This is a comment\nMAT 1:2 = MAT 1:1\nMAT 1:2 = MAT 1:1\n";
            using var reader = new StringReader(src);
            customVrs3 = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
        }

        ScrVers customVrs4;
        using (CorporaUtils.VersificationLock.Lock())
        {
            string src = "&MAT 1:2-3 = MAT 1:2\nMAT 1:4 = MAT 1:3\n";
            using var reader = new StringReader(src);
            customVrs4 = Versification.Table.Implementation.Load(reader, "vers.txt", ScrVers.English, "custom");
            Versification.Table.Implementation.RemoveAllUnknownVersifications();
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ScrVers.English.IsEquivalentTo(ScrVers.English));
            Assert.That(!ScrVers.English.IsEquivalentTo(ScrVers.Original));
            Assert.That(customVrs1.IsEquivalentTo(customVrs1));
            Assert.That(customVrs1.IsEquivalentTo(customVrs2));
            Assert.That(customVrs1.IsEquivalentTo(customVrs3));
            Assert.That(!customVrs1.IsEquivalentTo(customVrs4));
        }
    }
}
