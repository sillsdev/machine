using NUnit.Framework;

namespace SIL.Machine.QualityEstimation;

[TestFixture]
public class BookConfidenceTests
{
    [Test]
    public void IsBookConfidenceUnusuallyLow_AtThreshold()
    {
        Assert.That(BookConfidence.IsBookConfidenceUnusuallyLow(BookConfidence.LowBookConfidenceThreshold), Is.False);
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_MinConfidence()
    {
        Assert.That(BookConfidence.IsBookConfidenceUnusuallyLow(0.0), Is.True);
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_MaxConfidence()
    {
        Assert.That(BookConfidence.IsBookConfidenceUnusuallyLow(1.0), Is.False);
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_WithBookIdAndModel()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                BookConfidence.IsBookConfidenceUnusuallyLow(0.3, "MAT", "facebook/nllb-200-distilled-1.3B"),
                Is.True
            );
            Assert.That(
                BookConfidence.IsBookConfidenceUnusuallyLow(0.9, "MAT", "facebook/nllb-200-distilled-1.3B"),
                Is.False
            );
        }
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_Negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BookConfidence.IsBookConfidenceUnusuallyLow(-0.5));
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_GreaterThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BookConfidence.IsBookConfidenceUnusuallyLow(1.5));
    }

    [Test]
    public void IsBookConfidenceUnusuallyLow_Nan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BookConfidence.IsBookConfidenceUnusuallyLow(double.NaN));
    }
}
