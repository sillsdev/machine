using System.Linq;

namespace SIL.Machine.Translation
{
	public class TransferEngine
	{
		private readonly ISourceAnalyzer _sourceAnalyzer;
		private readonly IMorphemeMapper _morphemeMapper;
		private readonly ITargetGenerator _targetGenerator;

		public TransferEngine(ISourceAnalyzer sourceAnalyzer, IMorphemeMapper morphemeMapper, ITargetGenerator targetGenerator)
		{
			_sourceAnalyzer = sourceAnalyzer;
			_morphemeMapper = morphemeMapper;
			_targetGenerator = targetGenerator;
		}

		public bool TranslateWord(string sourceWord, out string targetWord)
		{
			WordAnalysis sourceAnalysis = _sourceAnalyzer.AnalyzeWord(sourceWord).FirstOrDefault();
			if (sourceAnalysis == null)
			{
				targetWord = null;
				return false;
			}
			// TODO: transfer rules should be applied here
			WordAnalysis targetAnalysis = new WordAnalysis(sourceAnalysis.Morphemes.Select(m => _morphemeMapper.GetTargetMorpheme(m)), sourceAnalysis.Category, sourceAnalysis.Gloss);
			targetWord = _targetGenerator.GenerateWord(targetAnalysis);
			return true;
		}
	}
}
