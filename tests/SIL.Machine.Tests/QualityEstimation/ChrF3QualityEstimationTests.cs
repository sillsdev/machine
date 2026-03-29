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
        var qualityEstimation = new ChrF3QualityEstimation(slope: 109.6145, intercept: -14.0633);
        List<(MultiKeyRef Key, double Confidence)> confidences =
        [
            (new MultiKeyRef("MAT.txt", 1), 0.6020749899712906),
            (new MultiKeyRef("MAT.txt", 2), 0.5416165991875662),
            (new MultiKeyRef("MRK.txt", 1), 0.40324150219671917),
        ];
        (List<SequenceUsability> usabilitySequences, List<TxtFileUsability> usabilityTxtFiles) =
            qualityEstimation.EstimateQuality(confidences);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(usabilitySequences, Has.Count.EqualTo(3));
            Assert.That(usabilitySequences[0].Label, Is.EqualTo(UsabilityLabel.Green));
            Assert.That(usabilitySequences[0].ProjectedChrF3, Is.EqualTo(51.93).Within(0.01));
            Assert.That(usabilitySequences[0].Usability, Is.EqualTo(0.765).Within(0.001));
            Assert.That(usabilitySequences[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilitySequences[1].ProjectedChrF3, Is.EqualTo(45.31).Within(0.01));
            Assert.That(usabilitySequences[1].Usability, Is.EqualTo(0.691).Within(0.001));
            Assert.That(usabilitySequences[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilitySequences[2].ProjectedChrF3, Is.EqualTo(30.14).Within(0.01));
            Assert.That(usabilitySequences[2].Usability, Is.EqualTo(0.465).Within(0.001));
            Assert.That(usabilityTxtFiles, Has.Count.EqualTo(2));
            Assert.That(usabilityTxtFiles[0].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilityTxtFiles[0].ProjectedChrF3, Is.EqualTo(48.53).Within(0.01));
            Assert.That(usabilityTxtFiles[0].Usability, Is.EqualTo(0.728).Within(0.001));
            Assert.That(usabilityTxtFiles[1].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityTxtFiles[1].ProjectedChrF3, Is.EqualTo(30.14).Within(0.01));
            Assert.That(usabilityTxtFiles[1].Usability, Is.EqualTo(0.465).Within(0.001));
        }
    }

    [Test]
    public void ChrF3QualityEstimation_Verses()
    {
        var qualityEstimation = new ChrF3QualityEstimation(slope: 109.6145, intercept: -14.0633);
        List<(ScriptureRef key, double confidence)> confidences =
        [
            (new ScriptureRef(new VerseRef(1, 1, 1)), 0.6020749899712906),
            (new ScriptureRef(new VerseRef(1, 1, 2)), 0.5416165991875662),
            (new ScriptureRef(new VerseRef(1, 2, 1)), 0.40324150219671917),
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
            Assert.That(usabilityVerses[0].ProjectedChrF3, Is.EqualTo(51.93).Within(0.01));
            Assert.That(usabilityVerses[0].Usability, Is.EqualTo(0.765).Within(0.001));
            Assert.That(usabilityVerses[1].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilityVerses[1].ProjectedChrF3, Is.EqualTo(45.31).Within(0.01));
            Assert.That(usabilityVerses[1].Usability, Is.EqualTo(0.691).Within(0.001));
            Assert.That(usabilityVerses[2].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityVerses[2].ProjectedChrF3, Is.EqualTo(30.14).Within(0.01));
            Assert.That(usabilityVerses[2].Usability, Is.EqualTo(0.465).Within(0.001));
            Assert.That(usabilityChapters, Has.Count.EqualTo(2));
            Assert.That(usabilityChapters[0].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilityChapters[0].ProjectedChrF3, Is.EqualTo(48.53).Within(0.01));
            Assert.That(usabilityChapters[0].Usability, Is.EqualTo(0.728).Within(0.001));
            Assert.That(usabilityChapters[1].Label, Is.EqualTo(UsabilityLabel.Red));
            Assert.That(usabilityChapters[1].ProjectedChrF3, Is.EqualTo(30.14).Within(0.01));
            Assert.That(usabilityChapters[1].Usability, Is.EqualTo(0.465).Within(0.001));
            Assert.That(usabilityBooks, Has.Count.EqualTo(1));
            Assert.That(usabilityBooks[0].Label, Is.EqualTo(UsabilityLabel.Yellow));
            Assert.That(usabilityBooks[0].ProjectedChrF3, Is.EqualTo(41.68).Within(0.01));
            Assert.That(usabilityBooks[0].Usability, Is.EqualTo(0.640).Within(0.001));
        }
    }
}
