using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Morphology;

namespace SIL.Machine.Translation
{
	public class SimpleTransferer : ITransferer
	{
		private readonly IMorphemeMapper _morphemeMapper;

		public SimpleTransferer(IMorphemeMapper morphemeMapper)
		{
			_morphemeMapper = morphemeMapper;
		}

		public IEnumerable<TransferResult> Transfer(IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses)
		{
			var targetAnalyses = new List<WordAnalysis>();
			foreach (IEnumerable<WordAnalysis> sourceAnalysisOptions in sourceAnalyses)
			{
				bool found = false;
				foreach (WordAnalysis sourceAnalysisOption in sourceAnalysisOptions)
				{
					var targetMorphemes = new List<IMorpheme>();
					foreach (IMorpheme sourceMorpheme in sourceAnalysisOption.Morphemes)
					{
						IMorpheme targetMorpheme;
						if (!_morphemeMapper.TryGetTargetMorpheme(sourceMorpheme, out targetMorpheme))
							break;

						targetMorphemes.Add(targetMorpheme);
					}
					if (targetMorphemes.Count == sourceAnalysisOption.Morphemes.Count)
					{
						targetAnalyses.Add(new WordAnalysis(targetMorphemes, sourceAnalysisOption.RootMorphemeIndex, sourceAnalysisOption.Category));
						found = true;
						break;
					}
				}

				if (!found)
					targetAnalyses.Add(new WordAnalysis());
			}

			var waMatrix = new WordAlignmentMatrix(targetAnalyses.Count, targetAnalyses.Count);
			for (int j = 0; j < targetAnalyses.Count; j++)
				waMatrix[j, j] = AlignmentType.Aligned;

			var result = new TransferResult(targetAnalyses, waMatrix);

			return result.ToEnumerable();
		}
	}
}
