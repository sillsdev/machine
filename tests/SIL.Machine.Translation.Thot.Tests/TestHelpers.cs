using System;
using System.Collections.Generic;
using System.IO;

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
	}
}
