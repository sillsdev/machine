using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public static class CorporaExtensions
	{
		#region IEnumerable<Text> extensions

		public static IEnumerable<TextRow> Tokenize(this IEnumerable<TextRow> corpus,
			ITokenizer<string, int, string> tokenizer)
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

		public static IEnumerable<TextRow> UnescapeSpaces(this IEnumerable<TextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.Segment = row.Segment.UnescapeSpaces();
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

		public static IEnumerable<TextRow> Truecase(this IEnumerable<TextRow> corpus, ITruecaser truecaser)
		{
			return corpus.Select(row =>
			{
				row.Segment = truecaser.Truecase(row.Segment);
				return row;
			});
		}

		#endregion

		#region ITextCorpus extensions

		public static ITextCorpus Tokenize(this ITextCorpus corpus, ITokenizer<string, int, string> tokenizer)
		{
			return corpus.Transform(row =>
			{
				row.Segment = tokenizer.Tokenize(row.Text).ToArray();
				return row;
			});
		}

		public static ITextCorpus Tokenize<T>(this ITextCorpus corpus)
			where T : ITokenizer<string, int, string>, new()
		{
			var tokenizer = new T();
			return corpus.Tokenize(tokenizer);
		}

		public static ITextCorpus Detokenize(this ITextCorpus corpus, IDetokenizer<string, string> detokenizer)
		{
			return corpus.Transform(row =>
			{
				row.Segment = new[] { detokenizer.Detokenize(row.Segment) };
				return row;
			});
		}

		public static ITextCorpus Detokenize<T>(this ITextCorpus corpus)
			where T : IDetokenizer<string, string>, new()
		{
			var detokenizer = new T();
			return corpus.Detokenize(detokenizer);
		}

		public static ITextCorpus Normalize(this ITextCorpus corpus,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return corpus.Transform(row =>
			{
				row.Segment = row.Segment.Normalize(normalizationForm);
				return row;
			});
		}

		public static ITextCorpus NfcNormalize(this ITextCorpus corpus)
		{
			return corpus.Normalize();
		}

		public static ITextCorpus NfdNormalize(this ITextCorpus corpus)
		{
			return corpus.Normalize(NormalizationForm.FormD);
		}

		public static ITextCorpus NfkcNormalize(this ITextCorpus corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKC);
		}

		public static ITextCorpus NfkdNormalize(this ITextCorpus corpus)
		{
			return corpus.Normalize(NormalizationForm.FormKD);
		}

		public static ITextCorpus EscapeSpaces(this ITextCorpus corpus)
		{
			return corpus.Transform(row =>
			{
				row.Segment = row.Segment.EscapeSpaces();
				return row;
			});
		}

		public static ITextCorpus UnescapeSpaces(this ITextCorpus corpus)
		{
			return corpus.Transform(row =>
			{
				row.Segment = row.Segment.UnescapeSpaces();
				return row;
			});
		}

		public static ITextCorpus Lowercase(this ITextCorpus corpus)
		{
			return corpus.Transform(row =>
			{
				row.Segment = row.Segment.Lowercase();
				return row;
			});
		}

		public static ITextCorpus Truecase(this ITextCorpus corpus, ITruecaser truecaser)
		{
			return corpus.Transform(row =>
			{
				row.Segment = truecaser.Truecase(row.Segment);
				return row;
			});
		}

		public static ITextCorpus Transform(this ITextCorpus corpus, Func<TextRow, TextRow> transform)
		{
			return new TransformTextCorpus(corpus, transform);
		}

		public static ITextCorpus FilterTexts(this ITextCorpus corpus, Func<IText, bool> predicate)
		{
			return new TextFilterTextCorpus(corpus, predicate);
		}

		public static IEnumerable<ParallelTextRow> AlignRows(this ITextCorpus sourceCorpus, ITextCorpus targetCorpus,
			IAlignmentCorpus alignmentCorpus = null, bool allSourceRows = false, bool allTargetRows = false)
		{
			return new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus)
			{
				AllSourceRows = allSourceRows,
				AllTargetRows = allTargetRows
			};
		}

		#endregion

		#region IAlignmentCorpus extensions

		public static IAlignmentCorpus Transform(this IAlignmentCorpus corpus,
			Func<AlignmentRow, AlignmentRow> transform)
		{
			return new TransformAlignmentCorpus(corpus, transform);
		}

		public static IAlignmentCorpus FilterAlignmentCollections(this IAlignmentCorpus corpus,
			Func<IAlignmentCollection, bool> predicate)
		{
			return new AlignmentCollectionFilterAlignmentCorpus(corpus, predicate);
		}

		#endregion

		#region IEnumerable<ParallelTextRow> extensions

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

		public static IEnumerable<ParallelTextRow> Invert(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Select(row => row.Invert());
		}

		public static (IEnumerable<T>, IEnumerable<T>, int, int) Split<T>(this IEnumerable<T> corpus,
			double? percent = null, int? size = null, int? seed = null)
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

		public static IEnumerable<ParallelTextRow> UnescapeSpaces(this IEnumerable<ParallelTextRow> corpus)
		{
			return corpus.Select(row =>
			{
				row.SourceSegment = row.SourceSegment.UnescapeSpaces();
				row.TargetSegment = row.TargetSegment.UnescapeSpaces();
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

		#endregion

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

		private class TransformTextCorpus : ITextCorpus
		{
			private readonly ITextCorpus _corpus;
			private readonly Func<TextRow, TextRow> _transform;

			public TransformTextCorpus(ITextCorpus corpus, Func<TextRow, TextRow> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public IEnumerable<IText> Texts => _corpus.Texts;

			public IEnumerator<TextRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<TextRow> GetRows(IEnumerable<string> textIds = null)
			{
				return _corpus.GetRows(textIds).Select(_transform);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private class TextFilterTextCorpus : ITextCorpus
		{
			private readonly ITextCorpus _corpus;
			private readonly Func<IText, bool> _predicate;

			public TextFilterTextCorpus(ITextCorpus corpus, Func<IText, bool> predicate)
			{
				_corpus = corpus;
				_predicate = predicate;
			}

			public IEnumerable<IText> Texts => _corpus.Texts.Where(_predicate);

			public IEnumerator<TextRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<TextRow> GetRows(IEnumerable<string> textIds = null)
			{
				return _corpus.GetRows(textIds ?? Texts.Select(t => t.Id));
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private class TransformAlignmentCorpus : IAlignmentCorpus
		{
			private readonly IAlignmentCorpus _corpus;
			private readonly Func<AlignmentRow, AlignmentRow> _transform;

			public TransformAlignmentCorpus(IAlignmentCorpus corpus, Func<AlignmentRow, AlignmentRow> transform)
			{
				_corpus = corpus;
				_transform = transform;
			}

			public IEnumerable<IAlignmentCollection> AlignmentCollections => _corpus.AlignmentCollections;

			public IEnumerator<AlignmentRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds = null)
			{
				return _corpus.GetRows(textIds).Select(_transform);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}


		private class AlignmentCollectionFilterAlignmentCorpus : IAlignmentCorpus
		{
			private readonly IAlignmentCorpus _corpus;
			private readonly Func<IAlignmentCollection, bool> _predicate;

			public AlignmentCollectionFilterAlignmentCorpus(IAlignmentCorpus corpus,
				Func<IAlignmentCollection, bool> predicate)
			{
				_corpus = corpus;
				_predicate = predicate;
			}

			public IEnumerable<IAlignmentCollection> AlignmentCollections => _corpus.AlignmentCollections
				.Where(_predicate);

			public IEnumerator<AlignmentRow> GetEnumerator()
			{
				return GetRows().GetEnumerator();
			}

			public IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds = null)
			{
				return _corpus.GetRows(textIds ?? AlignmentCollections.Select(t => t.Id));
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}
