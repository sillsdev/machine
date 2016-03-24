using System.Collections.Generic;
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
			targetWord = null;
			foreach (WordAnalysis sourceAnalysis in _sourceAnalyzer.AnalyzeWord(sourceWord))
			{
				var targetMorphemes = new List<MorphemeInfo>();
				foreach (MorphemeInfo sourceMorpheme in sourceAnalysis.Morphemes)
				{
					MorphemeInfo targetMorpheme;
					if (!_morphemeMapper.TryGetTargetMorpheme(sourceMorpheme, out targetMorpheme))
						return false;

					targetMorphemes.Add(targetMorpheme);
				}
				var targetAnalysis = new WordAnalysis(targetMorphemes, sourceAnalysis.Category);
				targetWord = _targetGenerator.GenerateWords(targetAnalysis).FirstOrDefault();
				return targetWord != null;
			}
			return false;
		}
	}
}
