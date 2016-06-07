using NSubstitute;
using SIL.Machine.Morphology;

namespace SIL.Machine.Tests.Translation
{
	internal static class TranslationTestExtensions
	{
		public static void AddAnalyses(this IMorphologicalAnalyzer sourceAnalyzer, string word, params WordAnalysis[] analyses)
		{
			sourceAnalyzer.AnalyzeWord(Arg.Is(word)).Returns(analyses);
		}

		public static void AddGeneratedWords(this IMorphologicalGenerator targetGenerator, WordAnalysis analysis, params string[] words)
		{
			targetGenerator.GenerateWords(Arg.Is(analysis)).Returns(words);
		}
	}
}
