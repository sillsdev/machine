using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace SIL.Machine.QualityEstimation;

[TestFixture]
public class ChrF3QualityEstimationTests
{
    [Test]
    public void ChrF3QualityEstimation_TxtFiles()
    {
        var qualityEstimation = new ChrF3QualityEstimation(slope: 0.6, intercept: 1.0);
        List<(MultiKeyRef Key, double Confidence)> confidences =
        [
            (new MultiKeyRef("MAT.txt", 1), 85.0),
            (new MultiKeyRef("MAT.txt", 2), 80.0),
            (new MultiKeyRef("MRK.txt", 1), 60.0),
        ];
        (List<SequenceUsability> usabilitySequences, List<TxtFileUsability> usabilityTxtFiles) =
            qualityEstimation.EstimateQuality(confidences);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(usabilitySequences, Has.Count.EqualTo(3));
            Assert.That(usabilitySequences[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(usabilitySequences[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilitySequences[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityTxtFiles, Has.Count.EqualTo(2));
            Assert.That(usabilityTxtFiles[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(usabilityTxtFiles[1].Label, Is.EqualTo(UsabilityLabel.Red));
        }
    }

    [Test]
    public void ChrF3QualityEstimation_Verses()
    {
        var qualityEstimation = new ChrF3QualityEstimation(slope: 0.6, intercept: 1.0);
        List<(ScriptureRef key, double confidence)> confidences =
        [
            (new ScriptureRef(new VerseRef(1, 1, 1)), 85.0),
            (new ScriptureRef(new VerseRef(1, 1, 2)), 80.0),
            (new ScriptureRef(new VerseRef(1, 2, 1)), 60.0),
        ];
        (
            List<VerseUsability> usabilityVerses,
            List<ChapterUsability> usabilityChapters,
            List<BookUsability> usabilityBooks
        ) = qualityEstimation.EstimateQuality(confidences);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(usabilityVerses, Has.Count.EqualTo(3));
            Assert.That(usabilityVerses[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(usabilityVerses[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilityVerses[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityChapters, Has.Count.EqualTo(2));
            Assert.That(usabilityChapters[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(usabilityChapters[1].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityBooks, Has.Count.EqualTo(1));
            Assert.That(usabilityBooks[0].Label, Is.EqualTo(UsabilityLabel.Yellow));
        }
    }
}
