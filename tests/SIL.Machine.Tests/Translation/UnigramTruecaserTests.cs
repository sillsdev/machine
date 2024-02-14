using NUnit.Framework;

namespace SIL.Machine.Translation;

[TestFixture]
public class UnigramTruecaserTests
{
    [Test]
    public void Truecase_Empty()
    {
        UnigramTruecaser truecaser = CreateTruecaser();
        IReadOnlyList<string> result = truecaser.Truecase(Array.Empty<string>());
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Truecase_CapitializedName()
    {
        UnigramTruecaser truecaser = CreateTruecaser();
        IReadOnlyList<string> result = truecaser.Truecase(new[] { "THE", "ADVENTURES", "OF", "SHERLOCK", "HOLMES" });
        Assert.That(result, Is.EqualTo(new[] { "the", "adventures", "of", "Sherlock", "Holmes" }));
    }

    [Test]
    public void Truecase_UnknownWord()
    {
        UnigramTruecaser truecaser = CreateTruecaser();
        IReadOnlyList<string> result = truecaser.Truecase(new[] { "THE", "EXPLOITS", "OF", "SHERLOCK", "HOLMES" });
        Assert.That(result, Is.EqualTo(new[] { "the", "EXPLOITS", "of", "Sherlock", "Holmes" }));
    }

    [Test]
    public void Truecase_MultipleSentences()
    {
        UnigramTruecaser truecaser = CreateTruecaser();
        IReadOnlyList<string> result = truecaser.Truecase(
            new[] { "SHERLOCK", "HOLMES", "IS", "SMART", ".", "YOU", "AGREE", "." }
        );
        Assert.That(result, Is.EqualTo(new[] { "Sherlock", "Holmes", "is", "smart", ".", "you", "agree", "." }));
    }

    [Test]
    public void Truecase_IgnoreFirstWordDuringTraining()
    {
        UnigramTruecaser truecaser = CreateTruecaser();
        IReadOnlyList<string> result = truecaser.Truecase(new[] { "HE", "IS", "SMART", "." });
        Assert.That(result, Is.EqualTo(new[] { "HE", "is", "smart", "." }));
    }

    private static UnigramTruecaser CreateTruecaser()
    {
        var truecaser = new UnigramTruecaser();
        truecaser.TrainSegment(new[] { "The", "house", "is", "made", "of", "wood", "." });
        truecaser.TrainSegment(new[] { "I", "go", "on", "adventures", "." });
        truecaser.TrainSegment(new[] { "He", "read", "the", "book", "about", "Sherlock", "Holmes", "." });
        truecaser.TrainSegment(new[] { "John", "and", "I", "agree", "that", "you", "are", "smart", "." });
        return truecaser;
    }
}
