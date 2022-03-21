using System;
using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class ParallelTextCorpus : IParallelTextCorpusView
	{
		private readonly ITextCorpusView _sourceCorpus;
		private readonly ITextCorpusView _targetCorpus;
		private readonly ITextAlignmentCorpusView _alignmentCorpus;
		private readonly IComparer<object> _rowRefComparer;

		public ParallelTextCorpus(ITextCorpusView sourceCorpus, ITextCorpusView targetCorpus,
			ITextAlignmentCorpusView alignmentCorpus = null, IComparer<object> rowRefComparer = null)
		{
			_sourceCorpus = sourceCorpus;
			_targetCorpus = targetCorpus;
			_alignmentCorpus = alignmentCorpus ?? new DictionaryTextAlignmentCorpus();
			_rowRefComparer = rowRefComparer ?? new DefaultRowRefComparer();
		}

		public IEnumerable<ParallelTextCorpusRow> GetRows(bool allSourceRows = false, bool allTargetRows = false)
		{
			using (IEnumerator<TextCorpusRow> srcEnumerator = _sourceCorpus.GetRows().GetEnumerator())
			using (IEnumerator<TextCorpusRow> trgEnumerator = _targetCorpus.GetRows(_sourceCorpus).GetEnumerator())
			using (IEnumerator<TextAlignmentCorpusRow> alignmentEnumerator = _alignmentCorpus.GetRows().GetEnumerator())
			{
				var rangeInfo = new RangeInfo();
				var sourceSameRefRows = new List<TextCorpusRow>();
				var targetSameRefRows = new List<TextCorpusRow>();

				bool srcCompleted = !srcEnumerator.MoveNext();
				bool trgCompleted = !trgEnumerator.MoveNext();
				while (!srcCompleted && !trgCompleted)
				{
					int compare1 = _rowRefComparer.Compare(srcEnumerator.Current.Ref,
						trgEnumerator.Current.Ref);
					if (compare1 < 0)
					{
						if (!allTargetRows && srcEnumerator.Current.IsInRange)
						{
							if (rangeInfo.IsInRange && trgEnumerator.Current.IsInRange
								&& trgEnumerator.Current.Segment.Count > 0)
							{
								yield return rangeInfo.CreateRow();
							}
							rangeInfo.TextId = srcEnumerator.Current.TextId;
							rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
							targetSameRefRows.Clear();
							rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
							if (rangeInfo.IsSourceEmpty)
							{
								rangeInfo.IsSourceEmpty = srcEnumerator.Current.IsEmpty;
								rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
							}
						}
						else
						{
							foreach (ParallelTextCorpusRow row in CreateSourceRows(rangeInfo,
								srcEnumerator.Current, targetSameRefRows, allSourceRows))
							{
								yield return row;
							}
						}

						sourceSameRefRows.Add(srcEnumerator.Current);
						srcCompleted = !srcEnumerator.MoveNext();
					}
					else if (compare1 > 0)
					{
						if (!allSourceRows && trgEnumerator.Current.IsInRange)
						{
							if (rangeInfo.IsInRange && srcEnumerator.Current.IsInRange
								&& srcEnumerator.Current.Segment.Count > 0)
							{
								yield return rangeInfo.CreateRow();
							}
							rangeInfo.TextId = trgEnumerator.Current.TextId;
							rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
							sourceSameRefRows.Clear();
							rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
							if (rangeInfo.IsTargetEmpty)
							{
								rangeInfo.IsTargetEmpty = trgEnumerator.Current.IsEmpty;
								rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
							}
						}
						else
						{
							foreach (ParallelTextCorpusRow row in CreateTargetRows(rangeInfo,
								trgEnumerator.Current, sourceSameRefRows, allTargetRows))
							{
								yield return row;
							}
						}

						targetSameRefRows.Add(trgEnumerator.Current);
						trgCompleted = !trgEnumerator.MoveNext();
					}
					else
					{
						int compare2;
						do
						{
							compare2 = alignmentEnumerator.MoveNext()
								? _rowRefComparer.Compare(srcEnumerator.Current.Ref,
									alignmentEnumerator.Current.Ref)
								: 1;
						} while (compare2 < 0);

						if ((!allTargetRows && srcEnumerator.Current.IsInRange)
							|| (!allSourceRows && trgEnumerator.Current.IsInRange))
						{

							if (rangeInfo.IsInRange
								&& ((srcEnumerator.Current.IsInRange && !trgEnumerator.Current.IsInRange
									&& srcEnumerator.Current.Segment.Count > 0)
								|| (!srcEnumerator.Current.IsInRange && trgEnumerator.Current.IsInRange
									&& trgEnumerator.Current.Segment.Count > 0)
								|| (srcEnumerator.Current.IsInRange && trgEnumerator.Current.IsInRange
									&& srcEnumerator.Current.Segment.Count > 0 && trgEnumerator.Current.Segment.Count > 0)))
							{
								yield return rangeInfo.CreateRow();
							}

							rangeInfo.TextId = srcEnumerator.Current.TextId;
							rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
							rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
							sourceSameRefRows.Clear();
							targetSameRefRows.Clear();
							rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
							rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
							if (rangeInfo.IsSourceEmpty)
							{
								rangeInfo.IsSourceEmpty = srcEnumerator.Current.IsEmpty;
								rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
							}
							if (rangeInfo.IsTargetEmpty)
							{
								rangeInfo.IsTargetEmpty = trgEnumerator.Current.IsEmpty;
								rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
							}
						}
						else
						{
							if (CheckSameRefRows(sourceSameRefRows, trgEnumerator.Current))
							{
								foreach (TextCorpusRow prevSourceRow in sourceSameRefRows)
								{
									foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, prevSourceRow,
										trgEnumerator.Current))
									{
										yield return row;
									}
								}
							}

							if (CheckSameRefRows(targetSameRefRows, srcEnumerator.Current))
							{
								foreach (TextCorpusRow prevTargetRow in targetSameRefRows)
								{
									foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo,
										srcEnumerator.Current, prevTargetRow))
									{
										yield return row;
									}
								}
							}

							foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, srcEnumerator.Current,
								trgEnumerator.Current,
								compare2 == 0 ? alignmentEnumerator.Current.AlignedWordPairs : null))
							{
								yield return row;
							}
						}

						sourceSameRefRows.Add(srcEnumerator.Current);
						srcCompleted = !srcEnumerator.MoveNext();

						targetSameRefRows.Add(trgEnumerator.Current);
						trgCompleted = !trgEnumerator.MoveNext();
					}
				}

				while (!srcCompleted)
				{
					foreach (ParallelTextCorpusRow row in CreateSourceRows(rangeInfo, srcEnumerator.Current,
						targetSameRefRows, allSourceRows))
					{
						yield return row;
					}
					srcCompleted = !srcEnumerator.MoveNext();
				}

				while (!trgCompleted)
				{
					foreach (ParallelTextCorpusRow row in CreateTargetRows(rangeInfo, trgEnumerator.Current,
						sourceSameRefRows, allTargetRows))
					{
						yield return row;
					}
					trgCompleted = !trgEnumerator.MoveNext();
				}

				if (rangeInfo.IsInRange)
					yield return rangeInfo.CreateRow();
			}
		}

		private IEnumerable<ParallelTextCorpusRow> CreateRows(RangeInfo rangeInfo, TextCorpusRow srcRow,
			TextCorpusRow trgRow, IReadOnlyCollection<AlignedWordPair> alignedWordPairs = null)
		{
			if (rangeInfo.IsInRange)
				yield return rangeInfo.CreateRow();

			string textId = srcRow?.TextId ?? trgRow.TextId;
			var sourceRefs = srcRow != null ? new object[] { srcRow.Ref } : Array.Empty<object>();
			var targetRefs = trgRow != null ? new object[] { trgRow.Ref } : Array.Empty<object>();
			yield return new ParallelTextCorpusRow(textId, sourceRefs, targetRefs)
			{
				SourceSegment = srcRow != null ? srcRow.Segment : Array.Empty<string>(),
				TargetSegment = trgRow != null ? trgRow.Segment : Array.Empty<string>(),
				AlignedWordPairs = alignedWordPairs,
				IsSourceSentenceStart = srcRow != null && srcRow.IsSentenceStart,
				IsSourceInRange = srcRow != null && srcRow.IsInRange,
				IsSourceRangeStart = srcRow != null && srcRow.IsRangeStart,
				IsTargetSentenceStart = trgRow != null && trgRow.IsSentenceStart,
				IsTargetInRange = trgRow != null && trgRow.IsInRange,
				IsTargetRangeStart = trgRow != null && trgRow.IsRangeStart,
				IsEmpty = srcRow == null || srcRow.IsEmpty || trgRow == null || trgRow.IsEmpty
			};
		}

		private bool CheckSameRefRows(List<TextCorpusRow> sameRefRows, TextCorpusRow otherRow)
		{
			if (sameRefRows.Count > 0
				&& _rowRefComparer.Compare(sameRefRows[0].Ref, otherRow.Ref) != 0)
			{
				sameRefRows.Clear();
			}

			return sameRefRows.Count > 0;
		}

		private IEnumerable<ParallelTextCorpusRow> CreateSourceRows(RangeInfo rangeInfo, TextCorpusRow sourceRow,
			List<TextCorpusRow> targetSameRefRows, bool allSourceRows)
		{
			if (CheckSameRefRows(targetSameRefRows, sourceRow))
			{
				foreach (TextCorpusRow targetSameRefRow in targetSameRefRows)
				{
					foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, sourceRow, targetSameRefRow))
						yield return row;
				}
			}
			else if (allSourceRows)
			{
				foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, sourceRow, null))
					yield return row;
			}
		}

		private IEnumerable<ParallelTextCorpusRow> CreateTargetRows(RangeInfo rangeInfo, TextCorpusRow targetRow,
			List<TextCorpusRow> sourceSameRefRows, bool allTargetRows)
		{
			if (CheckSameRefRows(sourceSameRefRows, targetRow))
			{
				foreach (TextCorpusRow sourceSameRefRow in sourceSameRefRows)
				{
					foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, sourceSameRefRow, targetRow))
						yield return row;
				}
			}
			else if (allTargetRows)
			{
				foreach (ParallelTextCorpusRow row in CreateRows(rangeInfo, null, targetRow))
					yield return row;
			}
		}

		private class RangeInfo
		{
			public string TextId { get; set; }
			public List<object> SourceRefs { get; } = new List<object>();
			public List<object> TargetRefs { get; } = new List<object>();
			public List<string> SourceSegment { get; } = new List<string>();
			public List<string> TargetSegment { get; } = new List<string>();
			public bool IsSourceSentenceStart { get; set; } = false;
			public bool IsTargetSentenceStart { get; set; } = false;
			public bool IsInRange => SourceRefs.Count > 0 && TargetRefs.Count > 0;
			public bool IsSourceEmpty { get; set; } = true;
			public bool IsTargetEmpty { get; set; } = true;

			public ParallelTextCorpusRow CreateRow()
			{
				var row = new ParallelTextCorpusRow(TextId, SourceRefs.ToArray(), TargetRefs.ToArray())
				{
					SourceSegment = SourceSegment.ToArray(),
					TargetSegment = TargetSegment.ToArray(),
					IsSourceSentenceStart = IsSourceSentenceStart,
					IsTargetSentenceStart = IsTargetSentenceStart,
					IsEmpty = IsSourceEmpty || IsTargetEmpty
				};
				TextId = null;
				SourceRefs.Clear();
				TargetRefs.Clear();
				SourceSegment.Clear();
				TargetSegment.Clear();
				IsSourceSentenceStart = false;
				IsTargetSentenceStart = false;
				IsSourceEmpty = true;
				IsTargetEmpty = true;
				return row;
			}
		}

		private class DefaultRowRefComparer : IComparer<object>
		{
			private static readonly VerseRefComparer VerseRefComparer = new VerseRefComparer(compareSegments: false);

			public int Compare(object x, object y)
			{
				// Do not use the default comparer for VerseRef, since we want to compare all verses in a range or
				// sequence
				if (x is VerseRef vx && y is VerseRef vy)
					return VerseRefComparer.Compare(vx, vy);

				return Comparer<object>.Default.Compare(x, y);
			}
		}
	}
}
