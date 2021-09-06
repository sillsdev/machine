using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.Machine.Translation.Thot
{
	[TestFixture]
	public class ThotIbm4WordAlignmentModelTests
	{
		[Test]
		public void CreateTrainer()
		{
			using (var model = new ThotIbm4WordAlignmentModel
			{
				Parameters = new ThotWordAlignmentModelParameters
				{
					Ibm1IterationCount = 2,
					HmmIterationCount = 2,
					Ibm3IterationCount = 2,
					Ibm4IterationCount = 2,
					HmmP0 = 0.1,

					SourceWordClasses = new Dictionary<string, string>
					{
						// pronouns
						{ "isthay", "1" }, { "ouyay", "1" }, { "ityay", "1" },
						// verbs
						{ "isyay", "2" }, { "ouldshay", "2" }, { "orkway-V", "2" }, { "ancay", "2" }, { "ebay", "2" },
						{ "esttay-V", "2" },
						// articles
						{ "ayay", "3" },
						// nouns
						{ "esttay-N", "4" }, { "orkway-N", "4" }, { "ordway", "4" },
						// punctuation
						{ ".", "5" }, {"?", "5" }, { "!", "5" },
						// adverbs
						{ "oftenyay", "6" },
						// adjectives
						{ "ardhay", "7" }, { "orkingway", "7" }
					},

					TargetWordClasses = new Dictionary<string, string>
					{
						// pronouns
						{ "this", "1" }, { "you", "1" }, { "it", "1" },
						// verbs
						{ "is", "2" }, { "should", "2" }, { "can", "2" }, { "be", "2" },
						// articles
						{ "a", "3" },
						// nouns
						{ "word", "4" },
						// punctuation
						{ ".", "5" }, {"?", "5" }, { "!", "5" },
						// adverbs
						{ "often", "6" },
						// adjectives
						{ "hard", "7" }, { "working", "7" },
						// nouns/verbs
						{ "test", "8" }, { "work", "8" },
						// disambiguators
						{ "N", "9" }, { "V", "9" }
					}
				}
			})
			{

				ITrainer trainer = model.CreateTrainer(TestHelpers.CreateTestParallelCorpus());
				trainer.Train();
				trainer.Save();

				WordAlignmentMatrix matrix = model.GetBestAlignment("isthay isyay ayay esttay-N .".Split(),
					"this is a test N .".Split());
				var expected = new WordAlignmentMatrix(5, 6,
					new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (3, 3), (3, 4), (4, 5) });
				Assert.That(matrix.ValueEquals(expected), Is.True);

				matrix = model.GetBestAlignment("isthay isyay otnay ayay esttay-N .".Split(),
					"this is not a test N .".Split());
				expected = new WordAlignmentMatrix(6, 7,
					new HashSet<(int, int)> { (0, 0), (1, 1), (3, 3), (4, 4), (4, 5), (5, 6) });
				Assert.That(matrix.ValueEquals(expected), Is.True);

				matrix = model.GetBestAlignment("isthay isyay ayay esttay-N ardhay .".Split(),
					"this is a hard test N .".Split());
				expected = new WordAlignmentMatrix(6, 7,
					new HashSet<(int, int)> { (0, 0), (1, 1), (2, 2), (4, 3), (3, 4), (3, 5), (5, 6) });
				Assert.That(matrix.ValueEquals(expected), Is.True);
			}
		}
	}
}
