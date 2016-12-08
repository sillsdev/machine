using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TransferEngine : DisposableBase, ITranslationEngine
	{
		private readonly IMorphologicalAnalyzer _sourceAnalyzer;
		private readonly ITransferer _transferer;
		private readonly IMorphologicalGenerator _targetGenerator;

		public TransferEngine(IMorphologicalAnalyzer sourceAnalyzer, ITransferer transferer, IMorphologicalGenerator targetGenerator)
		{
			_sourceAnalyzer = sourceAnalyzer;
			_transferer = transferer;
			_targetGenerator = targetGenerator;
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			return Translate(1, segment).First();
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			string[] segmentArray = segment.ToArray();
			IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses = segmentArray.Select(word => _sourceAnalyzer.AnalyzeWord(word));

			foreach (TransferResult transferResult in _transferer.Transfer(sourceAnalyses).Take(n))
			{
				IReadOnlyList<WordAnalysis> targetAnalyses = transferResult.TargetAnalyses;
				WordAlignmentMatrix waMatrix = transferResult.WordAlignmentMatrix;

				var translation = new List<string>();
				var confidences = new List<double>();
				var sources = new List<TranslationSources>();
				var alignment = new WordAlignmentMatrix(segmentArray.Length, targetAnalyses.Count);
				for (int j = 0; j < targetAnalyses.Count; j++)
				{
					int[] sourceIndices = Enumerable.Range(0, waMatrix.RowCount).Where(i => waMatrix[i, j] == AlignmentType.Aligned).ToArray();
					string targetWord = targetAnalyses[j].IsEmpty ? null : _targetGenerator.GenerateWords(targetAnalyses[j]).FirstOrDefault();
					double confidence = 1.0;
					TranslationSources source = TranslationSources.Transfer;
					if (targetWord == null)
					{
						if (sourceIndices.Length > 0)
						{
							int i = sourceIndices[0];
							targetWord = segmentArray[i];
							confidence = 0;
							source = TranslationSources.None;
							alignment[i, j] = AlignmentType.Aligned;
						}
					}
					else
					{
						foreach (int i in sourceIndices)
							alignment[i, j] = AlignmentType.Aligned;
					}

					if (targetWord != null)
					{
						translation.Add(targetWord);
						confidences.Add(confidence);
						sources.Add(source);
					}
				}

				yield return new TranslationResult(segmentArray, translation, confidences, sources, alignment);
			}
		}
	}
}
