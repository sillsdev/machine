using System;
using System.Collections.Generic;
using System.IO;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
	public static class TestHelpers
	{
		public static string ToyCorpusHmmFolderName => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data",
			"toy_corpus_hmm");
		public static string ToyCorpusHmmConfigFileName => Path.Combine(ToyCorpusHmmFolderName, "smt.cfg");

		public static string ToyCorpusFastAlignFolderName => Path.Combine(AppContext.BaseDirectory, "..", "..", "..",
			"data", "toy_corpus_fa");
		public static string ToyCorpusFastAlignConfigFileName => Path.Combine(ToyCorpusFastAlignFolderName, "smt.cfg");

		public static IEnumerable<string> Split(this string segment)
		{
			return segment.Split(' ');
		}

		public static ParallelTextCorpus CreateTestParallelCorpus()
		{
			var srcCorpus = new DictionaryTextCorpus(
				new MemoryText("text1", new[]
				{
					Segment(1, "isthay isyay ayay esttay-N ."),
					Segment(2, "ouyay ouldshay esttay-V oftenyay ."),
					Segment(3, "isyay isthay orkingway ?"),
					Segment(4, "isthay ouldshay orkway-V ."),
					Segment(5, "ityay isyay orkingway ."),
					Segment(6, "orkway-N ancay ebay ardhay !"),
					Segment(7, "ayay esttay-N ancay ebay ardhay ."),
					Segment(8, "isthay isyay ayay ordway !")
				}));

			var trgCorpus = new DictionaryTextCorpus(
				new MemoryText("text1", new[]
				{
					Segment(1, "this is a test N ."),
					Segment(2, "you should test V often ."),
					Segment(3, "is this working ?"),
					Segment(4, "this should work V ."),
					Segment(5, "it is working ."),
					Segment(6, "work N can be hard !"),
					Segment(7, "a test N can be hard ."),
					Segment(8, "this is a word !")
				}));

			return new ParallelTextCorpus(srcCorpus, trgCorpus);
		}

		private static TextRow Segment(int segRef, string segment)
		{
			return new TextRow(new RowRef(segRef))
			{
				Segment = segment.Split(),
				IsEmpty = false
			};
		}
	}
}
