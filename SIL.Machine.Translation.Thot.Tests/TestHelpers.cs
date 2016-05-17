using System;
using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Translation.Thot.Tests
{
	public static class TestHelpers
	{
		public static string ToyCorpusFolderName
		{
			get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "data", "toy_corpus"); }
		}

		public static string ToyCorpusConfigFileName
		{
			get { return Path.Combine(ToyCorpusFolderName, "toy_corpus.cfg"); }
		}

		public static IEnumerable<string> Split(this string segment)
		{
			return segment.Split(' ');
		}
	}
}
