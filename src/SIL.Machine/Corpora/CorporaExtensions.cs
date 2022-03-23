using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public static class CorporaExtensions
	{
		public static IEnumerable<TextRow> Tokenize(this IEnumerable<TextRow> corpus, ITokenizer<string, int, string> tokenizer)
		{
			return corpus.Select(row =>
			{
				row.Segment = tokenizer.Tokenize(row.Text).ToArray();
				return row;
			});
		}

		public static IEnumerable<TextRow> Tokenize<T>(this IEnumerable<TextRow> corpus)
			where T : ITokenizer<string, int, string>, new()
		{
			var tokenizer = new T();
			return corpus.Tokenize(tokenizer);
		}

		public static IEnumerable<TextRow> Detokenize(this IEnumerable<TextRow> corpus,
			IDetokenizer<string, string> detokenizer)
		{
			return corpus.Select(row =>
			{
				row.Segment = new[] { detokenizer.Detokenize(row.Segment) };
				return row;
			});
		}

		public static IEnumerable<TextRow> Detokenize<T>(this IEnumerable<TextRow> corpus)
			where T : IDetokenizer<string, string>, new()
		{
			var detokenizer = new T();
			return corpus.Detokenize(detokenizer);
		}

		public static IEnumerable<TextRow> Normalize(this IEnumerable<TextRow> corpus,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return corpus.Select(row =>
			{
				row.Segment = row.Segment.Normalize(normalizationForm);
				return row;
			});
		}

		public static IEnumerable<TextRow> NfcNormalize(this IEnumerable<TextRow> corpus)
		{
			return corpus.Normalize();
		}

		public static IEnumerable<TextRow> NfdNormalize(this IEnumerable<TextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormD);
		}

		public static IEnumerable<TextRow> NfkcNormalize(this IEnumerable<TextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKC);
		}

		public static IEnumerable<TextRow> NfkdNormalize(this IEnumerable<TextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKD);
		}

		public static IEnumerable<TextRow> EscapeSpaces(this IEnumerable<TextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.Segment = row.Segment.EscapeSpaces();
				return row;
			});
		}

		public static IEnumerable<TextRow> Lowercase(this IEnumerable<TextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.Segment = row.Segment.Lowercase();
				return row;
			});
		}

		public static IEnumerable<ParallelTextRow> AlignRows(this IEnumerable<TextRow> sourceCorpus,
			IEnumerable<TextRow> targetCorpus, IEnumerable<AlignmentRow> alignmentCorpus = null,
			bool allSourceRows = false, bool allTargetRows = false)
		{
			return new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus)
			{
				AllSourceRows = allSourceRows,
				AllTargetRows = allTargetRows
			};
		}

		public static IEnumerable<ParallelTextRow> Tokenize(this IEnumerable<ParallelTextRow> corpus,
			ITokenizer<string, int, string> tokenizer)
		{
			return corpus.Tokenize(tokenizer, tokenizer);
		}

		public static IEnumerable<ParallelTextRow> Tokenize<T>(this IEnumerable<ParallelTextRow> corpus)
			where T : ITokenizer<string, int, string>, new()
		{
			var tokenizer = new T();
			return corpus.Tokenize(tokenizer);
		}

		public static IEnumerable<ParallelTextRow> Tokenize(this IEnumerable<ParallelTextRow> corpus,
			ITokenizer<string, int, string> sourceTokenizer, ITokenizer<string, int, string> targetTokenizer)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = sourceTokenizer.Tokenize(row.SourceText).ToArray();
				row.TargetSegment = targetTokenizer.Tokenize(row.TargetText).ToArray();
				return row;
			});
		}

		public static IEnumerable<ParallelTextRow> Tokenize<TSource, TTarget>(
			this IEnumerable<ParallelTextRow> corpus)
			where TSource : ITokenizer<string, int, string>, new()
			where TTarget : ITokenizer<string, int, string>, new()
		{
			var sourceTokenizer = new TSource();
			var targetTokenizer = new TTarget();
			return corpus.Tokenize(sourceTokenizer, targetTokenizer);
		}

		public static IEnumerable<ParallelTextRow> Detokenize(this IEnumerable<ParallelTextRow> corpus,
			IDetokenizer<string, string> detokenizer)
		{
			return corpus.Detokenize(detokenizer, detokenizer);
		}

		public static IEnumerable<ParallelTextRow> Detokenize<T>(this IEnumerable<ParallelTextRow> corpus)
			where T : IDetokenizer<string, string>, new()
		{
			var detokenizer = new T();
			return corpus.Detokenize(detokenizer);
		}

		public static IEnumerable<ParallelTextRow> Detokenize(this IEnumerable<ParallelTextRow> corpus,
			IDetokenizer<string, string> sourceDetokenizer, IDetokenizer<string, string> targetDetokenizer)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = new[] { sourceDetokenizer.Detokenize(row.SourceSegment) };
				row.TargetSegment = new[] { targetDetokenizer.Detokenize(row.TargetSegment) };
				return row;
			});
		}

		public static IEnumerable<ParallelTextRow> Detokenize<TSource, TTarget>(
			this IEnumerable<ParallelTextRow> corpus)
			where TSource : IDetokenizer<string, string>, new()
			where TTarget : IDetokenizer<string, string>, new()
		{
			var sourceDetokenizer = new TSource();
			var targetDetokenizer = new TTarget();
			return corpus.Detokenize(sourceDetokenizer, targetDetokenizer);
		}


		public static IEnumerable<ParallelTextRow> Invert(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Select(row => row.Invert());
		}

		public static (IEnumerable<ParallelTextRow>, IEnumerable<ParallelTextRow>, int, int) Split(
			this IEnumerable<ParallelTextRow> corpus, double? percent = null, int? size = null, int? seed = null)
		{
			if (percent == null && size == null)
				percent = 0.1;

			int corpusSize = corpus.Count();
			int splitSize;
			if (percent != null)
			{
				splitSize = (int)(percent * corpusSize);
				if (size != null)
					splitSize = Math.Min(splitSize, size.Value);
			}
			else
			{
				splitSize = size.Value;
			}

			var r = seed != null ? new Random(seed.Value) : new Random();
			var splitIndices = new HashSet<int>(Enumerable.Range(0, corpusSize).OrderBy(i => r.Next()).Take(splitSize));

			var mainCorpus = corpus.Where((row, i) => !splitIndices.Contains(i));
			var splitCorpus = corpus.Where((row, i) => splitIndices.Contains(i));
			return (mainCorpus, splitCorpus, corpusSize - splitIndices.Count, splitIndices.Count);
		}

		public static IEnumerable<ParallelTextRow> Normalize(this IEnumerable<ParallelTextRow> corpus,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.Normalize(normalizationForm);
				row.TargetSegment = row.TargetSegment.Normalize(normalizationForm);
				return row;
			});
		}

		public static IEnumerable<ParallelTextRow> NfcNormalize(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Normalize();
		}

		public static IEnumerable<ParallelTextRow> NfdNormalize(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormD);
		}

		public static IEnumerable<ParallelTextRow> NfkcNormalize(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKC);
		}

		public static IEnumerable<ParallelTextRow> NfkdNormalize(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKD);
		}

		public static IEnumerable<ParallelTextRow> EscapeSpaces(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.EscapeSpaces();
				row.TargetSegment = row.TargetSegment.EscapeSpaces();
				return row;
			});
		}

		public static IEnumerable<ParallelTextRow> Lowercase(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.Lowercase();
				row.TargetSegment = row.TargetSegment.Lowercase();
				return row;
			});
		}

		public static StringBuilder TrimEnd(this StringBuilder sb)
		{
			if (sb.Length == 0)
				return sb;

			int i = sb.Length - 1;
			for (; i >= 0; i--)
			{
				if (!char.IsWhiteSpace(sb[i]))
					break;
			}

			if (i < sb.Length - 1)
				sb.Length = i + 1;

			return sb;
		}
	}
}
