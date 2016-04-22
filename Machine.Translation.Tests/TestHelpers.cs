using System;
using System.Collections.Generic;
using System.IO;
using NSubstitute;

namespace SIL.Machine.Translation.Tests
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

		public static void AddAnalyses(this ISourceAnalyzer sourceAnalyzer, string word, params WordAnalysis[] analyses)
		{
			sourceAnalyzer.AnalyzeWord(Arg.Is(word)).Returns(analyses);
		}

		public static void AddGeneratedWords(this ITargetGenerator targetGenerator, WordAnalysis analysis, params string[] words)
		{
			targetGenerator.GenerateWords(Arg.Is(analysis)).Returns(words);
		}

		public static IEnumerable<string> Split(this string segment)
		{
			return segment.Split(' ');
		}
	}
}
