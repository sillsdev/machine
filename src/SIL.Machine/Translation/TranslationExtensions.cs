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
			bool isUnknown, Func<string, string> sourcePreprocessor = null,
			Func<string, string> targetPreprocessor = null, int maxCount = int.MaxValue)
		{
			foreach (ParallelTextSegment segment in corpus.Segments.Where(s => !s.IsEmpty).Take(maxCount))
				model.AddSegmentPair(segment, isUnknown);
		}

		public static void AddSegmentPair(this IWordAlignmentModel model, ParallelTextSegment segment,
			bool isUnknown, Func<string, string> sourcePreprocessor = null,
			Func<string, string> targetPreprocessor = null)
		{
			if (segment.IsEmpty)
				return;

			if (sourcePreprocessor == null)
				sourcePreprocessor = Preprocessors.Null;
			if (targetPreprocessor == null)
				targetPreprocessor = Preprocessors.Null;

			string[] sourceTokens = segment.SourceSegment.Select(sourcePreprocessor).ToArray();
			string[] targetTokens = segment.TargetSegment.Select(targetPreprocessor).ToArray();

			model.AddSegmentPair(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(isUnknown));
		}

		public static WordAlignmentMatrix GetBestAlignment(this ISegmentAligner aligner, ParallelTextSegment segment,
			bool isUnknown, Func<string, string> sourcePreprocessor = null,
			Func<string, string> targetPreprocessor = null)
		{
			if (sourcePreprocessor == null)
				sourcePreprocessor = Preprocessors.Null;
			if (targetPreprocessor == null)
				targetPreprocessor = Preprocessors.Null;

			string[] sourceTokens = segment.SourceSegment.Select(sourcePreprocessor).ToArray();
			string[] targetTokens = segment.TargetSegment.Select(targetPreprocessor).ToArray();

			return aligner.GetBestAlignment(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(isUnknown));
		}

		public static WordAlignmentMatrix CreateAlignmentMatrix(this ParallelTextSegment segment, bool isUnknown)
		{
			if (segment.AlignedWordPairs == null)
				return null;

			var matrix = new WordAlignmentMatrix(segment.SourceSegment.Count, segment.TargetSegment.Count,
				isUnknown ? AlignmentType.Unknown : AlignmentType.NotAligned);
			foreach (AlignedWordPair wordPair in segment.AlignedWordPairs)
			{
				matrix[wordPair.SourceIndex, wordPair.TargetIndex] = AlignmentType.Aligned;
				if (isUnknown)
				{
					for (int i = 0; i < segment.SourceSegment.Count; i++)
					{
						if (matrix[i, wordPair.TargetIndex] == AlignmentType.Unknown)
							matrix[i, wordPair.TargetIndex] = AlignmentType.NotAligned;
					}

					for (int j = 0; j < segment.TargetSegment.Count; j++)
					{
						if (matrix[wordPair.SourceIndex, j] == AlignmentType.Unknown)
							matrix[wordPair.SourceIndex, j] = AlignmentType.NotAligned;
					}
				}
			}

			return matrix;
		}
	}
}
