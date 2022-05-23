using NUnit.Framework;

namespace SIL.Machine.Statistics
{
    [TestFixture]
    public class MaxLikelihoodProbabilityDistributionTests
    {
        private FrequencyDistribution<string> _fd;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _fd = new FrequencyDistribution<string>();
            _fd.Increment("a", 1);
            _fd.Increment("b", 1);
            _fd.Increment("c", 2);
            _fd.Increment("d", 3);
            _fd.Increment("e", 4);
            _fd.Increment("f", 4);
            _fd.Increment("g", 4);
            _fd.Increment("h", 5);
            _fd.Increment("i", 5);
            _fd.Increment("j", 6);
            _fd.Increment("k", 6);
            _fd.Increment("l", 6);
            _fd.Increment("m", 7);
            _fd.Increment("n", 7);
            _fd.Increment("o", 8);
            _fd.Increment("p", 9);
            _fd.Increment("q", 10);
        }

        [Test]
        public void Probability()
        {
            var ml = new MaxLikelihoodProbabilityDistribution<string>(_fd);
            Assert.That(ml["a"], Is.EqualTo(0.01136).Within(0.00001));
            Assert.That(ml["c"], Is.EqualTo(0.02272).Within(0.00001));
            Assert.That(ml["d"], Is.EqualTo(0.03409).Within(0.00001));
            Assert.That(ml["o"], Is.EqualTo(0.09090).Within(0.00001));
            Assert.That(ml["q"], Is.EqualTo(0.11363).Within(0.00001));

            Assert.That(ml["t"], Is.EqualTo(0));
            Assert.That(ml["z"], Is.EqualTo(0));
        }

        [Test]
        public void NoSamples()
        {
            var ml = new MaxLikelihoodProbabilityDistribution<string>(new FrequencyDistribution<string>());
            Assert.That(ml["a"], Is.EqualTo(0));
            Assert.That(ml["b"], Is.EqualTo(0));
            Assert.That(ml["c"], Is.EqualTo(0));
        }
    }
}
