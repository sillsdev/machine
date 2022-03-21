using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public static class CorporaExtensions
	{
		public static int Count(this ITextCorpusView corpus)
		{
			return corpus.GetRows().Count();
		}

		public static int NonemptyCount(this ITextCorpusView corpus)
		{
			return corpus.GetRows().Count(row => !row.IsEmpty);
		}

		public static ITextCorpusView Tokenize(this ITextCorpusView corpus, ITokenizer<string, int, string> tokenizer)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.Segment = tokenizer.Tokenize(row.Text).ToArray();
				return row;
			}));
		}

		public static ITextCorpusView Tokenize<T>(this ITextCorpusView corpus)
			where T : ITokenizer<string, int, string>, new()
		{
			var tokenizer = new T();
			return corpus.Tokenize(tokenizer);
		}

		public static ITextCorpusView Filter(this ITextCorpusView corpus, Func<TextCorpusRow, bool> predicate)
		{
			return corpus.Transform(rows => rows.Where(predicate));
		}

		public static ITextCorpusView FilterEmpty(this ITextCorpusView corpus)
		{
			return corpus.Filter(row => !row.IsEmpty);
		}

		public static ITextCorpusView CapSize(this ITextCorpusView corpus, int count)
		{
			return corpus.Transform(rows => rows.Take(count));
		}

		public static ITextCorpusView Normalize(this ITextCorpusView corpus,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.Segment = row.Segment.Normalize(normalizationForm);
				return row;
			}));
		}

		public static ITextCorpusView NfcNormalize(this ITextCorpusView corpus)
		{
			return corpus.Normalize();
		}

		public static ITextCorpusView NfdNormalize(this ITextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormD);
		}

		public static ITextCorpusView NfkcNormalize(this ITextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKC);
		}

		public static ITextCorpusView NfkdNormalize(this ITextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKD);
		}

		public static ITextCorpusView EscapeSpaces(this ITextCorpusView corpus)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.Segment = row.Segment.EscapeSpaces();
				return row;
			}));
		}

		public static ITextCorpusView Lowercase(this ITextCorpusView corpus)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.Segment = row.Segment.Lowercase();
				return row;
			}));
		}

		public static ITextCorpusView Transform(this ITextCorpusView corpus,
			Func<IEnumerable<TextCorpusRow>, IEnumerable<TextCorpusRow>> transform)
		{
			return new TransformTextCorpusView(corpus, transform);
		}

		public static int Count(this IParallelTextCorpusView corpus)
		{
			return corpus.GetRows().Count();
		}

		public static int NonemptyCount(this IParallelTextCorpusView corpus)
		{
			return corpus.GetRows().Count(row => !row.IsEmpty);
		}

		public static IParallelTextCorpusView Tokenize(this IParallelTextCorpusView corpus,
			ITokenizer<string, int, string> tokenizer)
		{
			return corpus.Tokenize(tokenizer, tokenizer);
		}

		public static IParallelTextCorpusView Tokenize<T>(this IParallelTextCorpusView corpus)
			where T : ITokenizer<string, int, string>, new()
		{
			var tokenizer = new T();
			return corpus.Tokenize(tokenizer);
		}

		public static IParallelTextCorpusView Tokenize(this IParallelTextCorpusView corpus,
			ITokenizer<string, int, string> sourceTokenizer, ITokenizer<string, int, string> targetTokenizer)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.SourceSegment = sourceTokenizer.Tokenize(row.SourceText).ToArray();
				row.TargetSegment = targetTokenizer.Tokenize(row.TargetText).ToArray();
				return row;
			}));
		}

		public static IParallelTextCorpusView Tokenize<TSource, TTarget>(this IParallelTextCorpusView corpus)
			where TSource : ITokenizer<string, int, string>, new()
			where TTarget : ITokenizer<string, int, string>, new()
		{
			var sourceTokenizer = new TSource();
			var targetTokenizer = new TTarget();
			return corpus.Tokenize(sourceTokenizer, targetTokenizer);
		}

		public static IParallelTextCorpusView Invert(this IParallelTextCorpusView corpus)
		{
			return corpus.Transform(rows => rows.Select(row => row.Invert()));
		}

		public static IParallelTextCorpusView Filter(this IParallelTextCorpusView corpus,
			Func<ParallelTextCorpusRow, bool> predicate)
		{
			return corpus.Transform(rows => rows.Where(predicate));
		}

		public static IParallelTextCorpusView FilterEmpty(this IParallelTextCorpusView corpus)
		{
			return corpus.Filter(row => !row.IsEmpty);
		}

		public static IParallelTextCorpusView CapSize(this IParallelTextCorpusView corpus, int count)
		{
			return corpus.Transform(rows => rows.Take(count));
		}

		public static (IParallelTextCorpusView, IParallelTextCorpusView, int, int) Split(
			this IParallelTextCorpusView corpus, double? percent = null, int? size = null, int? seed = null)
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

			var mainCorpus = corpus.Transform(rows => rows.Where((row, i) => !splitIndices.Contains(i)));
			var splitCorpus = corpus.Transform(rows => rows.Where((row, i) => splitIndices.Contains(i)));
			return (mainCorpus, splitCorpus, corpusSize - splitIndices.Count, splitIndices.Count);
		}

		public static IParallelTextCorpusView Normalize(this IParallelTextCorpusView corpus,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.Normalize(normalizationForm);
				row.TargetSegment = row.TargetSegment.Normalize(normalizationForm);
				return row;
			}));
		}

		public static IParallelTextCorpusView NfcNormalize(this IParallelTextCorpusView corpus)
		{
			return corpus.Normalize();
		}

		public static IParallelTextCorpusView NfdNormalize(this IParallelTextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormD);
		}

		public static IParallelTextCorpusView NfkcNormalize(this IParallelTextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKC);
		}

		public static IParallelTextCorpusView NfkdNormalize(this IParallelTextCorpusView corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKD);
		}

		public static IParallelTextCorpusView EscapeSpaces(this IParallelTextCorpusView corpus)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.EscapeSpaces();
				row.TargetSegment = row.TargetSegment.EscapeSpaces();
				return row;
			}));
		}

		public static IParallelTextCorpusView Lowercase(this IParallelTextCorpusView corpus)
		{
			return corpus.Transform(rows => rows.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.Lowercase();
				row.TargetSegment = row.TargetSegment.Lowercase();
				return row;
			}));
		}

		public static IParallelTextCorpusView Transform(this IParallelTextCorpusView corpus,
			Func<IEnumerable<ParallelTextCorpusRow>, IEnumerable<ParallelTextCorpusRow>> transform)
		{
			return new TransformParallelTextCorpusView(corpus, transform);
		}

		public static ITextAlignmentCorpusView Filter(this ITextAlignmentCorpusView corpus,
			Func<TextAlignmentCorpusRow, bool> predicate)
		{
			return corpus.Transform(rows => rows.Where(predicate));
		}

		public static ITextAlignmentCorpusView Transform(this ITextAlignmentCorpusView corpus,
			Func<IEnumerable<TextAlignmentCorpusRow>, IEnumerable<TextAlignmentCorpusRow>> transform)
		{
			return new TransformTextAlignmentCorpusView(corpus, transform);
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

		private class TransformTextCorpusView : ITextCorpusView
		{
			private readonly ITextCorpusView _corpus;
			private readonly Func<IEnumerable<TextCorpusRow>, IEnumerable<TextCorpusRow>> _transform;

			public TransformTextCorpusView(ITextCorpusView corpus,
				Func<IEnumerable<TextCorpusRow>, IEnumerable<TextCorpusRow>> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public ITextCorpus Source => _corpus.Source;

			public IEnumerable<TextCorpusRow> GetRows(ITextCorpusView basedOn = null)
			{
				return _transform(_corpus.GetRows(basedOn));
			}
		}

		private class TransformParallelTextCorpusView : IParallelTextCorpusView
		{
			private readonly IParallelTextCorpusView _corpus;
			private readonly Func<IEnumerable<ParallelTextCorpusRow>, IEnumerable<ParallelTextCorpusRow>> _transform;

			public TransformParallelTextCorpusView(IParallelTextCorpusView corpus,
				Func<IEnumerable<ParallelTextCorpusRow>, IEnumerable<ParallelTextCorpusRow>> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public IEnumerable<ParallelTextCorpusRow> GetRows(bool allSourceRows = false, bool allTargetRows = false)
			{
				return _transform(_corpus.GetRows(allSourceRows, allTargetRows));
			}
		}

		private class TransformTextAlignmentCorpusView : ITextAlignmentCorpusView
		{
			private readonly ITextAlignmentCorpusView _corpus;
			private readonly Func<IEnumerable<TextAlignmentCorpusRow>, IEnumerable<TextAlignmentCorpusRow>> _transform;

			public TransformTextAlignmentCorpusView(ITextAlignmentCorpusView corpus,
				Func<IEnumerable<TextAlignmentCorpusRow>, IEnumerable<TextAlignmentCorpusRow>> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public ITextAlignmentCorpus Source => _corpus.Source;

			public IEnumerable<TextAlignmentCorpusRow> GetRows()
			{
				return _transform(_corpus.GetRows());
			}
		}
	}
}
