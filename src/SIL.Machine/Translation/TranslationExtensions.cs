using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public static class TranslationExtensions
	{
		public static IReadOnlyList<string> TranslateWord(this ITranslationEngine engine, string sourceWord)
		{
			TranslationResult result = engine.Translate(new[] { sourceWord });
			if (result.WordSources.Any(s => s == TranslationSources.None))
				return new string[0];
			return result.TargetSegment;
		}

		public static IEnumerable<TranslationSuggestion> GetSuggestions(this ITranslationSuggester suggester,
			InteractiveTranslator translator)
		{
			return translator.GetCurrentResults().Select(r =>
				suggester.GetSuggestion(translator.Prefix.Count, translator.IsLastWordComplete, r));
		}

		public static IEnumerable<TranslationSuggestion> GetSuggestions(this ITranslationSuggester suggester,
			InteractiveTranslator translator, IReadOnlyList<string> sourceSegment, ITruecaser truecaser)
		{
			return translator.GetCurrentResults().Select(r =>
				suggester.GetSuggestion(translator.Prefix.Count, translator.IsLastWordComplete,
					truecaser.Truecase(sourceSegment, r)));
		}

		public static double GetAlignmentProbability(this IWordAlignmentModel model, int sourceLen, int prevSourceIndex,
			int sourceIndex, int targetLen, int prevTargetIndex, int targetIndex)
		{
			switch (model)
			{
				case IHmmWordAlignmentModel hmmModel:
					return hmmModel.GetAlignmentProbability(sourceLen, prevSourceIndex, sourceIndex);

				case IIbm2WordAlignmentModel ibm2Model:
					return ibm2Model.GetAlignmentProbability(sourceLen, sourceIndex, targetLen, targetIndex);

				case SymmetrizedWordAlignmentModel symmModel:
					return symmModel.GetAlignmentProbability(sourceLen, prevSourceIndex, sourceIndex, targetLen,
						prevTargetIndex, targetIndex);

				default:
					return -1;
			}
		}

		public static Dictionary<string, Dictionary<string, double>> GetTranslationTable(this IWordAlignmentModel model,
			double threshold = 0)
		{
			var results = new Dictionary<string, Dictionary<string, double>>();
			for (int i = 0; i < model.SourceWords.Count; i++)
			{
				var row = new Dictionary<string, double>();
				for (int j = 0; j < model.TargetWords.Count; j++)
				{
					double prob = model.GetTranslationProbability(i, j);
					if (prob > threshold)
						row[model.TargetWords[j]] = prob;
				}
				results[model.SourceWords[i]] = row;
			}
			return results;
		}

		public static WordAlignmentMatrix GetBestAlignment(this IWordAligner aligner, ParallelTextSegment segment,
			ITokenProcessor sourcePreprocessor = null, ITokenProcessor targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = sourcePreprocessor.Process(segment.SourceSegment);
			IReadOnlyList<string> targetSegment = targetPreprocessor.Process(segment.TargetSegment);

			return aligner.GetBestAlignment(sourceSegment, targetSegment, segment.CreateAlignmentMatrix());
		}

		public static WordAlignmentMatrix GetBestAlignment(this IWordAligner aligner,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			WordAlignmentMatrix knownAlignment)
		{
			WordAlignmentMatrix estimatedAlignment = aligner.GetBestAlignment(sourceSegment, targetSegment);
			WordAlignmentMatrix alignment = estimatedAlignment;
			if (knownAlignment != null)
			{
				alignment = knownAlignment.Clone();
				alignment.PrioritySymmetrizeWith(estimatedAlignment);
			}
			return alignment;
		}

		public static WordAlignmentMatrix CreateAlignmentMatrix(this ParallelTextSegment segment)
		{
			if (segment.AlignedWordPairs == null)
				return null;

			var matrix = new WordAlignmentMatrix(segment.SourceSegment.Count, segment.TargetSegment.Count);
			foreach (AlignedWordPair wordPair in segment.AlignedWordPairs)
				matrix[wordPair.SourceIndex, wordPair.TargetIndex] = true;

			return matrix;
		}

		public static string GetAlignmentString(this IWordAlignmentModel model, ParallelTextSegment segment,
			bool includeProbs, ITokenProcessor sourcePreprocessor = null, ITokenProcessor targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = sourcePreprocessor.Process(segment.SourceSegment);
			IReadOnlyList<string> targetSegment = targetPreprocessor.Process(segment.TargetSegment);
			WordAlignmentMatrix alignment = model.GetBestAlignment(sourceSegment, targetSegment,
				segment.CreateAlignmentMatrix());

			if (includeProbs)
				return alignment.ToString(model, sourceSegment, targetSegment);
			return alignment.ToString();
		}

		public static string GetGizaFormatString(this IWordAligner aligner, ParallelTextSegment segment,
			ITokenProcessor sourcePreprocessor = null, ITokenProcessor targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = sourcePreprocessor.Process(segment.SourceSegment);
			IReadOnlyList<string> targetSegment = targetPreprocessor.Process(segment.TargetSegment);
			WordAlignmentMatrix alignment = aligner.GetBestAlignment(sourceSegment, targetSegment,
				segment.CreateAlignmentMatrix());

			return alignment.ToGizaFormat(sourceSegment, targetSegment);
		}

		public static void TrainSegment(this ITruecaser truecaser, TextSegment segment)
		{
			truecaser.TrainSegment(segment.Segment, segment.SentenceStart);
		}
	}
}
