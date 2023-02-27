using NUnit.Framework;

namespace SIL.Machine.Statistics
{
    [TestFixture]
    public class SimpleGoodTuringProbabilityDistributionTests
    {
        private FrequencyDistribution<string> _fd = default!;

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
        public void ProbabilityOneUnseen()
        {
            var sgt = new SimpleGoodTuringProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count + 1);
            Assert.That(sgt["a"], Is.EqualTo(0.01765).Within(0.00001));
            Assert.That(sgt["c"], Is.EqualTo(0.02728).Within(0.00001));
            Assert.That(sgt["d"], Is.EqualTo(0.03682).Within(0.00001));
            Assert.That(sgt["o"], Is.EqualTo(0.08433).Within(0.00001));
            Assert.That(sgt["q"], Is.EqualTo(0.1033).Within(0.0001));

            Assert.That(sgt["t"], Is.EqualTo(0.02273).Within(0.00001));
            Assert.That(sgt["z"], Is.EqualTo(0.02273).Within(0.00001));
        }

        [Test]
        public void ProbabilityTwoUnseen()
        {
            var sgt = new SimpleGoodTuringProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count + 2);
            Assert.That(sgt["a"], Is.EqualTo(0.01765).Within(0.00001));
            Assert.That(sgt["c"], Is.EqualTo(0.02728).Within(0.00001));
            Assert.That(sgt["d"], Is.EqualTo(0.03682).Within(0.00001));
            Assert.That(sgt["o"], Is.EqualTo(0.08433).Within(0.00001));
            Assert.That(sgt["q"], Is.EqualTo(0.1033).Within(0.0001));

            Assert.That(sgt["t"], Is.EqualTo(0.01136).Within(0.00001));
            Assert.That(sgt["z"], Is.EqualTo(0.01136).Within(0.00001));
        }

        [Test]
        public void IncorrectBinCount()
        {
            Assert.That(
                () =>
                {
                    var sgt = new SimpleGoodTuringProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count);
                },
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void NoSamples()
        {
            var sgt = new SimpleGoodTuringProbabilityDistribution<string>(new FrequencyDistribution<string>(), 1);
            Assert.That(sgt["a"], Is.EqualTo(0));
            Assert.That(sgt["b"], Is.EqualTo(0));
            Assert.That(sgt["c"], Is.EqualTo(0));
            Assert.That(sgt.Discount, Is.EqualTo(0));
        }
    }
}
