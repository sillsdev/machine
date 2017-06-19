using System;
using NUnit.Framework;

namespace SIL.Machine.Statistics
{
	[TestFixture]
	public class WittenBellProbabilityDistributionTests
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
		public void ProbabilityOneUnseen()
		{
			var wb = new WittenBellProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count + 1);
			Assert.That(wb["a"], Is.EqualTo(0.00952).Within(0.00001));
			Assert.That(wb["c"], Is.EqualTo(0.01904).Within(0.00001));
			Assert.That(wb["d"], Is.EqualTo(0.02857).Within(0.00001));
			Assert.That(wb["o"], Is.EqualTo(0.07619).Within(0.00001));
			Assert.That(wb["q"], Is.EqualTo(0.09523).Within(0.00001));

			Assert.That(wb["t"], Is.EqualTo(0.16190).Within(0.00001));
			Assert.That(wb["z"], Is.EqualTo(0.16190).Within(0.00001));
		}

		[Test]
		public void ProbabilityTwoUnseen()
		{
			var wb = new WittenBellProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count + 2);
			Assert.That(wb["a"], Is.EqualTo(0.00952).Within(0.00001));
			Assert.That(wb["c"], Is.EqualTo(0.01904).Within(0.00001));
			Assert.That(wb["d"], Is.EqualTo(0.02857).Within(0.00001));
			Assert.That(wb["o"], Is.EqualTo(0.07619).Within(0.00001));
			Assert.That(wb["q"], Is.EqualTo(0.09523).Within(0.00001));

			Assert.That(wb["t"], Is.EqualTo(0.08095).Within(0.00001));
			Assert.That(wb["z"], Is.EqualTo(0.08095).Within(0.00001));
		}

		[Test]
		public void IncorrectBinCount()
		{
			Assert.That(() =>
				{
					var wb = new WittenBellProbabilityDistribution<string>(_fd, _fd.ObservedSamples.Count);
				}, Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void NoSamples()
		{
			var wb = new WittenBellProbabilityDistribution<string>(new FrequencyDistribution<string>(), 1);
			Assert.That(wb["a"], Is.EqualTo(0));
			Assert.That(wb["b"], Is.EqualTo(0));
			Assert.That(wb["c"], Is.EqualTo(0));
		}
	}
}
