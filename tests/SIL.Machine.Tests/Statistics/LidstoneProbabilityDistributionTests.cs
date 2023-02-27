using NUnit.Framework;

namespace SIL.Machine.Statistics
{
    [TestFixture]
    public class LidstoneProbabilityDistributionTests
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
            var ls = new LidstoneProbabilityDistribution<string>(_fd, 1, _fd.ObservedSamples.Count + 1);
            Assert.That(ls["a"], Is.EqualTo(0.01886).Within(0.00001));
            Assert.That(ls["c"], Is.EqualTo(0.02830).Within(0.00001));
            Assert.That(ls["d"], Is.EqualTo(0.03773).Within(0.00001));
            Assert.That(ls["o"], Is.EqualTo(0.08490).Within(0.00001));
            Assert.That(ls["q"], Is.EqualTo(0.10377).Within(0.00001));

            Assert.That(ls["t"], Is.EqualTo(0.00943).Within(0.00001));
            Assert.That(ls["z"], Is.EqualTo(0.00943).Within(0.00001));
        }

        [Test]
        public void ProbabilityTwoUnseen()
        {
            var ls = new LidstoneProbabilityDistribution<string>(_fd, 1, _fd.ObservedSamples.Count + 2);
            Assert.That(ls["a"], Is.EqualTo(0.01869).Within(0.00001));
            Assert.That(ls["c"], Is.EqualTo(0.02803).Within(0.00001));
            Assert.That(ls["d"], Is.EqualTo(0.03738).Within(0.00001));
            Assert.That(ls["o"], Is.EqualTo(0.08411).Within(0.00001));
            Assert.That(ls["q"], Is.EqualTo(0.10280).Within(0.00001));

            Assert.That(ls["t"], Is.EqualTo(0.00934).Within(0.00001));
            Assert.That(ls["z"], Is.EqualTo(0.00934).Within(0.00001));
        }

        [Test]
        public void IncorrectBinCount()
        {
            Assert.That(
                () =>
                {
                    var ls = new LidstoneProbabilityDistribution<string>(_fd, 1, _fd.ObservedSamples.Count);
                },
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void NoSamples()
        {
            var ls = new LidstoneProbabilityDistribution<string>(new FrequencyDistribution<string>(), 1, 1);
            Assert.That(ls["a"], Is.EqualTo(0));
            Assert.That(ls["b"], Is.EqualTo(0));
            Assert.That(ls["c"], Is.EqualTo(0));
            Assert.That(ls.Discount, Is.EqualTo(0));
        }
    }
}
