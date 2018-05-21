using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			IInteractiveTranslationSession session)
		{
			return session.CurrentResults.Select(r =>
				suggester.GetSuggestion(session.Prefix.Count, session.IsLastWordComplete, r));
		}

		public static void AppendSuggestionToPrefix(this IInteractiveTranslationSession session, int resultIndex,
			IReadOnlyList<int> suggestion)
		{
			session.AppendToPrefix(suggestion.Select(j => session.CurrentResults[resultIndex].TargetSegment[j]));
		}

		public static string RecaseTargetWord(this TranslationResult result, IReadOnlyList<string> sourceSegment,
			int targetIndex)
		{
			return result.Alignment.RecaseTargetWord(sourceSegment, 0, result.TargetSegment, targetIndex);
		}

		public static string RecaseTargetWord(this WordAlignmentMatrix alignment, IReadOnlyList<string> sourceSegment,
			int sourceStartIndex, IReadOnlyList<string> targetSegment, int targetIndex)
		{
			string targetWord = targetSegment[targetIndex];
			if (alignment.GetColumnAlignedIndices(targetIndex)
				.Any(i => sourceSegment[sourceStartIndex + i].IsTitleCase()))
			{
				return targetWord.ToTitleCase();
			}
			return targetWord;
		}

		public static bool IsTitleCase(this string str)
		{
			return str.Length > 0 && char.IsUpper(str, 0)
				&& Enumerable.Range(1, str.Length - 1).All(i => char.IsLower(str, i));
		}

		public static string ToTitleCase(this string str)
		{
			if (str.Length == 0)
				return str;

			var sb = new StringBuilder();
			sb.Append(str.Substring(0, 1).ToUpperInvariant());
			if (str.Length > 1)
				sb.Append(str.Substring(1, str.Length - 1).ToLowerInvariant());
			return sb.ToString();
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

		public static IDictionary<string, IDictionary<string, double>> GetTranslationTable(
			this IWordAlignmentModel model, double threshold = 0)
		{
			var results = new Dictionary<string, IDictionary<string, double>>();
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

		public static void AddSegmentPairs(this IWordAlignmentModel model, ParallelTextCorpus corpus,
			Func<string, string> sourcePreprocessor = null, Func<string, string> targetPreprocessor = null,
			int maxCount = int.MaxValue)
		{
			foreach (ParallelTextSegment segment in corpus.Segments.Where(s => !s.IsEmpty).Take(maxCount))
				model.AddSegmentPair(segment, sourcePreprocessor, targetPreprocessor);
		}

		public static void AddSegmentPair(this IWordAlignmentModel model, ParallelTextSegment segment,
			Func<string, string> sourcePreprocessor = null, Func<string, string> targetPreprocessor = null)
		{
			if (segment.IsEmpty)
				return;

			IReadOnlyList<string> sourceSegment = segment.SourceSegment.Preprocess(sourcePreprocessor);
			IReadOnlyList<string> targetSegment = segment.TargetSegment.Preprocess(targetPreprocessor);

			model.AddSegmentPair(sourceSegment, targetSegment);
		}

		public static WordAlignmentMatrix GetBestAlignment(this ISegmentAligner aligner, ParallelTextSegment segment,
			Func<string, string> sourcePreprocessor = null, Func<string, string> targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = segment.SourceSegment.Preprocess(sourcePreprocessor);
			IReadOnlyList<string> targetSegment = segment.TargetSegment.Preprocess(targetPreprocessor);

			return aligner.GetBestAlignment(sourceSegment, targetSegment, segment.CreateAlignmentMatrix());
		}

		public static WordAlignmentMatrix GetBestAlignment(this ISegmentAligner aligner,
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

		public static IReadOnlyList<string> Preprocess(this IEnumerable<string> segment,
			Func<string, string> preprocessor)
		{
			if (preprocessor == null)
				preprocessor = Preprocessors.Null;
			return segment.Select(preprocessor).ToArray();
		}

		public static string GetAlignmentString(this IWordAlignmentModel model, ParallelTextSegment segment,
			bool includeProbs, Func<string, string> sourcePreprocessor = null,
			Func<string, string> targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = segment.SourceSegment.Preprocess(sourcePreprocessor);
			IReadOnlyList<string> targetSegment = segment.TargetSegment.Preprocess(targetPreprocessor);
			WordAlignmentMatrix alignment = model.GetBestAlignment(sourceSegment, targetSegment,
				segment.CreateAlignmentMatrix());

			if (includeProbs)
				return alignment.ToString(model, sourceSegment, targetSegment);
			return alignment.ToString();
		}

		public static string GetGizaFormatString(this ISegmentAligner aligner, ParallelTextSegment segment,
			Func<string, string> sourcePreprocessor = null, Func<string, string> targetPreprocessor = null)
		{
			IReadOnlyList<string> sourceSegment = segment.SourceSegment.Preprocess(sourcePreprocessor);
			IReadOnlyList<string> targetSegment = segment.TargetSegment.Preprocess(targetPreprocessor);
			WordAlignmentMatrix alignment = aligner.GetBestAlignment(sourceSegment, targetSegment,
				segment.CreateAlignmentMatrix());

			return alignment.ToGizaFormat(sourceSegment, targetSegment);
		}
	}
}
