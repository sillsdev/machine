using System;
using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Translation.Thot
{
	public static class TestHelpers
	{
		public static string ToyCorpusFolderName => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "toy_corpus");

		public static string ToyCorpusConfigFileName => Path.Combine(ToyCorpusFolderName, "toy_corpus.cfg");

		public static IEnumerable<string> Split(this string segment)
		{
			return segment.Split(' ');
		}
	}
}
