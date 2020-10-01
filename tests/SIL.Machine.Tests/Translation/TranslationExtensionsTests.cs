using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace SIL.Machine.Translation
{
	[TestFixture]
	public class TranslationExtensionsTests
	{
		[Test]
		public void GetBestAlignment_KnownAlignment()
		{
			var estimatedAlignment = new WordAlignmentMatrix(10, 7)
			{
				[1, 1] = true,
				[2, 1] = true,
				[4, 2] = true,
				[5, 1] = true,
				[6, 3] = true,
				[7, 4] = true,
				[8, 5] = true,
				[9, 6] = true
			};
			var aligner = Substitute.For<IWordAligner>();
			aligner.GetBestAlignment(Arg.Any<IReadOnlyList<string>>(), Arg.Any<IReadOnlyList<string>>())
				.Returns(estimatedAlignment);

			var knownAlignment = new WordAlignmentMatrix(10, 7)
			{
				[0, 0] = true,
				[6, 3] = true,
				[7, 5] = true,
				[8, 4] = true
			};

			WordAlignmentMatrix alignment = aligner.GetBestAlignment(
				"maria no daba una bofetada a la bruja verde .".Split(),
				"mary didn't slap the green witch .".Split(), knownAlignment);
			Assert.That(alignment.ToString(), Is.EqualTo("0-0 1-1 2-1 4-2 6-3 8-4 7-5 9-6"));
		}
	}
}
