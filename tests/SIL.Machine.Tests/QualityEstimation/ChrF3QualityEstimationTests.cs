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
        qualityEstimation.EstimateQuality(confidences);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(qualityEstimation.UsabilitySequences, Has.Count.EqualTo(3));
            Assert.That(qualityEstimation.UsabilitySequences[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(qualityEstimation.UsabilitySequences[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(qualityEstimation.UsabilitySequences[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(qualityEstimation.UsabilityTxtFiles, Has.Count.EqualTo(2));
            Assert.That(qualityEstimation.UsabilityTxtFiles[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(qualityEstimation.UsabilityTxtFiles[1].Label, Is.EqualTo(UsabilityLabel.Red));
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
        qualityEstimation.EstimateQuality(confidences);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(qualityEstimation.UsabilityVerses, Has.Count.EqualTo(3));
            Assert.That(qualityEstimation.UsabilityVerses[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(qualityEstimation.UsabilityVerses[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(qualityEstimation.UsabilityVerses[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(qualityEstimation.UsabilityChapters, Has.Count.EqualTo(2));
            Assert.That(qualityEstimation.UsabilityChapters[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(qualityEstimation.UsabilityChapters[1].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(qualityEstimation.UsabilityBooks, Has.Count.EqualTo(1));
            Assert.That(qualityEstimation.UsabilityBooks[0].Label, Is.EqualTo(UsabilityLabel.Yellow));
        }
    }
}
