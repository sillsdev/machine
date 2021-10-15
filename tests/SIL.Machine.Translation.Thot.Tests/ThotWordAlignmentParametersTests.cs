using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotWordAlignmentParametersTests
	{
		[Test]
		public void GetFastAlignIterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetFastAlignIterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(4));
			Assert.That(parameters.GetFastAlignIterationCount(ThotWordAlignmentModelType.Ibm1), Is.EqualTo(0));
		}

		[Test]
		public void GetFastAlignIterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				FastAlignIterationCount = 2
			};
			Assert.That(parameters.GetFastAlignIterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(2));
			Assert.That(parameters.GetFastAlignIterationCount(ThotWordAlignmentModelType.Ibm1), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm1IterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.Ibm1), Is.EqualTo(4));
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(5));
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm1IterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				Ibm1IterationCount = 2
			};
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.Ibm1), Is.EqualTo(2));
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(2));
			Assert.That(parameters.GetIbm1IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm2IterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.Ibm2), Is.EqualTo(4));
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(0));
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm2IterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				Ibm2IterationCount = 2
			};
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.Ibm2), Is.EqualTo(2));
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(2));
			Assert.That(parameters.GetIbm2IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetHmmIterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Hmm), Is.EqualTo(4));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(5));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetHmmIterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				HmmIterationCount = 2
			};
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Hmm), Is.EqualTo(2));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(2));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetHmmIterationCount_Ibm2Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				Ibm2IterationCount = 2
			};
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Hmm), Is.EqualTo(4));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(0));
			Assert.That(parameters.GetHmmIterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm3IterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.Ibm3), Is.EqualTo(4));
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(5));
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm3IterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				Ibm3IterationCount = 2
			};
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.Ibm3), Is.EqualTo(2));
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(2));
			Assert.That(parameters.GetIbm3IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm4IterationCount_Default()
		{
			var parameters = new ThotWordAlignmentParameters();
			Assert.That(parameters.GetIbm4IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(4));
			Assert.That(parameters.GetIbm4IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}

		[Test]
		public void GetIbm4IterationCount_Set()
		{
			var parameters = new ThotWordAlignmentParameters
			{
				Ibm4IterationCount = 2
			};
			Assert.That(parameters.GetIbm4IterationCount(ThotWordAlignmentModelType.Ibm4), Is.EqualTo(2));
			Assert.That(parameters.GetIbm4IterationCount(ThotWordAlignmentModelType.FastAlign), Is.EqualTo(0));
		}
	}
}
